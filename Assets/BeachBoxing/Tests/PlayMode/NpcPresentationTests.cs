using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NpcPresentationTests
    {
        [UnityTest]
        public IEnumerator DesktopNpcsStandOnTheBeachAndFitFixedSizeEquipment()
        {
            yield return NativePlayModeScene.WithScene(false, (flow, _) => CheckPresentation(flow));
        }

        private static IEnumerator CheckPresentation(BeachGameFlow flow)
        {
            var manager = flow.GetComponent<BeachNativeNpcManager>();
            manager.enabled = false;
            manager.ResetScene(false);
            manager.SetBeachActive(true);
            var connected = NativePlayModeScene.Field<GameObject>(manager, "connectedAlert");
            var disconnected = NativePlayModeScene.Field<GameObject>(manager, "disconnectedAlert");
            for (int i = 0; i < 61; i++)
            {
                NativePlayModeScene.Call(manager, "Tick", .05f, 60f);
                Assert.That(connected.activeSelf || disconnected.activeSelf, Is.False);
            }
            Assert.That(manager.ActiveCount, Is.EqualTo(3));
            manager.ResetScene(false);

            var player = flow.transform.Find("Player");
            var workoutModel = player.Find("Original Workout Player");
            var pool = NativePlayModeScene.Field<BeachNativeNpcActor[]>(manager, "actors");
            var stations = NativePlayModeScene.Field<BeachNativeNpcManager.Station[]>(manager, "stations");
            for (int i = 0; i < pool.Length; i++)
            {
                var actor = pool[i];
                var start = player.position + Vector3.right * (1.5f + i);
                NativePlayModeScene.Call(actor, "Spawn", manager, start, Quaternion.identity, false);
                Assert.That(actor.transform.lossyScale.x, Is.EqualTo(workoutModel.lossyScale.x).Within(.0001f));
            }
            for (int i = 0; i < 55; i++) yield return new WaitForFixedUpdate();
            foreach (var actor in pool)
            {
                var body = actor.GetComponent<Rigidbody>();
                Assert.That(Mathf.Abs(body.velocity.y), Is.LessThan(.05f), actor.name);
                Assert.That(Physics.Raycast(actor.transform.position + Vector3.up, Vector3.down, out var ground,
                    3f, ~(1 << actor.gameObject.layer), QueryTriggerInteraction.Ignore), Is.True, actor.name);
                Assert.That(SoleHeight(actor) - ground.point.y, Is.EqualTo(0f).Within(.035f), actor.name);
            }
            // Turn the camera and the actors independently to catch inherited or mirrored label rotation.
            var camera = NativePlayModeScene.SceneCamera(flow);
            flow.enabled = false;
            camera.transform.parent.Rotate(12, 65, 0);
            for (int i = 0; i < pool.Length; i++)
                pool[i].transform.rotation = Quaternion.Euler(0, 90f + i * 65f, 0);
            yield return new WaitForEndOfFrame();
            var playerTitle = player.Find("Original title canvas");
            foreach (var actor in pool)
            {
                AssertNameplate(actor, camera);
                Assert.That(actor.transform.Find("TitleCanvas").lossyScale.x, Is.EqualTo(playerTitle.lossyScale.x).Within(.0001f));
            }

            for (int i = 0; i < stations.Length; i++)
            {
                var station = stations[i];
                if (station.playerOnly || station.unavailableAtStart) continue;
                var actor = pool[i % pool.Length];
                Vector3 scale = station.holder.localScale;
                Vector3 position = station.holder.localPosition;
                Vector3 entry = station.playerPos.position;
                NativePlayModeScene.Call(manager, "TryOccupy", i, actor);
                Assert.That(NativePlayModeScene.Field<BeachNativeNpcActor>(station, "npc"), Is.EqualTo(actor));
                yield return new WaitForFixedUpdate();
                yield return new WaitForEndOfFrame();
                AssertNameplate(actor, camera);
                Assert.That(station.holder.localScale, Is.EqualTo(scale));
                Assert.That(station.holder.localPosition, Is.EqualTo(position));
                Assert.That(Vector3.Distance(actor.transform.position, entry), Is.LessThan(.01f));
                Assert.That(actor.GetComponent<Rigidbody>().isKinematic, Is.True);
                NativePlayModeScene.Call(manager, "Release", actor);
                Assert.That(actor.GetComponent<Rigidbody>().isKinematic, Is.False);
                Assert.That(actor.GetComponent<Rigidbody>().useGravity, Is.True);
            }
        }

        private static void AssertNameplate(BeachNativeNpcActor actor, Camera camera)
        {
            var billboard = actor.transform.Find("TitleCanvas").GetComponent<BeachNameBillboard>();
            Assert.That(billboard, Is.Not.Null);
            Assert.That(billboard.View, Is.EqualTo(camera));
            Assert.That(billboard.Head, Is.Not.Null);
            Assert.That(billboard.Head.IsChildOf(actor.transform), Is.True);
            Vector3 offset = billboard.transform.position - billboard.Head.position;
            Assert.That(offset.x, Is.EqualTo(0f).Within(.001f));
            Assert.That(offset.z, Is.EqualTo(0f).Within(.001f));
            Assert.That(offset.y, Is.InRange(.15f, .5f));
            Assert.That(Quaternion.Angle(billboard.transform.rotation, camera.transform.rotation), Is.LessThan(.1f));
        }

        private static float SoleHeight(BeachNativeNpcActor actor)
        {
            // The torso mesh includes the legs and shoes.
            var torso = actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == "Torso");
            var mesh = new Mesh();
            try
            {
                torso.BakeMesh(mesh);
                return mesh.vertices.Min(vertex => torso.transform.TransformPoint(vertex).y);
            }
            finally { Object.Destroy(mesh); }
        }
    }
}
