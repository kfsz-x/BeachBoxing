using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Tests
{
    public sealed class EntryTests
    {
        private const string InitPath = "Assets/GameFrame/Init.unity";
        private const string PlayPath = "Assets/BeachBoxing/Game.unity";

        [Test]
        public void GameplayObjectsAreSavedAndEditableBeforePlay()
        {
            var scene = SceneManager.GetSceneByPath(PlayPath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(PlayPath, OpenSceneMode.Additive);
            try
            {
                var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                Assert.That(transforms.Length, Is.GreaterThan(100));
                foreach (string name in new[] { "Training beach", "Arena", "Player", "Opponent", "GameUI", "Gameplay camera", "Sun", "Beach spawn" })
                    Assert.That(transforms.Any(item => item.name == name), Is.True, name);
                Assert.That(transforms.Count(item => item.name == "Original Kyle"), Is.EqualTo(1));
                Assert.That(transforms.Count(item => item.name == "Original Ramez"), Is.EqualTo(1));
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuildSettingsRoutesInitToGame()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Assert.That(scenes, Is.EqualTo(new[] { InitPath, PlayPath }));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(InitPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayPath), Is.Not.Null);
        }
    }
}
