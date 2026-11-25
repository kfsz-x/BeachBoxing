using System;
using System.Globalization;
using UnityEngine;

namespace BeachBoxing
{
    /// <summary>State owner for the complete preplaced source FightMenu457 hierarchy.</summary>
    public sealed class BeachNativeOpponentView : MonoBehaviour
    {
        public BeachPeripheralView peripheral;
        public NativeOpponentCatalog catalog;
        public AnimationClip menuOpen, menuClose;
        public string[] gauntletTitles;
        public bool IsOpen => open;
        public bool IsClosing => closing;
        private bool open, closing;
        private float elapsed;
        private int selection;
        private int lastSelection = -1, lastUnlocked = -1;
        private Action<int> change;
        private Action fight, close;
        private static readonly int[] StatIds = { 355, 1067, 336, 1149 };
        private static readonly int[] CircleIds = { 953, 117, 670, 728, 1127 };
        private static readonly int[] LineIds = { 0, 870, 304, 406, 63 };
        private static readonly int[] MarkerIds = { 624, 338, 465, 211, 668 };

        public void Bind(Action<int> change, Action fight, Action close)
        {
            this.change = change;
            this.fight = fight;
            this.close = close;
            Hook(553, () => this.change?.Invoke(-1));
            Hook(965, () => this.change?.Invoke(1));
            Hook(806, () => { if (selection < NativeOpponentCatalog.PlayableCount) this.fight?.Invoke(); });
            Hook(214, () => this.close?.Invoke());
            HideImmediately();
        }

        public void Render(bool visible, PlayerProgress progress, int selected)
        {
            if (!peripheral || !catalog) return;
            if (!visible)
            {
                if (open && !closing) { closing = true; elapsed = 0; }
                return;
            }
            if (!open || closing)
            {
                open = true;
                closing = false;
                elapsed = 0;
                Active(457, true);
                Active(417, false);
                Active(33, progress.unlockedOpponent == 0);
                Sample(menuOpen, 0);
                if (!menuOpen || menuOpen.length <= 0) Active(214, true);
                lastSelection = -1;
            }
            int unlocked = Mathf.Clamp(progress.unlockedOpponent, 0, NativeOpponentCatalog.PlayableCount);
            selection = Mathf.Clamp(selected, 0, unlocked);
            if (lastSelection == selection && lastUnlocked == unlocked) return;
            lastSelection = selection;
            lastUnlocked = unlocked;
            bool placeholder = selection == NativeOpponentCatalog.PlayableCount;
            var profile = placeholder ? null : catalog.Profiles[selection];
            Label(332, placeholder ? "COMING SOON" : profile.name);
            peripheral.Node(813).image.sprite = placeholder ? catalog.ComingSoonPortrait : catalog.Portraits[selection];
            int[] stats = placeholder ? new int[4] : new[] { profile.chest, profile.arms, profile.back, profile.legs };
            for (int i = 0; i < stats.Length; i++) Label(StatIds[i], BeachProgressionRules.FormatNumber(stats[i]));
            Label(1171, (50 * (selection + 1)).ToString(CultureInfo.InvariantCulture));
            Label(51, (3 * (selection + 1)).ToString(CultureInfo.InvariantCulture));
            int bracket = selection / 5;
            Label(150, gauntletTitles[bracket]);
            peripheral.Node(1164).image.sprite = catalog.GauntletBackgrounds[bracket];
            peripheral.Node(553).button.interactable = selection > 0;
            peripheral.Node(965).button.interactable = selection < unlocked;
            Active(806, !placeholder);
            for (int i = 0; i < CircleIds.Length; i++)
            {
                var color = new Color(1, 1, 1, unlocked / 5 != bracket || i <= unlocked % 5 ? 1 : .2f);
                peripheral.Node(CircleIds[i]).image.color = color;
                if (LineIds[i] != 0) peripheral.Node(LineIds[i]).image.color = color;
            }
        }

        public void HideImmediately()
        {
            open = closing = false;
            Active(457, false);
        }

        private void Update()
        {
            if (!open) return;
            var clip = closing ? menuClose : menuOpen;
            float duration = clip ? clip.length : 0;
            if (!closing && elapsed >= duration) return;
            elapsed += Time.deltaTime;
            Sample(clip, Mathf.Min(elapsed, Mathf.Max(0, duration - .000001f)));
            if (elapsed < duration) return;
            if (closing)
            {
                HideImmediately();
                Active(417, true);
            }
            else Active(214, true);
        }

        private void FixedUpdate()
        {
            if (!open || !peripheral) return;
            var arrow = peripheral.Node(935).transform;
            arrow.position = Vector3.Lerp(arrow.position, peripheral.Node(MarkerIds[selection % 5]).transform.position,
                Time.deltaTime * 15);
        }

        private void Hook(int id, Action action)
        {
            var button = peripheral.Node(id).button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => { if (open && !closing && button.interactable) action(); });
        }

        private void Label(int id, string text) => peripheral.Node(id).label.text = text;
        private void Active(int id, bool value)
        {
            var target = peripheral ? peripheral.Node(id)?.transform : null;
            if (target) target.gameObject.SetActive(value);
        }
        private void Sample(AnimationClip clip, float time)
        {
            if (clip) clip.SampleAnimation(peripheral.Node(457).transform.gameObject, time);
        }
    }
}
