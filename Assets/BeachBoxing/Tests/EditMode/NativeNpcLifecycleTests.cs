using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachBoxing.Tests
{
    public sealed class NativeNpcLifecycleTests
    {
        private GameObject root;
        private Component manager;
        private Type actorType;
        private GameObject[] pool;

        [TearDown]
        public void CleanUp()
        {
            if (root) Object.DestroyImmediate(root);
        }

        [Test]
        public void PhoneKeepsAllThreeSlotsInactive()
        {
            Create(true);
            Advance(12, 60);
            Assert.That(Count, Is.Zero);
        }

        [Test]
        public void DesktopRequiresFullThreeSecondWindowAndSpawnsThreeAtThreshold()
        {
            Create(false);
            Advance(2, 40);
            Assert.That(Count, Is.Zero);
            Advance(1, 40);
            Assert.That(Count, Is.EqualTo(3));
        }

        [Test]
        public void LowFpsReleasesEquipmentAndDoesNotRespawnTheInitialPopulation()
        {
            Create(false);
            Advance(3, 60);
            ClaimChest();
            Advance(3, 20);
            Assert.That(Count, Is.Zero);
            Assert.That(Call("IsPlayerStationAvailable", Muscle.Chest), Is.True);
            Advance(6, 60);
            Assert.That(Count, Is.Zero);
        }

        [Test]
        public void SceneResetReleasesOwnershipBeforeReusingIdenticalPoolObjects()
        {
            Create(false);
            Advance(3, 60);
            ClaimChest();
            Call("ResetScene", false);
            Assert.That(Call("IsPlayerStationAvailable", Muscle.Chest), Is.True);
            Assert.That(Count, Is.Zero);
            Call("SetBeachActive", true);
            Advance(3, 60);
            Assert.That(Count, Is.EqualTo(3));
            Assert.That(Array.TrueForAll(pool, slot => slot.activeSelf), Is.True);
        }

        [Test]
        public void OldNpcFacesTheSelectedStationWithoutWalkingBeforeStartupInteraction()
        {
            Create(false);
            var slot = pool[0];
            slot.SetActive(true);
            slot.transform.position = Vector3.right * 5f;
            var actor = slot.GetComponent(actorType);

            Invoke(actor, "FindStation", manager);
            Assert.That(Vector3.Dot(slot.transform.forward, Vector3.left), Is.GreaterThan(.99f));
            Invoke(actor, "Move", manager, .033f);

            Assert.That(slot.transform.position, Is.EqualTo(Vector3.right * 5f));
        }

        [Test]
        public void StaleInteractionDoesNotStartTravelWhileActorOwnsEquipment()
        {
            Create(false);
            var arena = new GameObject("Test arena");
            arena.transform.SetParent(root.transform);
            arena.transform.position = Vector3.right * 5f;
            var config = new SerializedObject(manager);
            config.FindProperty("arena").objectReferenceValue = arena.transform;
            config.ApplyModifiedPropertiesWithoutUndo();
            pool[0].SetActive(true);
            ClaimChest();
            var actor = pool[0].GetComponent(actorType);
            var action = actorType.GetNestedType("ActionKind", BindingFlags.NonPublic);
            Invoke(actor, "Schedule", Enum.Parse(action, "Interact"), 0d);

            Invoke(actor, "Advance", manager);
            Invoke(actor, "Move", manager, .033f);

            Assert.That(pool[0].transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(Call("IsPlayerStationAvailable", Muscle.Chest), Is.False);
        }

        [Test]
        public void EverySpawnSeedStartsMovementOrTrainingPromptly()
        {
            Create(false);
            var arena = new GameObject("Test arena");
            arena.transform.SetParent(root.transform);
            arena.transform.position = Vector3.forward * 15f;
            var config = new SerializedObject(manager);
            config.FindProperty("arena").objectReferenceValue = arena.transform;
            config.ApplyModifiedPropertiesWithoutUndo();
            var actor = pool[0].GetComponent(actorType);
            var random = UnityEngine.Random.state;
            try
            {
                foreach (bool oldNpc in new[] { false, true })
                for (int seed = 0; seed < 32; seed++)
                {
                    Call("ResetScene", false);
                    UnityEngine.Random.InitState(seed);
                    Vector3 spawn = Vector3.right * 5f;
                    Invoke(actor, "Spawn", manager, spawn, Quaternion.identity, oldNpc);
                    for (int frame = 0; frame < 20; frame++)
                    {
                        double now = frame * .033;
                        typeof(BeachNativeNpcManager).GetField("clock", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, now);
                        Invoke(actor, "Advance", manager);
                        Invoke(actor, "Move", manager, .033f);
                    }
                    bool training = !(bool)Call("IsPlayerStationAvailable", Muscle.Chest);
                    Assert.That(training || Vector3.Distance(pool[0].transform.position, spawn) > .5f,
                        Is.True, "NPC remained at spawn, seed " + seed + ", old NPC " + oldNpc);
                }
            }
            finally { UnityEngine.Random.state = random; }
        }

        [Test]
        public void ActorPhysicsUsesPlayerSizedCapsuleAtNpcScale()
        {
            Create(false);
            Invoke(pool[0].GetComponent(actorType), "ConfigureBody");
            var body = pool[0].GetComponent<Rigidbody>();
            var capsule = pool[0].GetComponent<CapsuleCollider>();
            Assert.That(pool[0].layer, Is.EqualTo(8));
            Assert.That(body.mass, Is.EqualTo(1f));
            Assert.That(body.useGravity, Is.True);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints.FreezeRotation));
            Assert.That(capsule.height * .356f, Is.EqualTo(1.784899f).Within(.000001f));
            Assert.That(capsule.center.y, Is.EqualTo(capsule.height * .5f).Within(.000001f));
            Assert.That(capsule.radius * .356f, Is.EqualTo(.267f).Within(.000001f));
            Assert.That(capsule.isTrigger, Is.False);
        }

        private void Create(bool phone)
        {
            var assembly = typeof(PlayerProgress).Assembly;
            var managerType = assembly.GetType("BeachBoxing.BeachNativeNpcManager");
            Assert.That(managerType, Is.Not.Null, "The source lifecycle manager is missing.");
            actorType = assembly.GetType("BeachBoxing.BeachNativeNpcActor");
            Assert.That(actorType, Is.Not.Null, "The reusable source NPC actor is missing.");
            root = new GameObject("NPC test");
            manager = root.AddComponent(managerType);
            pool = new GameObject[3];
            var config = new SerializedObject(manager);
            var slots = config.FindProperty("actors");
            slots.arraySize = pool.Length;
            var points = config.FindProperty("oldSpawnPoints");
            points.arraySize = 14;
            for (int i = 0; i < points.arraySize; i++)
                points.GetArrayElementAtIndex(i).objectReferenceValue = root.transform;
            for (int i = 0; i < slots.arraySize; i++)
            {
                pool[i] = new GameObject("Pool slot " + i);
                pool[i].transform.SetParent(root.transform);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = pool[i].AddComponent(actorType);
            }
            var stations = config.FindProperty("stations");
            stations.arraySize = 1;
            var chest = stations.GetArrayElementAtIndex(0);
            chest.FindPropertyRelative("playerMuscle").intValue = (int)Muscle.Chest;
            chest.FindPropertyRelative("playerPos").objectReferenceValue = root.transform;
            chest.FindPropertyRelative("exitPos").objectReferenceValue = root.transform;
            config.ApplyModifiedPropertiesWithoutUndo();
            Call("ResetScene", phone);
            Call("SetBeachActive", true);
        }

        private int Count => (int)manager.GetType().GetProperty("ActiveCount").GetValue(manager);

        private void ClaimChest()
        {
            Assert.That(Call("TryOccupy", 0, pool[0].GetComponent(actorType)), Is.True);
            Assert.That(Call("IsPlayerStationAvailable", Muscle.Chest), Is.False);
        }

        private void Advance(int seconds, int fps)
        {
            for (int i = 0; i < seconds * 20; i++) Call("Tick", .05f, (float)fps);
        }

        private object Call(string name, params object[] args) => Invoke(manager, name, args);

        private static object Invoke(Component component, string name, params object[] args)
        {
            var method = component.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, "Missing NPC behavior " + name);
            return method.Invoke(component, args);
        }
    }
}
