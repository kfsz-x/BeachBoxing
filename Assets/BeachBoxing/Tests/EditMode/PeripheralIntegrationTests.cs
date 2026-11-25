using System;
using NUnit.Framework;
using UnityCore;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class PeripheralIntegrationTests
    {
        [Test]
        public void TrainingCannotTurnTheLargestValidScoreNegative()
        {
            var progress = new PlayerProgress { chest = int.MaxValue - 4 };
            progress.Add(Muscle.Chest, 11);
            Assert.That(progress.chest, Is.EqualTo(int.MaxValue));
            Assert.DoesNotThrow(() => BeachProgressionRules.FormatNumber(progress.chest));
        }

        [Test]
        public void VictoryCannotPartiallySpendAnOverflowingWallet()
        {
            var progress = new PlayerProgress { coins = int.MaxValue - 1, stars = 7 };
            Assert.That(progress.RecordVictory(0, 50, 3), Is.False);
            Assert.That(new[] {progress.coins, progress.stars, progress.unlockedOpponent},
                Is.EqualTo(new[] {int.MaxValue - 1, 7, 0}));
        }
        [Test]
        public void LegacyVersionOneSaveKeepsBalancesAndAddsNativeDefaults()
        {
            var storage = new LocalStore("BeachPeripheralMigrationTest");
            const string key = "beachBoxing.progress";
            string previous = storage.GetString(key);
            try
            {
                storage.SetString(key, "{\"version\":1,\"chest\":341,\"arms\":352,\"back\":352,\"legs\":341,\"coins\":213,\"stars\":57,\"unlockedOpponent\":1}");
                var loaded = new ProgressStore(storage).Load();
                Assert.That(new[] { loaded.chest, loaded.arms, loaded.back, loaded.legs, loaded.coins, loaded.stars, loaded.unlockedOpponent },
                    Is.EqualTo(new[] {341, 352, 352, 341, 213, 57, 1}));
                var level = typeof(PlayerProgress).GetField("prestigeLevel");
                Assert.That(level, Is.Not.Null, "Manual prestige must be persisted without changing legacy strength.");
                Assert.That(level.GetValue(loaded), Is.EqualTo(1));
                var peripheral = typeof(PlayerProgress).GetField("peripheral");
                Assert.That(peripheral, Is.Not.Null);
                var state = peripheral.GetValue(loaded) as BeachPeripheralState;
                Assert.That(state, Is.Not.Null);
                Assert.That(state.equippedTrainer, Is.EqualTo(-1));
                Assert.That(state.trainerLevels, Is.EqualTo(new int[6]));
                Assert.That(state.dailyIndex, Is.Zero);
                Assert.That(state.claimedDays, Is.Zero);
                new ProgressStore(storage).Save(loaded);
                var roundTrip = new ProgressStore(storage).Load();
                Assert.That(JsonUtility.ToJson(roundTrip), Is.EqualTo(JsonUtility.ToJson(loaded)));
            }
            finally
            {
                if (string.IsNullOrEmpty(previous)) storage.Delete(key); else storage.SetString(key, previous);
                storage.Save();
            }
        }

        [Test]
        public void MenuViewExposesPreplacedNodesAndActionBinding()
        {
            var view = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachPeripheralView");
            Assert.That(view, Is.Not.Null, "Original trainer/daily/progress UI must have a bound static view.");
            Assert.That(view.GetField("nodes"), Is.Not.Null);
            Assert.That(view.GetMethod("Bind"), Is.Not.Null);
            Assert.That(view.GetMethod("Render"), Is.Not.Null);
        }
    }
}
