using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class AutoTrainingTests
    {
        [Test]
        public void AutoTrainingToggleDoesNotGrantOrCancelAcceleration()
        {
            WithTraining(training =>
            {
                Assert.That(Read<bool>(training, "Enabled"), Is.False);
                Invoke(training, "SetEnabled", true);
                Assert.That(Read<bool>(training, "Enabled"), Is.True);
                Assert.That(Read<bool>(training, "Accelerated"), Is.False);

                Invoke(training, "ResolveReward", true);
                Invoke(training, "SetEnabled", false);
                Assert.That(Read<bool>(training, "Enabled"), Is.False);
                Assert.That(Read<bool>(training, "Accelerated"), Is.True);
            });
        }

        [Test]
        public void CompletedRewardGrants120SecondsWithWholeSecondDisplay()
        {
            WithTraining(training =>
            {
                Invoke(training, "ResolveReward", true);
                Assert.That(Read<int>(training, "SecondsLeft"), Is.EqualTo(120));
                Invoke(training, "Advance", 0.5f);
                Assert.That(Read<int>(training, "SecondsLeft"), Is.EqualTo(120));
                Invoke(training, "Advance", 0.5f);
                Assert.That(Read<int>(training, "SecondsLeft"), Is.EqualTo(119));
                Invoke(training, "Advance", 119f);
                Assert.That(Read<int>(training, "SecondsLeft"), Is.Zero);
                Assert.That(Read<bool>(training, "Accelerated"), Is.False);
            });
        }

        [Test]
        public void EarlyCloseAndDuplicateCompletionDoNotRestartTheCountdown()
        {
            WithTraining(training =>
            {
                Invoke(training, "ResolveReward", false);
                Assert.That(Read<int>(training, "SecondsLeft"), Is.Zero);

                Invoke(training, "ResolveReward", true);
                Invoke(training, "Advance", 35f);
                Invoke(training, "ResolveReward", false);
                Invoke(training, "ResolveReward", true);
                Assert.That(Read<int>(training, "SecondsLeft"), Is.EqualTo(85));
            });
        }

        [Test]
        public void MissingAdConfigurationReportsFailureWithoutReward()
        {
            WithTraining(training =>
            {
                int completions = 0;
                bool result = true;
                Action<bool> complete = succeeded =>
                {
                    completions++;
                    result = succeeded;
                };
                Invoke(training, "RequestAcceleration", complete);

                Assert.That(completions, Is.EqualTo(1));
                Assert.That(result, Is.False);
                Assert.That(Read<bool>(training, "Accelerated"), Is.False);
            });
        }

        private static void WithTraining(Action<Component> run)
        {
            Type type = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachAutoTraining");
            Assert.That(type, Is.Not.Null, "BeachAutoTraining is missing from the runtime assembly.");
            var game = new GameObject("Auto training fixture");
            try
            {
                run(game.AddComponent(type));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(game);
            }
        }

        private static T Read<T>(Component training, string property)
        {
            PropertyInfo member = training.GetType().GetProperty(property);
            Assert.That(member, Is.Not.Null, $"BeachAutoTraining.{property} is missing.");
            return (T)member.GetValue(training);
        }

        private static void Invoke(Component training, string method, object input)
        {
            MethodInfo member = training.GetType().GetMethod(method);
            Assert.That(member, Is.Not.Null, $"BeachAutoTraining.{method} is missing.");
            member.Invoke(training, new[] { input });
        }
    }
}
