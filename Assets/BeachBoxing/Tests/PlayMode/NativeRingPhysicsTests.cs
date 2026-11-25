using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeRingPhysicsTests
    {
        [UnityTest]
        public IEnumerator BothAuthoredBodiesSettleOnTheOriginalRingFloor()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            typeof(BeachGameFlow).GetMethod("EnterFight", flags).Invoke(flow, null);
            var stage = typeof(BeachGameFlow).GetField("stage", flags).GetValue(flow);
            var player = (Transform)stage.GetType().GetField("Player", flags).GetValue(stage).GetType()
                .GetField("Root", flags).GetValue(stage.GetType().GetField("Player", flags).GetValue(stage));
            var enemy = (Transform)stage.GetType().GetField("Enemy", flags).GetValue(stage).GetType()
                .GetField("Root", flags).GetValue(stage.GetType().GetField("Enemy", flags).GetValue(stage));
            var arena = (Transform)stage.GetType().GetField("arena", flags).GetValue(stage);
            Assert.That(Vector3.Distance(player.position - arena.position, new Vector3(4.48916022f, .01779993f, 2.32112f)), Is.LessThan(.001f),
                "The original actor spawns away from arena origin, not at the center marker.");
            Assert.That(Vector3.Distance(enemy.position - arena.position, new Vector3(-1.68743992f, 0, -3.3286f)), Is.LessThan(.001f));
            yield return new WaitForSeconds(.8f);
            var playerBody = player.GetComponent<Rigidbody>();
            var enemyBody = enemy.GetComponent<Rigidbody>();
            Assert.That(playerBody.isKinematic || enemyBody.isKinematic, Is.False);
            Assert.That(playerBody.mass, Is.EqualTo(1));
            Assert.That(enemyBody.mass, Is.EqualTo(3));
            Assert.That(playerBody.useGravity && enemyBody.useGravity, Is.True);
            var playerShape = player.Find("PlayerCollider").GetComponent<CapsuleCollider>();
            var enemyShape = enemy.GetComponent<CapsuleCollider>();
            Assert.That(playerShape.bounds.min.y, Is.EqualTo(-.3524456f).Within(.025f));
            Assert.That(enemyShape.bounds.min.y, Is.EqualTo(-.3524456f).Within(.025f));
            Assert.That(Mathf.Abs(playerBody.velocity.y), Is.LessThan(.1f));
            Assert.That(Mathf.Abs(enemyBody.velocity.y), Is.LessThan(.1f));
        }
    }
}
