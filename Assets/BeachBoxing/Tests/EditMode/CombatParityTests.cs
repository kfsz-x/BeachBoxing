using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class CombatParityTests
    {
        [Test]
        public void PlayerPunchColliderExpiresAfterFiftyMilliseconds()
        {
            var player = new FighterState(50, 50, 20);
            player.Punch(1);
            player.Tick(0.14f, false);
            Assert.That(player.InHitWindow, Is.True);
            player.Tick(0.02f, false);
            Assert.That(player.InHitWindow, Is.False);
        }

        [Test]
        public void EnemyJabColliderOpensAtFrameFourAndClosesAtFrameSeven()
        {
            var enemy = new FighterState(20, 20, 20, enemyPunchTiming: true);
            enemy.Punch(3);
            enemy.Tick(0.06f, false);
            Assert.That(enemy.InHitWindow, Is.False);
            enemy.Tick(0.01f, false);
            Assert.That(enemy.InHitWindow, Is.True);
            enemy.Tick(0.05f, false);
            Assert.That(enemy.InHitWindow, Is.False);
        }

        [Test]
        public void PlayerSphereReachesEnemyCapsuleBeyondOldCenterDistanceCutoff()
        {
            Vector3 sphere = FightRules.PunchCenter(Vector3.zero, Quaternion.identity, false, 0.356f);
            Assert.That(FightRules.PunchOverlapsBody(sphere, new Vector3(0, 0, 1.7f), false, 0.356f), Is.True);
            Assert.That(FightRules.PunchOverlapsBody(sphere, new Vector3(0, 0, 2.1f), false, 0.356f), Is.False);
            Assert.That(FightRules.PunchOverlapsBody(sphere, new Vector3(0, 3, 1f), false, 0.356f), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void DistancePunchChecksKeepFixedCadenceEvenWhileTargetIsOutOfRange(int opponentIndex)
        {
            var profile = OpponentProfile.RookieRoster()[opponentIndex];
            var brain = new OpponentBrain(profile, 7);
            var self = new FighterState(40, 40, 20);
            var target = new FighterState(50, 50, 20);
            Assert.That(brain.Step(4.99f, 1f, self, target).attack, Is.False);
            Assert.That(brain.Step(0.02f, 3f, self, target).attack, Is.False);
            Assert.That(brain.Step(0.1f, 1f, self, target).attack, Is.False);
            Assert.That(brain.Step(profile.punchInterval - 0.1f, 1f, self, target).attack, Is.True);
        }

        [Test]
        public void LowStaminaStartsSeveralSecondsOfRecoveryGuard()
        {
            var brain = new OpponentBrain(OpponentProfile.RookieRoster()[0], 7);
            var self = new FighterState(20, 20, 20, recoverStaminaWhileGuarding: true);
            var target = new FighterState(50, 50, 20);
            self.Punch(15);
            Assert.That(brain.Step(0.5f, 1f, self, target).block, Is.False);
            Assert.That(brain.Step(0.5f, 1f, self, target).block, Is.True);
            self.Tick(1f, true);
            Assert.That(brain.Step(1f, 1f, self, target).block, Is.True);
            Assert.That(brain.Step(6f, 1f, self, target).block, Is.False);
        }

        [Test]
        public void KyleUsesStaminaHysteresisToRetreatAndMoveBackIn()
        {
            var brain = new OpponentBrain(OpponentProfile.RookieRoster()[1], 7);
            var self = new FighterState(40, 40, 20, recoverStaminaWhileGuarding: true);
            var target = new FighterState(50, 50, 20);
            Assert.That(brain.Step(5f, 2f, self, target).speed, Is.GreaterThan(0));
            self.Punch(36);
            Assert.That(brain.Step(1f, 2f, self, target).speed, Is.LessThan(0));
            self.Tick(1f, true);
            Assert.That(brain.Step(1f, 2f, self, target).speed, Is.GreaterThan(0));
        }
    }
}
