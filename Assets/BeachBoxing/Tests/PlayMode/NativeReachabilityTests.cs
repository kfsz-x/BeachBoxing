using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using static BeachBoxing.Tests.NativePlayModeScene;

namespace BeachBoxing.Tests
{
    public sealed class NativeReachabilityTests
    {
        [UnityTest]
        public IEnumerator NormalWalkFromSpawnOpensTheOpponentPickerOnEntry()
        {
            return WithScene(true, (flow, _) => WalkFromSpawn(flow, WalkToRing));
        }

        [UnityTest]
        public IEnumerator NormalWalkFromSpawnReachesTheNativeArmsInteraction()
        {
            return WithScene(true, (flow, _) => WalkFromSpawn(flow, loaded => ReachWorkout(loaded, Muscle.Arms)));
        }

        [UnityTest]
        public IEnumerator NormalWalkFromSpawnReachesTheNativeChestInteraction()
        {
            return WithScene(true, (flow, _) => WalkFromSpawn(flow, loaded => ReachWorkout(loaded, Muscle.Chest)));
        }

        [UnityTest]
        public IEnumerator NormalWalkFromSpawnReachesTheNativeBackInteraction()
        {
            return WithScene(true, (flow, _) => WalkFromSpawn(flow, loaded => ReachWorkout(loaded, Muscle.Back)));
        }

        [UnityTest]
        public IEnumerator NormalWalkFromSpawnReachesTheNativeLegsInteraction()
        {
            return WithScene(true, (flow, _) => WalkFromSpawn(flow, loaded => ReachWorkout(loaded, Muscle.Legs)));
        }

        [UnityTest]
        public IEnumerator WalkingThroughNpcBodiesStillStopsAtSolidGroundGeometry()
        {
            // Isolated probes in the active scene, clear of any authored level colliders.
            var origin = new Vector3(300, 1000, 300);
            var actor = new GameObject("Motor probe actor");
            var npc = new GameObject("Motor probe NPC");
            var ground = new GameObject("Motor probe Ground");
            try
            {
                actor.transform.position = origin;
                npc.layer = 8;
                npc.transform.position = origin + new Vector3(1, 1, 0);
                npc.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 1f);
                ground.layer = 3;
                ground.transform.position = origin + new Vector3(2.5f, 1, 0);
                ground.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 1f);
                Physics.SyncTransforms();

                BeachMotor.Translate(actor.transform, Vector3.right * 3, new Rect(290, 290, 20, 20));

                Assert.That(actor.transform.position.x - origin.x, Is.EqualTo(2.123f).Within(.025f),
                    "NPC layer 8 must not block the cast. The solid layer 3 wall still limits movement.");
            }
            finally
            {
                UnityEngine.Object.Destroy(actor);
                UnityEngine.Object.Destroy(npc);
                UnityEngine.Object.Destroy(ground);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator WalkingUsesThePlacedCapsulesScaledRadius()
        {
            var origin = new Vector3(330, 1000, 330);
            var actor = new GameObject("Motor capsule probe actor");
            var shape = new GameObject("PlayerCollider");
            shape.transform.SetParent(actor.transform, false);
            var wall = new GameObject("Motor capsule probe wall");
            try
            {
                actor.transform.position = origin;
                shape.transform.localScale = new Vector3(2f, 1f, 2f);
                var capsule = shape.AddComponent<CapsuleCollider>();
                capsule.radius = .35f;
                capsule.height = 2f;
                capsule.center = Vector3.up;
                wall.layer = 3;
                wall.transform.position = origin + new Vector3(2.5f, 1, 0);
                wall.AddComponent<BoxCollider>().size = new Vector3(.2f, 3f, 2f);
                Physics.SyncTransforms();

                BeachMotor.Translate(actor.transform, Vector3.right * 3, new Rect(320, 320, 20, 20));

                Assert.That(actor.transform.position.x - origin.x, Is.EqualTo(1.69f).Within(.025f),
                    "The cast should stop a 0.7-unit world-radius capsule before the wall.");
            }
            finally
            {
                UnityEngine.Object.Destroy(actor);
                UnityEngine.Object.Destroy(wall);
            }
            yield return null;
        }

        private static IEnumerator WalkFromSpawn(BeachGameFlow flow, Func<BeachGameFlow, IEnumerator> route)
        {
            var actor = flow.transform.Find("Player").GetComponent<Rigidbody>();
            var spawn = flow.transform.Find("Beach spawn");
            Assert.That(actor, Is.Not.Null, "The saved scene must provide the real roaming body.");
            Assert.That(actor.isKinematic, Is.False);
            Assert.That(HorizontalDistance(actor.position, spawn.position), Is.LessThan(.01f));
            yield return route(flow);
        }

        private static IEnumerator WalkToRing(BeachGameFlow flow)
        {
            var root = flow.transform;
            var player = root.Find("Player");
            var entry = root.Find(RingPath).GetComponent<CapsuleCollider>();
            var center = entry.transform.TransformPoint(entry.center);
            var view = flow.GetComponent<BeachPeripheralView>();
            var opponents = flow.GetComponent<BeachNativeOpponentView>();
            var picker = Node(view, 457);
            Assert.That(picker.activeInHierarchy, Is.False);

            yield return DriveTowards(flow, center, () => picker.activeInHierarchy);
            Assert.That(picker.activeInHierarchy, Is.True,
                $"The picker should open from movement alone. Player ended at {player.position}, entry center {center}.");
            Assert.That(HorizontalDistance(player.position, center), Is.LessThan(2.06f));
            AssertNoInteraction(view);

            yield return Until(() => view.Node(214).button.isActiveAndEnabled, 2f,
                "The picker close button must become available after opening.");
            Click(view, 214);
            yield return Until(() => !picker.activeInHierarchy && !opponents.IsClosing, 2f,
                "Closing the source picker must finish its animation before roaming resumes.");
            for (int frame = 0; frame < 12; frame++) yield return null;
            Assert.That(picker.activeInHierarchy, Is.False, "Closing inside the trigger must not reopen the picker.");
            AssertNoInteraction(view);

            var outside = center + Vector3.forward * 4f;
            yield return DriveTowards(flow, outside, () => HorizontalDistance(player.position, outside) < .25f);
            Assert.That(HorizontalDistance(player.position, outside), Is.LessThan(.25f));
            Assert.That(HorizontalDistance(player.position, center), Is.GreaterThan(3f));
            Assert.That(picker.activeInHierarchy, Is.False);
            AssertNoInteraction(view);
            yield return DriveTowards(flow, center, () => picker.activeInHierarchy);
            Assert.That(picker.activeInHierarchy, Is.True, "A fresh trigger entry must open the picker again.");
            AssertNoInteraction(view);
        }

        private static IEnumerator ReachWorkout(BeachGameFlow flow, Muscle muscle)
        {
            var root = flow.transform;
            var player = root.Find("Player");
            var equipment = root.Find($"Training beach/{muscle}/Original equipment");
            var glow = equipment.Find("GlowRing").gameObject;
            var view = flow.GetComponent<BeachPeripheralView>();
            var workout = flow.GetComponent<BeachNativeTrainingView>();
            Assert.That(glow.activeInHierarchy, Is.False);
            AssertNoInteraction(view);

            var center = equipment.position;
            var aisle = new Vector3(.5f, center.y, center.z);
            yield return DriveTowards(flow, aisle, () => HorizontalDistance(player.position, aisle) < .25f);
            Assert.That(HorizontalDistance(player.position, aisle), Is.LessThan(.25f), $"Could not reach the aisle for {muscle}.");
            yield return DriveTowards(flow, center, () => glow.activeInHierarchy);
            Assert.That(glow.activeInHierarchy, Is.True,
                $"The {muscle} glow should respond on approach. Player ended at {player.position}, station {center}.");
            Assert.That(Available(view.Node(1057).button), Is.True);
            Assert.That(Node(view, 1110).activeInHierarchy, Is.False);
            Assert.That(HorizontalDistance(player.position, center), Is.InRange(1.6f, 3.82f));
            foreach (Muscle other in Enum.GetValues(typeof(Muscle)))
                if (other != muscle)
                    Assert.That(root.Find($"Training beach/{other}/Original equipment/GlowRing").gameObject.activeInHierarchy, Is.False);

            Click(view, 1057);
            Assert.That(workout.IsOpen, Is.True);
            Assert.That(workout.Muscle, Is.EqualTo(muscle));
            var pose = equipment.Find("Workout pose");
            Assert.That(Vector3.Distance(player.position, pose.position), Is.LessThan(.02f));
            yield return null;
            Assert.That(glow.activeInHierarchy, Is.False);
            AssertNoInteraction(view);

            Click(view, 377);
            Assert.That(workout.IsOpen, Is.False);
            Assert.That(Vector3.Distance(player.position, equipment.Find("ExitPos").position), Is.LessThan(.02f));
            yield return null;

            yield return DriveTowards(flow, aisle, () => HorizontalDistance(player.position, aisle) < .25f);
            Assert.That(HorizontalDistance(player.position, aisle), Is.LessThan(.25f), $"Could not leave {muscle} for the aisle.");
            Assert.That(glow.activeInHierarchy, Is.False);
            AssertNoInteraction(view);
        }

        private static IEnumerator DriveTowards(BeachGameFlow flow, Vector3 target, Func<bool> reached)
        {
            if (reached()) yield break;
            var body = flow.transform.Find("Player").GetComponent<Rigidbody>();
            var camera = SceneCamera(flow).transform;
            var joystick = flow.GetComponent<BeachNativeMobileControls>().beachJoystick;
            var pointer = Pointer(21, new Vector2(Screen.width * .15f, Screen.height * .2f));
            var start = body.position;
            Assert.That(body.isKinematic, Is.False);
            Assert.That(joystick.isActiveAndEnabled, Is.True);
            ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            try
            {
                Assert.That(joystick.background.gameObject.activeInHierarchy, Is.True, "The source joystick must accept a fresh owner.");
                for (int frame = 0; frame < 360 && !reached(); frame++)
                {
                    var towards = Vector3.ProjectOnPlane(target - body.position, Vector3.up).normalized;
                    var right = Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized;
                    var forward = Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
                    Drag(joystick, pointer, new Vector2(Vector3.Dot(towards, right), Vector3.Dot(towards, forward)));
                    if (frame == 0)
                        Assert.That(joystick.Direction.sqrMagnitude, Is.EqualTo(1f).Within(.001f));
                    yield return null;
                }
                Assert.That(HorizontalDistance(body.position, start), Is.GreaterThan(.01f), "The native gesture must move the preplaced Rigidbody.");
            }
            finally { ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerUpHandler); }
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right) =>
            Vector3.ProjectOnPlane(left - right, Vector3.up).magnitude;

        private static void AssertNoInteraction(BeachPeripheralView view)
        {
            Assert.That(Node(view, 1057).activeInHierarchy, Is.False);
            Assert.That(Node(view, 1110).activeInHierarchy, Is.False, "The dormant source hand must stay inactive.");
        }
    }
}
