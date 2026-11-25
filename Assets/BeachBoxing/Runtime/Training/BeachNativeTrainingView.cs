using System;
using System.Globalization;
using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Presentation and controls for the preplaced source workout UI.</summary>
    public sealed class BeachNativeTrainingView : MonoBehaviour
    {
        public BeachPeripheralView peripheral;
        public TouchControl pulseControl;

        public BeachWeightRules Weight { get; } = new BeachWeightRules();
        public bool IsOpen { get; private set; }
        public Muscle Muscle { get; private set; }
        public bool SuppressesGuide => IsOpen && Muscle == BeachBoxing.Muscle.Arms && unlocked == 0;

        private Action<int> changeWeight;
        private Action recommended, exit;
        private int strength, unlocked;
        private float exitHintRemaining = -1;

        public void Bind(Action<int> changeWeight, Action recommended, Action exit)
        {
            this.changeWeight = changeWeight;
            this.recommended = recommended;
            this.exit = exit;
            Hook(159, () => ChangeWeight(-5));
            Hook(402, () => ChangeWeight(5));
            Hook(910, UseRecommended);
            Hook(377, () => { if (IsOpen) this.exit?.Invoke(); });
            // Original Button4834 has no click action: PointerDown alone queues a pulse.
            var pulseButton = peripheral ? peripheral.Node(804)?.button : null;
            if (pulseButton) pulseButton.onClick.RemoveAllListeners();
        }

        public void SetPhone(bool phone)
        {
            Active(1014, !phone);
            Active(455, !phone);
            Active(308, phone);
        }

        public void Enter(Muscle muscle, int strength, int unlocked)
        {
            Muscle = muscle;
            this.strength = strength;
            this.unlocked = unlocked;
            Weight.Enter(strength);
            IsOpen = true;
            Active(881, true);
            if (pulseControl) pulseControl.Release();
            Fill(0);
            RefreshLabelsAndHints();
        }

        public void Refresh(int strength, int unlocked, float fill)
        {
            if (!IsOpen) return;
            this.strength = strength;
            this.unlocked = unlocked;
            Weight.RefreshRecommendation(strength);
            Fill(fill);
            RefreshLabelsAndHints();
        }

        public void ChangeWeight(int amount)
        {
            if (!IsOpen) return;
            Weight.Change(amount, strength);
            Fill(0);
            RefreshLabelsAndHints();
            changeWeight?.Invoke(amount);
        }

        public void UseRecommended()
        {
            if (!IsOpen) return;
            Weight.UseRecommended(strength);
            Fill(0);
            RefreshLabelsAndHints();
            recommended?.Invoke();
        }

        public void Exit()
        {
            IsOpen = false;
            Active(881, false);
            if (pulseControl) pulseControl.Release();
            // Source ExitWorkout does not clear either child tooltip's activeSelf.
        }

        public bool ConsumePulse() => IsOpen && pulseControl && pulseControl.ConsumePress();

        public void Tick(float scaledDt)
        {
            if (scaledDt <= 0 || exitHintRemaining < 0) return;
            exitHintRemaining -= scaledDt;
            if (exitHintRemaining > 0) return;
            exitHintRemaining = -1;
            Active(314, true);
        }

        private void RefreshLabelsAndHints()
        {
            string kilograms = Weight.CurrentWeight.ToString(CultureInfo.InvariantCulture) + "kg";
            Label(28, kilograms);
            Label(139, kilograms);
            Label(937, Weight.RecommendedWeight.ToString(CultureInfo.InvariantCulture) + "kg");
            Active(513, Weight.TipNeeded);
            if (Muscle != BeachBoxing.Muscle.Arms || unlocked != 0) return;
            if (strength <= 49) Active(3, true);
            else
            {
                Active(3, false);
                // Repeated source Invoke calls retain the earliest pending activation.
                if (exitHintRemaining < 0) exitHintRemaining = .2f;
            }
        }

        private void Hook(int id, Action callback)
        {
            var button = peripheral ? peripheral.Node(id)?.button : null;
            if (!button) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
        }

        private void Active(int id, bool active)
        {
            var target = peripheral ? peripheral.Node(id)?.transform : null;
            if (target) target.gameObject.SetActive(active);
        }

        private void Label(int id, string value)
        {
            var label = peripheral ? peripheral.Node(id)?.label : null;
            if (label) label.text = value;
        }

        private void Fill(float value)
        {
            var image = peripheral ? peripheral.Node(1119)?.image : null;
            if (image) image.fillAmount = Mathf.Clamp01(value);
        }
    }
}
