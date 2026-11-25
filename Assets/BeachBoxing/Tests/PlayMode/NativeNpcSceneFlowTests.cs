using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeNpcSceneFlowTests
    {
        private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private BeachGameFlow flow;
        private BeachNativeNpcManager manager;
        private object stage;

        [UnityTest]
        public IEnumerator SavedBeachBindsTheThreeOriginalActorsAndEightEquipmentRecords()
        {
            yield return Load();

            var stations = Field<BeachNativeNpcManager.Station[]>(manager, "stations");
            Assert.That(stations.Select(station => station.sourceId),
                Is.EqualTo(new[] { 4955, 4877, 4793, 4697, 4791, 4598, 4918, 4718 }));
            Assert.That(stations.Where(station => station.playerOnly).Select(station => station.playerMuscle),
                Is.EqualTo(new[] { (int)Muscle.Arms }));
            Assert.That(stations.Where(station => station.playerMuscle >= 0).Select(station => station.playerMuscle),
                Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
            Assert.That(stations.Select(station => station.holder).Distinct().Count(), Is.EqualTo(8));
            foreach (var station in stations)
            {
                Assert.That(station.playerPos, Is.Not.Null, station.sourceId.ToString());
                Assert.That(station.exitPos, Is.Not.Null, station.sourceId.ToString());
                Assert.That(station.target, Is.Not.Null, station.sourceId.ToString());
                Assert.That(station.holder, Is.Not.Null, station.sourceId.ToString());
                Assert.That(station.idle, Does.EndWith("Idle"));
                Assert.That(station.move, Does.EndWith("Workout"));
            }

            var pool = Field<BeachNativeNpcActor[]>(manager, "actors");
            Assert.That(pool.Length, Is.EqualTo(3));
            var group = flow.transform.Find("Training beach/Original ambient NPCs");
            foreach (var actor in pool)
            {
                Assert.That(actor.transform.parent, Is.EqualTo(group));
                Assert.That(actor.CompareTag("NPC"), Is.True);
                Assert.That(actor.gameObject.layer, Is.EqualTo(8));
                Assert.That(actor.GetComponent<BeachAmbientNpc>().enabled, Is.False);
                var muscles = Field<BeachNativeNpcActor.MusclePose[]>(actor, "muscles");
                Assert.That(muscles.Length, Is.EqualTo(42));
                Assert.That(muscles.Select(muscle => muscle.renderer).Distinct().Count(), Is.EqualTo(42));
                Assert.That(muscles.All(muscle => muscle.renderer && muscle.renderer.transform.IsChildOf(actor.transform)), Is.True);
                var rig = Field<Animator>(actor, "rig");
                var audio = rig.GetComponent<BeachNativeBoxingAudio>();
                Assert.That(audio.step1.clip, Is.Not.Null);
                Assert.That(audio.step2.clip, Is.EqualTo(audio.step1.clip));
            }

            Assert.That(Field<Transform[]>(manager, "oldSpawnPoints").Length, Is.EqualTo(14));
            Assert.That(Field<Transform>(manager, "newSpawnPoint").name, Is.EqualTo("Source NPC transform 1597"));
            Assert.That(Field<Transform>(manager, "arena").name, Is.EqualTo("SM_FX_Glow_Ring_01"));
        }

        [UnityTest]
        public IEnumerator FlowClaimsPlayerEquipmentAfterEntryPoseAndReleasesItAfterExitPose()
        {
            yield return Load();
            var station = Field<TrainingStation[]>(flow, "stations").Single(s => s.Muscle == Muscle.Legs);
            var equipment = PlayerEquipment(station.Muscle);
            int index = (int)station.Muscle;
            var entry = Field<Transform[]>(stage, "workoutPoses")[index];
            var exit = Field<Transform[]>(stage, "workoutExits")[index];
            var player = flow.transform.Find("Player");
            var progress = Field<PlayerProgress>(flow, "progress");
            int oldLevel = progress.prestigeLevel;
            Vector3 originalScale = equipment.holder.localScale;
            Vector3 entryBeforeScale = entry.position;
            Vector3 exitBeforeScale = exit.position;

            SetField(flow, "nearestStation", station);
            try
            {
                progress.prestigeLevel = 4;
                Call(flow, "Interact");
                Assert.That(Field<TrainingStation>(flow, "activeStation"), Is.SameAs(station));
                Assert.That(manager.IsPlayerStationAvailable(station.Muscle), Is.False);
                Assert.That(equipment.pivot.activeSelf, Is.False);
                Assert.That(Vector3.Distance(equipment.holder.localScale, originalScale * 1.45f), Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(player.position, entryBeforeScale), Is.LessThan(.0001f));

                Vector3 exitWhileScaled = exit.position;
                Assert.That(Vector3.Distance(exitWhileScaled, exitBeforeScale), Is.GreaterThan(.02f));
                // StopTraining saves progress, so restore the temporary level before exercising it.
                progress.prestigeLevel = oldLevel;
                Call(flow, "StopTraining");
                Assert.That(Field<TrainingStation>(flow, "activeStation"), Is.Null);
                Assert.That(manager.IsPlayerStationAvailable(station.Muscle), Is.True);
                Assert.That(equipment.pivot.activeSelf, Is.True);
                Assert.That(Vector3.Distance(equipment.holder.localScale, originalScale), Is.LessThan(.00001f));
                Assert.That(Vector3.Distance(exit.position, exitBeforeScale), Is.LessThan(.0001f));
                Assert.That(Vector3.Distance(player.position, exitWhileScaled), Is.LessThan(.0001f));
            }
            finally
            {
                progress.prestigeLevel = oldLevel;
                if (Field<TrainingStation>(flow, "activeStation") != null) Call(flow, "StopTraining");
            }
        }

        [UnityTest]
        public IEnumerator NpcOccupancyBlocksCachedInteractionAndHidesTheNearbyGlow()
        {
            yield return Load();
            const Muscle muscle = Muscle.Chest;
            int index = (int)muscle;
            var triggers = Field<Collider[]>(stage, "stationTriggers");
            var glows = Field<GameObject[]>(stage, "stationGlows");
            var player = flow.transform.Find("Player");
            float height = (1f + .060997963f * player.lossyScale.y) * .356f;
            player.position = triggers[index].bounds.center - Vector3.up * height;
            Physics.SyncTransforms();

            Call(flow, "LocateInteraction");
            Assert.That(Field<TrainingStation>(flow, "nearestStation")?.Muscle, Is.EqualTo(muscle));
            Assert.That(glows[index].activeSelf, Is.True);

            var stations = Field<BeachNativeNpcManager.Station[]>(manager, "stations");
            int stationIndex = System.Array.IndexOf(stations, PlayerEquipment(muscle));
            var npc = Field<BeachNativeNpcActor[]>(manager, "actors")[0];
            npc.gameObject.SetActive(true);
            Assert.That(Call(manager, "TryOccupy", stationIndex, npc), Is.True);
            Call(flow, "Interact");
            Assert.That(Field<TrainingStation>(flow, "activeStation"), Is.Null);

            Call(flow, "LocateInteraction");
            Assert.That(Field<TrainingStation>(flow, "nearestStation"), Is.Null);
            Assert.That(glows[index].activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator DesktopBeachEntrySpawnsThePoolFightSuspendsItAndPhoneEntryClearsIt()
        {
            yield return Load();
            SetField(flow, "phone", false);
            Call(flow, "EnterHome");
            Assert.That(manager.ActiveCount, Is.Zero);
            TickThreeSeconds();
            Assert.That(manager.ActiveCount, Is.EqualTo(3));
            var beach = flow.transform.Find("Training beach");

            SetField(flow, "opponentIndex", 0);
            Call(flow, "EnterFight");
            Assert.That(beach.gameObject.activeSelf, Is.False);
            double suspendedAt = Field<double>(manager, "clock");
            TickThreeSeconds();
            Assert.That(Field<double>(manager, "clock"), Is.EqualTo(suspendedAt));
            Assert.That(manager.ActiveCount, Is.EqualTo(3));

            SetField(flow, "phone", true);
            Call(flow, "EnterHome");
            Assert.That(beach.gameObject.activeSelf, Is.True);
            Assert.That(manager.ActiveCount, Is.Zero);
            TickThreeSeconds();
            Assert.That(manager.ActiveCount, Is.Zero);

            SetField(flow, "phone", false);
            Call(flow, "EnterHome");
            TickThreeSeconds();
            Assert.That(manager.ActiveCount, Is.EqualTo(3));
        }

        private BeachNativeNpcManager.Station PlayerEquipment(Muscle muscle) =>
            Field<BeachNativeNpcManager.Station[]>(manager, "stations").Single(station => station.playerMuscle == (int)muscle);

        private void TickThreeSeconds()
        {
            for (int frame = 0; frame < 60; frame++) Call(manager, "Tick", .05f, 60f);
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            flow = Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            SetField(flow, "focused", true);
            manager = flow.GetComponent<BeachNativeNpcManager>();
            Assert.That(manager, Is.Not.Null);
            // Drive its clock explicitly so Editor FPS and load duration cannot affect the assertions.
            manager.enabled = false;
            stage = Field<object>(flow, "stage");
        }

        private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, Instance).GetValue(target);
        private static void SetField(object target, string field, object value) => target.GetType().GetField(field, Instance).SetValue(target, value);
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Instance).Invoke(target, args);
    }
}
