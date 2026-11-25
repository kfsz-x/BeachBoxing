using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    [Serializable]
    public sealed class NativeUiNode
    {
        public int sourceId;
        public RectTransform transform;
        public Image image;
        public Text label;
        public Button button;
    }

    [Serializable]
    public sealed class NativeUiPortrait
    {
        public RectTransform target, portrait, landscape;
        public bool retainPhoneScaleInLandscape;
        [NonSerialized] internal bool portraitApplied;
    }

    public enum BeachPeripheralAction
    {
        Pause, OpenTrainer, OpenDaily, OpenPrestige, Close, SelectTrainer,
        Purchase, Equip, Upgrade, ClaimDaily, LevelUp, Interact, AccelerateTraining
    }

    /// <summary>References to authored scene objects. Never creates a menu or artwork at runtime.</summary>
    public sealed class BeachPeripheralView : MonoBehaviour
    {
        public NativeUiNode[] nodes = Array.Empty<NativeUiNode>();
        public NativeUiPortrait[] portraits = Array.Empty<NativeUiPortrait>();
        public AnimationClip menuOpen, menuClose, alertClip;
        private readonly Dictionary<int, NativeUiNode> lookup = new Dictionary<int, NativeUiNode>();
        private Action<BeachPeripheralAction, int> action;
        private int selection, openPanel;
        private bool closing;
        private float menuTime, alertTime;
        private GameObject alert;
        private bool scrollRestored;
        private float trainerScrollPosition = 1f;
        private bool dailyAlertDismissed;
        private bool menuAnimationFinished;
        public bool IsBound => nodes != null && nodes.Length > 0;
        public bool IsOpen => openPanel != 0;
        public bool AcceptsActions => IsOpen && !closing;
        public int Selection => selection;

        private static readonly int[] TrainerButtons = {901,975,556,423,1024,826};
        private static readonly int[] TrainerPortraits = {781,977,837,658,644,893};
        private static readonly int[] TrainerLevelLabels = {985,361,72,925,492,1099};
        private static readonly int[] ClaimButtons = {427,364,848,97,791,1012};
        private static readonly int[] Claimable = {141,673,291,1001,639,153};
        private static readonly int[] Claimed = {1096,490,538,911,853,1202};
        private static readonly int[] Full = {324,251,1087,1029};
        private static readonly int[] Fills = {762,1083,120,480};
        private static readonly int[] StatLabels = {293,1172,1028,223};
        private static readonly int[] StatShadows = {802,716,1051,360};
        private static readonly int[] LevelPanels = {1126,1126,718,979,391,384,419,260,1004,685};
        private readonly Sprite[] trainerIcons = new Sprite[6];

        public NativeUiNode Node(int id)
        {
            if (lookup.Count == 0 && nodes != null)
                foreach (var node in nodes) if (node != null) lookup[node.sourceId] = node;
            return lookup.TryGetValue(id, out var found) ? found : null;
        }

        public void Bind(Action<BeachPeripheralAction, int> callback)
        {
            lookup.Clear();
            action = callback;
            for (int i = 0; i < TrainerButtons.Length; i++)
            {
                int index = i;
                Hook(TrainerButtons[i], BeachPeripheralAction.SelectTrainer, index);
                var icon = Node(TrainerPortraits[i]);
                if (icon?.image) trainerIcons[i] = icon.image.sprite;
                Hook(ClaimButtons[i], BeachPeripheralAction.ClaimDaily, index);
            }
            Hook(760, BeachPeripheralAction.Pause);
            Hook(855, BeachPeripheralAction.OpenTrainer);
            Hook(510, BeachPeripheralAction.OpenDaily);
            Hook(129, BeachPeripheralAction.OpenPrestige);
            Hook(54, BeachPeripheralAction.LevelUp);
            Hook(227, BeachPeripheralAction.Purchase);
            Hook(569, BeachPeripheralAction.Equip);
            Hook(618, BeachPeripheralAction.Upgrade);
            Hook(586, BeachPeripheralAction.AccelerateTraining);
            if (!GetComponent<BeachNativeMobileControls>())
            {
                Hook(1057, BeachPeripheralAction.Interact);
                var touch = Node(1110)?.transform?.GetComponent<NativeStationTouch>();
                if (touch) touch.Bind(() => action?.Invoke(BeachPeripheralAction.Interact, -1));
            }
            foreach (int id in new[] {1146,145,882}) Hook(id, BeachPeripheralAction.Close);
            foreach (int id in new[] {602,204,679,61,438}) Active(id, false);
            openPanel = 0;
            dailyAlertDismissed = false;
        }

        private void Hook(int id, BeachPeripheralAction request, int argument = -1)
        {
            var button = Node(id)?.button;
            if (!button) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action?.Invoke(request, argument));
        }

        public void ShowMenu(int sourceId, BeachPeripheralState state)
        {
            if (sourceId != 602 && sourceId != 204 && sourceId != 679) return;
            if (openPanel != 0) Active(openPanel, false);
            selection = state.equippedTrainer >= 0 ? state.equippedTrainer : 0;
            openPanel = sourceId;
            if (sourceId == 204) dailyAlertDismissed = true;
            closing = false;
            menuTime = 0;
            menuAnimationFinished = false;
            scrollRestored = false;
            var scroll = Node(41)?.transform?.GetComponent<ScrollRect>();
            if (scroll) scroll.enabled = false;
            Active(sourceId, true);
            SampleMenu();
        }

        public void CloseMenu()
        {
            if (!IsOpen || closing) return;
            var scroll = Node(41)?.transform?.GetComponent<ScrollRect>();
            if (openPanel == 602 && scroll)
            {
                trainerScrollPosition = scroll.verticalNormalizedPosition;
                scroll.enabled = false;
            }
            closing = true;
            menuTime = 0;
            menuAnimationFinished = false;
            SampleMenu();
        }

        public void HideMenuImmediately()
        {
            if (IsOpen) Active(openPanel, false);
            openPanel = 0;
            closing = false;
        }

        public void SelectTrainer(int trainer) => selection = Mathf.Clamp(trainer, 0, 5);

        public void Feedback(bool collected)
        {
            if (alert) alert.SetActive(false);
            alert = Node(collected ? 438 : 61)?.transform?.gameObject;
            alertTime = 0;
            if (alert) alert.SetActive(true);
            if (alert && alertClip) alertClip.SampleAnimation(alert, 0);
        }

        private void Update()
        {
            if (IsOpen)
            {
                menuTime += Time.unscaledDeltaTime;
                SampleMenu();
                if (closing && menuTime >= (menuClose ? menuClose.length : 1f / 6f))
                {
                    Active(openPanel, false);
                    openPanel = 0;
                    closing = false;
                }
            }
            if (alert)
            {
                alertTime += Time.unscaledDeltaTime;
                if (alertClip) alertClip.SampleAnimation(alert, Mathf.Min(alertTime, alertClip.length));
                if (alertTime >= (alertClip ? alertClip.length : 2.166667f))
                {
                    alert.SetActive(false);
                    alert = null;
                }
            }
        }

        private void SampleMenu()
        {
            if (menuAnimationFinished) return;
            var panel = Node(openPanel)?.transform;
            var clip = closing ? menuClose : menuOpen;
            if (panel && clip) clip.SampleAnimation(panel.gameObject, Mathf.Min(menuTime, clip.length));
            if (!closing && menuTime >= (menuOpen ? menuOpen.length : 1f / 3f))
            {
                Active(openPanel == 602 ? 1146 : openPanel == 204 ? 145 : 882, true);
                if (openPanel == 602 && !scrollRestored)
                {
                    Canvas.ForceUpdateCanvases();
                    var scroll = Node(41)?.transform?.GetComponent<ScrollRect>();
                    if (scroll) { scroll.enabled = true; scroll.verticalNormalizedPosition = trainerScrollPosition; }
                    scrollRestored = true;
                }
                menuAnimationFinished = true;
            }
        }

        public void SuspendChrome()
        {
            foreach (int id in new[] {895,417,339}) Active(id, false);
        }

        public void RenderAutoTraining(bool exercising, bool accelerated, int secondsLeft)
        {
            Active(881, exercising);
            Active(643, exercising);
            Active(378, !accelerated);
            Active(305, accelerated);
            Label(248, secondsLeft.ToString());
        }

        public void Render(PlayerProgress progress, BeachPhase phase, bool exercising, bool nearEquipment, bool touchLayout = false)
        {
            if (!IsBound) return;
            bool roaming = phase == BeachPhase.Training && !exercising;
            bool chrome = phase == BeachPhase.Training || phase == BeachPhase.OpponentSelect;
            bool phone = touchLayout;
            foreach (int id in new[] {52,385,472,714}) Active(id, !phone);
            Active(353, chrome);
            Active(895, roaming);
            Active(855, progress.unlockedOpponent > 0);
            Active(510, progress.unlockedOpponent > 0);
            Active(417, roaming && !IsOpen && nearEquipment);
            Active(1057, roaming && !IsOpen && nearEquipment);
            Active(559, phone && chrome);
            var state = progress.peripheral;
            Active(628, !dailyAlertDismissed && BeachPeripheralRules.HasDailyReward(state));
            Active(299, state.trainerAlert);
            int[] strengths = {progress.chest,progress.arms,progress.back,progress.legs};
            for (int i = 0; i < 4; i++)
            {
                string value = BeachProgressionRules.FormatNumber(strengths[i]);
                Label(StatLabels[i], value);
                Label(StatShadows[i], value);
                var fill = Node(Fills[i])?.image;
                if (fill) fill.fillAmount = BeachProgressionRules.Fraction(strengths[i], progress.prestigeLevel);
                Active(Full[i], BeachProgressionRules.IsFull(strengths[i], progress.prestigeLevel));
            }
            bool ready = BeachProgressionRules.CanLevelUp(progress, progress.prestigeLevel);
            Active(339, chrome && !IsOpen && ready);
            for (int i = 0; i < 6; i++)
            {
                int level = state.trainerLevels[i];
                Label(TrainerLevelLabels[i], "LVL " + level);
                Active(TrainerLevelLabels[i], level > 0);
                var button = Node(ClaimButtons[i])?.button;
                if (button) button.interactable = BeachPeripheralRules.CanClaim(state, i);
                Active(Claimable[i], BeachPeripheralRules.CanClaim(state, i));
                Active(Claimed[i], (state.claimedDays & (1 << i)) != 0);
            }
            Label(1085, "LVL " + state.trainerLevels[selection]);
            var iconNode = Node(1169)?.image;
            if (iconNode && trainerIcons[selection]) iconNode.sprite = trainerIcons[selection];
            Label(1157, "+" + BeachPeripheralRules.GetDisplayedModifier(state, selection, Muscle.Chest) + "%");
            Label(667, "+" + BeachPeripheralRules.GetDisplayedModifier(state, selection, Muscle.Arms) + "%");
            Label(410, "+" + BeachPeripheralRules.GetDisplayedModifier(state, selection, Muscle.Back) + "%");
            Label(1068, "+" + BeachPeripheralRules.GetDisplayedModifier(state, selection, Muscle.Legs) + "%");
            int cost = BeachPeripheralRules.GetPurchaseCost(selection);
            bool owned = state.trainerLevels[selection] > 0;
            Active(1085, owned);
            Active(227, !owned && cost >= 0);
            Active(737, !owned && cost < 0);
            Active(569, owned && state.equippedTrainer != selection);
            Active(618, owned && state.equippedTrainer == selection);
            Label(966, BeachPeripheralRules.GetUpgradeCost(state, selection).ToString());
            Label(652, cost.ToString()); Label(581, cost.ToString());
            Label(190, progress.coins.ToString()); Label(1044, progress.stars.ToString());
            int preview = progress.prestigeLevel < 10 ? LevelPanels[progress.prestigeLevel] : -1;
            foreach (int id in LevelPanels) Active(id, id == preview);
            var confirm = Node(54)?.button;
            if (confirm) confirm.interactable = ready;
            FitPortrait();
        }

        private void FitPortrait()
        {
            bool portrait = Screen.height > Screen.width;
            foreach (var binding in portraits)
            {
                if (binding?.target == null) continue;
                var source = portrait ? binding.portrait : binding.landscape;
                if (!source || source == binding.target) continue;
                if (portrait) binding.portraitApplied = true;
                // The original owner-only scale override is not reset by its landscape branch.
                if (!portrait && binding.retainPhoneScaleInLandscape && binding.portraitApplied) continue;
                binding.target.anchoredPosition3D = source.anchoredPosition3D;
                binding.target.localScale = source.localScale;
            }
        }

        private void Active(int id, bool value)
        {
            var node = Node(id);
            if (node?.transform && node.transform.gameObject.activeSelf != value) node.transform.gameObject.SetActive(value);
        }

        private void Label(int id, string value)
        {
            var label = Node(id)?.label;
            if (label && label.text != value) label.text = value;
        }
    }
}
