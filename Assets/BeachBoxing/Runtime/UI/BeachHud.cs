using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing
{
    [Serializable]
    internal sealed class BeachHud
    {
        private static readonly Color Panel = new Color(0.06f, 0.15f, 0.25f, 0.94f);
        private static readonly Color Green = new Color(0.13f, 0.56f, 0.26f);
        private static readonly Color Blue = new Color(0.12f, 0.37f, 0.62f);
        private static readonly Color Red = new Color(0.73f, 0.20f, 0.17f);
        [SerializeField] private RectTransform safe;
        [SerializeField] private GameObject move, attack, block, interact, workout, picker, result, combat, menu;
        [SerializeField] private Button previous, next, rematch;
        private static readonly string[] MuscleSprites = { "aRmwad 1", "aRmwad", "aRmwad 3", "aRmwad 2" };
        [SerializeField] private GameObject stats;
        [SerializeField] private Text[] statsValues = new Text[6];
        [SerializeField] private Sprite[] muscleIcons = new Sprite[4];
        [SerializeField] private Text prompt, workoutTitle, selection, resultText, countdown, playerValues, enemyValues, gainText, staminaHint;
        [SerializeField] private Image portrait, workoutIcon, gainIcon, hurtOverlay, resultPortrait;
        [SerializeField] private GameObject victoryTitle, victoryCard;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Button defeatHomeButton;
        [SerializeField] private Text returnLabel;
        [SerializeField] private GameObject goldReward, starReward;
        [SerializeField] private Text goldRewardValue, starRewardValue;
        [SerializeField] private Image workoutFill, playerHealth, playerStamina, playerGuard, enemyHealth, enemyStamina, enemyGuard;
        [SerializeField] private Font font;
        [SerializeField] private GameObject joystickGraphic, tutorialFinger, defeatedCaption;
        [SerializeField] private GameObject playerWorldBars, enemyWorldBars, trainingTitle;
        [SerializeField] private Text playerHealthValue, playerStaminaValue, enemyHealthValue, enemyStaminaValue;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseResume, pauseHome;
        [SerializeField] private RectTransform opponentCardArt, opponentProgressArrow;
        [SerializeField] private Text[] opponentStats = new Text[4];
        [SerializeField] private Text opponentCoinValue, opponentStarValue;
        [SerializeField] private Image[] opponentProgressNodes;
        [SerializeField] private GameObject desktopWorkoutLayout, mobileWorkoutLayout;
        [SerializeField] private TouchControl desktopBoost, mobileBoost;
        [SerializeField] private Button mobileExit;
        [SerializeField] private Image mobileFill;
        private BeachInput boundInput;
        private Rect lastSafe;
        private Vector2Int lastScreen;
        private int lastPortrait = -1;
        private float gainTime;
        private bool nativeChrome;
        private NativeOpponentCatalog opponentCatalog;
        public bool TouchLayout { get; private set; }

        public void SetNativeChrome(bool enabled)
        {
            nativeChrome = enabled;
            if (!enabled) return;
            if (stats) stats.SetActive(false);
            if (menu) menu.SetActive(false);
            if (interact) interact.SetActive(false);
            if (prompt) prompt.gameObject.SetActive(false);
        }
        private bool nativeOpponentMenu;
        public void SetNativeOpponentMenu(bool enabled)
        {
            nativeOpponentMenu = enabled;
            if (enabled && picker) picker.SetActive(false);
        }
        private bool nativeTraining;
        private bool nativeCombatFeedback;
        private bool nativeControls;
        public void SetNativeControls(bool enabled)
        {
            nativeControls = enabled;
            if (!enabled) return;
            if (move) move.SetActive(false);
            if (attack) attack.SetActive(false);
            if (block) block.SetActive(false);
            if (joystickGraphic) joystickGraphic.SetActive(false);
            if (tutorialFinger) tutorialFinger.SetActive(false);
        }
        public void SetNativeCombatFeedback(bool enabled) => nativeCombatFeedback = enabled;
        public void SetNativeTraining(bool enabled)
        {
            nativeTraining = enabled;
            if (!enabled) return;
            if (workout) workout.SetActive(false);
            if (tutorialFinger) tutorialFinger.SetActive(false);
            if (boundInput != null) boundInput.Boost = null;
        }

        public void BindOpponentCatalog(NativeOpponentCatalog catalog)
        {
            opponentCatalog = catalog;
            lastPortrait = -1;
        }

        public void BlockPeripheralInput()
        {
            if (move) move.SetActive(false);
            if (interact) interact.SetActive(false);
            if (joystickGraphic) joystickGraphic.SetActive(false);
            if (tutorialFinger) tutorialFinger.SetActive(false);
        }

        #if UNITY_EDITOR
        public BeachHud(Transform root, BeachInput input, Action use, Action exit, Action<int> select, Action fight, Action home, Action menuAction)
        {
            font = OriginalArt.Get<Font>("UI/LiberationSans");
            if (!EventSystem.current)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetParent(root, false);
            }
            var ui = new GameObject("Beach UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui.transform.SetParent(root, false);
            ui.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = ui.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(432, 768);
            scaler.matchWidthOrHeight = 0.45f;
            hurtOverlay = Fill(Box(ui.transform, "Damage overlay", 0, 0, 1, 1), Color.clear);
            Skin(hurtOverlay, "New Project (67)", Color.clear);
            hurtOverlay.raycastTarget = false;
            safe = Box(ui.transform, "Safe area", 0, 0, 1, 1);
            var moveArea = Box(safe, "Move", 0, 0, 0.53f, 0.58f);
            move = moveArea.gameObject;
            Fill(moveArea, new Color(0, 0, 0, 0.001f));
            input.Move = move.AddComponent<TouchControl>();
            input.Move.Joystick = true;
            Icon(moveArea, "AllAxis_Outline", 0.18f, 0.13f, 0.55f, 0.40f);

            attack = Hold(safe, "PUNCH", Red, 0.73f, 0.06f, 0.96f, 0.21f, out input.Attack, "PunchIcon");
            block = Hold(safe, "BLOCK", Blue, 0.77f, 0.23f, 0.97f, 0.36f, out input.Block, "ShieldIcon");
            stats = Box(safe, "Player stats", 0.03f, 0.78f, 0.69f, 0.98f).gameObject;
            for (int index = 0; index < statsValues.Length; index++)
            {
                int column = index % 2;
                int row = index / 2;
                var stat = Box(stats.transform, "Stat " + index, 0.5f * column, (2 - row) / 3f,
                    0.5f * (column + 1), (3 - row) / 3f);
                string icon = index < 4 ? MuscleSprites[index] : index == 4 ? "Coin" : "Gem";
                var statIcon = Icon(stat, icon, 0.03f, 0.08f, 0.37f, 0.92f);
                if (index < 4) muscleIcons[index] = statIcon.sprite;
                statsValues[index] = Label(stat, "", 20, 0.40f, 0, 1, 1, TextAnchor.MiddleLeft);
            }
            menu = ButtonAt(safe, "MENU", Panel, 0.78f, 0.89f, 0.97f, 0.975f, menuAction, "Menu Button").gameObject;
            interact = ButtonAt(safe, "INTERACT", Green, 0.31f, 0.23f, 0.69f, 0.34f, use).gameObject;
            prompt = Label(safe, "Approach equipment or the boxing ring", 17, 0.10f, 0.36f, 0.90f, 0.43f);

            workout = PanelAt(safe, "Workout", 0.07f, 0.12f, 0.93f, 0.34f);
            workoutIcon = Icon(workout.transform, "aRmwad", 0.08f, 0.70f, 0.23f, 0.99f);
            workoutTitle = Label(workout.transform, "", 22, 0.25f, 0.71f, 0.92f, 0.98f, TextAnchor.MiddleLeft);
            workoutFill = Bar(workout.transform, "Repetition", new Color(1, 0.74f, 0.16f), 0.06f, 0.55f, 0.94f, 0.66f, fillSprite: "CountBG FillAmount");
            gainIcon = Icon(safe, "aRmwad", 0.35f, 0.53f, 0.49f, 0.63f);
            gainText = Label(safe, "", 32, 0.51f, 0.53f, 0.69f, 0.63f, TextAnchor.MiddleLeft);
            gainText.color = new Color(1, 0.83f, 0.25f);
            Hold(workout.transform, "WORKOUT FASTER", Green, 0.06f, 0.1f, 0.61f, 0.46f, out input.Boost);
            exitButton = ButtonAt(workout.transform, "EXIT", Red, 0.66f, 0.1f, 0.94f, 0.46f, exit);

            picker = PanelAt(safe, "Pick opponent", 0.04f, 0.23f, 0.96f, 0.75f);
            Skin(picker.GetComponent<Image>(), "BG 1", Color.white);
            Label(picker.transform, "PICK OPPONENT", 29, 0.03f, 0.8f, 0.81f, 0.98f);
            closeButton = ButtonAt(picker.transform, "X", Red, 0.83f, 0.82f, 0.97f, 0.97f, exit);
            selection = Label(picker.transform, "", 23, 0.18f, 0.27f, 0.82f, 0.45f);
            var portraitPanel = Box(picker.transform, "Portrait panel", 0.22f, 0.44f, 0.78f, 0.80f);
            var portraitBackground = Fill(portraitPanel, Color.white);
            Skin(portraitBackground, "BG 2", Color.white);
            portraitBackground.raycastTarget = false;
            portrait = Icon(portraitPanel, "Fighter Icon 0", 0.05f, 0.04f, 0.95f, 0.96f);
            previous = ButtonAt(picker.transform, "<", Blue, 0.035f, 0.45f, 0.18f, 0.65f, () => select(-1));
            next = ButtonAt(picker.transform, ">", Blue, 0.82f, 0.45f, 0.965f, 0.65f, () => select(1));
            fightButton = ButtonAt(picker.transform, "FIGHT!", Green, 0.28f, 0.09f, 0.72f, 0.26f, fight);

            combat = Box(safe, "Fight status", 0.025f, 0.69f, 0.975f, 0.89f).gameObject;
            playerValues = Label(combat.transform, "", 19, 0.04f, 0.70f, 0.48f, 0.98f, TextAnchor.MiddleLeft);
            enemyValues = Label(combat.transform, "", 19, 0.53f, 0.70f, 0.97f, 0.98f, TextAnchor.MiddleRight);
            playerHealth = Bar(combat.transform, "Player health", Red, 0.058f, 0.57f, 0.47f, 0.66f);
            enemyHealth = Bar(combat.transform, "Opponent health", Red, 0.53f, 0.57f, 0.942f, 0.66f, fromRight: true);
            Icon(combat.transform, "Heart", 0, 0.565f, 0.055f, 0.675f);
            Icon(combat.transform, "Heart", 0.945f, 0.565f, 1, 0.675f);
            Icon(combat.transform, "EnergyIcon", 0, 0.435f, 0.055f, 0.545f);
            Icon(combat.transform, "EnergyIcon", 0.945f, 0.435f, 1, 0.545f);
            var yellow = new Color(1f, 0.78f, 0.1f);
            playerStamina = Bar(combat.transform, "Player stamina", yellow, 0.058f, 0.44f, 0.47f, 0.53f);
            enemyStamina = Bar(combat.transform, "Opponent stamina", yellow, 0.53f, 0.44f, 0.942f, 0.53f, fromRight: true);
            playerGuard = Bar(combat.transform, "Player guard", Blue, 0.058f, 0.31f, 0.47f, 0.40f);
            enemyGuard = Bar(combat.transform, "Opponent guard", Blue, 0.53f, 0.31f, 0.942f, 0.40f, fromRight: true);
            countdown = Label(safe, "", 86, 0.28f, 0.43f, 0.72f, 0.68f);
            staminaHint = Label(safe, "BLOCK TO RECOVER STAMINA", 19, 0.10f, 0.38f, 0.90f, 0.46f);
            staminaHint.color = yellow;

            result = PanelAt(safe, "Result", 0.06f, 0.28f, 0.94f, 0.74f);
            Skin(result.GetComponent<Image>(), "BG 1", Color.white);
            victoryTitle = Box(result.transform, "Victory title", 0.06f, 0.72f, 0.94f, 1.08f).gameObject;
            Icon(victoryTitle.transform, "VictoryScreen", 0.32f, 0, 0.68f, 1);
            Label(victoryTitle.transform, "VICTORY!", 38, 0, 0, 1, 1);
            victoryCard = Box(result.transform, "Defeated fighter", 0.06f, 0.23f, 0.94f, 0.70f).gameObject;
            Skin(Fill((RectTransform)victoryCard.transform, Color.white), "BG 3", Color.white);
            resultPortrait = Icon(victoryCard.transform, "Fighter Icon 0", 0.04f, 0.13f, 0.29f, 0.87f);
            resultText = Label(result.transform, "", 31, 0.35f, 0.51f, 0.89f, 0.69f, TextAnchor.MiddleLeft);
            goldReward = Box(victoryCard.transform, "Gold reward", 0.35f, 0.10f, 0.61f, 0.43f).gameObject;
            starReward = Box(victoryCard.transform, "Star reward", 0.65f, 0.10f, 0.91f, 0.43f).gameObject;
            Icon(goldReward.transform, "Coin", 0, 0, 0.42f, 1);
            Icon(starReward.transform, "Gem", 0, 0, 0.42f, 1);
            goldRewardValue = Label(goldReward.transform, "", 24, 0.43f, 0, 1, 1, TextAnchor.MiddleLeft);
            starRewardValue = Label(starReward.transform, "", 24, 0.43f, 0, 1, 1, TextAnchor.MiddleLeft);
            rematch = ButtonAt(result.transform, "TRY AGAIN", Blue, 0.22f, 0.26f, 0.78f, 0.44f, fight);
            homeButton = ButtonAt(result.transform, "RETURN HOME", Green, 0.22f, 0.05f, 0.78f, 0.23f, home);
            returnLabel = homeButton.GetComponentInChildren<Text>();
            portraits = new[] { OriginalArt.Get<Sprite>("UI/Fighter Icon 0"), OriginalArt.Get<Sprite>("UI/Fighter Icon 1") };
        }

        #endif

        [SerializeField] private Sprite[] portraits;
        [SerializeField] private Button exitButton, closeButton, fightButton, homeButton;

        public void Bind(BeachInput input, Action use, Action exit, Action<int> select, Action fight, Action home, Action menuAction)
        {
            boundInput = input;
            input.Move = move.GetComponent<TouchControl>();
            input.Move.Joystick = true;
            input.Attack = attack.GetComponent<TouchControl>();
            input.Block = block.GetComponent<TouchControl>();
            input.Boost = desktopBoost ? desktopBoost : workout.GetComponentInChildren<TouchControl>(true);
            BindButton(menu.GetComponent<Button>(), menuAction);
            BindButton(interact.GetComponent<Button>(), use);
            BindButton(exitButton, exit);
            if (mobileExit) BindButton(mobileExit, exit);
            BindButton(closeButton, exit);
            BindButton(previous, () => select(-1));
            BindButton(next, () => select(1));
            BindButton(fightButton, fight);
            BindButton(rematch, fight);
            BindButton(homeButton, home);
            if (defeatHomeButton) BindButton(defeatHomeButton, home);
        }

        private static void BindButton(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        public void BindPause(Action resume, Action home)
        {
            if (pauseResume) BindButton(pauseResume, resume);
            if (pauseHome) BindButton(pauseHome, home);
        }

        public void ShowPause(bool visible)
        {
            if (pausePanel) pausePanel.SetActive(visible);
        }

        public void TrainingGain(Muscle muscle, int amount)
        {
            if (!gainIcon || !gainText || muscleIcons == null) return;
            int index = (int)muscle;
            if (index < 0 || index >= muscleIcons.Length) return;
            gainTime = Time.unscaledTime + 0.7f;
            gainIcon.sprite = muscleIcons[index];
            gainText.text = $"+{amount}";
        }

        public void Render(BeachPhase phase, PlayerProgress progress, TrainingStation station, float repetition, bool nearby,
            OpponentProfile opponent, int opponentIndex, int unlocked, FightController fight, bool rewarded)
        {
            Fit();
            bool trainingArea = phase == BeachPhase.Training;
            bool exercising = trainingArea && station != null;
            bool roaming = trainingArea && !exercising;
            bool choosing = phase == BeachPhase.OpponentSelect;
            bool fighting = phase == BeachPhase.Fight;
            bool ended = phase == BeachPhase.Result;
            bool canFight = fighting && fight.Countdown <= 0 && fight.Outcome == FightOutcome.Pending;
            move.SetActive(!nativeControls && (roaming || canFight));
            interact.SetActive(!nativeChrome && roaming && nearby);
            prompt.gameObject.SetActive(!nativeChrome && roaming && !nearby && !string.IsNullOrEmpty(prompt.text));
            bool touchSupported = false;
            #if !UNITY_EDITOR
            touchSupported = Input.touchSupported;
            #endif
            bool portraitScreen = Screen.height > Screen.width;
            bool touchMode = boundInput != null ? boundInput.UseTouchLayout(Application.isMobilePlatform,
                touchSupported, portraitScreen, Input.touchCount > 0) : Application.isMobilePlatform || portraitScreen;
            TouchLayout = touchMode;
            if (desktopWorkoutLayout) desktopWorkoutLayout.SetActive(!touchMode);
            if (mobileWorkoutLayout) mobileWorkoutLayout.SetActive(touchMode);
            var activeBoost = touchMode && mobileBoost ? mobileBoost : desktopBoost;
            if (!nativeTraining && boundInput != null && activeBoost && boundInput.Boost != activeBoost)
            {
                if (boundInput.Boost) boundInput.Boost.Release();
                activeBoost.Release();
                boundInput.Boost = activeBoost;
            }
            if (joystickGraphic) joystickGraphic.SetActive(!nativeControls && touchMode && roaming);
            if (tutorialFinger) tutorialFinger.SetActive(!nativeControls && !nativeTraining && roaming && touchMode && progress.unlockedOpponent == 0 && Input.touchCount == 0);
            menu.SetActive((!nativeChrome || (!trainingArea && !choosing)) && !exercising && (!fighting || fight.Countdown <= 0));
            stats.SetActive(!nativeChrome && (trainingArea || choosing));
            workout.SetActive(exercising && !nativeTraining);
            bool showGain = exercising && Time.unscaledTime < gainTime;
            gainText.gameObject.SetActive(showGain);
            gainIcon.gameObject.SetActive(showGain);
            float hurt = fighting && !fight.Player.LastImpactWasBlocked ? Mathf.Clamp01(fight.PlayerFlash / 0.16f) : 0;
            float weak = fighting && (fight.Player.Health <= 15 || fight.Player.Stamina <= 15) ? 0.25f : 0;
            bool healing = fighting && fight.Player.Blocking &&
                (fight.Player.Health < fight.Player.MaxHealth || fight.Player.Stamina < fight.Player.MaxStamina);
            hurtOverlay.color = healing && hurt <= 0 ? new Color(0.08f, 1, 0.2f, 0.22f) :
                new Color(1, 0.13f, 0.10f, Mathf.Max(weak, hurt * 0.75f));
            hurtOverlay.enabled = !nativeCombatFeedback && (hurt > 0 || weak > 0 || healing);
            picker.SetActive(choosing && !nativeOpponentMenu);
            combat.SetActive(fighting);
            if (playerWorldBars) playerWorldBars.SetActive(canFight);
            if (enemyWorldBars) enemyWorldBars.SetActive(canFight);
            if (trainingTitle) trainingTitle.SetActive(trainingArea && !exercising);
            attack.SetActive(!nativeControls && (canFight || ended));
            block.SetActive(!nativeControls && (canFight || ended));
            staminaHint.gameObject.SetActive(!nativeCombatFeedback && canFight && fight.Player.Stamina <= 0);
            countdown.gameObject.SetActive(fighting && fight.Countdown > 0);
            bool lost = ended && fight.Outcome == FightOutcome.Defeat;
            result.SetActive(ended && (!defeatPanel || !lost));
            if (defeatPanel) defeatPanel.SetActive(lost);

            if (trainingArea || choosing)
            {
                statsValues[0].text = BeachProgressionRules.FormatNumber(progress.chest);
                statsValues[1].text = BeachProgressionRules.FormatNumber(progress.arms);
                statsValues[2].text = BeachProgressionRules.FormatNumber(progress.back);
                statsValues[3].text = BeachProgressionRules.FormatNumber(progress.legs);
                statsValues[4].text = progress.coins.ToString();
                statsValues[5].text = progress.stars.ToString();
            }
            if (exercising)
            {
                workoutTitle.text = touchMode ? "" : "(Press E)";
                SetBar(workoutFill, repetition);
                if (mobileFill) SetBar(mobileFill, repetition);
            }
            if (choosing)
            {
                bool comingSoon = opponentIndex >= portraits.Length;
                selection.text = comingSoon ? "COMING SOON" : opponent.name;
                if (opponentCardArt)
                {
                    float scale = Mathf.Min(1f, (safe.rect.width - 30f) / 600f, (safe.rect.height - 70f) / 480f);
                    opponentCardArt.localScale = Vector3.one * Mathf.Max(0.1f, scale);
                }
                int[] attributes = { opponent.chest, opponent.arms, opponent.back, opponent.legs };
                for (int i = 0; i < opponentStats.Length; i++)
                    if (opponentStats[i]) opponentStats[i].text = comingSoon ? "0" : BeachProgressionRules.FormatNumber(attributes[i]);
                if (opponentCoinValue) opponentCoinValue.text = comingSoon ? "0" : opponent.coins.ToString();
                if (opponentStarValue) opponentStarValue.text = comingSoon ? "0" : opponent.stars.ToString();
                if (opponentProgressArrow) opponentProgressArrow.anchoredPosition = new Vector2(465, -357 + opponentIndex % 5 * 65);
                if (opponentProgressNodes != null)
                    for (int node = 0; node < opponentProgressNodes.Length; node++)
                        if (opponentProgressNodes[node]) opponentProgressNodes[node].color = node == opponentIndex % 5 ? Color.white : new Color(1, 1, 1, 0.3f);
                if (opponentIndex != lastPortrait)
                {
                    lastPortrait = opponentIndex;
                    portrait.sprite = comingSoon && opponentCatalog ? opponentCatalog.ComingSoonPortrait : portraits[Mathf.Min(opponentIndex, portraits.Length - 1)];
                }
                previous.interactable = opponentIndex > 0;
                next.interactable = opponentIndex < unlocked;
                previous.gameObject.SetActive(true);
                next.gameObject.SetActive(true);
                fightButton.gameObject.SetActive(!comingSoon);
                fightButton.interactable = !comingSoon;
            }
            if (fighting)
            {
                countdown.text = Mathf.CeilToInt(fight.Countdown).ToString();
                Values("YOU", fight.Player, playerValues, playerHealth, playerStamina, playerGuard, playerWorldBars);
                Values(opponent.name.ToUpperInvariant(), fight.Enemy, enemyValues, enemyHealth, enemyStamina, enemyGuard, enemyWorldBars);
                if (playerHealthValue) playerHealthValue.text = Mathf.CeilToInt(fight.Player.Health).ToString();
                if (playerStaminaValue) playerStaminaValue.text = Mathf.CeilToInt(fight.Player.Stamina).ToString();
                if (enemyHealthValue) enemyHealthValue.text = Mathf.CeilToInt(fight.Enemy.Health).ToString();
                if (enemyStaminaValue) enemyStaminaValue.text = Mathf.CeilToInt(fight.Enemy.Stamina).ToString();
            }
            if (ended)
            {
                bool won = fight.Outcome == FightOutcome.Victory;
                victoryTitle.SetActive(won);
                victoryCard.SetActive(won);
                if (defeatedCaption) defeatedCaption.SetActive(won);
                resultPortrait.sprite = portraits[opponentIndex];
                resultText.rectTransform.anchorMin = won ? new Vector2(0.35f, 0.51f) : new Vector2(0.06f, 0.56f);
                resultText.rectTransform.anchorMax = won ? new Vector2(0.89f, 0.69f) : new Vector2(0.94f, 0.90f);
                resultText.alignment = won ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
                resultText.text = won ? (rewarded ? opponent.name : $"{opponent.name}\nReward claimed") : "DEFEAT";
                returnLabel.text = won ? "CONTINUE" : "RETURN HOME";
                returnLabel.fontSize = won ? 29 : 22;
                goldReward.SetActive(won && rewarded);
                starReward.SetActive(won && rewarded);
                if (won && rewarded)
                {
                    goldRewardValue.text = $"x{opponent.coins}";
                    starRewardValue.text = $"x{opponent.stars}";
                }
                rematch.gameObject.SetActive(!won);
            }
        }

        private void Fit()
        {
            var area = Screen.safeArea;
            var dimensions = new Vector2Int(Screen.width, Screen.height);
            if (dimensions == lastScreen && area == lastSafe) return;
            safe.anchorMin = new Vector2(area.xMin / dimensions.x, area.yMin / dimensions.y);
            safe.anchorMax = new Vector2(area.xMax / dimensions.x, area.yMax / dimensions.y);
            lastSafe = area;
            lastScreen = dimensions;
        }

        private static void Values(string name, FighterState state, Text text, Image health, Image stamina, Image guard, bool worldBars = false)
        {
            text.text = worldBars ? Mathf.CeilToInt(state.Guard).ToString() : $"{name}  {Mathf.CeilToInt(state.Health)} / {Mathf.CeilToInt(state.Stamina)} / {Mathf.CeilToInt(state.Guard)}";
            SetBar(health, state.Health / state.MaxHealth);
            SetBar(stamina, state.Stamina / state.MaxStamina);
            SetBar(guard, state.Guard / state.MaxGuard);
            guard.color = state.Blocking ? Color.cyan : worldBars ? new Color(1, 1, 1, 0.38f) : Blue;
        }

        private GameObject Hold(Transform root, string name, Color color, float x, float y, float right, float top, out TouchControl control,
            string icon = null)
        {
            var area = Box(root, name, x, y, right, top);
            Skin(Fill(area, color), icon != null ? "Circle" : "Join Room BTN 1", icon != null ? new Color(1, 1, 1, 0.3f) : Color.white);
            if (icon != null)
            {
                var graphic = Icon(area, icon, 0.1f, 0.1f, 0.9f, 0.9f);
                if (icon == "ShieldIcon") graphic.color = new Color(0.25f, 0.76f, 0.98f);
            }
            else Label(area, name, 22, 0.05f, 0.05f, 0.95f, 0.95f);
            control = area.gameObject.AddComponent<TouchControl>();
            return area.gameObject;
        }

        private Button ButtonAt(Transform root, string name, Color color, float x, float y, float right, float top, Action action,
            string icon = null)
        {
            var area = Box(root, name, x, y, right, top);
            var image = Fill(area, color);
            Skin(image, icon ?? (color == Red ? "Join Room BTN 3" : color == Blue ? "Join Room BTN 2" : "Join Room BTN 1"), Color.white);
            var button = area.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (icon == null) Label(area, name, 22, 0.05f, 0.05f, 0.95f, 0.95f);
            button.onClick.AddListener(() => action());
            return button;
        }

        private static GameObject PanelAt(Transform root, string name, float x, float y, float right, float top)
        {
            var area = Box(root, name, x, y, right, top);
            Skin(Fill(area, Panel), "BG", Color.white);
            return area.gameObject;
        }

        private static Image Bar(Transform root, string name, Color color, float x, float y, float right, float top,
            bool fromRight = false, string fillSprite = "CountBG Full")
        {
            var bar = Box(root, name, x, y, right, top);
            var background = Fill(bar, Panel);
            Skin(background, "CountBG (4)", new Color(0.25f, 0.25f, 0.25f));
            background.raycastTarget = false;
            var fill = Fill(Box(bar, "Fill", 0.025f, 0.1f, 0.975f, 0.9f), color);
            Skin(fill, fillSprite, color);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = fromRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            fill.raycastTarget = false;
            return fill;
        }

        private static void SetBar(Image fill, float fraction) => fill.fillAmount = Mathf.Clamp01(fraction);

        private Text Label(Transform root, string value, int size, float x, float y, float right, float top, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var area = Box(root, "Label", x, y, right, top);
            var label = area.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = size;
            label.alignment = alignment;
            label.text = value;
            label.raycastTarget = false;
            var shadow = area.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -2);
            return label;
        }

        private static Image Fill(RectTransform area, Color color)
        {
            var fill = area.gameObject.AddComponent<Image>();
            fill.color = color;
            return fill;
        }

        private static void Skin(Image image, string name, Color tint)
        {
            var sprite = OriginalArt.Get<Sprite>("UI/" + name);
            if (!sprite) return;
            image.sprite = sprite;
            image.color = tint;
        }

        private static Image Icon(Transform parent, string name, float x, float y, float right, float top)
        {
            var icon = Fill(Box(parent, "Icon", x, y, right, top), Color.white);
            Skin(icon, name, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private static RectTransform Box(Transform root, string name, float x, float y, float right, float top)
        {
            var area = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(root, false);
            area.anchorMin = new Vector2(x, y);
            area.anchorMax = new Vector2(right, top);
            area.offsetMin = area.offsetMax = Vector2.zero;
            return area;
        }
    }
}
