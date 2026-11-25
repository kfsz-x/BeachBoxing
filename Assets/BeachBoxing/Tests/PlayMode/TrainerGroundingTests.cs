using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class TrainerGroundingTests
    {
        [UnityTest]
        public IEnumerator EquippedTrainersStayAboveTerrainAndRecoverAfterBeingMovedBelowIt() =>
            NativePlayModeScene.WithScene(true, (flow, _) => CheckTrainers(flow));

        private static IEnumerator CheckTrainers(BeachGameFlow flow)
        {
            flow.enabled = false;
            var followers = flow.GetComponent<BeachTrainerFollowers>();
            var models = NativePlayModeScene.Field<BeachTrainerFollowers.Model[]>(followers, "models");
            var player = flow.transform.Find("Player");
            var state = new BeachPeripheralState();
            for (int i = 0; i < models.Length; i++)
            {
                state.equippedTrainer = i;
                state.trainerLevels[i] = 3;
                followers.Apply(state, player, false);
                for (int frame = 0; frame < 40; frame++) yield return new WaitForFixedUpdate();
                var root = models[i].root;
                Assert.That(root.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(root.GetComponent<Rigidbody>().useGravity, Is.False);
                AssertGrounded(root, player.position.y);
                root.position += Vector3.down * 50f;
                yield return null;
                yield return new WaitForEndOfFrame();
                AssertGrounded(root, player.position.y);
                var target = flow.transform.Find("Training beach/Arms/Original equipment/TrainerPos");
                Assert.That(target, Is.Not.Null);
                followers.Apply(state, player, true, target);
                for (int frame = 0; frame < 180; frame++) yield return new WaitForFixedUpdate();
                AssertGrounded(root, player.position.y);
                var offset = target.position - root.position;
                offset.y = 0;
                Assert.That(offset.magnitude, Is.LessThan(.4f), root.name + " must reach its workout position.");

                followers.Apply(state, player, false);
                Vector3 before = root.position;
                player.position += Vector3.right * 4f;
                for (int frame = 0; frame < 20; frame++) yield return new WaitForFixedUpdate();
                Assert.That(Vector2.Distance(new Vector2(root.position.x, root.position.z),
                    new Vector2(before.x, before.z)), Is.GreaterThan(.5f), root.name + " must follow the player.");
                AssertGrounded(root, player.position.y);
            }
            followers.Apply(null, player, false);
            foreach (var model in models) Assert.That(model.root.gameObject.activeSelf, Is.False);
        }

        private static void AssertGrounded(Transform root, float playerY)
        {
            Assert.That(Physics.Raycast(new Vector3(root.position.x, playerY + 3f, root.position.z),
                Vector3.down, out var hit, 8f, 1 << 3, QueryTriggerInteraction.Ignore), Is.True, root.name);
            var capsule = root.GetComponent<CapsuleCollider>();
            Assert.That(capsule.bounds.min.y - hit.point.y, Is.EqualTo(.01f).Within(.05f),
                root.name + " capsule must rest on the ground.");
        }
    }
}
