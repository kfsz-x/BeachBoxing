using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class RemainingNativeSceneTests
    {
        private const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";

        [Test]
        public void EveryPlayableOpponentAndPortraitExistsInTheSavedScene()
        {
            WithScene(flow =>
            {
                var catalog = flow.GetComponent<NativeOpponentCatalog>();
                Assert.That(catalog, Is.Not.Null, "The scene must own the complete native opponent catalog before Play.");
                Assert.That(catalog.Profiles.Length, Is.EqualTo(45));
                Assert.That(catalog.Models.Length, Is.EqualTo(45));
                Assert.That(catalog.Portraits.Length, Is.EqualTo(45));
                Assert.That(catalog.Models.All(model => model && model.gameObject.scene == flow.gameObject.scene), Is.True);
                Assert.That(catalog.Portraits.All(sprite => sprite), Is.True);
                Assert.That(catalog.Models.Select(model => model.GetInstanceID()).Distinct().Count(), Is.EqualTo(45));
                Assert.That(catalog.ComingSoonPortrait, Is.Not.Null);
                var data = new SerializedObject(flow);
                Assert.That(data.FindProperty("stage").FindPropertyRelative("Enemy").FindPropertyRelative("models").arraySize,
                    Is.EqualTo(45), "Combat selection must consume the preplaced catalog models.");
                Assert.That(data.FindProperty("hud").FindPropertyRelative("portraits").arraySize, Is.EqualTo(45));
            });
        }

        [Test]
        public void SourceSettingsSlidersAndNameInputExistBeforePlay()
        {
            WithScene(flow =>
            {
                var view = flow.GetComponent<BeachPeripheralView>();
                Assert.That(view.Node(80)?.transform, Is.Not.Null, "Restore the original PAUSE MENU tree.");
                foreach (int id in new[] { 166, 705 })
                {
                    var slider = view.Node(id)?.transform?.GetComponent<Slider>();
                    Assert.That(slider, Is.Not.Null, "Missing original volume slider " + id);
                    Assert.That(slider.minValue, Is.Zero);
                    Assert.That(slider.maxValue, Is.EqualTo(2));
                    Assert.That(slider.fillRect && slider.handleRect, Is.True);
                }
                var input = view.Node(458)?.transform?.GetComponent<InputField>();
                Assert.That(input, Is.Not.Null);
                Assert.That(input.textComponent, Is.TypeOf<OriginalSdfText>());
                Assert.That(input.characterLimit, Is.Zero);
                foreach (int id in new[] { 358, 641, 1105, 1212, 844, 920, 439, 809, 1183 })
                    Assert.That(view.Node(id)?.button, Is.Not.Null, "Missing original keybind button " + id);
            });
        }

        [Test]
        public void WeightButtonsAndWorkoutTouchArePreplaced()
        {
            WithScene(flow =>
            {
                var view = flow.GetComponent<BeachPeripheralView>();
                foreach (int id in new[] { 159, 402, 910, 804 })
                    Assert.That(view.Node(id)?.button, Is.Not.Null, "Missing original workout control " + id);
                Assert.That(view.Node(513)?.transform, Is.Not.Null, "The source recommended-weight tip must exist.");
            });
        }

        [Test]
        public void SourceTutorialHintsExistBeforePlay()
        {
            WithScene(flow =>
            {
                var view = flow.GetComponent<BeachPeripheralView>();
                foreach (int id in new[] { 601, 372, 3, 314 })
                    Assert.That(view.Node(id)?.transform, Is.Not.Null, "Missing source tutorial object " + id);
            });
        }

        private static void WithScene(Action<BeachGameFlow> run)
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                var flow = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BeachGameFlow>(true)).Single();
                run(flow);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }
    }
}
