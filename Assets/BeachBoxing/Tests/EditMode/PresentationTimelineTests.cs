using System;
using System.Reflection;
using NUnit.Framework;

namespace BeachBoxing.Tests
{
    public sealed class PresentationTimelineTests
    {
        // Reflection keeps this RED runnable before the production class exists.
        private static object Timeline()
        {
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachPresentationTimeline");
            Assert.That(type, Is.Not.Null, "The original presentation clock has not been restored.");
            return Activator.CreateInstance(type);
        }

        private static void Call(object timeline, string method, params object[] values) =>
            timeline.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance).Invoke(timeline, values);
        private static T Value<T>(object timeline, string name) =>
            (T)timeline.GetType().GetProperty(name).GetValue(timeline);
        private static bool Consume(object timeline, string name)
        {
            var method = timeline.GetType().GetMethod(name);
            Assert.That(method, Is.Not.Null, "The source animation event has not been restored: " + name);
            return (bool)method.Invoke(timeline, null);
        }

        private static object NativeCurve(params float[][] keys)
        {
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachPresentationCubicCurve");
            Assert.That(type, Is.Not.Null, "The native camera's streamed cubic curve sampler is missing.");
            var curve = Activator.CreateInstance(type);
            var field = type.GetField("keys");
            var keyType = field.FieldType.GetElementType();
            var data = Array.CreateInstance(keyType, keys.Length);
            string[] fields = { "time", "a", "b", "c", "d" };
            for (int index = 0; index < keys.Length; index++)
            {
                var key = Activator.CreateInstance(keyType);
                for (int axis = 0; axis < fields.Length; axis++) keyType.GetField(fields[axis]).SetValue(key, keys[index][axis]);
                data.SetValue(key, index);
            }
            field.SetValue(curve, data);
            return curve;
        }

        private static float CurveAt(object curve, float seconds) =>
            (float)curve.GetType().GetMethod("Evaluate").Invoke(curve, new object[] { seconds });

        [Test]
        public void IntroOrbitEvaluatesOriginalCubicYawInsteadOfLinearInterpolation()
        {
            var curve = NativeCurve(new[] { 0f, -28.800003f, 108.000008f, 0, 0 }, new[] { 2.5f, 0, 0, 0, 225f });
            Assert.That(CurveAt(curve, 1f), Is.EqualTo(79.200005f).Within(.0001f));
            Assert.That(CurveAt(curve, 2.5f), Is.EqualTo(225f));
            Assert.That(CurveAt(curve, 4.5f), Is.EqualTo(225f));
        }

        [Test]
        public void IntroCameraPositionClampsBeforeStartAndUsesNextNativeSegment()
        {
            var curve = NativeCurve(new[] { 0f, .753434f, -3.611586f, 0, 26.3f },
                new[] { 2.5f, .841425f, -1.272513f, -3.931034f, 15.5f }, new[] { 4.35f, 0, 0, 0, 9.2f });
            Assert.That(CurveAt(curve, -1f), Is.EqualTo(26.3f));
            Assert.That(CurveAt(curve, 1f), Is.EqualTo(23.441848f).Within(.0001f));
            Assert.That(CurveAt(curve, 2.5f), Is.EqualTo(15.5f));
            Assert.That(CurveAt(curve, 4.5f), Is.EqualTo(9.2f));
        }

        [Test]
        public void OpeningLocksCombatUntilFightBannerHasFinishedAtFourPointFiveSeconds()
        {
            var timeline = Timeline();
            Call(timeline, "BeginOpening");
            Call(timeline, "Advance", 4.49f, false);
            Assert.That(Value<bool>(timeline, "OpeningComplete"), Is.False);
            Call(timeline, "Advance", .01f, false);
            Assert.That(Value<bool>(timeline, "OpeningComplete"), Is.True);
        }

        [Test]
        public void KnockoutTimeScaleRecoversEachFrameUsingScaledDeltaTime()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            for (int frame = 0; frame < 60; frame++) Call(timeline, "Advance", 1f / 60f, false);
            Assert.That(Value<float>(timeline, "ResultTime"), Is.EqualTo(.1570372f).Within(.00001f));
            Assert.That(Value<float>(timeline, "TimeScale"), Is.EqualTo(.2309624f).Within(.00001f));
            for (int frame = 0; frame < 240; frame++) Call(timeline, "Advance", 1f / 60f, false);
            Assert.That(Value<float>(timeline, "ResultTime"), Is.EqualTo(2.7443643f).Within(.0001f));
            Assert.That(Value<float>(timeline, "TimeScale"), Is.EqualTo(.9430571f).Within(.00001f));
        }

        [Test]
        public void PausePreservesRecoveredSpeedAndResultAnimationTime()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            for (int frame = 0; frame < 60; frame++) Call(timeline, "Advance", 1f / 60f, false);
            Call(timeline, "Advance", 3f, true);
            Assert.That(Value<float>(timeline, "ResultTime"), Is.EqualTo(.1570372f).Within(.00001f));
            Assert.That(Value<float>(timeline, "TimeScale"), Is.EqualTo(.2309624f).Within(.00001f));
            Call(timeline, "Advance", 1f / 60f, false);
            Assert.That(Value<float>(timeline, "ResultTime"), Is.GreaterThan(.1570372f));
        }

        [Test]
        public void PausingKnockoutFreezesPresentationAndKeepsSlowMotionForResume()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            Call(timeline, "Advance", 3f, true);
            Assert.That(Value<float>(timeline, "ResultTime"), Is.Zero);
            Assert.That(Value<float>(timeline, "TimeScale"), Is.EqualTo(.1f));
            Call(timeline, "Advance", 2f, false);
            Assert.That(Value<float>(timeline, "ResultTime"), Is.EqualTo(.2f).Within(.00001f));
        }

        [Test]
        public void ReturnTransitionRestoresNormalTimeAndWaitsForOriginalLoadDelay()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            Call(timeline, "BeginTransition");
            Assert.That(Value<float>(timeline, "TimeScale"), Is.EqualTo(1f));
            Call(timeline, "Advance", .179f, false);
            Assert.That(Value<bool>(timeline, "TransitionComplete"), Is.False);
            Call(timeline, "Advance", .001f, false);
            Assert.That(Value<bool>(timeline, "TransitionComplete"), Is.True);
        }

        [Test]
        public void HomeResetClearsSlowMotionAndOldResultClockBeforeNextFight()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            Call(timeline, "Advance", 12f, false);
            Call(timeline, "Reset");
            Assert.That(Value<float>(timeline, "TimeScale"), Is.EqualTo(1f));
            Assert.That(Value<float>(timeline, "ResultTime"), Is.Zero);
            Call(timeline, "BeginOpening");
            Assert.That(Value<float>(timeline, "OpeningTime"), Is.Zero);
            Assert.That(Value<bool>(timeline, "OpeningComplete"), Is.False);
        }

        [Test]
        public void OpeningAudioFiresOnceAtSourceEventRatherThanEveryCountdownNumber()
        {
            var timeline = Timeline();
            Call(timeline, "BeginOpening");
            Call(timeline, "Advance", .11f, false);
            Assert.That(Consume(timeline, "ConsumeOpeningSound"), Is.False);
            Call(timeline, "Advance", .01f, false);
            Assert.That(Consume(timeline, "ConsumeOpeningSound"), Is.True);
            Call(timeline, "Advance", 1f, false);
            Assert.That(Consume(timeline, "ConsumeOpeningSound"), Is.False);
        }

        [Test]
        public void ResultSoundUsesClipTimeAndFiresOnceDuringKnockoutSlowMotion()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            Call(timeline, "Advance", 13.16f, false);
            Assert.That(Consume(timeline, "ConsumeResultSound"), Is.False);
            Call(timeline, "Advance", .01f, false);
            Assert.That(Consume(timeline, "ConsumeResultSound"), Is.True);
            Assert.That(Consume(timeline, "ConsumeResultSound"), Is.False);
        }

        [Test]
        public void VictoryParticlesWaitTwoScaledSecondsAndResetForNextResult()
        {
            var timeline = Timeline();
            Call(timeline, "BeginResult", .1f);
            Call(timeline, "Advance", 19.99f, false);
            Assert.That(Consume(timeline, "ConsumeVictoryParticles"), Is.False);
            Call(timeline, "Advance", .02f, false);
            Assert.That(Consume(timeline, "ConsumeVictoryParticles"), Is.True);
            Assert.That(Consume(timeline, "ConsumeVictoryParticles"), Is.False);
            Call(timeline, "BeginResult", 1f);
            Assert.That(Consume(timeline, "ConsumeVictoryParticles"), Is.False);
            Call(timeline, "Advance", 2f, false);
            Assert.That(Consume(timeline, "ConsumeVictoryParticles"), Is.True);
        }
    }
}
