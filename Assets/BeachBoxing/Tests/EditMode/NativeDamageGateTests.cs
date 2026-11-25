using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace BeachBoxing.Tests
{
    public sealed class NativeDamageGateTests
    {
        private static FighterState Defender(float immunity, float hurt)
        {
            var constructor = typeof(FighterState).GetConstructors().SingleOrDefault(c =>
                c.GetParameters().Any(p => p.Name == "damageImmunity") &&
                c.GetParameters().Any(p => p.Name == "movementHurtDuration"));
            Assert.That(constructor, Is.Not.Null, "Native damage immunity and locomotion hold require independent source values.");
            var args = constructor.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : (object)50).ToArray();
            for (int i = 0; i < args.Length; i++)
            {
                string name = constructor.GetParameters()[i].Name;
                if (name == "damageImmunity") args[i] = immunity;
                if (name == "movementHurtDuration") args[i] = hurt;
            }
            return (FighterState)constructor.Invoke(args);
        }
        private static bool Flag(FighterState state, string name) => (bool)typeof(FighterState).GetProperty(name).GetValue(state);
        private static FighterState ReadyPunch()
        {
            var attacker = new FighterState(50, 50, 20);
            attacker.Punch(1);
            attacker.Tick(.1f, false);
            return attacker;
        }

        [Test]
        public void EnemyImmunityConsumesAnIgnoredContactWithoutRetryOrTimerExtension()
        {
            var enemy = Defender(.2f, .2f);
            Assert.That(ReadyPunch().TryLand(enemy, true, 3), Is.True);
            var ignored = ReadyPunch();
            Assert.That(ignored.TryLand(enemy, true, 3), Is.False);
            Assert.That(enemy.Health, Is.EqualTo(47));
            Assert.That(enemy.ImpactCount, Is.EqualTo(1));
            enemy.Tick(.2f, false);
            Assert.That(Flag(enemy, "DamageImmune"), Is.False);
            Assert.That(ignored.TryLand(enemy, true, 3), Is.False, "Native OnTriggerEnter does not retry an ignored overlap.");
            Assert.That(ReadyPunch().TryLand(enemy, true, 3), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerRepeatedHealthAndGuardHitsRestartMovementHoldWithoutImmunity(bool blocking)
        {
            var player = Defender(0, .3f);
            player.Tick(0, blocking);
            Assert.That(ReadyPunch().TryLand(player, true, 3), Is.True);
            player.Tick(.1f, blocking);
            Assert.That(ReadyPunch().TryLand(player, true, 3), Is.True);
            Assert.That(player.ImpactCount, Is.EqualTo(2));
            Assert.That(Flag(player, "DamageImmune"), Is.False);
            player.Tick(.21f, false);
            Assert.That(Flag(player, "MovementBlocked"), Is.True);
            Assert.That(player.Punch(1), Is.True, "TakingDamage does not cancel or gate the source punch scheduler.");
            player.Tick(.1f, false);
            Assert.That(Flag(player, "MovementBlocked"), Is.False);
        }

        [Test]
        public void EnemyGuardHitAlsoStartsBothNativeTimers()
        {
            var enemy = Defender(.2f, .2f);
            enemy.Tick(0, true);
            Assert.That(ReadyPunch().TryLand(enemy, true, 3), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(50));
            Assert.That(Flag(enemy, "DamageImmune"), Is.True);
            Assert.That(Flag(enemy, "MovementBlocked"), Is.True);
        }
    }
}
