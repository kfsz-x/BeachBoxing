using NUnit.Framework;

namespace BeachBoxing.Tests
{
    public sealed class PeripheralRulesTests
    {
        private BeachPeripheralState state;

        [SetUp]
        public void CreatePeripheralState() => state = new BeachPeripheralState();

        [TestCase(0, "Cody", -1, 5, 5, 5, 5)]
        [TestCase(1, "Dion", 50, 10, 10, 10, 10)]
        [TestCase(2, "Nova", 200, 30, 0, 30, 0)]
        [TestCase(3, "Zara", -1, 10, 40, 10, 40)]
        [TestCase(4, "Serena", 500, 50, 30, 20, 50)]
        [TestCase(5, "Yui", 900, 50, 50, 50, 50)]
        public void TrainerDetailsUseOriginalShopData(int id, string name, int stars, int chest, int arms, int back, int legs)
        {
            Assert.That(BeachPeripheralRules.GetTrainerName(id), Is.EqualTo(name));
            Assert.That(BeachPeripheralRules.GetPurchaseCost(id), Is.EqualTo(stars));
            Assert.That(BeachPeripheralRules.GetDisplayedModifier(state, id, Muscle.Chest), Is.EqualTo(chest));
            Assert.That(BeachPeripheralRules.GetDisplayedModifier(state, id, Muscle.Arms), Is.EqualTo(arms));
            Assert.That(BeachPeripheralRules.GetDisplayedModifier(state, id, Muscle.Back), Is.EqualTo(back));
            Assert.That(BeachPeripheralRules.GetDisplayedModifier(state, id, Muscle.Legs), Is.EqualTo(legs));
        }

        [Test]
        public void PurchaseSpendsStarsAndLeavesTheCurrentTrainerEquipped()
        {
            var progress = new PlayerProgress { coins = 65, stars = 250 };
            Assert.That(BeachPeripheralRules.TryPurchaseTrainer(progress, state, 1), Is.True);
            Assert.That(state.equippedTrainer, Is.EqualTo(-1));
            Assert.That(BeachPeripheralRules.TryEquipTrainer(state, 1), Is.True);
            Assert.That(BeachPeripheralRules.TryPurchaseTrainer(progress, state, 2), Is.True);
            Assert.That(BeachPeripheralRules.TryPurchaseTrainer(progress, state, 2), Is.False);
            Assert.That(progress.stars, Is.Zero);
            Assert.That(progress.coins, Is.EqualTo(65));
            Assert.That(state.trainerLevels, Is.EqualTo(new[] { 0, 1, 1, 0, 0, 0 }));
            Assert.That(state.equippedTrainer, Is.EqualTo(1));
            Assert.That(BeachPeripheralRules.GetAppliedModifier(state, Muscle.Chest), Is.EqualTo(11));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(6)]
        public void FailedTrainerActionsLeaveBalancesAndOwnershipUntouched(int id)
        {
            var progress = new PlayerProgress { coins = 100, stars = 49 };
            Assert.That(BeachPeripheralRules.TryPurchaseTrainer(progress, state, id), Is.False);
            Assert.That(BeachPeripheralRules.TryEquipTrainer(state, id), Is.False);
            Assert.That(BeachPeripheralRules.TryUpgradeTrainer(progress, state, id), Is.False);
            Assert.That(state.trainerLevels, Is.EqualTo(new int[6]));
            Assert.That(state.equippedTrainer, Is.EqualTo(-1));
            Assert.That(progress.coins, Is.EqualTo(100));
            Assert.That(progress.stars, Is.EqualTo(49));
        }

        [Test]
        public void UpgradeChangesTheDisplayedBonusAndRefreshesAppliedBonusOnEquip()
        {
            var progress = new PlayerProgress { coins = 16, stars = 500 };
            Assert.That(BeachPeripheralRules.TryPurchaseTrainer(progress, state, 4), Is.True);
            Assert.That(BeachPeripheralRules.TryUpgradeTrainer(progress, state, 4), Is.False);
            Assert.That(BeachPeripheralRules.TryEquipTrainer(state, 4), Is.True);
            Assert.That(BeachPeripheralRules.GetUpgradeCost(state, 4), Is.EqualTo(16));
            Assert.That(BeachPeripheralRules.GetDisplayedModifier(state, 4, Muscle.Back), Is.EqualTo(20));
            Assert.That(state.appliedModifiers, Is.EqualTo(new[] { 51, 31, 21, 51 }));

            Assert.That(BeachPeripheralRules.TryUpgradeTrainer(progress, state, 4), Is.True);
            Assert.That(progress.coins, Is.Zero);
            Assert.That(state.trainerLevels[4], Is.EqualTo(2));
            Assert.That(BeachPeripheralRules.GetUpgradeCost(state, 4), Is.EqualTo(24));
            Assert.That(BeachPeripheralRules.TryUpgradeTrainer(progress, state, 4), Is.False);
            Assert.That(BeachPeripheralRules.GetDisplayedModifier(state, 4, Muscle.Back), Is.EqualTo(22));
            Assert.That(BeachPeripheralRules.GetAppliedModifier(state, Muscle.Back), Is.EqualTo(21));

            Assert.That(BeachPeripheralRules.TryEquipTrainer(state, 4), Is.True);
            Assert.That(state.appliedModifiers, Is.EqualTo(new[] { 52, 32, 22, 52 }));
        }

        [Test]
        public void NormalizeRepairsMalformedArraysAndClearsUnownedEquipment()
        {
            state.trainerLevels = new[] { -1, 3 };
            state.equippedTrainer = 5;
            state.appliedModifiers = new[] { 80, 80 };
            state.dailyIndex = -1;
            state.lastDailyDate = null;
            state.claimedDays = 1 << 8;

            BeachPeripheralRules.Normalize(state);

            Assert.That(state.trainerLevels, Is.EqualTo(new[] { 0, 3, 0, 0, 0, 0 }));
            Assert.That(state.equippedTrainer, Is.EqualTo(-1));
            Assert.That(state.appliedModifiers, Is.EqualTo(new int[4]));
            Assert.That(state.dailyIndex, Is.Zero);
            Assert.That(state.claimedDays, Is.Zero);
        }

        [Test]
        public void RefreshReappliesTheStoredTrainerLevelAtStartup()
        {
            state.trainerLevels = new[] { 0, 0, 2, 0, 0, 0 };
            state.equippedTrainer = 2;
            state.appliedModifiers = new[] { 31, 1, 31, 1 };

            BeachPeripheralRules.Normalize(state);
            Assert.That(BeachPeripheralRules.GetAppliedModifier(state, Muscle.Arms), Is.EqualTo(1));
            BeachPeripheralRules.RefreshEquippedModifiers(state);
            Assert.That(state.appliedModifiers, Is.EqualTo(new[] { 32, 2, 32, 2 }));
        }

        [Test]
        public void EachDifferentLocalDateUnlocksOneReward()
        {
            Assert.That(BeachPeripheralRules.HasDailyReward(state), Is.False);
            Assert.That(BeachPeripheralRules.AdvanceDailyDate(state, "2026-11-20"), Is.True);
            Assert.That(BeachPeripheralRules.CanClaim(state, 0), Is.True);
            Assert.That(BeachPeripheralRules.CanClaim(state, 1), Is.False);
            Assert.That(BeachPeripheralRules.AdvanceDailyDate(state, "2026-11-20"), Is.False);
            Assert.That(BeachPeripheralRules.CanClaim(state, 1), Is.False);
            Assert.That(BeachPeripheralRules.AdvanceDailyDate(state, "2026-11-21"), Is.True);
            Assert.That(BeachPeripheralRules.CanClaim(state, 1), Is.True);
            Assert.That(BeachPeripheralRules.AdvanceDailyDate(state, "2026-12-01"), Is.True);
            Assert.That(BeachPeripheralRules.CanClaim(state, 2), Is.True);
            Assert.That(BeachPeripheralRules.CanClaim(state, 3), Is.False);
            Assert.That(state.lastDailyDate, Is.EqualTo("2026-12-01"));
        }

        [TestCase(0, 0, 0, 1, 0, true)]
        [TestCase(1, 3000, 0, 0, 0, false)]
        [TestCase(2, 0, 60, 0, 0, false)]
        [TestCase(3, 10000, 0, 0, 0, false)]
        [TestCase(4, 0, 100, 0, 0, false)]
        [TestCase(5, 0, 0, 0, 1, true)]
        public void EachDailyRewardGrantsItsOriginalPrizeOnce(int day, int coins, int stars, int cody, int zara, bool trainerAlert)
        {
            var progress = new PlayerProgress();
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, day), Is.False);
            state.dailyIndex = 6;
            state.claimedDays = 63 ^ (1 << day);
            Assert.That(BeachPeripheralRules.HasDailyReward(state), Is.True);
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, day), Is.True);
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, day), Is.False);
            Assert.That(BeachPeripheralRules.HasDailyReward(state), Is.False);
            Assert.That(progress.coins, Is.EqualTo(coins));
            Assert.That(progress.stars, Is.EqualTo(stars));
            Assert.That(state.trainerLevels, Is.EqualTo(new[] { cody, 0, 0, zara, 0, 0 }));
            Assert.That(state.trainerAlert, Is.EqualTo(trainerAlert));
            Assert.That(state.equippedTrainer, Is.EqualTo(-1));
            Assert.That(state.claimedDays, Is.EqualTo(63));
        }

        [Test]
        public void AdvancingAfterTheSixthDayNeverReopensOrAddsRewards()
        {
            var progress = new PlayerProgress();
            state.dailyIndex = 6;
            state.claimedDays = 63;
            Assert.That(BeachPeripheralRules.AdvanceDailyDate(state, "2026-11-20"), Is.True);
            Assert.That(state.dailyIndex, Is.EqualTo(7));
            Assert.That(BeachPeripheralRules.HasDailyReward(state), Is.False);
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, -1), Is.False);
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, 0), Is.False);
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, 6), Is.False);
        }

        [Test]
        public void UnrepresentableCurrencyChangesLeaveTheRewardAndLevelAvailable()
        {
            var progress = new PlayerProgress { coins = int.MaxValue, stars = int.MaxValue };
            state.dailyIndex = 3;
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, 1), Is.False);
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, state, 2), Is.False);
            Assert.That(BeachPeripheralRules.CanClaim(state, 1), Is.True);
            Assert.That(BeachPeripheralRules.CanClaim(state, 2), Is.True);

            state.trainerLevels = new[] { 0, int.MaxValue, 0, 0, 0, 0 };
            state.equippedTrainer = 1;
            Assert.That(BeachPeripheralRules.TryUpgradeTrainer(progress, state, 1), Is.False);
            Assert.That(state.trainerLevels[1], Is.EqualTo(int.MaxValue));
            Assert.That(progress.coins, Is.EqualTo(int.MaxValue));
            Assert.That(progress.stars, Is.EqualTo(int.MaxValue));
        }
    }
}
