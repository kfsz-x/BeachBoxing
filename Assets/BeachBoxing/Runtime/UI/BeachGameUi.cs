using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing
{
    public enum BeachUiModal
    {
        None,
        Challenge,
        Boxer,
        SignIn
    }

    public sealed class BeachGameUi : MonoBehaviour
    {
        [Header("Platform")]
        public bool simulateTouchInEditor;

        [Header("Root Canvas")]
        public Canvas canvas;
        public RectTransform safeRoot;

        [Header("HUD (Roaming + Workout)")]
        public GameObject hudRoot;
        public Text[] statValueLabels = new Text[4];
        public Image[] statProgressFills = new Image[4];
        public Text coinValueLabel;
        public Text starValueLabel;
        public Button coinAdButton;
        public Button starAdButton;
        public Button settingsButton;
        public Button boxerButton;
        public GameObject boxerNewBadge;
        public Button signInButton;
        public GameObject signInAlertBadge;
        public GameObject tipBannerRoot;
        public CanvasGroup tipBannerGroup;
        public Text tipBannerText;

        [Header("Mobile Roaming Controls")]
        public GameObject roamingControlsRoot;
        public BeachMobileStick roamingJoystick;
        public BeachHoldButton roamingJumpHold;

        [Header("Workout HUD")]
        public GameObject workoutRoot;
        public BeachHoldButton quickWorkoutHold;
        public Button exitWorkoutButton;
        public Button doubleWorkoutButton;
        public Text doubleWorkoutLabel;
        public GameObject doubleWorkoutAdBadge;
        public Image workoutProgressFill;

        [Header("Combat HUD")]
        public GameObject combatRoot;
        public GameObject combatMobileControlsRoot;
        public BeachMobileStick combatJoystick;
        public BeachHoldButton punchHold;
        public BeachHoldButton blockHold;
        public Image combatScreenOverlay;

        [Header("World Space Fighter Bars")]
        public GameObject playerBarsRoot;
        public Image playerHealthFill;
        public Text playerHealthText;
        public Image playerStaminaFill;
        public Text playerStaminaText;
        public GameObject enemyBarsRoot;
        public Image enemyHealthFill;
        public Text enemyHealthText;
        public Image enemyStaminaFill;
        public Text enemyStaminaText;
        public GameObject playerTitleRoot;

        [Header("Countdown")]
        public GameObject countdownRoot;
        public RectTransform countdownBadge;
        public Text countdownText;

        [Header("Challenge Modal")]
        public GameObject challengeRoot;
        public Button challengeCloseButton;
        public Text challengeNameText;
        public Image challengeStageBackground;
        public Image challengePortraitImage;
        public Button challengePrevButton;
        public Button challengeNextButton;
        public Text[] challengeStatTexts = new Text[4];
        public Text challengeCoinRewardText;
        public Text challengeStarRewardText;
        public Image[] challengeProgressNodes = new Image[5];
        public Sprite challengeNodeActiveSprite;
        public Sprite challengeNodePendingSprite;
        public Sprite challengeNodeCrownSprite;
        public Button challengeStartButton;

        [Header("Boxer Selection Modal")]
        public GameObject boxerRoot;
        public Button boxerCloseButton;
        public Button[] boxerCardButtons = new Button[6];
        public Image[] boxerCardBackgrounds = new Image[6];
        public Image[] boxerCardPortraits = new Image[6];
        public Text[] boxerCardNames = new Text[6];
        public GameObject[] boxerCardCheckmarks = new GameObject[6];
        public RectTransform boxerSelectionFrame;
        public Sprite boxerCardEquippedSprite;
        public Sprite boxerCardNormalSprite;
        public Sprite[] boxerPortraitSprites = new Sprite[6];
        public Sprite[] boxerFullSprites = new Sprite[6];
        public Image boxerPreviewImage;
        public Text boxerPreviewNameText;
        public Text boxerPreviewTypeText;
        public Text[] boxerBonusTexts = new Text[4];
        public Button boxerActionButton;
        public Text boxerActionLabel;

        [Header("7-Day Sign-In Modal")]
        public GameObject signInRoot;
        public Button signInCloseButton;
        public Button[] signInDayButtons = new Button[7];
        public Image[] signInDayBackgrounds = new Image[7];
        public GameObject[] signInDayCheckmarks = new GameObject[7];
        public GameObject[] signInDayLockOverlays = new GameObject[7];
        public Text[] signInDayTitleTexts = new Text[7];
        public Text[] signInDayRewardTexts = new Text[7];
        public RectTransform signInSelectionFrame;
        public Sprite signInCardClaimedSprite;
        public Sprite signInCardNormalSprite;
        public Button signInClaimButton;
        public Button signInDoubleClaimButton;

        [Header("Result Modal (Victory / Defeat)")]
        public GameObject resultRoot;
        public Button resultCloseButton;
        public Image resultPanelBg;
        public Image resultTitleArt;
        public Image resultPodiumImage;
        public GameObject resultConfetti;
        public Image resultOpponentAvatar;
        public Text resultOpponentLabel;
        public Image resultStatusBadge;
        public GameObject resultRewardSection;
        public Text resultCoinValueText;
        public Text resultStarValueText;
        public Button resultContinueButton;
        public Button resultRestartButton;
        public Image resultRestartImage;
        public RectTransform resultRestartRect;
        public Text resultRestartLabel;
        public Sprite victoryPanelSprite;
        public Sprite defeatPanelSprite;
        public Sprite victoryTitleSprite;
        public Sprite defeatTitleSprite;
        public Sprite victoryPodiumSprite;
        public Sprite defeatPodiumSprite;
        public Sprite victoryStatusSprite;
        public Sprite defeatStatusSprite;
        public Sprite yellowBtnSprite;
        public Sprite blueBtnSprite;
        public Vector2 victoryRestartPosition = new Vector2(415.5f, -377f);
        public Vector2 defeatRestartPosition = new Vector2(202.5f, -377f);

        public BeachUiModal OpenModal { get; private set; }
        public bool IsModalOpen => OpenModal != BeachUiModal.None;
        public int SelectedBoxer { get; private set; }
        public int SelectedSignInDay { get; private set; }
        public bool UseTouch { get; private set; }

        public Vector2 MoveDirection
        {
            get
            {
                if (!UseTouch) return Vector2.zero;
                if (combatRoot != null && combatRoot.activeInHierarchy && combatJoystick != null)
                    return combatJoystick.Direction;
                if (roamingControlsRoot != null && roamingControlsRoot.activeInHierarchy && roamingJoystick != null)
                    return roamingJoystick.Direction;
                return Vector2.zero;
            }
        }

        public bool PunchHeld => UseTouch && combatRoot != null && combatRoot.activeInHierarchy && punchHold != null && punchHold.Held;
        public bool BlockHeld => UseTouch && combatRoot != null && combatRoot.activeInHierarchy && blockHold != null && blockHold.Held;
        public bool WorkoutBoostHeld => workoutRoot != null && workoutRoot.activeInHierarchy && quickWorkoutHold != null && quickWorkoutHold.Held;

        public bool ConsumePunchPress() => UseTouch && punchHold != null && punchHold.ConsumePress();
        public bool ConsumeWorkoutBoostPress() => quickWorkoutHold != null && quickWorkoutHold.ConsumePress();
        public bool ConsumeJumpPress() => UseTouch && roamingControlsRoot != null && roamingControlsRoot.activeInHierarchy && roamingJumpHold != null && roamingJumpHold.ConsumePress();
        public bool ConsumeJumpRelease() => UseTouch && roamingJumpHold != null && roamingJumpHold.ConsumeRelease();

        public const float TipBannerHoldSeconds = 3f;
        public const float TipBannerFadeSeconds = 0.6f;

        private NativeOpponentCatalog catalog;
        private int lastCountdownInt = -1;
        private float countdownPopTimer;
        private int lastChallengePortrait = -1;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float tipBannerElapsed;
        private bool tipBannerDismissed;
        private const float QuickWorkoutPulseDuration = 0.18f;
        private float quickWorkoutPulseTimer = QuickWorkoutPulseDuration;
        private float quickWorkoutShakeSign = 1f;
        private bool quickWorkoutPoseCached;
        private Vector2 quickWorkoutBasePos;
        private Vector3 quickWorkoutBaseScale = Vector3.one;

        private Action onOpenSettings;
        private Action onCoinAd, onStarAd;
        private Action onExitWorkout, onDoubleWorkout;
        private Action<int> onChangeOpponent;
        private Action onStartChallenge, onCloseChallenge;
        private Action<int> onBoxerAction;
        private Action<int, bool> onSignInClaim;
        private Action onResultContinue, onResultRestart, onResultClose;

        public void Bind(
            NativeOpponentCatalog opponentCatalog,
            Func<bool> canAcceptRoamingStick,
            Func<bool> canAcceptCombatControls,
            Func<bool> canAcceptWorkoutControls,
            Action openSettings,
            Action coinAd,
            Action starAd,
            Action exitWorkout,
            Action doubleWorkout,
            Action<int> changeOpponent,
            Action startChallenge,
            Action closeChallenge,
            Action<int> boxerAction,
            Action<int, bool> signInClaim,
            Action resultContinue,
            Action resultRestart,
            Action resultClose)
        {
            catalog = opponentCatalog;
            onOpenSettings = openSettings;
            onCoinAd = coinAd;
            onStarAd = starAd;
            onExitWorkout = exitWorkout;
            onDoubleWorkout = doubleWorkout;
            onChangeOpponent = changeOpponent;
            onStartChallenge = startChallenge;
            onCloseChallenge = closeChallenge;
            onBoxerAction = boxerAction;
            onSignInClaim = signInClaim;
            onResultContinue = resultContinue;
            onResultRestart = resultRestart;
            onResultClose = resultClose;

            UseTouch = BeachPlatformInput.DetectMobile(simulateTouchInEditor);

            if (roamingJoystick != null) roamingJoystick.Bind(canAcceptRoamingStick);
            if (roamingJumpHold != null) roamingJumpHold.Bind(canAcceptRoamingStick);
            if (combatJoystick != null) combatJoystick.Bind(canAcceptCombatControls);
            if (punchHold != null) punchHold.Bind(canAcceptCombatControls);
            if (blockHold != null) blockHold.Bind(canAcceptCombatControls);
            if (quickWorkoutHold != null) quickWorkoutHold.Bind(canAcceptWorkoutControls);

            Hook(settingsButton, () => onOpenSettings?.Invoke());
            Hook(boxerButton, () => OpenBoxerModal());
            Hook(signInButton, () => OpenSignInModal());
            Hook(coinAdButton, () => onCoinAd?.Invoke());
            Hook(starAdButton, () => onStarAd?.Invoke());

            Hook(exitWorkoutButton, () => onExitWorkout?.Invoke());
            Hook(doubleWorkoutButton, () => onDoubleWorkout?.Invoke());

            Hook(challengeCloseButton, () => onCloseChallenge?.Invoke());
            Hook(challengePrevButton, () => onChangeOpponent?.Invoke(-1));
            Hook(challengeNextButton, () => onChangeOpponent?.Invoke(1));
            Hook(challengeStartButton, () => onStartChallenge?.Invoke());

            Hook(boxerCloseButton, CloseModals);
            Hook(boxerActionButton, () => onBoxerAction?.Invoke(SelectedBoxer));
            for (int i = 0; i < boxerCardButtons.Length; i++)
            {
                int idx = i;
                Hook(boxerCardButtons[i], () => SelectedBoxer = idx);
            }

            Hook(signInCloseButton, CloseModals);
            for (int i = 0; i < signInDayButtons.Length; i++)
            {
                int day = i;
                Hook(signInDayButtons[i], () => SelectedSignInDay = day);
            }
            Hook(signInClaimButton, () => onSignInClaim?.Invoke(SelectedSignInDay, false));
            Hook(signInDoubleClaimButton, () => onSignInClaim?.Invoke(SelectedSignInDay, true));

            Hook(resultCloseButton, () => onResultClose?.Invoke());
            Hook(resultContinueButton, () => onResultContinue?.Invoke());
            Hook(resultRestartButton, () => onResultRestart?.Invoke());

            CloseModals();
            ResetTipBanner();
            ClearCombatOverlay();
            SetActive(workoutRoot, false);
            SetActive(combatRoot, false);
            SetActive(countdownRoot, false);
            SetActive(resultRoot, false);
            SetActive(playerBarsRoot, false);
            SetActive(enemyBarsRoot, false);
        }

        public void SetUseTouch(bool enabled)
        {
            UseTouch = enabled;
            BeachPlatformInput.SetTouchOverride(enabled);
            ResetTouchControls();
        }

        public void OpenBoxerModal(int initialSelection = -1)
        {
            if (initialSelection >= 0) SelectedBoxer = Mathf.Clamp(initialSelection, 0, BeachPeripheralRules.TrainerCount - 1);
            OpenModal = BeachUiModal.Boxer;
            ResetTouchControls();
            BeachCommonUI.PlayPopupOpen();
        }

        public void OpenSignInModal(int initialDay = -1)
        {
            if (initialDay >= 0) SelectedSignInDay = Mathf.Clamp(initialDay, 0, BeachPeripheralRules.DailyRewardCount - 1);
            OpenModal = BeachUiModal.SignIn;
            ResetTouchControls();
            BeachCommonUI.PlayPopupOpen();
        }

        public void CloseModals()
        {
            OpenModal = BeachUiModal.None;
            SetActive(boxerRoot, false);
            SetActive(signInRoot, false);
            SetActive(challengeRoot, false);
        }

        public void ResetTouchControls()
        {
            if (roamingJoystick != null) roamingJoystick.ResetStick();
            if (roamingJumpHold != null) roamingJumpHold.ResetButton();
            if (combatJoystick != null) combatJoystick.ResetStick();
            if (punchHold != null) punchHold.ResetButton();
            if (blockHold != null) blockHold.ResetButton();
            if (quickWorkoutHold != null) quickWorkoutHold.ResetButton();
            ResetQuickWorkoutPulse();
        }

        public void PulseQuickWorkoutButton()
        {
            CacheQuickWorkoutPose();
            quickWorkoutPulseTimer = 0f;
            quickWorkoutShakeSign = -quickWorkoutShakeSign;
            ApplyQuickWorkoutPulse(0f);
        }

        public void AdvanceQuickWorkoutPulse(float dt)
        {
            if (quickWorkoutHold == null) return;
            if (quickWorkoutPulseTimer >= QuickWorkoutPulseDuration) return;
            quickWorkoutPulseTimer = Mathf.Min(QuickWorkoutPulseDuration, quickWorkoutPulseTimer + Mathf.Max(0f, dt));
            ApplyQuickWorkoutPulse(quickWorkoutPulseTimer);
        }

        private void CacheQuickWorkoutPose()
        {
            if (quickWorkoutPoseCached || quickWorkoutHold == null) return;
            var rect = quickWorkoutHold.transform as RectTransform;
            if (rect == null) return;
            quickWorkoutBasePos = rect.anchoredPosition;
            quickWorkoutBaseScale = rect.localScale;
            quickWorkoutPoseCached = true;
        }

        private void ResetQuickWorkoutPulse()
        {
            quickWorkoutPulseTimer = QuickWorkoutPulseDuration;
            if (!quickWorkoutPoseCached || quickWorkoutHold == null) return;
            var rect = quickWorkoutHold.transform as RectTransform;
            if (rect == null) return;
            rect.anchoredPosition = quickWorkoutBasePos;
            rect.localScale = quickWorkoutBaseScale;
            rect.localRotation = Quaternion.identity;
        }

        private void ApplyQuickWorkoutPulse(float elapsed)
        {
            if (quickWorkoutHold == null) return;
            CacheQuickWorkoutPose();
            var rect = quickWorkoutHold.transform as RectTransform;
            if (rect == null) return;

            if (elapsed >= QuickWorkoutPulseDuration)
            {
                rect.anchoredPosition = quickWorkoutBasePos;
                rect.localScale = quickWorkoutBaseScale;
                rect.localRotation = Quaternion.identity;
                return;
            }

            float u = Mathf.Clamp01(elapsed / QuickWorkoutPulseDuration);
            float scaleMul;
            if (u < 0.22f)
            {
                // Crisp initial finger-press squash (1.0 -> 0.90)
                scaleMul = Mathf.Lerp(1f, 0.90f, u / 0.22f);
            }
            else if (u < 0.52f)
            {
                // Rebound pop overshoot (0.90 -> 1.12)
                float k = (u - 0.22f) / 0.30f;
                scaleMul = Mathf.Lerp(0.90f, 1.12f, Mathf.Sin(k * Mathf.PI * 0.5f));
            }
            else
            {
                // Smooth settle back to 1.0
                float k = (u - 0.52f) / 0.48f;
                scaleMul = Mathf.Lerp(1.12f, 1f, k * k * (3f - 2f * k));
            }

            float decay = (1f - u) * (1f - u);
            float tiltDeg = quickWorkoutShakeSign * 4.2f * Mathf.Sin(u * Mathf.PI * 2.5f) * decay;
            float offsetX = quickWorkoutShakeSign * 4.5f * Mathf.Sin(u * Mathf.PI * 3f) * decay;
            float offsetY = -3.0f * Mathf.Sin(u * Mathf.PI) * decay;

            rect.localScale = quickWorkoutBaseScale * scaleMul;
            rect.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
            rect.anchoredPosition = quickWorkoutBasePos + new Vector2(offsetX, offsetY);
        }

        public void Render(
            BeachPhase phase,
            PlayerProgress progress,
            bool exercising,
            float workoutProgress,
            bool accelerated,
            int acceleratedSeconds,
            OpponentProfile opponent,
            int opponentIndex,
            int unlockedOpponents,
            FightController fight,
            bool rewarded,
            bool settingsOpen)
        {
            UpdateSafeArea();

            bool training = phase == BeachPhase.Training;
            bool roaming = training && !exercising;
            bool choosing = phase == BeachPhase.OpponentSelect;
            bool fighting = phase == BeachPhase.Fight && fight != null;
            bool countdownActive = fighting && fight.Countdown > 0f;
            bool canFight = fighting && fight.Countdown <= 0f && fight.Outcome == FightOutcome.Pending;
            bool ended = phase == BeachPhase.Result && fight != null;

            bool showHud = (training || choosing) && !settingsOpen;
            SetActive(hudRoot, showHud);
            UpdateTipBanner(showHud && roaming && !IsModalOpen, Time.deltaTime);
            SetActive(roamingControlsRoot, UseTouch && showHud && roaming && !IsModalOpen);
            SetActive(workoutRoot, showHud && exercising && !IsModalOpen);
            SetActive(playerTitleRoot, training && !exercising);

            SetActive(combatRoot, canFight && !settingsOpen);
            SetActive(combatMobileControlsRoot, UseTouch && canFight && !settingsOpen);
            SetActive(playerBarsRoot, canFight);
            SetActive(enemyBarsRoot, canFight);
            SetActive(countdownRoot, countdownActive);

            SetActive(challengeRoot, choosing && !settingsOpen);
            SetActive(boxerRoot, training && OpenModal == BeachUiModal.Boxer && !settingsOpen);
            SetActive(signInRoot, training && OpenModal == BeachUiModal.SignIn && !settingsOpen);
            SetActive(resultRoot, ended && !settingsOpen);

            if (showHud)
                RenderHud(progress);

            if (exercising && workoutRoot != null && workoutRoot.activeSelf)
                RenderWorkout(workoutProgress, accelerated, acceleratedSeconds);

            if (choosing && challengeRoot != null && challengeRoot.activeSelf)
                RenderChallenge(opponent, opponentIndex, unlockedOpponents);

            if (boxerRoot != null && boxerRoot.activeSelf)
                RenderBoxerModal(progress);

            if (signInRoot != null && signInRoot.activeSelf)
                RenderSignInModal(progress);

            if (fighting && !settingsOpen)
                RenderCombat(fight, countdownActive, canFight);
            else
                ClearCombatOverlay();

            if (ended && resultRoot != null && resultRoot.activeSelf)
                RenderResult(fight, opponent, opponentIndex, rewarded);
        }

        private void RenderHud(PlayerProgress progress)
        {
            int[] values = { progress.chest, progress.arms, progress.back, progress.legs };
            int level = Mathf.Clamp(progress.prestigeLevel, 1, 10);
            for (int i = 0; i < values.Length; i++)
            {
                if (statValueLabels != null && i < statValueLabels.Length)
                    SetText(statValueLabels[i], BeachProgressionRules.FormatNumber(values[i]));
                if (statProgressFills != null && i < statProgressFills.Length && statProgressFills[i] != null)
                {
                    var fill = statProgressFills[i];
                    fill.fillAmount = BeachProgressionRules.Fraction(values[i], level);
                    fill.color = Color.white;
                }
            }

            SetText(coinValueLabel, progress.coins.ToString(CultureInfo.InvariantCulture));
            SetText(starValueLabel, progress.stars.ToString(CultureInfo.InvariantCulture));
            SetActive(boxerNewBadge, progress.peripheral != null && progress.peripheral.trainerAlert);
            SetActive(signInAlertBadge, BeachPeripheralRules.HasDailyReward(progress.peripheral));
        }

        private void RenderWorkout(float progress01, bool accelerated, int secondsLeft)
        {
            AdvanceQuickWorkoutPulse(Time.unscaledDeltaTime);
            if (workoutProgressFill != null)
                workoutProgressFill.fillAmount = Mathf.Clamp01(progress01);
            if (doubleWorkoutButton != null)
                doubleWorkoutButton.interactable = !accelerated;
            SetText(doubleWorkoutLabel, accelerated ? $"{secondsLeft}s" : "2倍");
            SetActive(doubleWorkoutAdBadge, !accelerated);
        }

        private void RenderCombat(FightController fight, bool countdownActive, bool canFight)
        {
            if (countdownActive)
            {
                int ceil = Mathf.Clamp(Mathf.CeilToInt(fight.Countdown), 1, 3);
                if (ceil != lastCountdownInt)
                {
                    lastCountdownInt = ceil;
                    countdownPopTimer = 0.25f;
                }
                SetText(countdownText, ceil.ToString(CultureInfo.InvariantCulture));
                if (countdownBadge != null)
                {
                    countdownPopTimer = Mathf.Max(0f, countdownPopTimer - Time.unscaledDeltaTime);
                    float t = countdownPopTimer / 0.25f;
                    float s = 1f + 0.25f * t * t;
                    countdownBadge.localScale = Vector3.one * s;
                }
            }
            else
            {
                lastCountdownInt = -1;
            }

            if (canFight)
            {
                UpdateFighterBar(fight.Player, playerHealthFill, playerHealthText, playerStaminaFill, playerStaminaText);
                UpdateFighterBar(fight.Enemy, enemyHealthFill, enemyHealthText, enemyStaminaFill, enemyStaminaText);

                if (combatScreenOverlay != null)
                {
                    float hurt = !fight.Player.LastImpactWasBlocked ? Mathf.Clamp01(fight.PlayerFlash / 0.16f) : 0f;
                    float weak = (fight.Player.Health <= 15f || fight.Player.Stamina <= 15f) ? 0.22f : 0f;
                    bool healing = fight.Player.Blocking &&
                        (fight.Player.Health < fight.Player.MaxHealth || fight.Player.Stamina < fight.Player.MaxStamina);
                    if (healing && hurt <= 0f)
                        combatScreenOverlay.color = new Color(0.08f, 1f, 0.2f, 0.18f);
                    else if (hurt > 0f || weak > 0f)
                        combatScreenOverlay.color = new Color(1f, 0.13f, 0.1f, Mathf.Max(weak, hurt * 0.65f));
                    else
                        combatScreenOverlay.color = Color.clear;
                    combatScreenOverlay.enabled = combatScreenOverlay.color.a > 0.001f;
                }
            }
            else
            {
                ClearCombatOverlay();
            }
        }

        private void ClearCombatOverlay()
        {
            if (combatScreenOverlay == null) return;
            combatScreenOverlay.color = Color.clear;
            combatScreenOverlay.enabled = false;
        }

        private static void UpdateFighterBar(FighterState state, Image hpFill, Text hpText, Image stFill, Text stText)
        {
            if (state == null) return;
            if (hpFill != null) hpFill.fillAmount = state.MaxHealth > 0 ? Mathf.Clamp01(state.Health / state.MaxHealth) : 0f;
            if (stFill != null) stFill.fillAmount = state.MaxStamina > 0 ? Mathf.Clamp01(state.Stamina / state.MaxStamina) : 0f;
            SetText(hpText, $"{Mathf.CeilToInt(state.Health)}/{Mathf.CeilToInt(state.MaxHealth)}");
            SetText(stText, $"{Mathf.CeilToInt(state.Stamina)}/{Mathf.CeilToInt(state.MaxStamina)}");
        }

        private void RenderChallenge(OpponentProfile opponent, int index, int unlocked)
        {
            bool comingSoon = index >= NativeOpponentCatalog.PlayableCount || opponent == null;
            SetText(challengeNameText, comingSoon ? "敬请期待" : BeachChineseText.Translate(opponent.name));

            int[] stats = comingSoon ? new int[4] : new[] { opponent.chest, opponent.arms, opponent.back, opponent.legs };
            for (int i = 0; i < challengeStatTexts.Length && i < stats.Length; i++)
                SetText(challengeStatTexts[i], BeachProgressionRules.FormatNumber(stats[i]));

            int coinReward = comingSoon ? 0 : 50 * (index + 1);
            int starReward = comingSoon ? 0 : 3 * (index + 1);
            SetText(challengeCoinRewardText, "x" + coinReward.ToString(CultureInfo.InvariantCulture));
            SetText(challengeStarRewardText, "x" + starReward.ToString(CultureInfo.InvariantCulture));

            if (catalog != null && lastChallengePortrait != index)
            {
                lastChallengePortrait = index;
                int bracket = Mathf.Clamp(index / 5, 0, Math.Max(0, catalog.GauntletBackgrounds.Length - 1));
                if (challengeStageBackground != null && catalog.GauntletBackgrounds.Length > 0)
                    challengeStageBackground.sprite = catalog.GauntletBackgrounds[bracket];
                if (challengePortraitImage != null)
                    challengePortraitImage.sprite = comingSoon
                        ? catalog.ComingSoonPortrait
                        : catalog.Portraits[Mathf.Clamp(index, 0, catalog.Portraits.Length - 1)];
            }

            if (challengePrevButton != null) challengePrevButton.interactable = index > 0;
            if (challengeNextButton != null) challengeNextButton.interactable = index < unlocked;
            if (challengeStartButton != null)
            {
                challengeStartButton.gameObject.SetActive(!comingSoon);
                challengeStartButton.interactable = !comingSoon;
            }

            // Progress column: index 0 is bottom node, index 4 is top crown
            int posInBracket = index % 5;
            for (int i = 0; i < challengeProgressNodes.Length; i++)
            {
                var node = challengeProgressNodes[i];
                if (node == null) continue;
                bool isCrown = i == challengeProgressNodes.Length - 1;
                if (isCrown)
                {
                    if (challengeNodeCrownSprite != null) node.sprite = challengeNodeCrownSprite;
                    node.color = posInBracket == i ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                }
                else
                {
                    bool reached = i <= posInBracket;
                    if (reached && challengeNodeActiveSprite != null) node.sprite = challengeNodeActiveSprite;
                    else if (!reached && challengeNodePendingSprite != null) node.sprite = challengeNodePendingSprite;
                    node.color = Color.white;
                }
            }
        }

        private void RenderBoxerModal(PlayerProgress progress)
        {
            var state = progress.peripheral;
            SelectedBoxer = Mathf.Clamp(SelectedBoxer, 0, BeachPeripheralRules.TrainerCount - 1);

            for (int i = 0; i < BeachPeripheralRules.TrainerCount; i++)
            {
                bool equipped = state.equippedTrainer == i;
                bool owned = state.trainerLevels[i] > 0;
                if (i < boxerCardBackgrounds.Length && boxerCardBackgrounds[i] != null)
                    boxerCardBackgrounds[i].sprite = equipped ? boxerCardEquippedSprite : boxerCardNormalSprite;
                if (i < boxerCardCheckmarks.Length)
                    SetActive(boxerCardCheckmarks[i], equipped);
                if (i < boxerCardNames.Length)
                    SetText(boxerCardNames[i], BeachChineseText.Translate(BeachPeripheralRules.GetTrainerName(i)));
                if (i < boxerCardPortraits.Length && boxerCardPortraits[i] != null)
                {
                    if (i < boxerPortraitSprites.Length && boxerPortraitSprites[i] != null)
                        boxerCardPortraits[i].sprite = boxerPortraitSprites[i];
                    boxerCardPortraits[i].color = owned ? Color.white : new Color(0.68f, 0.72f, 0.8f, 1f);
                }
            }

            if (boxerSelectionFrame != null && SelectedBoxer < boxerCardButtons.Length && boxerCardButtons[SelectedBoxer] != null)
            {
                var cardRect = (RectTransform)boxerCardButtons[SelectedBoxer].transform;
                boxerSelectionFrame.position = cardRect.position;
            }

            if (boxerPreviewImage != null && SelectedBoxer < boxerFullSprites.Length && boxerFullSprites[SelectedBoxer] != null)
                boxerPreviewImage.sprite = boxerFullSprites[SelectedBoxer];

            SetText(boxerPreviewNameText, BeachChineseText.Translate(BeachPeripheralRules.GetTrainerName(SelectedBoxer)));
            SetText(boxerPreviewTypeText, BeachPeripheralRules.GetTrainerArchetype(SelectedBoxer));

            Muscle[] order = { Muscle.Arms, Muscle.Legs, Muscle.Back, Muscle.Chest };
            for (int i = 0; i < boxerBonusTexts.Length && i < order.Length; i++)
            {
                int bonus = BeachPeripheralRules.GetDisplayedModifier(state, SelectedBoxer, order[i]);
                SetText(boxerBonusTexts[i], "+" + bonus.ToString(CultureInfo.InvariantCulture) + "%");
            }

            bool selOwned = state.trainerLevels[SelectedBoxer] > 0;
            bool selEquipped = state.equippedTrainer == SelectedBoxer;
            int cost = BeachPeripheralRules.GetPurchaseCost(SelectedBoxer);
            if (selEquipped)
            {
                SetText(boxerActionLabel, "已出战");
                if (boxerActionButton != null) boxerActionButton.interactable = false;
            }
            else if (selOwned)
            {
                SetText(boxerActionLabel, "选择出战");
                if (boxerActionButton != null) boxerActionButton.interactable = true;
            }
            else if (cost >= 0)
            {
                SetText(boxerActionLabel, $"{cost}星星解锁");
                if (boxerActionButton != null) boxerActionButton.interactable = true;
            }
            else
            {
                SetText(boxerActionLabel, "签到解锁");
                if (boxerActionButton != null) boxerActionButton.interactable = false;
            }
        }

        private void RenderSignInModal(PlayerProgress progress)
        {
            var state = progress.peripheral;
            int nextClaimable = BeachPeripheralRules.GetNextClaimableDay(state);
            if (nextClaimable >= 0 && !BeachPeripheralRules.CanClaim(state, SelectedSignInDay))
                SelectedSignInDay = nextClaimable;
            SelectedSignInDay = Mathf.Clamp(SelectedSignInDay, 0, BeachPeripheralRules.DailyRewardCount - 1);

            string[] dayTitles = { "第一天", "第二天", "第三天", "第四天", "第五天", "第六天", "第七天" };
            string[] rewardTexts = { "拳手体验", "3.00k", "60", "10.00k", "100", "1000k", "豪华大奖" };

            for (int day = 0; day < BeachPeripheralRules.DailyRewardCount; day++)
            {
                bool claimed = (state.claimedDays & (1 << day)) != 0;
                bool canClaim = BeachPeripheralRules.CanClaim(state, day);
                bool locked = !claimed && !canClaim;

                if (day < signInDayTitleTexts.Length) SetText(signInDayTitleTexts[day], dayTitles[day]);
                if (day < signInDayRewardTexts.Length && day < 6) SetText(signInDayRewardTexts[day], rewardTexts[day]);
                if (day < signInDayBackgrounds.Length && signInDayBackgrounds[day] != null && day < 6)
                    signInDayBackgrounds[day].sprite = claimed ? signInCardClaimedSprite : signInCardNormalSprite;
                if (day < signInDayCheckmarks.Length)
                    SetActive(signInDayCheckmarks[day], claimed);
                if (day < signInDayLockOverlays.Length)
                    SetActive(signInDayLockOverlays[day], locked);
            }

            if (signInSelectionFrame != null)
            {
                bool showFrame = SelectedSignInDay < 6 && BeachPeripheralRules.CanClaim(state, SelectedSignInDay);
                signInSelectionFrame.gameObject.SetActive(showFrame);
                if (showFrame && SelectedSignInDay < signInDayButtons.Length && signInDayButtons[SelectedSignInDay] != null)
                    signInSelectionFrame.position = signInDayButtons[SelectedSignInDay].transform.position;
            }

            bool claimable = BeachPeripheralRules.CanClaim(state, SelectedSignInDay);
            if (signInClaimButton != null) signInClaimButton.interactable = claimable;
            if (signInDoubleClaimButton != null) signInDoubleClaimButton.interactable = claimable;
        }

        private void RenderResult(FightController fight, OpponentProfile opponent, int opponentIndex, bool rewarded)
        {
            bool won = fight.Outcome == FightOutcome.Victory;
            if (resultPanelBg != null) resultPanelBg.sprite = won ? victoryPanelSprite : defeatPanelSprite;
            if (resultTitleArt != null) resultTitleArt.sprite = won ? victoryTitleSprite : defeatTitleSprite;
            if (resultPodiumImage != null) resultPodiumImage.sprite = won ? victoryPodiumSprite : defeatPodiumSprite;
            SetActive(resultConfetti, won);

            if (resultStatusBadge != null) resultStatusBadge.sprite = won ? victoryStatusSprite : defeatStatusSprite;

            string oppName = opponent != null ? BeachChineseText.Translate(opponent.name) : "";
            SetText(resultOpponentLabel, won ? $"击败对手：{oppName}" : $"对手：{oppName}");

            if (resultOpponentAvatar != null && catalog != null && catalog.Portraits.Length > 0)
                resultOpponentAvatar.sprite = catalog.Portraits[Mathf.Clamp(opponentIndex, 0, catalog.Portraits.Length - 1)];

            int coins = won && rewarded && opponent != null ? opponent.coins : 0;
            int stars = won && rewarded && opponent != null ? opponent.stars : 0;
            SetActive(resultRewardSection, true);
            SetText(resultCoinValueText, $"x{coins}");
            SetText(resultStarValueText, $"x{stars}");

            SetActive(resultContinueButton != null ? resultContinueButton.gameObject : null, won);
            if (resultRestartImage != null)
                resultRestartImage.sprite = won ? blueBtnSprite : yellowBtnSprite;
            if (resultRestartRect != null)
                resultRestartRect.anchoredPosition = won ? victoryRestartPosition : defeatRestartPosition;
            SetText(resultRestartLabel, won ? "重新开始" : "重新游戏");
            if (resultRestartLabel != null)
            {
                Color outlineColor = won
                    ? new Color32(0x03, 0x42, 0xA1, 0xFF)
                    : new Color32(0x6D, 0x27, 0x01, 0xFF);
                foreach (var outline in resultRestartLabel.GetComponents<Outline>())
                    outline.effectColor = outlineColor;
            }
        }

        public void ResetTipBanner()
        {
            tipBannerElapsed = 0f;
            tipBannerDismissed = false;
            EnsureTipBannerGroup();
            if (tipBannerGroup != null) tipBannerGroup.alpha = 1f;
        }

        public void AdvanceTipBanner(float deltaTime)
        {
            UpdateTipBanner(true, deltaTime);
        }

        private void UpdateTipBanner(bool canShow, float deltaTime)
        {
            if (tipBannerRoot == null) return;
            if (tipBannerDismissed)
            {
                SetActive(tipBannerRoot, false);
                return;
            }
            if (!canShow)
            {
                SetActive(tipBannerRoot, false);
                return;
            }

            EnsureTipBannerGroup();
            if (deltaTime > 0f) tipBannerElapsed += deltaTime;

            float totalDuration = TipBannerHoldSeconds + TipBannerFadeSeconds;
            if (tipBannerElapsed >= totalDuration)
            {
                tipBannerDismissed = true;
                if (tipBannerGroup != null) tipBannerGroup.alpha = 0f;
                SetActive(tipBannerRoot, false);
                return;
            }

            SetActive(tipBannerRoot, true);
            if (tipBannerGroup != null)
            {
                tipBannerGroup.alpha = tipBannerElapsed <= TipBannerHoldSeconds
                    ? 1f
                    : Mathf.Clamp01(1f - (tipBannerElapsed - TipBannerHoldSeconds) / TipBannerFadeSeconds);
            }
        }

        private void EnsureTipBannerGroup()
        {
            if (tipBannerRoot == null) return;
            if (tipBannerGroup == null)
            {
                tipBannerGroup = tipBannerRoot.GetComponent<CanvasGroup>();
                if (tipBannerGroup == null)
                    tipBannerGroup = tipBannerRoot.AddComponent<CanvasGroup>();
            }
            tipBannerGroup.blocksRaycasts = false;
            tipBannerGroup.interactable = false;
        }

        private void UpdateSafeArea()
        {
            if (safeRoot == null) return;
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (area == lastSafeArea && size == lastScreenSize) return;
            lastSafeArea = area;
            lastScreenSize = size;
            if (size.x <= 0 || size.y <= 0) return;
            safeRoot.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safeRoot.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        private static void Hook(Button button, Action action)
        {
            if (button == null) return;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject == button.gameObject)
                    es.SetSelectedGameObject(null);
                BeachCommonUI.PlayButtonClick();
                action?.Invoke();
            });
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        private static void SetText(Text label, string value)
        {
            if (label != null && label.text != value) label.text = value;
        }
    }
}
