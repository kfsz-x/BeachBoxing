using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Tests
{
    public sealed class NativeColliderParityTests
    {
        private const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        private const string WorkoutWorld = "Training beach/Original Workout world/";
        private const string WorkoutBoxing = WorkoutWorld + "LEVEL0/LVL1/Boxing Area/";
        private const string CombatWorld = "Arena/Original Combat world/";

        [TestCase("Cube (1)")]
        [TestCase("Cube (2)")]
        [TestCase("Cube (3)")]
        public void SavedWorkoutRingApproachUsesNativeGroundBoxes(string floorName)
        {
            InspectSavedScene(root =>
            {
                var floor = root.Find(WorkoutBoxing + floorName).GetComponent<BoxCollider>();
                Assert.That(floor.size, Is.EqualTo(Vector3.one));
                Assert.That(floor.center, Is.EqualTo(Vector3.zero));
                Assert.That(floor.enabled, Is.True);
                Assert.That(floor.isTrigger, Is.False);
                Assert.That(floor.gameObject.layer, Is.EqualTo(3));
            });
        }

        [TestCase(WorkoutBoxing + "Ring (3)")]
        [TestCase(WorkoutBoxing + "Ring (4)")]
        [TestCase(CombatWorld + "LEVEL0/LVL1/Boxing Area/Ring (1)")]
        public void SavedRingRopesUseNativeAsymmetricBoxes(string ringPath)
        {
            InspectSavedScene(root =>
            {
                var ropes = root.Find(ringPath).GetComponentsInChildren<BoxCollider>(true)
                    .Where(box => box.name == "Sprite").ToArray();
                Assert.That(ropes, Has.Length.EqualTo(4));
                foreach (var rope in ropes)
                {
                    Assert.That(Vector3.Distance(rope.size, new Vector3(16.013098f, 31.094793f, .19999997f)),
                        Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(rope.center, new Vector3(-5.3065534f, 10.42095f, .000005719839f)),
                        Is.LessThan(.00001f));
                    Assert.That(rope.enabled, Is.True);
                    Assert.That(rope.isTrigger, Is.False);
                    Assert.That(rope.gameObject.layer, Is.Zero);
                }
            });
        }

        [TestCase("Ring (3)")]
        [TestCase("Ring (4)")]
        public void SavedWorkoutRingBasesUseNativeGroundBoxes(string ringName)
        {
            InspectSavedScene(root =>
            {
                var ring = root.Find(WorkoutBoxing + ringName + "/Ring").GetComponent<BoxCollider>();
                Assert.That(Vector3.Distance(ring.size, new Vector3(.99999988f, .99999976f, .99999988f)),
                    Is.LessThan(.0000005f));
                Assert.That(Vector3.Distance(ring.center, new Vector3(-.000000238419f, .000000223517f, .000000543892f)),
                    Is.LessThan(.0000001f));
                Assert.That(ring.enabled, Is.True);
                Assert.That(ring.isTrigger, Is.False);
                Assert.That(ring.gameObject.layer, Is.EqualTo(3));
            });
        }

        [Test]
        public void SavedCombatWorldContainsAllNativeOuterWalls()
        {
            InspectSavedScene(root =>
            {
                var wallRoot = root.Find(CombatWorld + "InvisWalls");
                var walls = wallRoot ? wallRoot.GetComponentsInChildren<BoxCollider>(true) : Array.Empty<BoxCollider>();
                var nativeCenters = new Dictionary<string, Vector3>
                {
                    { "Cube", new Vector3(-4.984f, -2.4564f, -53.8272f) },
                    { "Cube (1)", new Vector3(-4.984f, -2.4564f, 41.6876f) },
                    { "Cube (2)", new Vector3(-47.3124f, -2.4564f, -6.069908f) },
                    { "Cube (3)", new Vector3(48.2024f, -2.4564f, -6.069696f) }
                };
                CollectionAssert.AreEquivalent(nativeCenters.Keys, walls.Select(wall => wall.gameObject.name));
                foreach (var wall in walls)
                {
                    Assert.That(wall.size, Is.EqualTo(Vector3.one));
                    Assert.That(wall.center, Is.EqualTo(Vector3.zero));
                    Assert.That(wall.enabled, Is.True);
                    Assert.That(wall.isTrigger, Is.False);
                    Assert.That(wall.gameObject.layer, Is.Zero);
                    var arenaCenter = root.Find("Arena").InverseTransformPoint(wall.transform.TransformPoint(wall.center));
                    Assert.That(Vector3.Distance(arenaCenter, nativeCenters[wall.name]), Is.LessThan(.0001f));
                }
            });
        }

        [TestCase("Chest")]
        [TestCase("Arms")]
        [TestCase("Back")]
        [TestCase("Legs")]
        public void SavedMachineInteractionUsesNativeSphere(string station)
        {
            InspectSavedScene(root =>
            {
                var sphere = root.Find("Training beach/" + station + "/Original equipment").GetComponent<SphereCollider>();
                Assert.That(sphere.radius, Is.EqualTo(15f));
                Assert.That(sphere.center, Is.EqualTo(Vector3.zero));
                Assert.That(sphere.enabled, Is.True);
                Assert.That(sphere.isTrigger, Is.True);
                Assert.That(sphere.gameObject.layer, Is.EqualTo(6));
            });
        }

        [Test]
        public void SavedFightEntryUsesNativeCapsule()
        {
            InspectSavedScene(root =>
            {
                var entry = root.Find(WorkoutWorld + "LEVEL0/LVL1/SM_FX_Glow_Ring_01").GetComponent<CapsuleCollider>();
                Assert.That(entry.radius, Is.EqualTo(1.25f));
                Assert.That(entry.height, Is.EqualTo(1.1718177f).Within(.00001f));
                Assert.That(entry.direction, Is.EqualTo(1));
                Assert.That(Vector3.Distance(entry.center, new Vector3(-.000001192093f, .5859069f, -.0000004768372f)),
                    Is.LessThan(.00001f));
                Assert.That(entry.enabled, Is.True);
                Assert.That(entry.isTrigger, Is.True);
                Assert.That(entry.gameObject.layer, Is.Zero);
            });
        }

        private static void InspectSavedScene(Action<Transform> inspect)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    Assert.Ignore("Save open scenes before inspecting saved collider geometry.");

            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = scene.GetRootGameObjects().Single(node => node.GetComponent<BeachGameFlow>() != null);
                inspect(root.transform);
            }
            finally
            {
                if (setup.Any(scene => !string.IsNullOrEmpty(scene.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
