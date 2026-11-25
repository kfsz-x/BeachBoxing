using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeCombatCameraBehaviourTests
    {
        GameObject fixture;
        Component camera;
        Transform player, enemy, target, view, arena;

        [SetUp]
        public void SetUp()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == "BeachBoxing.BeachNativeCombatCamera");
            Assert.That(type, Is.Not.Null, "The shipped follow/orbit camera must replace midpoint/distance approximations.");
            fixture = new GameObject("native camera fixture");
            camera = fixture.AddComponent(type);
            arena = Child("arena", fixture.transform);
            player = Child("player", fixture.transform);
            enemy = Child("enemy", fixture.transform);
            target = Child("target", player);
            view = Child("view", fixture.transform);
            player.position = new Vector3(4.48916022f, .01779993f, 2.32112f);
            enemy.position = new Vector3(-1.68743992f, 0, -3.3286f);
            foreach (var row in new[] { ("player", player), ("enemy", enemy), ("target", target), ("view", view), ("arena", arena) })
                type.GetField(row.Item1).SetValue(camera, row.Item2);
        }

        [TearDown] public void TearDown() { if (fixture) UnityEngine.Object.DestroyImmediate(fixture); }

        [TestCase(false, 4.22178243f, 1.7622f, 6.27399473f)]
        [TestCase(true, 6.87041461f, 6.01996f, 3.90581557f)]
        public void SourceStationaryPoseKeepsThePhoneWorldToLocalUsage(bool phone, float x, float y, float z)
        {
            Call("Initialize", phone, 1);
            Call("Step", 1f, true, true);
            Assert.That(Vector3.Distance(view.position, new Vector3(x, y, z)), Is.LessThan(.002f));
            Assert.That(Quaternion.Angle(view.rotation, Quaternion.Euler(8.383f, 192.93492528f, 0)), Is.LessThan(.003f));
        }

        [Test]
        public void FighterDistanceDoesNotZoomAndInactiveOpeningDoesNotFollow()
        {
            view.position = Vector3.one * 100;
            Call("Initialize", false, 1);
            Call("Step", 1f, false, true);
            Assert.That(view.position, Is.EqualTo(Vector3.one * 100));
            Call("Step", 1f, true, true);
            var before = view.position;
            enemy.position = player.position + 2 * (enemy.position - player.position);
            Call("Step", 1f, true, true);
            Assert.That(Vector3.Distance(view.position, before), Is.LessThan(.001f));
        }

        [Test]
        public void FollowingUsesClampedDeltaTimesEightAndResultKeepsItsLastYaw()
        {
            Call("Initialize", false, 1);
            Call("Step", .01f, true, true);
            var rig = Quaternion.Euler(0, 47.55092528f, 0);
            var follow = view.position - rig * (new Vector3(-8.7f, 4.9f, 6.94f) * .356f);
            var expected = Vector3.Lerp(new Vector3(0, 4f * .356f, 0), target.position, .08f);
            Assert.That(Vector3.Distance(follow, expected), Is.LessThan(.001f));
            var rotation = view.rotation;
            player.position += Vector3.right;
            enemy.position += Vector3.left * 20;
            Call("Step", 1f, true, false);
            Assert.That(Quaternion.Angle(view.rotation, rotation), Is.LessThan(.001f));
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
