using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class NativeTrainingViewsTests
    {
        private GameObject root;
        private BeachPeripheralView peripheral;
        private readonly Dictionary<int, NativeUiNode> nodes = new Dictionary<int, NativeUiNode>();

        [SetUp]
        public void CreatePrivateAuthoredControls()
        {
            root = new GameObject("native-training-test");
            peripheral = root.AddComponent<BeachPeripheralView>();
            foreach (int id in new[] { 881, 28, 139, 937, 159, 402, 910, 377, 804, 1119, 513, 3, 314, 372, 601 })
            {
                var target = new GameObject("source-" + id, typeof(RectTransform), typeof(Image), typeof(Button));
                target.transform.SetParent(root.transform, false);
                var text = new GameObject("text", typeof(RectTransform), typeof(Text));
                text.transform.SetParent(target.transform, false);
                nodes[id] = new NativeUiNode { sourceId = id, transform = (RectTransform)target.transform,
                    image = target.GetComponent<Image>(), button = target.GetComponent<Button>(), label = text.GetComponent<Text>() };
            }
            nodes[3].transform.SetParent(nodes[881].transform, false);
            nodes[314].transform.SetParent(nodes[881].transform, false);
            nodes[3].transform.gameObject.SetActive(false);
            nodes[314].transform.gameObject.SetActive(false);
            peripheral.nodes = new List<NativeUiNode>(nodes.Values).ToArray();
        }

        [TearDown] public void DestroyPrivateControls() { UnityEngine.Object.DestroyImmediate(root); nodes.Clear(); }

        [Test]
        public void SourceWeightButtonsResetBarAndReenterUsesTheNewRecommendation()
        {
            object view = View("BeachNativeTrainingView");
            int resets = 0;
            Call(view, "Bind", (Action<int>)(_ => resets++), (Action)(() => resets++), (Action)(() => { }));
            Call(view, "Enter", Muscle.Chest, 1000, 0);
            Assert.That(nodes[28].label.text, Is.EqualTo("25kg"));
            Assert.That(nodes[139].label.text, Is.EqualTo("25kg"));
            Call(view, "Refresh", 1000, 0, .6f);
            Assert.That(nodes[1119].image.fillAmount, Is.EqualTo(.6f));
            nodes[159].button.onClick.Invoke();
            Assert.That(nodes[28].label.text, Is.EqualTo("20kg"));
            Assert.That(nodes[513].transform.gameObject.activeSelf, Is.True);
            Assert.That(nodes[1119].image.fillAmount, Is.Zero);
            nodes[910].button.onClick.Invoke();
            Assert.That(nodes[28].label.text, Is.EqualTo("25kg"));
            Assert.That(nodes[937].label.text, Is.EqualTo("25kg"));
            Assert.That(nodes[513].transform.gameObject.activeSelf, Is.False);
            Assert.That(resets, Is.EqualTo(2));
            Call(view, "Exit");
            Call(view, "Enter", Muscle.Chest, 0, 0);
            Assert.That(nodes[28].label.text, Is.EqualTo("5kg"));
        }

        [Test]
        public void ArmsHintEndsAtFiftyAndExitOnlyHidesItsParent()
        {
            object view = View("BeachNativeTrainingView");
            Call(view, "Enter", Muscle.Arms, 49, 0);
            Assert.That(nodes[3].transform.gameObject.activeSelf, Is.True);
            Call(view, "Refresh", 50, 0, .1f);
            Assert.That(nodes[3].transform.gameObject.activeSelf, Is.False);
            Call(view, "Tick", .19f);
            Assert.That(nodes[314].transform.gameObject.activeSelf, Is.False);
            Call(view, "Tick", .011f);
            Assert.That(nodes[314].transform.gameObject.activeSelf, Is.True);
            Call(view, "Exit");
            Assert.That(nodes[881].transform.gameObject.activeSelf, Is.False);
            Assert.That(nodes[314].transform.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void WorkoutPointerDownQueuesOnePulseAndClickOrHoldingCannotRepeatIt()
        {
            object view = View("BeachNativeTrainingView");
            var touch = nodes[804].transform.gameObject.AddComponent<TouchControl>();
            view.GetType().GetField("pulseControl").SetValue(view, touch);
            Call(view, "Enter", Muscle.Chest, 0, 1);
            touch.OnPointerDown(new PointerEventData(null) { button = PointerEventData.InputButton.Left, pointerId = 1 });
            Assert.That(Call(view, "ConsumePulse"), Is.True);
            Assert.That(Call(view, "ConsumePulse"), Is.False);
            nodes[804].button.onClick.Invoke();
            Assert.That(Call(view, "ConsumePulse"), Is.False);
        }

        [Test]
        public void MovementHintResetsAtBeachEntryButOnlyFirstMotionPersists()
        {
            object view = View("BeachNativeTutorialView");
            var progress = new PlayerProgress { firstMove = 1 };
            int saves = 0;
            Call(view, "Bind", progress, (Action)(() => saves++), false);
            Call(view, "BeachEntered");
            Assert.That(nodes[601].transform.gameObject.activeSelf, Is.True);
            Call(view, "ObserveMove", Vector2.zero);
            Assert.That(nodes[601].transform.gameObject.activeSelf, Is.True);
            Call(view, "ObserveMove", new Vector2(.00002f, 0));
            Assert.That(nodes[601].transform.gameObject.activeSelf, Is.False);
            Assert.That(saves, Is.Zero);
            progress.firstMove = 0;
            Call(view, "BeachEntered");
            Call(view, "ObserveMove", Vector2.right);
            Call(view, "ObserveMove", Vector2.right);
            Assert.That(progress.firstMove, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void FirstWinHintIsConsumedAtBeachEntryIncludingPhone(bool phone, bool shown)
        {
            object view = View("BeachNativeTutorialView");
            var progress = new PlayerProgress();
            int saves = 0;
            Call(view, "Bind", progress, (Action)(() => saves++), phone);
            Call(view, "BeachEntered");
            progress.unlockedOpponent = 1;
            Call(view, "UpdateProgress");
            Assert.That(progress.keepTrainingHint, Is.Zero);
            Call(view, "BeachEntered");
            Assert.That(nodes[372].transform.gameObject.activeSelf, Is.EqualTo(shown));
            Assert.That(progress.keepTrainingHint, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Call(view, "Tick", 4.49f);
            Assert.That(nodes[372].transform.gameObject.activeSelf, Is.EqualTo(shown));
            Call(view, "Tick", .02f);
            Assert.That(nodes[372].transform.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void TutorialCompletesAtFiftyOneAndSavesOnlyOnce()
        {
            object view = View("BeachNativeTutorialView");
            var progress = new PlayerProgress { arms = 50 };
            int saves = 0;
            Call(view, "Bind", progress, (Action)(() => saves++), false);
            Call(view, "UpdateProgress");
            Assert.That(progress.completedTutorial, Is.Zero);
            progress.arms = 51;
            Call(view, "UpdateProgress");
            Call(view, "UpdateProgress");
            Assert.That(progress.completedTutorial, Is.EqualTo(1));
            Assert.That(saves, Is.EqualTo(1));
            Assert.That(view.GetType().GetProperty("Target").GetValue(view).ToString(), Is.EqualTo("Ring"));
        }

        [TestCase("SetNativeChrome", "stats,menu,interact,prompt")]
        [TestCase("SetNativeControls", "move,attack,block,joystickGraphic,tutorialFinger")]
        [TestCase("SetNativeTraining", "workout,tutorialFinger")]
        [TestCase("SetNativeOpponentMenu", "picker")]
        public void NativeOwnershipImmediatelyHidesPreplacedLegacyDuplicates(string setter, string duplicateFields)
        {
            var type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachHud");
            var hud = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            var duplicates = new List<GameObject>();
            foreach (string name in duplicateFields.Split(','))
            {
                var target = new GameObject(name, typeof(RectTransform));
                target.transform.SetParent(root.transform, false);
                object reference = name == "prompt" ? (object)target.AddComponent<Text>() : target;
                type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, reference);
                duplicates.Add(target);
            }
            var unique = new GameObject("unique-combat-and-result");
            unique.transform.SetParent(root.transform, false);
            type.GetField("combat", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, unique);
            type.GetField("result", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, unique);

            Call(hud, setter, false);
            foreach (var target in duplicates) Assert.That(target.activeSelf, Is.True, target.name);
            Call(hud, setter, true);
            foreach (var target in duplicates) Assert.That(target.activeSelf, Is.False, target.name);
            Assert.That(unique.activeSelf, Is.True, "Native ownership must retain unique combat and result UI.");
        }

        private object View(string name)
        {
            var type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing." + name);
            Assert.That(type, Is.Not.Null, name + " is missing from the runtime assembly.");
            var view = root.AddComponent(type);
            var field = type.GetField("peripheral");
            Assert.That(field, Is.Not.Null, "Bind the preplaced source nodes.");
            field.SetValue(view, peripheral);
            return view;
        }

        private static object Call(object view, string name, params object[] args)
        {
            var method = view.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Missing native view behavior " + name);
            return method.Invoke(view, args);
        }
    }
}
