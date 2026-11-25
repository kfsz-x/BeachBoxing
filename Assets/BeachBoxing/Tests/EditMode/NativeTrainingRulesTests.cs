using System;
using System.Reflection;
using NUnit.Framework;

namespace BeachBoxing.Tests
{
    public sealed class NativeTrainingRulesTests
    {
        [Test]
        public void SourceWorkoutSpeedStartsAtPointThree()
        {
            Assert.That(new TrainingCycle().Speed, Is.EqualTo(.3f).Within(.0001f));
        }

        [TestCase(false, .01f, 2.75f, .0275f)]
        [TestCase(true, .02f, 3.56f, .0712f)]
        public void APressStillInterpolatesDuringTheSameFrame(bool touch, float seconds, float speed, float progress)
        {
            var cycle = new TrainingCycle();
            cycle.AdvanceWorkout(seconds, true, true, touch);
            Assert.That(cycle.Speed, Is.EqualTo(speed).Within(.0001f));
            Assert.That(cycle.Progress, Is.EqualTo(progress).Within(.0001f));
        }

        [Test]
        public void HoldingWithoutAnotherPressDoesNotGrantAnAdvertisingBoost()
        {
            var cycle = new TrainingCycle();
            cycle.AdvanceWorkout(1, false, true, false);
            Assert.That(cycle.Speed, Is.EqualTo(.5f).Within(.0001f));
        }

        [Test]
        public void ResetClearsTheBarButPreservesNativeSpeedAcrossWeightChangesAndExit()
        {
            var cycle = new TrainingCycle();
            cycle.AdvanceWorkout(.01f, true, false, false);
            float retained = cycle.Speed;
            cycle.Reset();
            Assert.That(cycle.Progress, Is.Zero);
            Assert.That(cycle.Speed, Is.EqualTo(retained).Within(.0001f));
        }

        [TestCase(0, 5)]
        [TestCase(127, 5)]
        [TestCase(128, 10)]
        [TestCase(381, 10)]
        [TestCase(382, 15)]
        [TestCase(15000, 300)]
        [TestCase(int.MaxValue, 300)]
        public void RecommendedWeightUsesTheOriginalFloat32RoundingAndBounds(int strength, int expected)
        {
            var type = WeightType();
            Assert.That(type.GetMethod("RecommendedFor").Invoke(null, new object[] { strength }), Is.EqualTo(expected));
        }

        [Test]
        public void ManualWeightHasOnlyTheOriginalMinimumAndRecommendationDoesNotAutoReplaceIt()
        {
            var type = WeightType();
            var state = Activator.CreateInstance(type);
            type.GetMethod("Enter").Invoke(state, new object[] { 0 });
            type.GetMethod("Change").Invoke(state, new object[] { 500, 0 });
            Assert.That(type.GetProperty("CurrentWeight").GetValue(state), Is.EqualTo(505));
            type.GetMethod("RefreshRecommendation").Invoke(state, new object[] { 15000 });
            Assert.That(type.GetProperty("CurrentWeight").GetValue(state), Is.EqualTo(505));
            type.GetMethod("Change").Invoke(state, new object[] { -1000, 15000 });
            Assert.That(type.GetProperty("CurrentWeight").GetValue(state), Is.EqualTo(5));
            Assert.That(type.GetProperty("TipNeeded").GetValue(state), Is.True);
            type.GetMethod("UseRecommended").Invoke(state, new object[] { 15000 });
            Assert.That(type.GetProperty("CurrentWeight").GetValue(state), Is.EqualTo(300));
            Assert.That(type.GetProperty("TipNeeded").GetValue(state), Is.False);
        }

        private static Type WeightType()
        {
            var type = typeof(TrainingCycle).Assembly.GetType("BeachBoxing.BeachWeightRules");
            Assert.That(type, Is.Not.Null, "The source weight rules are missing.");
            return type;
        }
    }

    public sealed class NativeTutorialRulesTests
    {
        [Test]
        public void ArmsFiftyDoesNotAdvanceTheLineButFiftyOnePersistsTheRingStage()
        {
            var type = TutorialType();
            var progress = new PlayerProgress { arms = 50 };
            var update = type.GetMethod("UpdateProgress");
            Assert.That(update.Invoke(null, new object[] { progress }), Is.False);
            Assert.That(Convert.ToInt32(type.GetMethod("Target").Invoke(null, new object[] { progress })), Is.EqualTo(1));
            progress.arms = 51;
            Assert.That(update.Invoke(null, new object[] { progress }), Is.True);
            Assert.That(typeof(PlayerProgress).GetField("completedTutorial").GetValue(progress), Is.EqualTo(1));
            Assert.That(Convert.ToInt32(type.GetMethod("Target").Invoke(null, new object[] { progress })), Is.EqualTo(2));
            Assert.That(update.Invoke(null, new object[] { progress }), Is.False);
            progress.unlockedOpponent = 1;
            Assert.That(Convert.ToInt32(type.GetMethod("Target").Invoke(null, new object[] { progress })), Is.Zero);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void ReturningAfterTheFirstWinConsumesTheOneTimeHintEvenOnPhone(bool phone, bool visible)
        {
            var type = TutorialType();
            var progress = new PlayerProgress { unlockedOpponent = 1 };
            var entered = type.GetMethod("OnBeachEntered");
            Assert.That(entered.Invoke(null, new object[] { progress, phone }), Is.EqualTo(visible));
            Assert.That(typeof(PlayerProgress).GetField("keepTrainingHint").GetValue(progress), Is.EqualTo(1));
            Assert.That(entered.Invoke(null, new object[] { progress, false }), Is.False);
        }

        private static Type TutorialType()
        {
            var type = typeof(TrainingCycle).Assembly.GetType("BeachBoxing.BeachTutorialRules");
            Assert.That(type, Is.Not.Null, "The complete source tutorial rules are missing.");
            return type;
        }
    }
}
