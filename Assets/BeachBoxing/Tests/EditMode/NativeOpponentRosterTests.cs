using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeOpponentRosterTests
    {
        [Test]
        public void PlayableRosterStopsBeforeComingSoon()
        {
            var roster = OpponentProfile.RookieRoster();
            Assert.That(roster.Length, Is.EqualTo(45), "Native CombatManager has 45 enemy prefabs; index 45 is display only.");
            Assert.That(System.Array.Exists(roster, p => p.name == "COMING SOON"), Is.False);
        }

        [TestCase(2, "Kenji", 500, 150, 9, 12f, 8f, .4f)]
        [TestCase(44, "Ramez (Rematch)", 100000, 2250, 135, 20f, 13f, .33f)]
        public void LaterOpponentsUseShippedSelectionAndRewardValues(int index, string name,
            int chest, int gold, int stars, float speed, float blockSpeed, float interval)
        {
            var roster = OpponentProfile.RookieRoster();
            Assert.That(roster.Length, Is.GreaterThan(index), "Missing original opponent.");
            var profile = roster[index];
            Assert.That(profile.name, Is.EqualTo(name));
            Assert.That(profile.rating, Is.EqualTo(chest));
            Assert.That(profile.coins, Is.EqualTo(gold));
            Assert.That(profile.stars, Is.EqualTo(stars));
            Assert.That(profile.approachSpeed, Is.EqualTo(speed));
            Assert.That(profile.retreatSpeed, Is.EqualTo(blockSpeed));
            Assert.That(profile.punchInterval, Is.EqualTo(interval).Within(.00001f));
        }

        [TestCase(2, 65, 65, 33)]
        [TestCase(44, 5050, 5050, 1686)]
        public void CombatUsesDistinctNativeMuscleStatsAndPrefabCapacities(int index, int health, int stamina, int guard)
        {
            var roster = OpponentProfile.RookieRoster();
            Assert.That(roster.Length, Is.GreaterThan(index), "Missing original opponent.");
            var player = new GameObject("native roster player test");
            var enemy = new GameObject("native roster enemy test");
            try
            {
                enemy.transform.position = Vector3.forward * 2;
                var fight = new FightController(player.transform, enemy.transform, new PlayerProgress(),
                    roster[index], new Rect(-10, -10, 20, 20), 19);
                Assert.That(fight.Enemy.MaxHealth, Is.EqualTo(health));
                Assert.That(fight.Enemy.MaxStamina, Is.EqualTo(stamina));
                Assert.That(fight.Enemy.MaxGuard, Is.EqualTo(guard));
                if (index != 2) return;
                var attacker = new FighterState(100, 100, 100);
                attacker.Punch(0);
                attacker.Tick(.1f, false);
                Assert.That(attacker.TryLand(fight.Enemy, true, 10), Is.True);
                fight.Enemy.Tick(1, true);
                Assert.That(fight.Enemy.Health, Is.EqualTo(57.6f).Within(.0001f),
                    "Kenji prefab recovery 1.6 plus integer back/800, independent of chest.");
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }

        [TestCase(5)]
        [TestCase(7)]
        [TestCase(12)]
        public void NativeConstantAndRandomAttackModesDoNotRequireDistance(int index)
        {
            var brain = new OpponentBrain(OpponentProfile.RookieRoster()[index], 7);
            var self = new FighterState(1000, 1000, 100);
            var target = new FighterState(1000, 1000, 100);
            Assert.That(brain.Step(4.99f, 100f, self, target).attack, Is.False);
            Assert.That(brain.Step(.02f, 100f, self, target).attack, Is.True);
        }

        [Test]
        public void FinalRematchRetreatsAtItsNativeTwoHundredStaminaThreshold()
        {
            var brain = new OpponentBrain(OpponentProfile.RookieRoster()[44], 7);
            var self = new FighterState(1000, 1000, 100);
            var target = new FighterState(1000, 1000, 100);
            Assert.That(brain.Step(5f, 100f, self, target).speed, Is.GreaterThan(0));
            self.Punch(801);
            Assert.That(brain.Step(1f, 100f, self, target).speed, Is.LessThan(0));
        }
    }
}
