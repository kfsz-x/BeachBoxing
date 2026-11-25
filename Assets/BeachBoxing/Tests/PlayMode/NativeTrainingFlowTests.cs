using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeTrainingFlowTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator EveryStationUsesOriginalWeightControlsAndPreservesSpeedOnWeightReset()
        {
            yield return Load();
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            typeof(BeachGameFlow).GetField("focused", Flags).SetValue(flow, true);
            var stations = (TrainingStation[])typeof(BeachGameFlow).GetField("stations", Flags).GetValue(flow);
            var view = flow.GetComponent<BeachNativeTrainingView>();
            var cycle = (TrainingCycle)typeof(BeachGameFlow).GetField("workout", Flags).GetValue(flow);
            foreach (var station in stations)
            {
                typeof(BeachGameFlow).GetField("nearestStation", Flags).SetValue(flow, station);
                Call(flow, "Interact");
                Assert.That(view.IsOpen, Is.True, "Station entry must use original881 UI.");
                Assert.That(view.Muscle, Is.EqualTo(station.Muscle));
                cycle.AdvanceWorkout(.01f, true, false, false);
                float speed = cycle.Speed;
                Assert.That(cycle.Progress, Is.GreaterThan(0));
                int kilograms = view.Weight.CurrentWeight;
                view.peripheral.Node(402).button.onClick.Invoke();
                Assert.That(view.Weight.CurrentWeight, Is.EqualTo(kilograms + 5));
                Assert.That(cycle.Progress, Is.Zero);
                Assert.That(cycle.Speed, Is.EqualTo(speed));
                view.peripheral.Node(377).button.onClick.Invoke();
                Assert.That(view.IsOpen, Is.False);
                Assert.That(typeof(BeachGameFlow).GetField("activeStation", Flags).GetValue(flow), Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator TutorialProgressUsesFiftyOneAndSourceHeadLine()
        {
            yield return Load();
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            typeof(BeachGameFlow).GetField("focused", Flags).SetValue(flow, true);
            var progress = (PlayerProgress)typeof(BeachGameFlow).GetField("progress", Flags).GetValue(flow);
            progress.unlockedOpponent = progress.completedTutorial = 0;
            progress.arms = 50;
            var tutorial = flow.GetComponent<BeachNativeTutorialView>();
            tutorial.BeachEntered();
            Call(flow, "Update");
            Assert.That(tutorial.Target, Is.EqualTo(BeachTutorialTarget.Arms));
            progress.arms = 51;
            Call(flow, "Update");
            Assert.That(progress.completedTutorial, Is.EqualTo(1));
            Assert.That(tutorial.Target, Is.EqualTo(BeachTutorialTarget.Ring));
            var line = flow.GetComponentInChildren<LineRenderer>(true);
            Assert.That(line.enabled, Is.True);
            Assert.That(line.GetPosition(1).y - flow.transform.Find("Player").position.y, Is.GreaterThan(1),
                "Original line follows player head instead of floor, independently of world normalization offset.");
        }

        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
        }

        private static void Call(BeachGameFlow flow, string method) => typeof(BeachGameFlow).GetMethod(method, Flags).Invoke(flow, null);
    }
}
