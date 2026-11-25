using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeFacingClockTests
    {
        [Test]
        public void PlayerMovementKeepsTheLastRenderedTargetAcrossPhysicsTicks()
        {
            var player = new GameObject("native player target basis");
            var enemy = new GameObject("native enemy target basis");
            try
            {
                var body = player.AddComponent<Rigidbody>();
                body.useGravity = false;
                enemy.AddComponent<Rigidbody>().useGravity = false;
                enemy.transform.position = Vector3.forward * 10;
                var fight = new FightController(player.transform, enemy.transform, new PlayerProgress(),
                    OpponentProfile.RookieRoster()[0], new Rect(-20, -20, 40, 40), 1, nativePhysics: true);
                fight.Tick(5, default);
                enemy.transform.position = Vector3.right * 10;
                fight.Tick(.033f, new InputFrame(Vector2.up, false, false, false, false, false));
                Assert.That(Mathf.Abs(body.velocity.x), Is.LessThan(.001f));
                Assert.That(body.velocity.z, Is.GreaterThan(1));
            }
            finally { Object.DestroyImmediate(player); Object.DestroyImmediate(enemy); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LethalDamageFreezesTheLosingBodyBeforeThePhysicsStep(bool enemyLoses)
        {
            var player = new GameObject("native knockout player");
            var enemy = new GameObject("native knockout enemy");
            try
            {
                var playerBody = player.AddComponent<Rigidbody>();
                var enemyBody = enemy.AddComponent<Rigidbody>();
                enemy.transform.position = Vector3.forward * 10;
                var fight = new FightController(player.transform, enemy.transform, new PlayerProgress(),
                    OpponentProfile.RookieRoster()[0], new Rect(-20, -20, 40, 40), 1, nativePhysics: true);
                fight.Tick(5, default);
                typeof(FighterState).GetProperty("Health").SetValue(enemyLoses ? fight.Enemy : fight.Player, 0f);
                fight.Tick(.033f, default);
                Assert.That(enemyLoses ? enemyBody.isKinematic : playerBody.isKinematic, Is.True);
                Assert.That(enemyLoses ? playerBody.isKinematic : enemyBody.isKinematic, Is.False);
            }
            finally { Object.DestroyImmediate(player); Object.DestroyImmediate(enemy); }
        }

        [Test]
        public void EnemyFacingUsesRenderedClockRatherThanEachPhysicsStep()
        {
            var player = new GameObject("native facing player");
            var enemy = new GameObject("native facing enemy");
            try
            {
                player.AddComponent<Rigidbody>().useGravity = false;
                enemy.AddComponent<Rigidbody>().useGravity = false;
                enemy.transform.position = Vector3.forward * 10;
                var fight = new FightController(player.transform, enemy.transform, new PlayerProgress(),
                    OpponentProfile.RookieRoster()[0], new Rect(-20, -20, 40, 40), 1, nativePhysics: true);
                fight.Tick(5, default);
                enemy.transform.rotation = Quaternion.Euler(0, 90, 0);
                fight.Tick(.033f, default);
                Assert.That(Quaternion.Angle(enemy.transform.rotation, Quaternion.Euler(0, 90, 0)), Is.LessThan(.01f),
                    "Source EnemyManager rotates in Update; multiple FixedUpdate ticks must not replace its heading.");
                var update = typeof(FightController).GetMethod("UpdateNativeFacing", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(update, Is.Not.Null);
                var before = enemy.transform.rotation;
                var target = Quaternion.LookRotation((player.transform.position - enemy.transform.position).normalized);
                update.Invoke(fight, new object[] { .01f });
                Assert.That(Quaternion.Angle(enemy.transform.rotation, Quaternion.Lerp(before, target, .5f)), Is.LessThan(.01f));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }
    }
}
