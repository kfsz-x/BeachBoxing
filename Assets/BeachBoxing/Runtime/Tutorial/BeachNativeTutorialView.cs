using System;
using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Source tutorial visibility and scaled hint lifetime; world line targets belong to Stage.</summary>
    public sealed class BeachNativeTutorialView : MonoBehaviour
    {
        public BeachPeripheralView peripheral;
        public BeachTutorialTarget Target => progress == null ? BeachTutorialTarget.None : BeachTutorialRules.Target(progress);
        public bool LineVisible => tutorialEnabled && Target != BeachTutorialTarget.None;

        private PlayerProgress progress;
        private Action save;
        private bool phone, tutorialEnabled;
        private float keepTipRemaining = -1;

        public void Bind(PlayerProgress progress, Action save, bool phone)
        {
            this.progress = progress;
            this.save = save;
            this.phone = phone;
            tutorialEnabled = progress != null && progress.unlockedOpponent <= 0;
            Active(559, phone);
        }

        public void BeachEntered()
        {
            if (progress == null) return;
            tutorialEnabled = progress.unlockedOpponent <= 0;
            // FirstMove is saved for the SDK; source Start does not use it to hide this object.
            Active(601, true);
            Active(559, phone);
            Active(372, false);
            keepTipRemaining = -1;
            int previousHint = progress.keepTrainingHint;
            bool show = BeachTutorialRules.OnBeachEntered(progress, phone);
            if (progress.keepTrainingHint != previousHint) save?.Invoke();
            if (show)
            {
                Active(372, true);
                keepTipRemaining = 4.5f;
            }
        }

        public void ObserveMove(Vector2 movement)
        {
            if (progress == null || movement.sqrMagnitude < 1e-10f) return;
            Active(601, false);
            if (progress.firstMove != 0) return;
            progress.firstMove = 1;
            save?.Invoke();
        }

        public void UpdateProgress()
        {
            if (progress != null && tutorialEnabled && BeachTutorialRules.UpdateProgress(progress)) save?.Invoke();
        }

        public void Tick(float scaledDt)
        {
            if (scaledDt <= 0 || keepTipRemaining < 0) return;
            keepTipRemaining -= scaledDt;
            if (keepTipRemaining > 0) return;
            keepTipRemaining = -1;
            Active(372, false);
        }

        private void Active(int id, bool active)
        {
            var target = peripheral ? peripheral.Node(id)?.transform : null;
            if (target) target.gameObject.SetActive(active);
        }
    }
}
