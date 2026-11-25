using NUnit.Framework;

namespace BeachBoxing.Tests
{
    public sealed class RuleTests
    {
        [Test]
        public void TouchCapabilitySelectsMobileControlsBeforeFirstPress()
        {
            var input = new BeachInput();
            Assert.That(input.UseTouchLayout(false, true, false, false), Is.True);
        }

        [Test]
        public void TouchDeviceModeSurvivesReleaseAndInputReset()
        {
            var input = new BeachInput();
            Assert.That(input.UseTouchLayout(false, false, false, true), Is.True);
            input.Reset();
            Assert.That(input.UseTouchLayout(false, false, false, false), Is.True);
        }

        [Test]
        public void PortraitPreviewCanReturnToDesktopWithoutTouchDeviceSignals()
        {
            var input = new BeachInput();
            Assert.That(input.UseTouchLayout(false, false, true, false), Is.True);
            Assert.That(input.UseTouchLayout(false, false, false, false), Is.False);
        }

        [TestCase(0, 50)]
        [TestCase(19, 50)]
        [TestCase(20, 51)]
        public void PlayerHealthIncreasesOnlyAtWholeTrainingThresholds(int chest, int expected)
        {
            Assert.That(FightRules.PlayerCapacity(chest), Is.EqualTo(expected));
        }

        [Test]
        public void RepeatedFightWinsPayAgainWithoutAdvancingTheNextOpponent()
        {
            var progress = new PlayerProgress();
            Assert.That(progress.RecordVictory(0, 50, 3), Is.True);
            Assert.That(progress.RecordVictory(0, 50, 3), Is.True);
            Assert.That(progress.coins, Is.EqualTo(100));
            Assert.That(progress.stars, Is.EqualTo(6));
            Assert.That(progress.unlockedOpponent, Is.EqualTo(1));
        }

        [Test]
        public void FinalOpponentPaysAgainButComingSoonNeverAwardsOrAdvances()
        {
            var progress = new PlayerProgress { unlockedOpponent = 44 };
            Assert.That(progress.RecordVictory(44, 2250, 135), Is.True);
            Assert.That(progress.unlockedOpponent, Is.EqualTo(45));
            Assert.That(progress.RecordVictory(44, 2250, 135), Is.True);
            Assert.That(progress.RecordVictory(45, 2300, 138), Is.False);
            Assert.That(new[] { progress.coins, progress.stars, progress.unlockedOpponent }, Is.EqualTo(new[] { 4500, 270, 45 }));
        }

        [Test]
        public void OnlyCompletedRepetitionsAwardTrainingAndExitClearsPartialProgress()
        {
            var cycle = new TrainingCycle();
            Assert.That(cycle.Advance(0.75f, 1f), Is.EqualTo(0));
            Assert.That(cycle.Advance(0.5f, 1f), Is.EqualTo(1));
            cycle.Reset();
            Assert.That(cycle.Advance(0.75f, 1f), Is.EqualTo(0));
        }

        [Test]
        public void OnePunchConsumesStaminaAndLandsOnlyOnceWithinRange()
        {
            var player = new FighterState(50, 50, 20);
            var opponent = new FighterState(20, 20, 20);
            Assert.That(player.Punch(2), Is.True);
            Assert.That(player.Stamina, Is.EqualTo(48));
            Assert.That(player.TryLand(opponent, 1f, 3), Is.False);
            player.Tick(0.12f, false);
            Assert.That(player.TryLand(opponent, 2f, 3), Is.False);
            Assert.That(player.TryLand(opponent, 1f, 3), Is.True);
            Assert.That(player.TryLand(opponent, 1f, 3), Is.False);
            Assert.That(opponent.Health, Is.EqualTo(17));
        }

        [Test]
        public void HeldGuardAbsorbsDamageAndEmptyStaminaStopsPunching()
        {
            var player = new FighterState(50, 2, 20);
            var opponent = new FighterState(20, 20, 20);
            opponent.Tick(0.12f, true);
            Assert.That(player.Punch(2), Is.True);
            player.Tick(0.12f, false);
            Assert.That(player.TryLand(opponent, 1f, 3), Is.True);
            Assert.That(opponent.Health, Is.EqualTo(20));
            Assert.That(opponent.Guard, Is.EqualTo(17));
            player.Tick(0.25f, false);
            Assert.That(player.Punch(2), Is.False);
        }
    }
}
