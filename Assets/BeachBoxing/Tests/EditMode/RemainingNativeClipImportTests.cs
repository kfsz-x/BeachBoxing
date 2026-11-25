using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class RemainingNativeClipImportTests
    {
        [Test]
        public void OriginalImageAlphaTrackSamplesTheGraphicColor()
        {
            var root = new GameObject("source Fade", typeof(RectTransform), typeof(Image));
            try
            {
                Clip(351).SampleAnimation(root, 0.5f);
                Assert.That(root.GetComponent<Image>().color.a, Is.EqualTo(0.78039217f).Within(0.0001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void OriginalTmpColorTrackSamplesTheRestoredSdfText()
        {
            var root = new GameObject("source Countdown", typeof(RectTransform), typeof(OriginalSdfText));
            try
            {
                Clip(348).SampleAnimation(root, 0);
                var color = root.GetComponent<OriginalSdfText>().color;
                Assert.That(color.r, Is.EqualTo(1).Within(0.0001f));
                Assert.That(color.g, Is.EqualTo(0.7170844f).Within(0.0001f));
                Assert.That(color.b, Is.Zero.Within(0.0001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void OriginalPointingTrackSamplesAnchoredPositionInsteadOfTransformFallback()
        {
            var root = new GameObject("source Pointing", typeof(RectTransform));
            try
            {
                Clip(342).SampleAnimation(root, 0.5f);
                Assert.That(root.GetComponent<RectTransform>().anchoredPosition.y,
                    Is.EqualTo(-35.2f).Within(0.0001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static AnimationClip Clip(int id)
        {
            string path = "Assets/BeachBoxing/OriginalArt/UI/NativeAnimations/Native-" + id + ".anim";
            Assert.That(AssetDatabase.LoadAssetAtPath<AnimationClip>(path), Is.Not.Null,
                "The editor append must generate the original mapped clip before motion can play.");
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("BeachBoxing.Editor.BeachRemainingUiRestore")).FirstOrDefault(t => t != null);
            Assert.That(type, Is.Not.Null, "The incremental editor importer is missing.");
            return (AnimationClip)type.GetMethod("Clip").Invoke(null, new object[] { id });
        }
    }
}
