using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeLandscapeFlowTests
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [UnityTest]
        public IEnumerator SourceJoystickMovesTheActualActorAndReleasesBehindSettings()
        {
            yield return WithScene(CheckRoaming);
        }

        [UnityTest]
        public IEnumerator SourcePunchAndBlockReachCombatAndMenuClearsBothPointers()
        {
            yield return WithScene(CheckCombat);
        }

        [UnityTest]
        public IEnumerator OpeningHandsOffAtFourAndAHalfRenderedSecondsWithoutFixedTicks()
        {
            yield return WithScene(CheckOpeningHandoff);
        }

        static IEnumerator CheckOpeningHandoff(BeachGameFlow flow)
        {
            float fixedStep = Time.fixedDeltaTime;
            try
            {
                Time.fixedDeltaTime = 10f;
                Call(flow, "EnterFight");
                var timeline = (BeachPresentationTimeline)Field(flow, "presentationTimeline");
                var fight = (FightController)Field(flow, "fight");
                var opening = (BeachPresentationView)Field(flow, "presentation");
                Assert.That(opening.introCamera.IsBound, Is.True);
                Time.timeScale = 0f;
                yield return null;
                yield return null;
                Assert.That(timeline.OpeningTime, Is.Zero, "Paused render frames must leave the opening frozen.");
                Time.timeScale = 3f;
                float start = Time.time;
                float deadline = Time.realtimeSinceStartup + 6f;
                while (!timeline.OpeningComplete && Time.realtimeSinceStartup < deadline) yield return null;

                Assert.That(timeline.OpeningComplete, Is.True);
                Assert.That(fight.Countdown, Is.Zero, "Gameplay must be available as soon as the rendered opening completes.");
                Assert.That(Time.time - start, Is.LessThan(5f), "The opening must advance in scaled time.");
                var controls = flow.GetComponent<BeachNativeMobileControls>();
                var feedback = flow.GetComponent<BeachNativeCombatFeedback>();
                var strike = Pointer(301, Vector2.zero);
                controls.PressPunch(strike);
                var nativeState = (ControlsVisible: controls.combatRoot.activeInHierarchy, TouchAccepted: controls.PunchHeld,
                    FeedbackVisible: feedback.screen.activeInHierarchy);
                controls.ReleasePunch(strike);
                Assert.That(nativeState, Is.EqualTo((true, true, true)),
                    "The source combat controls, touch gate, and feedback screen must open on the threshold frame.");
                Call(flow, "LateUpdate");
                var rig = opening.introCamera.target;
                var followRotation = rig.rotation;
                opening.FrameOpeningCamera(4.5f);
                Assert.That(Quaternion.Angle(followRotation, rig.rotation), Is.GreaterThan(1f),
                    "The final opening sample must not overwrite the combat follow camera at the handoff.");
            }
            finally { Time.fixedDeltaTime = fixedStep; }
        }

        static IEnumerator CheckRoaming(BeachGameFlow flow)
        {
            var controls = flow.GetComponent<BeachNativeMobileControls>();
            controls.Render(true, BeachPhase.Training, false, true, false);
            var stage = Field(flow, "stage");
            var boxer = stage.GetType().GetField("Player", Flags).GetValue(stage);
            var actor = (Transform)boxer.GetType().GetField("Root", Flags).GetValue(boxer);
            yield return new WaitForSeconds(.15f);
            var before = actor.position;
            var pointer = Pointer(101, new Vector2(300, 200));
            controls.beachJoystick.OnPointerDown(pointer);
            pointer.position += Vector2.up * 100;
            controls.beachJoystick.OnDrag(pointer);
            yield return new WaitForSeconds(.15f);
            var displacement = actor.position - before;
            displacement.y = 0;
            Assert.That(displacement.magnitude, Is.GreaterThan(.2f), "The source joystick must drive the preplaced Rigidbody.");
            Call(flow, "TogglePause");
            Assert.That(controls.beachJoystick.Direction, Is.EqualTo(Vector2.zero));
            Assert.That(controls.beachJoystick.background.gameObject.activeSelf, Is.False);
            yield return new WaitForSecondsRealtime(.4f);
            var settings = flow.GetComponent<BeachNativeSettingsView>();
            var hits = new System.Collections.Generic.List<RaycastResult>();
            var input = Pointer(102, RectTransformUtility.WorldToScreenPoint(null, settings.nameInput.transform.position));
            EventSystem.current.RaycastAll(input, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.transform.IsChildOf(settings.nameInput.transform), Is.True,
                "The full-screen transparent joystick must stay behind the original menu input.");
        }

        static IEnumerator CheckCombat(BeachGameFlow flow)
        {
            Call(flow, "EnterFight");
            var fight = (FightController)Field(flow, "fight");
            var timeline = (BeachPresentationTimeline)Field(flow, "presentationTimeline");
            timeline.Advance(BeachPresentationTimeline.OpeningDuration, false);
            var controls = flow.GetComponent<BeachNativeMobileControls>();
            controls.Render(true, BeachPhase.Fight, true, false, true);
            yield return null;
            float stamina = fight.Player.Stamina;
            controls.PressPunch(Pointer(201, Vector2.zero));
            yield return new WaitForSeconds(.15f);
            Assert.That(fight.Player.Stamina, Is.LessThan(stamina), "The source punch down/up events must reach FightController.");
            controls.PressBlock(Pointer(202, Vector2.zero));
            yield return new WaitForFixedUpdate();
            Assert.That(fight.Player.Blocking, Is.True);
            controls.ReleasePunch(Pointer(201, Vector2.zero));
            Assert.That(controls.BlockHeld, Is.True);
            Call(flow, "TogglePause");
            yield return new WaitForSecondsRealtime(.4f);
            Call(flow, "TogglePause");
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(controls.PunchHeld || controls.BlockHeld, Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(fight.Player.Blocking, Is.False);
        }

        static IEnumerator WithScene(Func<BeachGameFlow, IEnumerator> run)
        {
            var core = CoreRoot.GetOrCreate();
            var storageProperty = typeof(CoreRoot).GetProperty(nameof(CoreRoot.Storage));
            var original = core.Storage;
            var temporary = new LocalStore("BeachBoxing.NativeControls." + Guid.NewGuid());
            BeachGameFlow flow = null;
            try
            {
                storageProperty.SetValue(core, temporary);
                new ProgressStore(temporary).Save(new PlayerProgress { generatedName = true, playerName = "Control Test", unlockedOpponent = 1 });
                yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
                yield return null;
                flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
                typeof(BeachGameFlow).GetField("phone", Flags).SetValue(flow, true);
                typeof(BeachGameFlow).GetField("focused", Flags).SetValue(flow, true);
                ((BeachInput)Field(flow, "input")).UseTouchLayout(true, false, false, false);
                yield return run(flow);
            }
            finally
            {
                storageProperty.SetValue(core, original);
                if (flow) UnityEngine.Object.Destroy(flow.gameObject);
                foreach (string key in new[] { "beachBoxing.progress", "beachBoxing.progress.backup", "FirstTimeInGame", "FirstTimeStarted", "MusicVolume", "SFXVolume", "Sensitivity" }) temporary.Delete(key);
                for (int index = 0; index < 9; index++) temporary.Delete("Keybind" + index);
                temporary.Save();
                Time.timeScale = 1;
            }
        }

        static object Field(BeachGameFlow target, string name) => typeof(BeachGameFlow).GetField(name, Flags).GetValue(target);
        static void Call(BeachGameFlow target, string name) => typeof(BeachGameFlow).GetMethod(name, Flags).Invoke(target, null);
        static PointerEventData Pointer(int id, Vector2 position) => new PointerEventData(EventSystem.current)
            { pointerId = id, position = position, button = PointerEventData.InputButton.Left };
    }
}
