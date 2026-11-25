using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeBeachCameraBehaviourTests
    {
        GameObject root;
        Component camera;
        Transform player, target, view;
        readonly Vector3 idle = new Vector3(0, 9.1f, 17.8f) * .356f;

        [SetUp]
        public void SetUp()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "BeachBoxing.BeachNativeBeachCamera");
            Assert.That(type, Is.Not.Null, "Source workout camera requires independent parent follow and child interpolation.");
            root = new GameObject("source beach camera fixture");
            camera = root.AddComponent(type);
            player = Child("player", root.transform);
            target = Child("target", player);
            view = Child("view", root.transform);
            type.GetField("player").SetValue(camera, player);
            type.GetField("target").SetValue(camera, target);
            type.GetField("view").SetValue(camera, view);
            Call("Initialize", idle, new Vector3(22.639f, -180, 0));
        }

        [TearDown] public void TearDown() { if (root) UnityEngine.Object.DestroyImmediate(root); }

        [Test]
        public void SceneEntrySeedsFollowFromTheAuthoredCameraParent()
        {
            var trainingWorld = Child("source training coordinates", root.transform);
            trainingWorld.localPosition = new Vector3(5, 9, 11);
            trainingWorld.localScale = Vector3.one * 2;
            trainingWorld.localRotation = Quaternion.Euler(0, 90, 0);
            camera.GetType().GetField("trainingWorld")?.SetValue(camera, trainingWorld);
            player.position = Vector3.right * 100;

            Call("Initialize", idle, Vector3.zero);

            var expectedParent = new Vector3(70.82f, 17.72f, 14.06f);
            Assert.That(Vector3.Distance(view.position, expectedParent + idle), Is.LessThan(.0001f));
        }

        [Test]
        public void EntrySeparatesParentFollowFromChildLocalInterpolation()
        {
            player.position = Vector3.right * 10;
            Call("BeginWorkout", Vector3.one, Vector3.one * 4, new Vector3(0, 90, 0));
            Call("Step", .1f, 1);
            var expected = player.position + Vector3.Lerp(idle, Vector3.one, .5f);
            Assert.That(Vector3.Distance(view.position, expected), Is.LessThan(.0001f));
            var rotation = Quaternion.Lerp(Quaternion.Euler(22.639f, -180, 0), Quaternion.Euler(0, 90, 0), .5f);
            Assert.That(Quaternion.Angle(view.rotation, rotation), Is.LessThan(.001f));
        }

        [Test]
        public void LocalTransitionStopsAfterOneSecondWithoutSnappingToEndpoint()
        {
            Call("BeginWorkout", Vector3.one, Vector3.one * 4, Vector3.zero);
            for (int index = 0; index < 20; index++) Call("Step", .05f, 10);
            var expected = Vector3.one * 4 + (idle - Vector3.one * 4) * Mathf.Pow(.75f, 20);
            Assert.That(Vector3.Distance(view.position, expected), Is.LessThan(.0001f));
            Call("Step", 2f, 10);
            Assert.That(Vector3.Distance(view.position, expected), Is.LessThan(.0001f));
        }

        [Test]
        public void ExitSnapsStoredLocalPoseWhileTheParentKeepsFollowing()
        {
            player.position = Vector3.right * 10;
            Call("BeginWorkout", Vector3.one, Vector3.one, Vector3.zero);
            Call("Step", .1f, 1);
            player.position = Vector3.right * 20;
            Call("ExitWorkout");
            Assert.That(Vector3.Distance(view.position, Vector3.right * 10 + idle), Is.LessThan(.0001f));
            Call("Step", .01f, 1);
            Assert.That(Vector3.Distance(view.position, Vector3.right * 11.4f + idle), Is.LessThan(.0001f));
        }

        void Call(string name, params object[] args) => camera.GetType().GetMethod(name).Invoke(camera, args);
        static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}
