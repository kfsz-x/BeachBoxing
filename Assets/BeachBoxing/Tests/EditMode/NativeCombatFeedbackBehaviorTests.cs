using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class NativeCombatFeedbackBehaviorTests
    {
        private GameObject root;
        private Component feedback;
        private GameObject screen, damage, low, healing, energy, hint, marker;
        private Transform spawn;
        private Text label;

        [SetUp]
        public void SetUp()
        {
            var type = typeof(FightController).Assembly.GetType("BeachBoxing.BeachNativeCombatFeedback");
            Assert.That(type, Is.Not.Null, "The pending fight has no native combat feedback owner.");
            root = new GameObject("Native combat feedback test");
            feedback = root.AddComponent(type);
            screen = Child("UIScreen", root.transform);
            damage = Child("TakeDamage", screen.transform);
            low = Child("LowHealth", screen.transform);
            healing = Child("Healing", screen.transform);
            energy = Child("OutOfStaminaAlert", screen.transform);
            hint = Child("BlockHintScreen", screen.transform);
            marker = Child("HitMarker", root.transform);
            label = new GameObject("Text", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(marker.transform, false);
            spawn = Child("HitMarkerSpawn", root.transform).transform;
            Bind("screen", screen);
            Bind("takeDamage", damage);
            Bind("lowHealth", low);
            Bind("healing", healing);
            Bind("outOfStamina", energy);
            Bind("blockHint", hint);
            Bind("markerRoots", new[] { marker.transform });
            Bind("markerLabels", new[] { label });
            Bind("markerSpawns", new[] { spawn });
            Invoke("ResetEffects");
        }

        [TearDown]
        public void TearDown()
        {
            if (root) UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void ThresholdsAreAbsoluteAndHintStaysVisibleDuringGuard()
        {
            Render(16, 16, false, true);
            Assert.That(low.activeInHierarchy, Is.False);
            Assert.That(hint.activeInHierarchy, Is.False);
            Render(15, 15, true, true);
            Assert.That(low.activeInHierarchy, Is.True);
            Assert.That(hint.activeInHierarchy, Is.True);
        }

        [Test]
        public void AcceptedGuardShowsHealingAtFullHealthAndScreenGateClearsIt()
        {
            Render(100, 100, true, true);
            Assert.That(healing.activeInHierarchy, Is.True);
            Render(100, 100, false, true);
            Assert.That(healing.activeInHierarchy, Is.False);
            Render(10, 10, true, false, true);
            Assert.That(screen.activeSelf, Is.False);
            Assert.That(low.activeInHierarchy || healing.activeInHierarchy || hint.activeInHierarchy || energy.activeInHierarchy, Is.False);
        }

        [Test]
        public void OnlyHealthHitsRestartTheHalfSecondDamageOverlay()
        {
            Render(100, 100, false, true);
            Invoke("PlayerHit", true);
            Assert.That(damage.activeInHierarchy, Is.False);
            Invoke("PlayerHit", false);
            Advance(.375f);
            Assert.That(damage.activeInHierarchy, Is.True);
            Invoke("PlayerHit", false);
            Advance(.375f);
            Assert.That(damage.activeInHierarchy, Is.True);
            Advance(.125f);
            Assert.That(damage.activeInHierarchy, Is.False);
        }

        [Test]
        public void ActiveEnergyAlertDoesNotExtendOnRepeatedEmptyPunch()
        {
            Render(100, 0, false, true, true);
            Advance(1f);
            Assert.That(energy.activeInHierarchy, Is.True);
            Render(100, 0, false, true, true);
            Advance(.5f);
            Assert.That(energy.activeInHierarchy, Is.False);
            Render(100, 0, false, true, true);
            Assert.That(energy.activeInHierarchy, Is.True);
        }

        [Test]
        public void OnlyEnemyHealthHitsUseAWorldFixedMarkerThatExpiresAndResets()
        {
            Render(100, 100, false, true);
            Invoke("SelectOpponent", 0);
            spawn.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(0, 30, 0));
            Invoke("PlayerHit", false);
            Invoke("EnemyHit", 0, true, 4);
            Assert.That(marker.activeInHierarchy, Is.False);
            Invoke("EnemyHit", 0, false, 7);
            Assert.That(marker.activeInHierarchy, Is.True);
            Assert.That(label.text, Is.EqualTo("-7"));
            Assert.That(label.transform.localScale.x, Is.Zero);
            spawn.SetPositionAndRotation(new Vector3(4, 5, 6), Quaternion.identity);
            Advance(.75f);
            Assert.That(marker.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(Quaternion.Angle(marker.transform.rotation, Quaternion.Euler(0, 30, 0)), Is.LessThan(.1f));
            Assert.That(marker.transform.lossyScale.x, Is.EqualTo(1.068f).Within(.0001f));
            Advance(.25f);
            Assert.That(marker.activeInHierarchy, Is.False);
            Invoke("EnemyHit", 0, false, 3);
            Assert.That(marker.activeInHierarchy, Is.True);
            Assert.That(label.text, Is.EqualTo("-3"));
            Assert.That(label.transform.localScale.x, Is.Zero);
            Assert.That(marker.transform.position, Is.EqualTo(new Vector3(4, 5, 6)));
        }

        [Test]
        public void NativePcmBindingsExposeSourceSamples()
        {
            const string path = "Assets/BeachBoxing/OriginalArt/NativeCombatAudio/native-combat-1-108.mp3";
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.samples, Is.GreaterThan(1024));
            var samples = new float[clip.samples * clip.channels];
            Assert.That(clip.GetData(samples, 0), Is.True);
            Assert.That(samples.Any(sample => Mathf.Abs(sample) > .01f), Is.True);
            var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
        }

        [Test]
        public void MarkerChildFadesAndShiftsWhileTheMarkerRootStaysAtTheSpawn()
        {
            BindIfPresent("markerAnchoredY", AnimationCurve.EaseInOut(.5f, 0, 1f, -2f));
            BindIfPresent("markerAlpha", AnimationCurve.EaseInOut(.5f, 1f, 1f, 0f));
            Invoke("SelectOpponent", 0);
            spawn.position = new Vector3(1, 2, 3);
            Invoke("EnemyHit", 0, false, 4);
            spawn.position = Vector3.zero;
            Advance(.75f);
            Assert.That(marker.transform.position, Is.EqualTo(new Vector3(1, 2, 3)));
            Assert.That(label.rectTransform.anchoredPosition.y, Is.EqualTo(-1f).Within(.0001f));
            Assert.That(label.color.a, Is.EqualTo(.5f).Within(.0001f));
            Advance(.25f);
            Invoke("EnemyHit", 0, false, 7);
            Assert.That(label.rectTransform.anchoredPosition.y, Is.Zero);
            Assert.That(label.color.a, Is.EqualTo(1f));
        }

        [Test]
        public void GuardPitchIsSelectedOnceForEachFightScreenActivation()
        {
            var guard = Child("Guard source", root.transform);
            guard.SetActive(true);
            var source = guard.AddComponent<AudioSource>();
            Bind("playerGuardAudio", source);
            var previousRandom = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(604);
                var firstActivation = UnityEngine.Random.Range(1.2f, .8f);
                var secondActivation = UnityEngine.Random.Range(1.2f, .8f);
                UnityEngine.Random.InitState(604);
                Render(100, 100, false, true);
                Assert.That(source.pitch, Is.EqualTo(firstActivation).Within(.0001f));
                Invoke("PlayerHit", true);
                Invoke("PlayerHit", true);
                Render(100, 100, false, true);
                Assert.That(source.pitch, Is.EqualTo(firstActivation).Within(.0001f));
                Render(100, 100, false, false);
                Render(100, 100, false, true);
                Assert.That(source.pitch, Is.EqualTo(secondActivation).Within(.0001f));
            }
            finally { UnityEngine.Random.state = previousRandom; }
        }

        [Test]
        public void RestoreAddsAndReusesNativeCanvasOnTheMarkerObject()
        {
            var restore = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("BeachBoxing.Editor.BeachNativeCombatFeedbackRestore"))
                .FirstOrDefault(type => type != null);
            Assert.That(restore, Is.Not.Null);
            var getOrAddCanvas = restore.GetMethod("Component", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(typeof(Canvas));
            var canvas = (Canvas)getOrAddCanvas.Invoke(null, new object[] { marker });
            Assert.That((bool)canvas, Is.True, "An absent native component must be added to the marker.");
            canvas.renderMode = RenderMode.WorldSpace;
            var existing = getOrAddCanvas.Invoke(null, new object[] { marker });
            Assert.That(existing, Is.SameAs(canvas));
            Assert.That(marker.GetComponents<Canvas>().Length, Is.EqualTo(1));
        }

        private void Render(float hp, float stamina, bool guarding, bool visible, bool emptyPunch = false)
            => Invoke("Render", hp, stamina, guarding, visible, emptyPunch);

        private void Advance(float seconds) => Invoke("AdvanceEffects", seconds);

        private void Bind(string field, object value)
        {
            var binding = feedback.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(binding, Is.Not.Null, field);
            binding.SetValue(feedback, value);
        }

        private void BindIfPresent(string field, object value)
        {
            var binding = feedback.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            binding?.SetValue(feedback, value);
        }

        private void Invoke(string name, params object[] args)
        {
            var method = feedback.GetType().GetMethod(name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                args.Select(arg => arg.GetType()).ToArray(), null);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(feedback, args);
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.SetActive(false);
            return child;
        }
    }
}
