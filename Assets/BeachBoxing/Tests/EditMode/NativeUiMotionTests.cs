using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeUiMotionTests
    {
        [Test]
        public void ReappearingSourceHintsRestartTheirOriginalLoop()
        {
            WithMotion((root, target, runner, step) =>
            {
                step.Invoke(runner, new object[] { .75f, .75f });
                Assert.That(target.anchoredPosition.y, Is.EqualTo(7.5f).Within(.001f));
                step.Invoke(runner, new object[] { .5f, .5f });
                Assert.That(target.anchoredPosition.y, Is.EqualTo(2.5f).Within(.001f));
                target.gameObject.SetActive(false);
                step.Invoke(runner, new object[] { 3f, 3f });
                target.gameObject.SetActive(true);
                step.Invoke(runner, new object[] { .1f, .1f });
                Assert.That(target.anchoredPosition.y, Is.EqualTo(1f).Within(.001f),
                    "An inactive source Animator does not accumulate hidden time and restarts when revealed.");
            });
        }

        [Test]
        public void NormalSourceMotionStopsWithTheScaledClock()
        {
            WithMotion((root, target, runner, step) =>
            {
                step.Invoke(runner, new object[] { 0f, .5f });
                Assert.That(target.anchoredPosition.y, Is.Zero.Within(.001f));
                step.Invoke(runner, new object[] { .2f, .2f });
                Assert.That(target.anchoredPosition.y, Is.EqualTo(2f).Within(.001f));
            });
        }

        private static void WithMotion(Action<GameObject, RectTransform, Component, MethodInfo> check)
        {
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachNativeUiMotion");
            Assert.That(type, Is.Not.Null, "The prebound source UI motion runner is missing.");
            var root = new GameObject("Source UI motion fixture");
            var target = new GameObject("Animated source hint", typeof(RectTransform)).GetComponent<RectTransform>();
            var clip = new AnimationClip { legacy = true };
            try
            {
                target.SetParent(root.transform, false);
                clip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.y", AnimationCurve.Linear(0, 0, 1, 10));
                var runner = root.AddComponent(type);
                var bindingType = type.GetNestedType("Binding");
                var binding = Activator.CreateInstance(bindingType);
                bindingType.GetField("target").SetValue(binding, target);
                bindingType.GetField("clip").SetValue(binding, clip);
                bindingType.GetField("loop").SetValue(binding, true);
                var bindings = Array.CreateInstance(bindingType, 1);
                bindings.SetValue(binding, 0);
                type.GetField("bindings").SetValue(runner, bindings);
                var step = type.GetMethod("Step", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(step, Is.Not.Null);
                check(root, target, runner, step);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}