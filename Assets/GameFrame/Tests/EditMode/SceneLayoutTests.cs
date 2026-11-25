using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnityCore.Tests
{
    public class SceneLayoutTests
    {
        [Test]
        public void BuiltInScenesContainEditableUiBeforePlay()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    Assert.Ignore("Save open scenes before running this scene layout test.");

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
                Assert.That(scenes.Length, Is.EqualTo(2));
                Assert.That(scenes[0].path, Is.EqualTo("Assets/GameFrame/Init.unity"));
                Assert.That(scenes[1].path, Is.EqualTo("Assets/BeachBoxing/Game.unity"));

                VerifyScene(scenes[0].path, (init, roots) =>
                {
                    Assert.That(init.name, Is.EqualTo("Init"));
                    var homeCanvas = roots.Single(root => root.name == "HomePanel").transform;
                    Assert.That(homeCanvas.GetComponent<Canvas>(), Is.Not.Null);
                    var startBtn = homeCanvas.GetComponentsInChildren<Button>(true).Single(b => b.name == "StartBtn");
                    Assert.That(startBtn.GetComponent<Image>().sprite, Is.Not.Null);
                });

                VerifyScene(scenes[1].path, (game, roots) =>
                {
                    Assert.That(game.name, Is.EqualTo("Game"));
                    var gameUi = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                        .Single(t => t.name == "GameUI");
                    Assert.That(gameUi.GetComponent<Canvas>(), Is.Not.Null);
                });
            }
            finally
            {
                if (setup.Any(scene => !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void VerifyScene(string path, Action<Scene, GameObject[]> verify)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            verify(scene, scene.GetRootGameObjects());
        }
    }
}
