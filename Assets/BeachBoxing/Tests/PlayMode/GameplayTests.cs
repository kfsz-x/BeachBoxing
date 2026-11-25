using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static BeachBoxing.Tests.NativePlayModeScene;

namespace BeachBoxing.Tests
{
    public sealed class GameplayTests
    {
        private const int Punch = 1000450, Block = 1000308;
        private static readonly Vector3 IdleCameraEuler = new Vector3(8.383f, -214.616f, 0);

        [UnityTest]
        public IEnumerator BeachTrainingPersistsAndOpensTheRing() => WithScene(true, CheckVictory);

        private static IEnumerator CheckVictory(BeachGameFlow flow, LocalStore storage)
        {
            var view = flow.GetComponent<BeachPeripheralView>();
            var player = flow.transform.Find("Player");
            int placed = flow.GetComponentsInChildren<Transform>(true).Length;
            var equipment = flow.transform.Find("Training beach/Arms/Original equipment");
            PlacePlayer(player, equipment.position);
            yield return Until(() => Available(view.Node(1057).button), 2f, "The arms detector must expose Interact.");
            Click(view, 1057);

            var boxingRig = player.Find("Original Player").GetComponentInChildren<Animator>(true);
            var workoutRig = player.Find("Original Workout Player").GetComponentInChildren<Animator>(true);
            Assert.That(boxingRig.runtimeAnimatorController.name, Is.EqualTo("BoxingArmature"));
            Assert.That(workoutRig.runtimeAnimatorController.name, Is.EqualTo("Armature"));
            AssertPose(player, equipment.Find("Workout pose"));
            var progress = new ProgressStore(storage);
            yield return Until(() => progress.Load().arms > 0, 8f, "A completed repetition must reach the save.");
            Assert.That(progress.Load().arms, Is.EqualTo(11));
            Click(view, 377);
            Assert.That(Vector3.Distance(player.position, equipment.Find("ExitPos").position), Is.LessThan(.01f));

            yield return StartFight(flow, view);
            var controls = flow.GetComponent<BeachNativeMobileControls>();
            var punch = Node(view, Punch);
            var block = Node(view, Block);
            var timeline = Field<BeachPresentationTimeline>(flow, "presentationTimeline");
            var fight = CurrentFight(flow);
            var enemy = flow.transform.Find("Opponent");
            Assert.That(boxingRig.runtimeAnimatorController.name, Is.EqualTo("BoxingArmature"));
            Assert.That(workoutRig.runtimeAnimatorController.name, Is.EqualTo("Armature"));
            Assert.That(enemy.gameObject.activeInHierarchy, Is.True);
            Assert.That(fight.Countdown, Is.GreaterThan(0));
            Assert.That(controls.combatRoot.activeInHierarchy || punch.activeInHierarchy || block.activeInHierarchy, Is.False);
            foreach (string field in new[] { "menu", "playerWorldBars", "enemyWorldBars" })
                Assert.That(HudObject(flow, field).activeInHierarchy, Is.False, "Opening must hide " + field);
            Assert.That(flow.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placed));

            yield return Until(() => controls.combatRoot.activeInHierarchy,
                BeachPresentationTimeline.OpeningDuration + 8f, "The rendered opening must hand off to native combat.");
            Assert.That(punch.activeInHierarchy && block.activeInHierarchy, Is.True);
            Assert.That(HudObject(flow, "menu").activeInHierarchy, Is.True);
            Assert.That(HudObject(flow, "playerWorldBars").activeInHierarchy, Is.True);

            var result = HudObject(flow, "result");
            var strike = Pointer(42, ScreenCenter(punch.transform));
            try
            {
                ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerDownHandler);
                Assert.That(controls.PunchHeld, Is.True, "The authored trigger must route the held punch.");
                float deadline = Time.realtimeSinceStartup + 12f;
                while (!result.activeInHierarchy && fight.Outcome == FightOutcome.Pending && Time.realtimeSinceStartup < deadline)
                {
                    // Keep the opponent in contact range while the real held punch resolves.
                    enemy.position = player.position + Vector3.forward * .9f;
                    yield return null;
                }
            }
            finally { ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerUpHandler); }

            Assert.That(fight.Outcome, Is.EqualTo(FightOutcome.Victory));
            Assert.That(fight.Enemy.ImpactCount, Is.GreaterThan(0));
            Assert.That(result.activeInHierarchy, Is.True);
            Assert.That(HudObject(flow, "defeatPanel").activeInHierarchy, Is.False);
            AssertResultStartsHidden(timeline, result);
            Assert.That(controls.combatRoot.activeInHierarchy || punch.activeInHierarchy || block.activeInHierarchy, Is.False,
                "Victory must hide the source gameplay canvas controls.");
            yield return Until(() => boxingRig.GetCurrentAnimatorStateInfo(0).IsName("Boxing_Idle"),
                4f, "The winning boxer must finish the last action and return to the native idle state.");
            Assert.That(boxingRig.GetBool("Dead"), Is.False);
            Assert.That(enemy.GetComponentInChildren<Animator>().GetBool("Dead"), Is.True);

            var victory = progress.Load();
            Assert.That(victory.arms, Is.EqualTo(11));
            Assert.That(victory.coins, Is.EqualTo(50));
            Assert.That(victory.stars, Is.EqualTo(3));
            Assert.That(victory.unlockedOpponent, Is.EqualTo(1));
            var home = ResultButton(result, "RETURN HOME");
            yield return RevealResultAction(timeline, result, home);
            Click(home);
            yield return Until(() => CurrentFight(flow) == null && flow.transform.Find("Training beach").gameObject.activeInHierarchy,
                BeachPresentationTimeline.SceneLoadDelay + 2f, "Victory Home must return to the same beach.");
            Assert.That(result.activeInHierarchy, Is.False);
            yield return OpenPicker(flow, view);
            Assert.That(view.Node(332).label.text, Is.EqualTo("Kyle"));
            Assert.That(flow.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placed));
        }

        [UnityTest]
        public IEnumerator AllOriginalWorkoutsUseBoundPosesAndSingleAnimationSpeed() => WithScene(true, CheckWorkouts);

        private static IEnumerator CheckWorkouts(BeachGameFlow flow, LocalStore storage)
        {
            var view = flow.GetComponent<BeachPeripheralView>();
            var training = flow.GetComponent<BeachNativeTrainingView>();
            var cycle = Field<TrainingCycle>(flow, "workout");
            var player = flow.transform.Find("Player");
            var rig = player.Find("Original Workout Player").GetComponentInChildren<Animator>(true);
            float previousSpeed = .3f;
            foreach (Muscle muscle in new[] { Muscle.Arms, Muscle.Chest, Muscle.Back, Muscle.Legs })
            {
                var equipment = flow.transform.Find($"Training beach/{muscle}/Original equipment");
                PlacePlayer(player, equipment.position);
                yield return Until(() => Available(view.Node(1057).button) &&
                    Field<TrainingStation>(flow, "nearestStation")?.Muscle == muscle,
                    2f, $"{muscle} must expose the authored interaction.");
                Click(view, 1057);
                Assert.That(training.IsOpen, Is.True);
                Assert.That(training.Muscle, Is.EqualTo(muscle));
                Assert.That(cycle.Speed, Is.EqualTo(previousSpeed).Within(.00001f));
                Assert.That(cycle.Progress, Is.Zero);
                AssertPose(player, equipment.Find("Workout pose"));
                yield return null;

                float expected = previousSpeed + (.5f - previousSpeed) * (Time.deltaTime * 16f);
                Assert.That(cycle.Speed, Is.EqualTo(expected).Within(.00001f), "One idle phone frame must use the source speed ramp.");
                Assert.That(rig.runtimeAnimatorController.name, Is.EqualTo("Armature"));
                Assert.That(rig.GetBool("Grounded"), Is.True);
                Assert.That(rig.GetBool("WorkingOut"), Is.True);
                foreach (Muscle other in Enum.GetValues(typeof(Muscle)))
                {
                    Assert.That(rig.GetBool(other + "Idle"), Is.EqualTo(other == muscle));
                    Assert.That(rig.GetBool(other + "Workout"), Is.EqualTo(other == muscle));
                }
                Assert.That(rig.speed, Is.EqualTo(1f));
                Assert.That(rig.GetFloat("WorkoutSpeed"), Is.EqualTo(cycle.Speed).Within(.00001f));

                previousSpeed = cycle.Speed;
                Click(view, 377);
                Assert.That(training.IsOpen, Is.False);
                Assert.That(cycle.Speed, Is.EqualTo(previousSpeed).Within(.00001f));
                Assert.That(Vector3.Distance(player.position, equipment.Find("ExitPos").position), Is.LessThan(.01f));
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator LandscapeFightCameraTurnsImmediatelyAndFollowsThePlayerTarget() =>
            WithScene(false, (flow, _) => CheckFightCamera(flow, new Vector3(-8.7f, 4.9f, 6.94f), true));

        [UnityTest]
        public IEnumerator PhoneLandscapeFightUsesSourceWorldMarkerAsTheLocalOffset() =>
            WithScene(true, (flow, _) => CheckFightCamera(flow, new Vector3(1.22999954f, 16.85999966f, 7.94000006f), false));

        private static IEnumerator CheckFightCamera(BeachGameFlow flow, Vector3 sourceOffset, bool turn)
        {
            yield return StartFight(flow, flow.GetComponent<BeachPeripheralView>());
            var stage = Field<object>(flow, "stage");
            var rig = Field<BeachNativeCombatCamera>(stage, "nativeFightCamera");
            var camera = SceneCamera(flow);
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.view, Is.EqualTo(camera.transform.parent), "Stage must drive the saved camera rig parent.");
            Assert.That(rig.target.parent, Is.EqualTo(rig.player), "The source follow target belongs to the player.");
            Assert.That(camera.aspect, Is.EqualTo(1920f / 1080f).Within(.00001f));

            var timeline = Field<BeachPresentationTimeline>(flow, "presentationTimeline");
            timeline.Advance(BeachPresentationTimeline.OpeningDuration, false);
            Assert.That(CurrentFight(flow).Countdown, Is.Zero);
            var initialFollow = rig.arena.TransformPoint(new Vector3(0, 1.424f, 0));
            var worldScaleOffset = sourceOffset * .356f;
            var firstYaw = FollowYaw(rig);
            var firstFollow = Vector3.Lerp(initialFollow, rig.target.position, Time.deltaTime * 8f);
            Call(stage, "FrameFight", 0f, true);
            AssertCameraPose(rig.view, firstFollow, firstYaw, worldScaleOffset);

            if (turn)
            {
                var separation = rig.enemy.position - rig.player.position;
                rig.player.position += new Vector3(1, 0, .5f);
                rig.enemy.position = rig.player.position - separation;
                var reversedYaw = FollowYaw(rig);
                Assert.That(Quaternion.Angle(firstYaw, reversedYaw), Is.EqualTo(180f).Within(.01f));
                var secondFollow = Vector3.Lerp(firstFollow, rig.target.position, Time.deltaTime * 8f);
                Call(stage, "FrameFight", 0f, true);
                AssertCameraPose(rig.view, secondFollow, reversedYaw, worldScaleOffset);
            }
        }

        [UnityTest]
        public IEnumerator DefeatUsesBoundPanelAndAllowsReplayAndHome() => WithScene(true, CheckDefeat);

        private static IEnumerator CheckDefeat(BeachGameFlow flow, LocalStore storage)
        {
            var view = flow.GetComponent<BeachPeripheralView>();
            var controls = flow.GetComponent<BeachNativeMobileControls>();
            int placed = flow.GetComponentsInChildren<Transform>(true).Length;
            yield return StartFight(flow, view);
            var opening = Field<BeachPresentationTimeline>(flow, "presentationTimeline");
            var defeat = HudObject(flow, "defeatPanel");
            var victory = HudObject(flow, "result");
            var punch = Node(view, Punch);
            var block = Node(view, Block);
            var artworkRetained = new bool[2];
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var fight = CurrentFight(flow);
                opening.Advance(BeachPresentationTimeline.OpeningDuration, false);
                yield return Until(() => controls.combatRoot.activeInHierarchy, 2f, "The opening must give way to combat.");
                typeof(FighterState).GetProperty(nameof(FighterState.Health)).SetValue(fight.Player, 0f);
                float deadline = Time.realtimeSinceStartup + 2f;
                while (!defeat.activeInHierarchy && Time.realtimeSinceStartup < deadline) yield return null;

                Assert.That(defeat.activeInHierarchy, Is.True, "Defeat must show the bound compact panel.");
                Assert.That(fight.Outcome, Is.EqualTo(FightOutcome.Defeat));
                Assert.That(victory.activeInHierarchy, Is.False);
                AssertResultStartsHidden(opening, defeat);
                artworkRetained[attempt] = controls.combatRoot.activeInHierarchy && punch.activeInHierarchy && block.activeInHierarchy;
                Assert.That(controls.PunchHeld || controls.BlockHeld, Is.False);
                Assert.That(flow.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placed));

                var action = ResultButton(defeat, attempt == 0 ? "TRY AGAIN" : "Defeat RETURN HOME");
                yield return RevealResultAction(opening, defeat, action);
                Click(action);
                if (attempt == 0)
                    yield return Until(() => CurrentFight(flow) != null && CurrentFight(flow) != fight,
                        BeachPresentationTimeline.SceneLoadDelay + 2f, "Try Again must start a new fight.");
                else
                    yield return Until(() => CurrentFight(flow) == null && flow.transform.Find("Training beach").gameObject.activeInHierarchy,
                        BeachPresentationTimeline.SceneLoadDelay + 2f, "Defeat Home must return to the beach.");
                Assert.That(defeat.activeInHierarchy, Is.False);
                Assert.That(flow.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placed));
            }
            Assert.That(artworkRetained, Is.EqualTo(new[] { true, true }),
                "The source defeat screen retains gameplay control artwork during both attempts.");
        }

        [UnityTest]
        public IEnumerator NativeCombatPointersReleaseOnlyTheirOwnersAndClearInSettings() => WithScene(true, CheckPointerOwnership);

        private static IEnumerator CheckPointerOwnership(BeachGameFlow flow, LocalStore storage)
        {
            var view = flow.GetComponent<BeachPeripheralView>();
            yield return StartFight(flow, view);
            Field<BeachPresentationTimeline>(flow, "presentationTimeline").Advance(BeachPresentationTimeline.OpeningDuration, false);
            var controls = flow.GetComponent<BeachNativeMobileControls>();
            yield return Until(() => controls.combatRoot.activeInHierarchy, 2f, "Native combat controls must be eligible.");
            var joystick = controls.combatJoystick;
            var punch = Node(view, Punch);
            var block = Node(view, Block);
            Assert.That(punch.activeInHierarchy && block.activeInHierarchy && joystick.isActiveAndEnabled, Is.True);
            var move = Pointer(1, new Vector2(Screen.width * .15f, Screen.height * .2f));
            var strike = Pointer(2, ScreenCenter(punch.transform));
            var guard = Pointer(3, ScreenCenter(block.transform));
            try
            {
                ExecuteEvents.Execute(joystick.gameObject, move, ExecuteEvents.pointerDownHandler);
                Drag(joystick, move, Vector2.up);
                ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(block, guard, ExecuteEvents.pointerDownHandler);
                Assert.That(Vector2.Distance(joystick.Direction, Vector2.up), Is.LessThan(.00001f));
                Assert.That(controls.PunchHeld && controls.BlockHeld, Is.True);

                ExecuteEvents.Execute(joystick.gameObject, strike, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(punch, move, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(block, strike, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(joystick.gameObject, move, ExecuteEvents.pointerExitHandler);
                ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerExitHandler);
                ExecuteEvents.Execute(block, guard, ExecuteEvents.pointerExitHandler);
                Assert.That(Vector2.Distance(joystick.Direction, Vector2.up), Is.LessThan(.00001f));
                Assert.That(controls.PunchHeld && controls.BlockHeld, Is.True);

                ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerUpHandler);
                Assert.That(controls.PunchHeld, Is.False);
                Assert.That(controls.BlockHeld, Is.True);
                Assert.That(Vector2.Distance(joystick.Direction, Vector2.up), Is.LessThan(.00001f));
                ExecuteEvents.Execute(joystick.gameObject, move, ExecuteEvents.pointerUpHandler);
                Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero));
                Assert.That(controls.BlockHeld, Is.True);
                ExecuteEvents.Execute(block, guard, ExecuteEvents.pointerUpHandler);
                Assert.That(controls.BlockHeld, Is.False);

                ExecuteEvents.Execute(joystick.gameObject, move, ExecuteEvents.pointerDownHandler);
                Drag(joystick, move, Vector2.up);
                ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(block, guard, ExecuteEvents.pointerDownHandler);
                Assert.That(controls.PunchHeld && controls.BlockHeld && joystick.background.gameObject.activeInHierarchy, Is.True);
                Click(HudObject(flow, "menu").GetComponent<Button>());
                Assert.That(flow.GetComponent<BeachNativeSettingsView>().IsOpen, Is.True);
                Assert.That(controls.PunchHeld || controls.BlockHeld, Is.False);
                Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero));
                Assert.That(joystick.background.gameObject.activeSelf, Is.False);
            }
            finally
            {
                ExecuteEvents.Execute(joystick.gameObject, move, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(punch, strike, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(block, guard, ExecuteEvents.pointerUpHandler);
            }
        }

        private static IEnumerator StartFight(BeachGameFlow flow, BeachPeripheralView view)
        {
            yield return OpenPicker(flow, view);
            yield return Until(() => Available(view.Node(806).button), 2f, "The preplaced fight confirm must be available.");
            Click(view, 806);
            yield return Until(() => CurrentFight(flow) != null,
                BeachPresentationTimeline.SceneLoadDelay + 2f, "Selecting the native fight confirm must enter combat.");
        }

        private static IEnumerator OpenPicker(BeachGameFlow flow, BeachPeripheralView view)
        {
            var player = flow.transform.Find("Player");
            PlacePlayer(player, flow.transform.Find(RingPath).position);
            yield return Until(() => Node(view, 457).activeInHierarchy, 2f, "Entering the ring trigger must show the native picker.");
        }

        private static FightController CurrentFight(BeachGameFlow flow) => Field<FightController>(flow, "fight");
        private static GameObject HudObject(BeachGameFlow flow, string field) => Field<GameObject>(Field<object>(flow, "hud"), field);
        private static Button ResultButton(GameObject panel, string name) =>
            panel.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

        private static void AssertPose(Transform actor, Transform marker)
        {
            Assert.That(Vector3.Distance(actor.position, marker.position), Is.LessThan(.01f));
            Assert.That(Quaternion.Angle(actor.rotation, marker.rotation), Is.LessThan(.1f));
        }

        private static Quaternion FollowYaw(BeachNativeCombatCamera rig)
        {
            var flat = Vector3.ProjectOnPlane(rig.enemy.position - rig.player.position, Vector3.up);
            return Quaternion.LookRotation(flat) * Quaternion.Euler(0, 180f, 0);
        }

        private static void AssertCameraPose(Transform camera, Vector3 follow, Quaternion yaw, Vector3 localOffset)
        {
            Assert.That(Vector3.Distance(camera.position, follow + yaw * localOffset), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(camera.rotation, yaw * Quaternion.Euler(IdleCameraEuler)), Is.LessThan(.1f));
        }

        private static void AssertResultStartsHidden(BeachPresentationTimeline timeline, GameObject panel)
        {
            Assert.That(timeline.ResultActive, Is.True);
            float firstElapsed = Time.unscaledDeltaTime * .1f;
            Assert.That(timeline.ResultTime, Is.EqualTo(firstElapsed).Within(.00001f));
            Assert.That(timeline.TimeScale, Is.EqualTo(.1f + .9f * firstElapsed).Within(.00001f));
            Assert.That(Time.timeScale, Is.EqualTo(timeline.TimeScale).Within(.00001f));
            Assert.That(panel.transform.localScale.x, Is.Zero.Within(.0001f));
            Assert.That(panel.transform.localScale.y, Is.Zero.Within(.0001f));
        }

        private static IEnumerator RevealResultAction(BeachPresentationTimeline timeline, GameObject panel, Button button)
        {
            // Advance only the clock with the original per-frame recovery, then let the scene sample its bound clip.
            float expectedTime = timeline.ResultTime;
            float expectedScale = timeline.TimeScale;
            for (int frame = 0; frame < 336; frame++)
            {
                float elapsed = expectedScale / 60f;
                expectedTime += elapsed;
                expectedScale += (1f - expectedScale) * elapsed;
                timeline.Advance(1f / 60f, false);
            }
            Assert.That(timeline.ResultTime, Is.EqualTo(expectedTime).Within(.00001f));
            Assert.That(timeline.TimeScale, Is.EqualTo(expectedScale).Within(.00001f));
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(timeline.TimeScale).Within(.00001f));
            Assert.That(panel.transform.localScale.x, Is.GreaterThan(.01f));
            Assert.That(panel.transform.localScale.y, Is.GreaterThan(.01f));
            Assert.That(Available(button), Is.True);
            Assert.That(Mathf.Abs(button.transform.lossyScale.x), Is.GreaterThan(.0001f));
            Assert.That(Mathf.Abs(button.transform.lossyScale.y), Is.GreaterThan(.0001f));
        }
    }

    internal static class NativePlayModeScene
    {
        internal const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        internal const string RingPath = "Training beach/Original Workout world/LEVEL0/LVL1/SM_FX_Glow_Ring_01";
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static IEnumerator WithScene(bool phone, Func<BeachGameFlow, LocalStore, IEnumerator> run)
        {
            var core = CoreRoot.GetOrCreate();
            var storageProperty = typeof(CoreRoot).GetProperty(nameof(CoreRoot.Storage));
            var previousStorage = core.Storage;
            var storage = new LocalStore("BeachBoxing.NativePlayMode." + Guid.NewGuid());
            float previousScale = Time.timeScale;
            float previousDelta = Time.captureDeltaTime;
            BeachGameFlow flow = null;
            Camera camera = null;
            bool previousFocus = true;
            float previousAspect = 0f;
            try
            {
                storageProperty.SetValue(core, storage);
                Time.timeScale = 1f;
                Time.captureDeltaTime = 1f / 60f;
                yield return SceneManager.LoadSceneAsync(ScenePath);
                yield return null;
                flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
                Assert.That(flow, Is.Not.Null);
                previousFocus = Field<bool>(flow, "focused");
                SetField(flow, "focused", true);
                ConfigurePhone(flow, storage, phone);
                camera = SceneCamera(flow);
                previousAspect = camera.aspect;
                camera.aspect = 1920f / 1080f;
                storageProperty.SetValue(core, previousStorage);
                yield return null;
                yield return run(flow, storage);
            }
            finally
            {
                storageProperty.SetValue(core, previousStorage);
                if (camera) camera.aspect = previousAspect;
                if (flow)
                {
                    SetField(flow, "focused", previousFocus);
                    UnityEngine.Object.Destroy(flow.gameObject);
                }
                foreach (var key in new[] { "beachBoxing.progress", "beachBoxing.progress.backup", "FirstTimeInGame",
                    "FirstTimeStarted", "MusicVolume", "SFXVolume", "Sensitivity" })
                    storage.Delete(key);
                for (int index = 0; index < 9; index++) storage.Delete("Keybind" + index);
                storage.Save();
                Time.timeScale = previousScale;
                Time.captureDeltaTime = previousDelta;
            }
        }

        internal static void ConfigurePhone(BeachGameFlow flow, LocalStore storage, bool phone)
        {
            SetField(flow, "phone", phone);
            Call(Field<object>(flow, "stage"), "SetPhone", phone);
            var input = Field<BeachInput>(flow, "input");
            input.Phone = phone;
            input.UseTouchLayout(phone, false, false, false);
            flow.GetComponent<BeachNativeTrainingView>().SetPhone(phone);

            var progress = Field<PlayerProgress>(flow, "progress");
            var tutorial = flow.GetComponent<BeachNativeTutorialView>();
            tutorial.Bind(progress, () => new ProgressStore(storage).Save(progress), phone);
            var npcs = flow.GetComponent<BeachNativeNpcManager>();
            npcs.ResetScene(phone);
            npcs.SetBeachActive(true);
        }

        internal static Camera SceneCamera(BeachGameFlow flow) => Field<Camera>(Field<object>(flow, "stage"), "camera");
        internal static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
        internal static void SetField(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
        internal static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
        internal static GameObject Node(BeachPeripheralView view, int id) => view.Node(id).transform.gameObject;
        internal static bool Available(Button button) => button && button.isActiveAndEnabled && button.IsInteractable();
        internal static void Click(BeachPeripheralView view, int id) => Click(view.Node(id).button);

        internal static void Click(Button button)
        {
            Assert.That(Available(button), Is.True, button ? button.name + " must be available." : "Missing preplaced button.");
            ExecuteEvents.Execute(button.gameObject, Pointer(0, ScreenCenter(button.transform)), ExecuteEvents.pointerClickHandler);
        }

        internal static PointerEventData Pointer(int id, Vector2 position) => new PointerEventData(EventSystem.current)
        {
            pointerId = id,
            position = position,
            button = PointerEventData.InputButton.Left
        };

        internal static Vector2 ScreenCenter(Transform target) => RectTransformUtility.WorldToScreenPoint(null, target.position);

        internal static void Drag(BeachNativeFloatingJoystick joystick, PointerEventData pointer, Vector2 direction)
        {
            var canvas = joystick.canvas;
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var center = RectTransformUtility.WorldToScreenPoint(eventCamera, joystick.background.position);
            var radius = joystick.background.sizeDelta * (.5f * canvas.scaleFactor);
            pointer.position = center + Vector2.Scale(direction, radius);
            ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.dragHandler);
        }

        internal static void PlacePlayer(Transform player, Vector3 position)
        {
            player.position = position;
            Physics.SyncTransforms();
        }

        internal static IEnumerator Until(Func<bool> condition, float seconds, string message)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, message);
        }
    }
}
