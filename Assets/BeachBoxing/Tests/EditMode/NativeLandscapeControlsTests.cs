using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing.Tests
{
    public sealed class NativeLandscapeControlsTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("Native landscape controls fixture", typeof(RectTransform), typeof(Canvas));

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(root);

        [Test]
        public void FloatingJoystickUsesCanvasScaledRadiusAndPreservesHitRect()
        {
            var hit = Child("hit", root.transform);
            var marker = Child("landscape", root.transform);
            var background = Child("background", hit);
            var handle = Child("handle", background);
            hit.sizeDelta = new Vector2(10000, 10000);
            hit.anchorMin = hit.anchorMax = new Vector2(.3f, .6f);
            hit.pivot = new Vector2(.2f, .8f);
            marker.position = new Vector3(230, 180);
            marker.localScale = new Vector3(.4f, .4f, 1);
            background.sizeDelta = new Vector2(256, 256);
            background.anchorMin = background.anchorMax = Vector2.zero;
            var canvas = root.GetComponent<Canvas>();
            canvas.scaleFactor = 2;
            var joystick = Add("BeachNativeFloatingJoystick", hit.gameObject);
            Set(joystick, "canvas", canvas);
            Set(joystick, "background", background);
            Set(joystick, "handle", handle);
            Set(joystick, "landscape", marker);
            Call(joystick, "ApplyLandscapePose");
            Assert.That(hit.sizeDelta, Is.EqualTo(new Vector2(10000, 10000)));
            Assert.That(hit.anchorMin, Is.EqualTo(new Vector2(.3f, .6f)));
            Assert.That(hit.pivot, Is.EqualTo(new Vector2(.2f, .8f)));
            Assert.That(hit.localScale, Is.EqualTo(marker.localScale));
            Assert.That(hit.position, Is.EqualTo(marker.position));

            var pointer = new PointerEventData(null) { pointerId = 3, position = new Vector2(320, 260) };
            Call(joystick, "OnPointerDown", pointer);
            Assert.That(background.gameObject.activeSelf, Is.True);
            pointer.position += new Vector2(128, 0);
            Call(joystick, "OnDrag", pointer);
            Assert.That(Direction(joystick).x, Is.EqualTo(.5f).Within(.001f));
            Assert.That(handle.anchoredPosition.x, Is.EqualTo(64).Within(.01f));
            pointer.position += new Vector2(1000, 1000);
            Call(joystick, "OnDrag", pointer);
            Assert.That(Direction(joystick).magnitude, Is.EqualTo(1).Within(.001f));
            Call(joystick, "ResetInput");
            Assert.That(background.gameObject.activeSelf, Is.False);
            Assert.That(Direction(joystick), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CombatGateResetsHeldActionsAndEachPointerReleasesItsOwnAction()
        {
            var view = Add("BeachNativeMobileControls", root);
            bool gate = false;
            Call(view, "Bind", (Func<bool>)(() => false), (Func<bool>)(() => gate), null, null, null);
            Call(view, "Render", true, BeachPhase.Fight, true, false, true);
            var punch = new PointerEventData(null) { pointerId = 4 };
            var block = new PointerEventData(null) { pointerId = 8 };
            Call(view, "PressPunch", punch);
            Assert.That(Held(view, "PunchHeld"), Is.False);
            gate = true;
            Call(view, "PressPunch", punch);
            Call(view, "PressBlock", block);
            Call(view, "ReleasePunch", block);
            Assert.That(Held(view, "PunchHeld"), Is.True);
            Call(view, "ReleaseBlock", block);
            Assert.That(Held(view, "BlockHeld"), Is.False);
            Assert.That(Held(view, "PunchHeld"), Is.True);
            Call(view, "Render", true, BeachPhase.Fight, true, false, false);
            Assert.That(Held(view, "PunchHeld"), Is.False);
        }

        [Test]
        public void WorkoutPulsesOnEveryPointerDownWhileAnotherFingerIsHeld()
        {
            var control = root.AddComponent<TouchControl>();
            var mode = typeof(TouchControl).GetField("pulseEveryPointerDown");
            Assert.That(mode, Is.Not.Null, "Expected source workout down-event mode.");
            mode.SetValue(control, true);
            var first = new PointerEventData(null) { pointerId = 3 };
            var second = new PointerEventData(null) { pointerId = 6 };
            control.OnPointerDown(first);
            Assert.That(control.ConsumePress(), Is.True);
            control.OnPointerDown(second);
            control.OnPointerUp(second);
            Assert.That(control.ConsumePress(), Is.True);
            Assert.That(control.Held, Is.True);
        }

        [Test]
        public void BeachUsesGreenInteractInBothLayoutsAndKeepsPhoneControlsInWorkout()
        {
            var view = root.AddComponent<BeachPeripheralView>();
            var mobile = Child("phone", root.transform);
            var main = Child("main", mobile);
            var hand = Child("hand", main);
            var jump = Child("jump", main);
            var holder = Child("green holder", root.transform);
            var interact = Child("green interact", holder);
            main.gameObject.SetActive(false);
            hand.gameObject.SetActive(false);
            view.nodes = new[] { Node(559, mobile), Node(140, main), Node(1110, hand), Node(532, jump),
                Node(417, holder), Node(1057, interact) };
            var progress = new PlayerProgress();
            view.Render(progress, BeachPhase.Training, false, true, false);
            Assert.That(mobile.gameObject.activeSelf, Is.False);
            Assert.That(interact.gameObject.activeInHierarchy, Is.True);
            view.Render(progress, BeachPhase.Training, false, true, true);
            Assert.That(mobile.gameObject.activeSelf, Is.True);
            Assert.That(interact.gameObject.activeInHierarchy, Is.True);
            Assert.That(main.gameObject.activeSelf, Is.False);
            Assert.That(hand.gameObject.activeSelf, Is.False);
            Assert.That(jump.gameObject.activeSelf, Is.True);
            view.Render(progress, BeachPhase.Training, true, true, true);
            Assert.That(mobile.gameObject.activeSelf, Is.True);
            Assert.That(interact.gameObject.activeInHierarchy, Is.False);
        }

        private static NativeUiNode Node(int id, RectTransform rect) => new NativeUiNode { sourceId = id, transform = rect };

        private static RectTransform Child(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static Component Add(string name, GameObject owner)
        {
            var type = typeof(BeachPeripheralView).Assembly.GetType("BeachBoxing." + name);
            Assert.That(type, Is.Not.Null, $"Expected prebound native control component {name}.");
            return owner.AddComponent(type);
        }

        private static void Set(Component target, string name, object value) =>
            target.GetType().GetField(name).SetValue(target, value);

        private static void Call(Component target, string name, params object[] args) =>
            target.GetType().GetMethod(name).Invoke(target, args);

        private static bool Held(Component target, string name) => (bool)target.GetType().GetProperty(name).GetValue(target);
        private static Vector2 Direction(Component target) => (Vector2)target.GetType().GetProperty("Direction").GetValue(target);
    }
}
