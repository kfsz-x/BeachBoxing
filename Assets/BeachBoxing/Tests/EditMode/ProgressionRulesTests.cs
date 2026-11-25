using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;

namespace BeachBoxing.Tests
{
    public sealed class ProgressionRulesTests
    {
        private Type rules;

        [SetUp]
        public void FindRules()
        {
            rules = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachProgressionRules");
            Assert.That(rules, Is.Not.Null, "BeachProgressionRules is missing from the runtime assembly.");
        }

        [TestCase(0, "0")]
        [TestCase(999, "999")]
        [TestCase(1000, "1.00k")]
        [TestCase(4240, "4.24k")]
        [TestCase(1000000, "1000.00k")]
        public void StrengthFormattingUsesInvariantKilounits(int strength, string expected)
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.That(Invoke<string>("FormatNumber", strength), Is.EqualTo(expected));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [TestCase(0, 1, 0f)]
        [TestCase(500, 1, 0.5f)]
        [TestCase(1000, 2, 0f)]
        [TestCase(2000, 2, 0.5f)]
        [TestCase(500, 2, 0f)]
        [TestCase(6000, 4, 0f)]
        [TestCase(39000, 8, 0.5f)]
        [TestCase(85000, 10, 0.5f)]
        [TestCase(int.MaxValue, 10, 1f)]
        public void FillMeasuresOnlyTheCurrentBand(int strength, int level, float expected)
        {
            Assert.That(Invoke<float>("Fraction", strength, level), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(1, 1000)]
        [TestCase(2, 3000)]
        [TestCase(3, 6000)]
        [TestCase(4, 10000)]
        [TestCase(5, 15000)]
        [TestCase(6, 23000)]
        [TestCase(7, 33000)]
        [TestCase(8, 45000)]
        [TestCase(9, 70000)]
        [TestCase(10, 100000)]
        public void FullIndicatorStartsAtBandMaximum(int level, int maximum)
        {
            Assert.That(Invoke<bool>("IsFull", maximum - 1, level), Is.False);
            Assert.That(Invoke<bool>("IsFull", maximum, level), Is.True);
            Assert.That(Invoke<bool>("IsFull", maximum + 1, level), Is.True);
        }

        [Test]
        public void NewProgressStartsUnreadyAtLevelOne()
        {
            var progress = new PlayerProgress();

            Assert.That(Invoke<bool>("CanLevelUp", progress, 1), Is.False);
            Assert.That(Invoke<int>("NextLevel", progress, 1), Is.EqualTo(1));
        }

        [TestCase(2999, 3000, 3000, 3000)]
        [TestCase(3000, 2999, 3000, 3000)]
        [TestCase(3000, 3000, 2999, 3000)]
        [TestCase(3000, 3000, 3000, 2999)]
        public void EachMuscleMustFillBeforeLevelUp(int chest, int arms, int back, int legs)
        {
            var progress = new PlayerProgress { chest = chest, arms = arms, back = back, legs = legs };

            Assert.That(Invoke<bool>("CanLevelUp", progress, 2), Is.False);
            Assert.That(Invoke<int>("NextLevel", progress, 2), Is.EqualTo(2));
        }

        [Test]
        public void LevelUpAdvancesOneLevelAndPreservesScores()
        {
            var progress = new PlayerProgress { chest = 70000, arms = 80000, back = 90000, legs = 100000 };

            Assert.That(Invoke<bool>("CanLevelUp", progress, 9), Is.True);
            Assert.That(Invoke<int>("NextLevel", progress, 9), Is.EqualTo(10));
            Assert.That(new[] { progress.chest, progress.arms, progress.back, progress.legs },
                Is.EqualTo(new[] { 70000, 80000, 90000, 100000 }));
        }

        [Test]
        public void MaximumLevelCannotAdvanceWithAllMusclesFull()
        {
            var progress = new PlayerProgress { chest = 100000, arms = 100000, back = 100000, legs = 100000 };

            Assert.That(Invoke<bool>("CanLevelUp", progress, 10), Is.False);
            Assert.That(Invoke<int>("NextLevel", progress, 10), Is.EqualTo(10));
        }

        [TestCase(1, 11, 1f, 0f)]
        [TestCase(2, 13, 1.15f, 1f)]
        [TestCase(3, 15, 1.3f, 2f)]
        [TestCase(4, 20, 1.45f, 2.5f)]
        [TestCase(5, 24, 1.6f, 3f)]
        [TestCase(6, 28, 1.7f, 3.2f)]
        [TestCase(7, 33, 1.8f, 3.4f)]
        [TestCase(8, 38, 1.9f, 3.6f)]
        [TestCase(9, 43, 2f, 3.8f)]
        [TestCase(10, 48, 2.1f, 4f)]
        public void PrestigeSelectsTheNativeGainAndBodyTargets(int level, int gain, float scale, float cameraY)
        {
            Assert.That(Invoke<int>("BaseGain", level), Is.EqualTo(gain));
            Assert.That(Invoke<float>("RootScale", level), Is.EqualTo(scale));
            Assert.That(Invoke<float>("CameraTargetY", level), Is.EqualTo(cameraY));
        }

        [TestCase(1, 0, 11)]
        [TestCase(1, 15, 12)]
        [TestCase(8, 10, 41)]
        [TestCase(10, 20, 57)]
        public void GainAppliesTheModifierBeforeTruncating(int level, int modifier, int expected)
        {
            Assert.That(Invoke<int>("CalculateGain", level, modifier), Is.EqualTo(expected));
        }

        [TestCase(0, "Rookie")]
        [TestCase(4, "Rookie")]
        [TestCase(5, "Amateur")]
        [TestCase(10, "Semi Pro")]
        [TestCase(15, "Rising Star")]
        [TestCase(20, "Pro Boxer")]
        [TestCase(25, "Division Threat")]
        [TestCase(30, "Title Challenger")]
        [TestCase(35, "Title Champion")]
        [TestCase(40, "World Champion")]
        [TestCase(45, "Boxing Legend")]
        [TestCase(50, "Boxing Legend")]
        public void RankDependsOnOpponentGroupsOfFive(int unlockedOpponent, string expected)
        {
            Assert.That(Invoke<string>("RankTitle", unlockedOpponent), Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(11)]
        public void InvalidPrestigeLevelsAreRejected(int level)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<float>("Fraction", 0, level));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<bool>("IsFull", 0, level));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<bool>("CanLevelUp", new PlayerProgress(), level));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<int>("NextLevel", new PlayerProgress(), level));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<int>("BaseGain", level));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<int>("CalculateGain", level, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<float>("RootScale", level));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<float>("CameraTargetY", level));
        }

        [Test]
        public void NegativeProgressionInputsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<string>("FormatNumber", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<float>("Fraction", -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<bool>("IsFull", -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<int>("CalculateGain", 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Invoke<string>("RankTitle", -1));
        }

        [Test]
        public void MissingOrMalformedPlayerProgressCannotAdvance()
        {
            Assert.Throws<ArgumentNullException>(() => Invoke<int>("NextLevel", null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Invoke<int>("NextLevel", new PlayerProgress { legs = -1 }, 1));
        }

        private T Invoke<T>(string name, params object[] args)
        {
            MethodInfo method = rules.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"BeachProgressionRules.{name} is missing.");
            try
            {
                return (T)method.Invoke(null, args);
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
