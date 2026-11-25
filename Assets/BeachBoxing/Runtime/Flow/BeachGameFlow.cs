using UnityCore;
using UnityEngine;

namespace BeachBoxing
{
    public enum BeachPhase { Training, OpponentSelect, Fight, Result }

    public sealed class BeachGameFlow : MonoBehaviour
    {
        private static readonly Vector3 RingOrigin = new Vector3(0, 0, 25);
        private static readonly Vector3 Entrance = new Vector3(0, 0, 7);
        private readonly TrainingStation[] stations =
        {
            new TrainingStation(Muscle.Arms, new Vector3(3, 0, 1)),
            new TrainingStation(Muscle.Chest, new Vector3(-3, 0, 1)),
            new TrainingStation(Muscle.Back, new Vector3(-3, 0, 5)),
            new TrainingStation(Muscle.Legs, new Vector3(3, 0, 5))
        };
        private OpponentProfile[] opponents = OpponentProfile.RookieRoster();
        private readonly TrainingCycle workout = new TrainingCycle();
        private readonly BeachInput input = new BeachInput();
        private ProgressStore store;
        private PlayerProgress progress;
        [SerializeField] private BeachHud hud;
        [SerializeField] private BeachStage stage;
        [SerializeField] private BeachGameUi gameUi;
        [SerializeField] private BeachPresentationView presentation = new BeachPresentationView();
        private BeachCommonUI commonUi;
        private float workoutExitCooldown;
        private readonly BeachPresentationTimeline presentationTimeline = new BeachPresentationTimeline();
        private System.Action pendingTransition;
        private TrainingStation activeStation, nearestStation;
        private FightController fight;
        private BeachNativeCombatFeedback combatFeedback;
        private BeachNativeMobileControls nativeControls;
        private BeachNativeNpcManager nativeNpcs;
        private bool phone;
        private BeachPhase phase;
        private InputFrame latest;
        private bool queuedAttack, byRing, focused = true, paused, returning;
        private bool rewarded;
        private bool menuPaused;
        private float resumeTimeScale = 1;
        private int opponentIndex;
        private BeachPeripheralView peripherals;
        private bool peripheralWasOpen;
        private BeachTrainerFollowers trainers;
        private BeachAutoTraining autoTraining;
        private BeachNativeSettingsView settings;
        private BeachNativeTrainingView nativeTraining;
        private BeachNativeTutorialView tutorial;
        private BeachNativeOpponentView nativeOpponents;
        private BeachNativeTrainingFeedback trainingFeedback;
        private BeachNativeCameraFeedback cameraFeedback;
        private bool CommonSettingsOpen => commonUi != null && commonUi.Settings != null && commonUi.Settings.IsOpen;
        private bool CanAcceptGameplayAction => focused && !paused && !menuPaused && !returning &&
            pendingTransition == null && !CommonSettingsOpen &&
            !(gameUi && gameUi.IsModalOpen) &&
            !(peripherals && peripherals.IsOpen) && !(settings && settings.IsOpen) &&
            !(nativeOpponents && nativeOpponents.IsClosing);
        private OpponentProfile SelectedOpponent => opponents[Mathf.Clamp(opponentIndex, 0, opponents.Length - 1)];

        #if UNITY_EDITOR
        public void PreviewSceneLighting()
        {
            if (stage != null) stage.PreviewSceneLighting();
        }

        [ContextMenu("Create editable scene objects (empty scene only)")]
        public void BakeScene()
        {
            if (transform.childCount > 0) throw new System.InvalidOperationException("Scene already populated. Edit existing objects directly.");
            stage = new BeachStage(transform, stations, Entrance, RingOrigin);
            hud = new BeachHud(transform, input, Interact, StopTraining, ChangeOpponent, BeginFight, Home, ReturnToMenu);
            stage.EnterBeach();
            stage.FrameBeach(false);
            hud.Render(BeachPhase.Training, new PlayerProgress(), null, 0, false, opponents[0], 0, 0, null, false);
        }
        #endif

        private void Start()
        {
            store = new ProgressStore(CoreRoot.GetOrCreate().Storage);
            progress = store.Load();
            if (gameUi == null) gameUi = GetComponentInChildren<BeachGameUi>(true);
            if (stage == null || (hud == null && gameUi == null))
                throw new System.InvalidOperationException("Bake the BeachBoxing scene before playing.");
            commonUi = BeachCommonUI.Ensure();
            if (commonUi != null && commonUi.Settings != null)
            {
                commonUi.Settings.AudioChanged += OnCommonAudioChanged;
                commonUi.Settings.Closed += SettingsClosed;
            }
            stage.Initialize();
            var opponentCatalog = GetComponent<NativeOpponentCatalog>();
            if (opponentCatalog && opponentCatalog.Profiles.Length == NativeOpponentCatalog.PlayableCount)
                opponents = opponentCatalog.Profiles;
            if (gameUi == null && hud != null) hud.BindOpponentCatalog(opponentCatalog);
            peripherals = GetComponent<BeachPeripheralView>();
            trainers = GetComponent<BeachTrainerFollowers>();
            nativeNpcs = GetComponent<BeachNativeNpcManager>();
            autoTraining = GetComponent<BeachAutoTraining>();
            if (!progress.generatedName)
            {
                if (string.IsNullOrWhiteSpace(progress.playerName))
                {
                    var names = GetComponent<BeachPlayerNames>();
                    progress.playerName = names ? names.Generate() : "Player";
                }
                progress.generatedName = true;
            }
            BeachPeripheralRules.RefreshEquippedModifiers(progress.peripheral);
            BeachPeripheralRules.AdvanceDailyDate(progress.peripheral, System.DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            store.Save(progress);
            if (gameUi == null && peripherals && peripherals.IsBound) peripherals.Bind(PeripheralAction);
            settings = gameUi == null ? GetComponent<BeachNativeSettingsView>() : null;
            if (settings)
            {
                settings.Bind(CoreRoot.GetOrCreate().Storage, () => progress.DisplayName, RenamePlayer,
                    SettingsClosed, ReturnFromFightSettings);
                input.KeyBinding = settings.GetKey;
                var close = peripherals.Node(1089)?.button;
                if (close) close.onClick.AddListener(ResumeSettingsClock);
            }
            nativeTraining = gameUi == null ? GetComponent<BeachNativeTrainingView>() : null;
            tutorial = gameUi == null ? GetComponent<BeachNativeTutorialView>() : null;
            phone = gameUi != null
                ? BeachPlatformInput.DetectMobile(gameUi.simulateTouchInEditor)
                : BeachPlatformInput.DetectMobile(false);
            stage.SetPhone(phone);
            input.UseTouchLayout(phone, false, false, false);
            if (nativeTraining)
            {
                nativeTraining.Bind(_ => ResetWorkoutBar(), ResetWorkoutBar, StopTraining);
                nativeTraining.SetPhone(phone);
            }
            if (tutorial) tutorial.Bind(progress, () => store.Save(progress), phone);
            if (gameUi == null && hud != null) hud.SetNativeTraining(nativeTraining != null);
            nativeOpponents = gameUi == null ? GetComponent<BeachNativeOpponentView>() : null;
            if (nativeOpponents) nativeOpponents.Bind(ChangeOpponent, BeginFight, StopTraining);
            if (gameUi == null && hud != null) hud.SetNativeOpponentMenu(nativeOpponents != null);
            cameraFeedback = GetComponentInChildren<BeachNativeCameraFeedback>(true);
            trainingFeedback = GetComponent<BeachNativeTrainingFeedback>();
            combatFeedback = GetComponent<BeachNativeCombatFeedback>();
            if (gameUi == null && hud != null) hud.SetNativeCombatFeedback(combatFeedback && combatFeedback.IsBound);
            if (trainingFeedback) trainingFeedback.Bind((intensity, duration) =>
            {
                if (cameraFeedback) cameraFeedback.Shake(intensity, duration);
            });
            if (gameUi == null && hud != null) hud.SetNativeChrome(peripherals && peripherals.IsBound);
            stage.ApplyProgress(progress);
            if (gameUi != null)
            {
                gameUi.Bind(
                    opponentCatalog,
                    () => CanAcceptGameplayAction && phase == BeachPhase.Training && activeStation == null,
                    () => CanAcceptGameplayAction && phase == BeachPhase.Fight && fight != null &&
                        fight.Countdown <= 0f && fight.Outcome == FightOutcome.Pending,
                    () => CanAcceptGameplayAction && phase == BeachPhase.Training && activeStation != null,
                    TogglePause,
                    RequestCoinRewardAd,
                    RequestStarRewardAd,
                    StopTraining,
                    RequestDoubleWorkoutAd,
                    ChangeOpponent,
                    BeginFight,
                    StopTraining,
                    HandleBoxerAction,
                    HandleSignInClaim,
                    Home,
                    Rematch,
                    Home);
                input.NativeMove = () => gameUi.MoveDirection;
                input.NativePunch = () => gameUi.PunchHeld;
                input.NativeBlock = () => gameUi.BlockHeld;
            }
            else if (hud != null)
            {
                hud.Bind(input, Interact, StopTraining, ChangeOpponent, BeginFight, Home, TogglePause);
                hud.BindPause(TogglePause, ReturnToMenu);
                nativeControls = GetComponent<BeachNativeMobileControls>();
                if (nativeControls)
                {
                    nativeControls.Bind(() => CanAcceptGameplayAction && phase == BeachPhase.Training && activeStation == null,
                        () => CanAcceptGameplayAction && phase == BeachPhase.Fight && fight != null &&
                            fight.Countdown <= 0 && fight.Outcome == FightOutcome.Pending,
                        () => stage.JumpPlayer(true, false), Interact);
                    input.NativeMove = () => nativeControls.MoveDirection;
                    input.NativePunch = () => nativeControls.PunchHeld;
                    input.NativeBlock = () => nativeControls.BlockHeld;
                }
                hud.SetNativeControls(nativeControls != null);
            }
            input.Phone = phone;
            EnterHome();
        }

        private void Update()
        {
            if ((hud == null && gameUi == null) || !focused || paused || returning) return;
            if (workoutExitCooldown > 0f) workoutExitCooldown = Mathf.Max(0f, workoutExitCooldown - Time.deltaTime);
            if (gameUi != null && phone != gameUi.UseTouch)
            {
                phone = gameUi.UseTouch;
                stage.SetPhone(phone);
            }
            input.Phone = phone;
            RenderNativeGameplay();
            if (fight != null && phase == BeachPhase.Fight) fight.UpdateNativeFacing(Time.deltaTime);
            if (trainingFeedback) trainingFeedback.Tick(Time.deltaTime);
            if (nativeTraining) nativeTraining.Tick(Time.deltaTime);
            if (tutorial) { tutorial.Tick(Time.deltaTime); tutorial.UpdateProgress(); }
            if (nativeOpponents) nativeOpponents.Render(phase == BeachPhase.OpponentSelect, progress, opponentIndex);
            if (nativeOpponents && nativeOpponents.IsClosing) { ResetInput(); return; }
            if (autoTraining) autoTraining.Advance(Time.deltaTime);
            if (Input.GetKeyDown(input.Key(5)) && !(settings && (settings.IsEditingText || settings.IsListening)))
            {
                if (gameUi != null && gameUi.IsModalOpen)
                {
                    gameUi.CloseModals();
                    ResetInput();
                }
                else if (phase == BeachPhase.OpponentSelect)
                {
                    StopTraining();
                }
                else if (activeStation != null)
                {
                    StopTraining();
                }
                else
                {
                    TogglePause();
                }
            }
            if (CommonSettingsOpen)
            {
                ResetInput();
                RenderGameUi();
                return;
            }
            if (settings && settings.IsOpen)
            {
                ResetInput();
                if (phase == BeachPhase.Training && activeStation != null) AdvanceWorkout(default);
                return;
            }
            if (menuPaused)
            {
                RenderGameUi();
                return;
            }
            if (gameUi != null && gameUi.IsModalOpen)
            {
                ResetInput();
                RenderGameUi();
                return;
            }
            if (peripherals && peripherals.IsBound && gameUi == null)
            {
                if (peripherals.IsOpen)
                {
                    peripherals.Render(progress, phase, activeStation != null, nearestStation != null, hud.TouchLayout);
                    peripheralWasOpen = true;
                    ResetInput();
                    return;
                }
                if (peripheralWasOpen) { ResetInput(); peripheralWasOpen = false; }
            }
            bool openingWasInProgress = presentationTimeline.OpeningActive && !presentationTimeline.OpeningComplete;
            presentationTimeline.Advance(presentationTimeline.OpeningActive ? Time.deltaTime : Time.unscaledDeltaTime, false);
            if (openingWasInProgress && presentationTimeline.OpeningComplete) RenderNativeGameplay();
            if (presentationTimeline.ResultActive) Time.timeScale = presentationTimeline.TimeScale;
            if (pendingTransition != null)
            {
                presentation.Render(presentationTimeline, fight != null && fight.Outcome == FightOutcome.Victory);
                if (!presentationTimeline.TransitionComplete) return;
                var completed = pendingTransition;
                pendingTransition = null;
                completed();
                if (returning) return;
            }
            if (presentationTimeline.ConsumeOpeningSound()) stage.OpeningPresentationSound();
            if (gameUi == null && presentationTimeline.ConsumeResultSound()) stage.ResultPresentationSound(fight.Outcome == FightOutcome.Victory);
            if (fight != null && fight.Outcome == FightOutcome.Victory && presentationTimeline.ConsumeVictoryParticles())
                stage.VictoryPresentationParticles();
            latest = input.ReadFrame();
            if (phase == BeachPhase.Fight && fight.Countdown <= 0 && fight.Outcome == FightOutcome.Pending)
                queuedAttack |= latest.AttackPressed || (gameUi != null && gameUi.ConsumePunchPress());
            if (phase == BeachPhase.Training)
            {
                if (activeStation != null)
                {
                    if (latest.JumpPressed || Input.GetKeyDown(KeyCode.Escape))
                    {
                        if (trainingFeedback) trainingFeedback.Sample(workout.Speed, Time.time, Time.deltaTime);
                        StopTraining();
                        workout.AdvanceWorkout(Time.deltaTime, latest.BoostPressed, latest.BoostHeld,
                            gameUi != null ? false : latest.BoostTouch, autoTraining && autoTraining.Accelerated);
                    }
                    else AdvanceWorkout(latest);
                }
                else
                {
                    bool jumpPressed = latest.JumpPressed || (gameUi != null && gameUi.ConsumeJumpPress());
                    bool jumpReleased = latest.JumpReleased || (gameUi != null && gameUi.ConsumeJumpRelease());
                    stage.JumpPlayer(jumpPressed, jumpReleased);
                    stage.WalkPlayer(latest.Move, Time.deltaTime);
                    stage.Player.Walk(latest.Move.magnitude, stage.PlayerGrounded);
                    if (tutorial) tutorial.ObserveMove(latest.Move);
                    LocateInteraction();
                    bool mobileAutoEnter = gameUi != null && phone && nearestStation != null && workoutExitCooldown <= 0f;
                    if (latest.InteractPressed || mobileAutoEnter) Interact();
                }
            }
            stage.Guide(progress, phase == BeachPhase.Training && (!nativeTraining || !nativeTraining.SuppressesGuide));
            if (gameUi != null)
                RenderGameUi();
            else if (hud != null)
                hud.Render(phase, progress, activeStation, workout.Progress, nearestStation != null || byRing,
                    SelectedOpponent, opponentIndex, LastUnlocked(), fight, rewarded);
            presentation.Render(presentationTimeline, fight != null && fight.Outcome == FightOutcome.Victory);
            if (peripherals && peripherals.IsBound && gameUi == null)
            {
                peripherals.Render(progress, phase, activeStation != null, nearestStation != null, hud.TouchLayout);
                peripherals.RenderAutoTraining(activeStation != null, autoTraining && autoTraining.Accelerated,
                    autoTraining ? autoTraining.SecondsLeft : 0);
            }
        }

        private void RenderGameUi()
        {
            if (gameUi == null) return;
            gameUi.Render(
                phase,
                progress,
                activeStation != null,
                workout.Progress,
                autoTraining != null && autoTraining.Accelerated,
                autoTraining != null ? autoTraining.SecondsLeft : 0,
                SelectedOpponent,
                opponentIndex,
                LastUnlocked(),
                fight,
                rewarded,
                CommonSettingsOpen);
        }

        private void RenderNativeGameplay()
        {
            bool fighting = phase == BeachPhase.Fight && fight != null && fight.Countdown <= 0 && fight.Outcome == FightOutcome.Pending;
            if (nativeControls)
                nativeControls.Render(phone, phase, fighting, CanAcceptGameplayAction && activeStation == null,
                    CanAcceptGameplayAction && fighting);
            if (combatFeedback && combatFeedback.IsBound) combatFeedback.Render(fight, fighting, false);
        }

        private void AdvanceWorkout(InputFrame frame)
        {
            bool nativePulse = (nativeTraining && nativeTraining.ConsumePulse()) ||
                (gameUi != null && gameUi.ConsumeWorkoutBoostPress());
            bool boostPulse = frame.BoostPressed || nativePulse;
            if (boostPulse && gameUi != null) gameUi.PulseQuickWorkoutButton();
            bool boostHeld = frame.BoostHeld || (gameUi != null && gameUi.WorkoutBoostHeld);
            if (trainingFeedback) trainingFeedback.Sample(workout.Speed, Time.time, Time.deltaTime);
            if (trainingFeedback) trainingFeedback.Observe(activeStation.Muscle, workout.Progress, progress, boostPulse);
            int completed = workout.AdvanceWorkout(Time.deltaTime, boostPulse, boostHeld,
                gameUi != null ? false : frame.BoostTouch, autoTraining && autoTraining.Accelerated);
            if (completed > 0)
            {
                int gain = completed * BeachProgressionRules.CalculateGain(progress.prestigeLevel,
                    BeachPeripheralRules.GetAppliedModifier(progress.peripheral, activeStation.Muscle));
                progress.Add(activeStation.Muscle, gain);
                if (gameUi != null && BeachProgressionRules.CanLevelUp(progress, progress.prestigeLevel))
                {
                    progress.prestigeLevel = BeachProgressionRules.NextLevel(progress, progress.prestigeLevel);
                    BeachCommonUI.PlayReward();
                    BeachAdService.Notify($"实力晋级 lv{progress.prestigeLevel}！");
                }
                stage.ApplyProgress(progress);
                store.Save(progress);
                if (trainingFeedback && trainingFeedback.HasBoundFeedback)
                    trainingFeedback.Completed(activeStation.Muscle, gain);
                else
                {
                    if (trainingFeedback) trainingFeedback.Completed(activeStation.Muscle, gain);
                    stage.Play("Training", 0.27f);
                    if (gameUi == null && hud != null) hud.TrainingGain(activeStation.Muscle, gain);
                }
                stage.ResetWorkout(activeStation.Muscle);
            }
            stage.Player.Workout(activeStation.Muscle, workout.Speed);
            if (nativeTraining) nativeTraining.Refresh(progress.Get(activeStation.Muscle), progress.unlockedOpponent, workout.Progress);
        }

        private void ResetWorkoutBar()
        {
            if (activeStation == null) return;
            workout.Reset();
            if (trainingFeedback) trainingFeedback.ResetBar();
            stage.ResetWorkout(activeStation.Muscle);
        }

        private void FixedUpdate()
        {
            if (!focused || paused || menuPaused || pendingTransition != null || phase != BeachPhase.Fight || fight == null || fight.Outcome != FightOutcome.Pending) return;
            var frame = new InputFrame(latest.Move, latest.AttackHeld || queuedAttack, false, latest.BlockHeld, false, false);
            queuedAttack = false;
            int playerImpacts = fight.Player.ImpactCount;
            int enemyImpacts = fight.Enemy.ImpactCount;
            float previousPunch = fight.Player.PunchTime;
            if (nativeControls && fight.Countdown <= 0) nativeControls.ObserveCombatMove(frame.Move);
            fight.Tick(Time.fixedDeltaTime, frame);
            if (cameraFeedback && fight.Player.PunchTime < previousPunch) cameraFeedback.Punch(fight.Player.LeftPunch);
            if (cameraFeedback && fight.Player.ImpactCount != playerImpacts) cameraFeedback.Shake(.1f, .23f);
            bool nativeFeedback = combatFeedback && combatFeedback.IsBound;
            if (nativeFeedback)
            {
                combatFeedback.Render(fight, fight.Countdown <= 0, fight.EmptyPunchRequested);
                if (fight.Player.ImpactCount != playerImpacts) combatFeedback.PlayerHit(fight.Player.LastImpactWasBlocked);
                if (fight.Enemy.ImpactCount != enemyImpacts)
                    combatFeedback.EnemyHit(opponentIndex, fight.Enemy.LastImpactWasBlocked, FightRules.PlayerPunchDamage(progress.arms));
            }
            stage.Feedback(fight, fight.Player.ImpactCount != playerImpacts, fight.Enemy.ImpactCount != enemyImpacts, nativeFeedback);
            if (fight.Outcome == FightOutcome.Pending) return;
            rewarded = fight.Outcome == FightOutcome.Victory &&
                progress.RecordVictory(opponentIndex, opponents[opponentIndex].coins, opponents[opponentIndex].stars);
            if (rewarded) store.Save(progress);
            presentationTimeline.BeginResult(BeachPresentationTimeline.KnockoutTimeScale);
            Time.timeScale = presentationTimeline.TimeScale;
            if (gameUi != null) stage.ResultPresentationSound(fight.Outcome == FightOutcome.Victory);
            SetPhase(BeachPhase.Result);
        }

        private void LateUpdate()
        {
            if (stage == null) return;
            if (trainers && progress != null)
                trainers.Apply(phase == BeachPhase.Training || phase == BeachPhase.OpponentSelect ? progress.peripheral : null,
                    stage.Player.Root, activeStation != null,
                    activeStation != null ? stage.TrainerWorkoutTarget(activeStation.Muscle) : null);
            if (fight != null && (phase == BeachPhase.Fight || phase == BeachPhase.Result))
            {
                if (fight.Outcome == FightOutcome.Pending)
                {
                    stage.Player.FightIntent(fight.Player, fight.PlayerFlash, fight.PlayerAnimationAxes);
                    stage.Enemy.FightIntent(fight.Enemy, fight.EnemyFlash, fight.EnemyAnimationAxes);
                }
                else
                {
                    stage.Player.Result(fight.Outcome == FightOutcome.Victory);
                    stage.Enemy.Result(fight.Outcome == FightOutcome.Defeat);
                }
                float countdown = fight.Countdown;
                stage.FrameFight(countdown, fight.Outcome == FightOutcome.Pending);
                if (countdown > 0) presentation.FrameOpeningCamera(presentationTimeline.OpeningTime);
            }
            else stage.FrameBeach(activeStation != null);
        }

        private void LocateInteraction()
        {
            nearestStation = null;
            bool insideRing = stage.RingContainsPlayer();
            bool enteredRing = insideRing && !byRing;
            byRing = insideRing;
            int nearby = stage.NearbyStationIndex();
            if (nearby >= 0 && nativeNpcs && !nativeNpcs.IsPlayerStationAvailable((Muscle)nearby)) nearby = -1;
            foreach (var station in stations)
            {
                if ((int)station.Muscle == nearby) nearestStation = station;
            }
            stage.ShowStationGlow(nearby);
            if (enteredRing && phase == BeachPhase.Training && activeStation == null)
            {
                opponentIndex = LastUnlocked();
                stage.Player.Walk(0, stage.PlayerGrounded);
                stage.ShowStationGlow(-1);
                BeachCommonUI.PlayPopupOpen(fallbackToClick: true);
                SetPhase(BeachPhase.OpponentSelect);
            }
        }

        private void Interact()
        {
            if (!CanAcceptGameplayAction) return;
            if (phase != BeachPhase.Training || activeStation != null) return;
            if (peripherals && peripherals.IsOpen) return;
            if (nearestStation != null)
            {
                if (nativeNpcs && !nativeNpcs.IsPlayerStationAvailable(nearestStation.Muscle)) return;
                activeStation = nearestStation;
                workout.Reset();
                stage.BeginWorkout(activeStation);
                if (nativeNpcs) nativeNpcs.PlayerEntered(activeStation.Muscle, progress.prestigeLevel);
                if (nativeTraining) nativeTraining.Enter(activeStation.Muscle, progress.Get(activeStation.Muscle), progress.unlockedOpponent);
                if (trainingFeedback) trainingFeedback.Enter(activeStation.Muscle);
                if (autoTraining) autoTraining.SetEnabled(true);
                stage.ShowStationGlow(-1);
                ResetInput();
            }
        }

        private void StopTraining()
        {
            if (!CanAcceptGameplayAction) return;
            if (phase == BeachPhase.OpponentSelect)
            {
                SetPhase(BeachPhase.Training);
                return;
            }
            if (activeStation == null) return;
            stage.EndWorkout(activeStation);
            if (nativeNpcs) nativeNpcs.PlayerExited(activeStation.Muscle);
            if (nativeTraining) nativeTraining.Exit();
            if (trainingFeedback) trainingFeedback.Exit();
            activeStation = null;
            workoutExitCooldown = 1.2f;
            if (autoTraining) autoTraining.SetEnabled(false);
            workout.Reset();
            store.Save(progress);
            ResetInput();
        }

        private void PeripheralAction(BeachPeripheralAction request, int argument)
        {
            if (!peripherals || returning || pendingTransition != null) return;
            if (menuPaused) return;
            if (request == BeachPeripheralAction.AccelerateTraining)
            {
                if (activeStation != null && phase == BeachPhase.Training && autoTraining)
                    autoTraining.RequestAcceleration();
                return;
            }
            if (request == BeachPeripheralAction.Interact) { Interact(); return; }
            if (request == BeachPeripheralAction.Pause) { TogglePause(); return; }
            if (request == BeachPeripheralAction.Close) { peripherals.CloseMenu(); ResetInput(); return; }
            if (request == BeachPeripheralAction.OpenTrainer || request == BeachPeripheralAction.OpenDaily ||
                request == BeachPeripheralAction.OpenPrestige)
            {
                if (phase != BeachPhase.Training || activeStation != null || peripherals.IsOpen) return;
                if (request != BeachPeripheralAction.OpenPrestige && progress.unlockedOpponent <= 0) return;
                if (request == BeachPeripheralAction.OpenTrainer) progress.peripheral.trainerAlert = false;
                peripherals.ShowMenu(request == BeachPeripheralAction.OpenTrainer ? 602 :
                    request == BeachPeripheralAction.OpenDaily ? 204 : 679, progress.peripheral);
                stage.Player.Walk(0);
                stage.ShowStationGlow(-1);
                ResetInput();
                if (gameUi == null && hud != null) hud.BlockPeripheralInput();
                store.Save(progress);
                return;
            }
            if (!peripherals.AcceptsActions) return;
            if (request == BeachPeripheralAction.SelectTrainer) { peripherals.SelectTrainer(argument); return; }
            bool changed = false;
            switch (request)
            {
                case BeachPeripheralAction.Purchase:
                    changed = BeachPeripheralRules.TryPurchaseTrainer(progress, progress.peripheral, peripherals.Selection); break;
                case BeachPeripheralAction.Equip:
                    changed = BeachPeripheralRules.TryEquipTrainer(progress.peripheral, peripherals.Selection); break;
                case BeachPeripheralAction.Upgrade:
                    changed = BeachPeripheralRules.TryUpgradeTrainer(progress, progress.peripheral, peripherals.Selection); break;
                case BeachPeripheralAction.ClaimDaily:
                    changed = BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, argument);
                    if (changed) peripherals.Feedback(true);
                    break;
                case BeachPeripheralAction.LevelUp:
                    int next = BeachProgressionRules.NextLevel(progress, progress.prestigeLevel);
                    changed = next != progress.prestigeLevel;
                    if (changed) { progress.prestigeLevel = next; stage.ApplyProgress(progress); peripherals.HideMenuImmediately(); }
                    break;
            }
            if (changed) store.Save(progress);
            else if (request == BeachPeripheralAction.Purchase || request == BeachPeripheralAction.Upgrade) peripherals.Feedback(false);
            peripherals.Render(progress, phase, activeStation != null, nearestStation != null, hud.TouchLayout);
        }

        private void ChangeOpponent(int offset)
        {
            if (!CanAcceptGameplayAction) return;
            if (phase == BeachPhase.OpponentSelect)
                opponentIndex = Mathf.Clamp(opponentIndex + offset, 0, LastUnlocked());
        }

        private int LastUnlocked() => Mathf.Clamp(progress.unlockedOpponent, 0, opponents.Length);

        private void BeginFight()
        {
            if (!CanAcceptGameplayAction) return;
            if (opponentIndex < 0 || opponentIndex >= opponents.Length) return;
            if (phase != BeachPhase.OpponentSelect && phase != BeachPhase.Result) return;
            BeginSceneTransition(EnterFight);
        }

        private void Rematch()
        {
            if (!CanAcceptGameplayAction) return;
            if (phase != BeachPhase.Result || opponentIndex < 0 || opponentIndex >= opponents.Length) return;
            BeginSceneTransition(EnterFight);
        }

        private void RequestCoinRewardAd()
        {
            BeachAdService.ShowRewardedAd("hud_coins", () =>
            {
                progress.coins = progress.coins <= int.MaxValue - 500 ? progress.coins + 500 : int.MaxValue;
                store.Save(progress);
                BeachCommonUI.PlayReward();
                BeachAdService.Notify("获得 500 金币！");
            });
        }

        private void RequestStarRewardAd()
        {
            BeachAdService.ShowRewardedAd("hud_stars", () =>
            {
                progress.stars = progress.stars <= int.MaxValue - 10 ? progress.stars + 10 : int.MaxValue;
                store.Save(progress);
                BeachCommonUI.PlayReward();
                BeachAdService.Notify("获得 10 星星！");
            });
        }

        private void RequestDoubleWorkoutAd()
        {
            if (activeStation == null || phase != BeachPhase.Training || autoTraining == null || autoTraining.Accelerated) return;
            BeachAdService.ShowRewardedAd("workout_double", () =>
            {
                autoTraining.ResolveReward(true);
                BeachCommonUI.PlayReward();
                BeachAdService.Notify("已开启 2 倍锻炼加速！");
            });
        }

        private void HandleBoxerAction(int trainerId)
        {
            if (progress?.peripheral == null) return;
            progress.peripheral.trainerAlert = false;
            bool owned = progress.peripheral.trainerLevels[trainerId] > 0;
            if (owned)
            {
                if (BeachPeripheralRules.TryEquipTrainer(progress.peripheral, trainerId))
                {
                    store.Save(progress);
                    BeachAdService.Notify("已选择出战！");
                }
                return;
            }
            if (BeachPeripheralRules.TryPurchaseTrainer(progress, progress.peripheral, trainerId))
            {
                BeachPeripheralRules.TryEquipTrainer(progress.peripheral, trainerId);
                store.Save(progress);
                BeachCommonUI.PlayReward();
                BeachAdService.Notify("解锁并出战成功！");
            }
            else
            {
                BeachCommonUI.PlayError();
                BeachAdService.Notify("星星不足！");
            }
        }

        private void HandleSignInClaim(int day, bool doubleReward)
        {
            if (progress?.peripheral == null || !BeachPeripheralRules.CanClaim(progress.peripheral, day)) return;
            if (!doubleReward)
            {
                if (BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, day, 1))
                {
                    store.Save(progress);
                    BeachCommonUI.PlayReward();
                    BeachAdService.Notify("奖励已领取！");
                }
                return;
            }
            BeachAdService.ShowRewardedAd("signin_double", () =>
            {
                if (BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, day, 2))
                {
                    store.Save(progress);
                    BeachCommonUI.PlayReward();
                    BeachAdService.Notify("双倍奖励已领取！");
                }
            });
        }

        private void OnCommonAudioChanged()
        {
            if (stage != null) stage.SyncAudioState();
        }

        private void EnterFight()
        {
            if (gameUi) gameUi.CloseModals();
            if (nativeNpcs) nativeNpcs.SetBeachActive(false);
            if (combatFeedback) combatFeedback.SelectOpponent(opponentIndex);
            if (cameraFeedback) cameraFeedback.ResetEffects();
            if (nativeOpponents) nativeOpponents.HideImmediately();
            if (opponentIndex < 0 || opponentIndex >= opponents.Length) return;
            Time.timeScale = resumeTimeScale = 1;
            presentationTimeline.BeginOpening();
            stage.EnterRing(opponents[opponentIndex].modelName);
            var ring = stage.RingBounds;
            fight = new FightController(stage.Player.Root, stage.Enemy.Root, progress, opponents[opponentIndex], ring,
                Random.Range(0, int.MaxValue), nativePhysics: true, externalOpening: presentationTimeline);
            rewarded = false;
            SetPhase(BeachPhase.Fight);
        }

        private void Home()
        {
            if (!CanAcceptGameplayAction) return;
            if (stage == null || (phase != BeachPhase.Result && fight != null)) return;
            if (fight != null) BeginSceneTransition(EnterHome);
            else EnterHome();
        }

        private void EnterHome()
        {
            if (gameUi) gameUi.CloseModals();
            if (combatFeedback) combatFeedback.ResetEffects();
            bool returnedFromFight = fight != null;
            if (cameraFeedback) cameraFeedback.ResetEffects();
            if (returnedFromFight)
            {
                workout.ResetScene();
                if (trainingFeedback) trainingFeedback.ResetScene();
            }
            if (nativeOpponents) nativeOpponents.HideImmediately();
            if (settings) { settings.HideImmediately(); settings.SetFightContext(false); }
            if (nativeTraining) nativeTraining.Exit();
            menuPaused = false;
            Time.timeScale = resumeTimeScale = 1;
            presentationTimeline.Reset();
            fight = null;
            activeStation = null;
            workout.Reset();
            stage.EnterBeach(progress.prestigeLevel);
            if (nativeNpcs)
            {
                nativeNpcs.ResetScene(phone);
                nativeNpcs.SetBeachActive(true);
            }
            if (tutorial) tutorial.BeachEntered();
            LocateInteraction();
            SetPhase(BeachPhase.Training);
        }

        private void BeginSceneTransition(System.Action completed)
        {
            if (pendingTransition != null || returning) return;
            Time.timeScale = resumeTimeScale = 1;
            ResetInput();
            if (!presentation.IsBound) { completed(); return; }
            presentationTimeline.BeginTransition();
            pendingTransition = completed;
            presentation.Render(presentationTimeline, fight != null && fight.Outcome == FightOutcome.Victory);
        }

        private void SetPhase(BeachPhase next)
        {
            phase = next;
            ResetInput();
        }

        private void ResetInput()
        {
            input.Reset();
            if (nativeControls) nativeControls.ResetInput();
            if (gameUi) gameUi.ResetTouchControls();
            if (stage != null) stage.StopPlayerMovement();
            latest = default;
            queuedAttack = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            ResetInput();
            if (!hasFocus && progress != null && store != null) store.Save(progress);
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            ResetInput();
            if (isPaused && progress != null && store != null) store.Save(progress);
        }

        private void OnApplicationQuit()
        {
            if (progress != null && store != null) store.Save(progress);
        }

        private void TogglePause()
        {
            if (gameUi != null && commonUi != null && commonUi.Settings != null)
            {
                if (commonUi.Settings.IsOpen)
                {
                    ResumeSettingsClock();
                    commonUi.Settings.Close();
                    return;
                }
                if (pendingTransition != null || returning) return;
                menuPaused = true;
                bool boxing = phase == BeachPhase.Fight || phase == BeachPhase.Result;
                if (boxing) Time.timeScale = 0;
                commonUi.Settings.Open();
                if (!boxing) stage.StopPlayerVelocity();
                stage.Player.Walk(0);
                ResetInput();
                return;
            }
            if (peripherals && peripherals.IsOpen) return;
            if (settings)
            {
                if (settings.IsOpen)
                {
                    if (settings.AcceptsActions) { ResumeSettingsClock(); settings.Close(); }
                    return;
                }
                if (pendingTransition != null || returning || phase == BeachPhase.OpponentSelect) return;
                menuPaused = true;
                bool boxing = phase == BeachPhase.Fight || phase == BeachPhase.Result;
                settings.SetFightContext(boxing);
                if (boxing) Time.timeScale = 0;
                settings.Open();
                if (!boxing) stage.StopPlayerVelocity();
                if (peripherals) peripherals.SuspendChrome();
                stage.Player.Walk(0);
                ResetInput();
                return;
            }
            menuPaused = !menuPaused;
            if (menuPaused)
            {
                resumeTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
            else Time.timeScale = resumeTimeScale;
            if (gameUi == null && hud != null) hud.ShowPause(menuPaused);
            if (peripherals && peripherals.IsBound)
            {
                if (menuPaused) peripherals.SuspendChrome();
                else peripherals.Render(progress, phase, activeStation != null, nearestStation != null, hud.TouchLayout);
            }
            ResetInput();
        }

        private void RenamePlayer(string value)
        {
            progress.playerName = value;
            progress.generatedName = true;
            progress.customPlayerName = true;
            store.Save(progress);
            stage.ApplyProgress(progress);
        }

        private void ResumeSettingsClock()
        {
            if (phase == BeachPhase.Fight || phase == BeachPhase.Result) Time.timeScale = 1;
        }

        private void SettingsClosed()
        {
            menuPaused = false;
            ResumeSettingsClock();
            ResetInput();
            if (peripherals && gameUi == null) peripherals.Render(progress, phase, activeStation != null, nearestStation != null, hud.TouchLayout);
        }

        private void ReturnFromFightSettings()
        {
            menuPaused = false;
            Time.timeScale = 1;
            ResetInput();
            BeginSceneTransition(EnterHome);
        }

        private void OnDestroy()
        {
            if (commonUi != null && commonUi.Settings != null)
            {
                commonUi.Settings.AudioChanged -= OnCommonAudioChanged;
                commonUi.Settings.Closed -= SettingsClosed;
            }
            Time.timeScale = 1;
        }

        private void ReturnToMenu()
        {
            if (returning) return;
            if (menuPaused) TogglePause();
            BeginSceneTransition(LoadMenu);
        }

        private void LoadMenu()
        {
            Time.timeScale = resumeTimeScale = 1;
            returning = true;
            ResetInput();
            store.Save(progress);
            var common = BeachCommonUI.Ensure();
            if (common != null) common.ReturnToInit();
        }

    }
}
