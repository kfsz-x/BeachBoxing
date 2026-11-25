using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeTrainingFeedbackTests
    {
        GameObject root;
        Component feedback;
        [SetUp] public void Setup()
        {
            var type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachNativeTrainingFeedback");
            Assert.That(type, Is.Not.Null, "Missing source training presentation component");
            root = new GameObject("Training feedback fixture");
            feedback = root.AddComponent(type);
        }
        [TearDown] public void Cleanup() { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        object Call(string method, params object[] args) => feedback.GetType().GetMethod(method).Invoke(feedback, args);
        T Read<T>(string name) => (T)feedback.GetType().GetProperty(name).GetValue(feedback);
        [Test] public void HistoryUsesOldSpeedHalfSecondWindowAndPreservesItAcrossWorkoutLifecycle()
        {
            Call("Enter", Muscle.Chest);
            Call("Sample", .5f, 0f, 0f);
            Call("Sample", 1.5f, .5f, .5f);
            Assert.That(Read<float>("MeanSpeed"), Is.EqualTo(1f).Within(.00001f));
            Call("ResetBar"); Call("Exit"); Call("Enter", Muscle.Arms);
            Assert.That(Read<int>("HistoryCount"), Is.EqualTo(2));
            Call("Sample", 2f, 1.001f, .501f);
            Assert.That(Read<float>("MeanSpeed"), Is.EqualTo(2f).Within(.00001f));
            Call("ResetScene");
            Assert.That(Read<int>("HistoryCount"), Is.Zero);
        }
        [TestCase(.59f, 43, 0)] [TestCase(.6f, 45, 20)]
        [TestCase(1f, 46, 50)] [TestCase(1.5f, 46, 90)]
        public void SourceThresholdsDriveFaceAndSweat(float speed, int face, int sweat)
        {
            Call("Enter", Muscle.Chest); Call("Sample", speed, 1f, .5f);
            Assert.That(Read<int>("FaceSourceId"), Is.EqualTo(face));
            Assert.That(Read<int>("SweatRate"), Is.EqualTo(sweat));
            Call("Sample", 3f, 2f, .5f);
            Assert.That(Read<int>("FaceSourceId"), Is.EqualTo(face));
            Assert.That(Read<int>("SweatRate"), Is.EqualTo(sweat));
        }
        [Test] public void CompletionIsPresentationOnlyAndUsesSourceShake()
        {
            float amplitude = 0, duration = 0;
            Call("Bind", new Action<float,float>((a,d) => { amplitude=a; duration=d; }));
            var progress = new PlayerProgress { chest = 20, coins = 10 };
            Call("Enter", Muscle.Chest); Call("Observe", Muscle.Chest, .5f, progress, false);
            Call("Completed", Muscle.Chest, 11);
            Assert.That(progress.chest, Is.EqualTo(20)); Assert.That(progress.coins, Is.EqualTo(10));
            Assert.That(amplitude, Is.EqualTo(.08f)); Assert.That(duration, Is.EqualTo(.14f));
        }
        [Test] public void RewardClockDelaysFlightExpiresAndResetsOnReuse()
        {
            var type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachNativeRewardParticle");
            Assert.That(type, Is.Not.Null);
            var go = new GameObject("pool slot", typeof(RectTransform)); go.transform.SetParent(root.transform);
            var slot = go.AddComponent(type);
            var target = new GameObject("target", typeof(RectTransform)); target.transform.SetParent(root.transform);
            target.transform.position = Vector3.right * 100;
            type.GetField("target").SetValue(slot, target.transform);
            var begin = type.GetMethod("Begin"); var tick = type.GetMethod("Tick"); var fixedTick = type.GetMethod("FixedTick");
            begin.Invoke(slot, new object[] { Vector3.zero, Quaternion.identity, 11 });
            fixedTick.Invoke(slot, new object[] { .1f }); Assert.That(go.transform.position.x, Is.Zero);
            fixedTick.Invoke(slot, new object[] { .1f }); Assert.That(go.transform.position.x, Is.GreaterThan(0));
            tick.Invoke(slot, new object[] { 1.5f }); Assert.That(go.activeSelf, Is.False);
            begin.Invoke(slot, new object[] { Vector3.zero, Quaternion.identity, 17 });
            Assert.That((float)type.GetProperty("FlightClock").GetValue(slot), Is.EqualTo(-.15f));
            Assert.That((bool)type.GetProperty("CollectionPlayed").GetValue(slot), Is.False);
            tick.Invoke(slot, new object[] { .916667f });
            Assert.That((bool)type.GetProperty("CollectionPlayed").GetValue(slot), Is.True);
        }
    }
}

