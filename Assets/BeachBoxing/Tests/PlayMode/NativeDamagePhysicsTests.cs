using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeDamagePhysicsTests
    {
        [UnityTest]
        public IEnumerator ABlockedEnemyPunchStillAppliesNativePlayerKnockback()
        {
            var scene = SceneManager.CreateScene("Native blocked knockback", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var player = new GameObject("player");
            var enemy = new GameObject("enemy");
            SceneManager.MoveGameObjectToScene(player, scene);
            SceneManager.MoveGameObjectToScene(enemy, scene);
            try
            {
                player.transform.position = new Vector3(1000, 1000, 1000);
                enemy.transform.position = new Vector3(1000, 1000, 1001);
                var body = player.AddComponent<Rigidbody>();
                body.useGravity = false;
                var enemyBody = enemy.AddComponent<Rigidbody>();
                enemyBody.useGravity = false;
                enemyBody.mass = 3;
                var fight = new FightController(player.transform, enemy.transform, new PlayerProgress(),
                    OpponentProfile.RookieRoster()[0], new Rect(900, 900, 200, 200), 1);
                fight.Tick(5, default);
                Assert.That(fight.Enemy.Punch(1), Is.True);
                fight.Tick(.07f, new InputFrame(Vector2.zero, false, false, true, false, false));
                Assert.That(fight.Player.LastImpactWasBlocked, Is.True);
                Assert.That(fight.Player.ImpactCount, Is.EqualTo(1));
                scene.GetPhysicsScene().Simulate(.033f);
                Assert.That(body.velocity.z, Is.LessThan(-.001f), "Source guard and health branches both merge before AddForce.");
                yield return null;
            }
            finally
            {
                Object.Destroy(player);
                Object.Destroy(enemy);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
