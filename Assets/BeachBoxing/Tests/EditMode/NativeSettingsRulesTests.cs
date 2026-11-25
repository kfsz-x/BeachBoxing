using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityCore;
using UnityEngine;

namespace BeachBoxing.Tests
{
    // Reflection keeps the RED tests compilable before the new runtime types exist.
    public sealed class NativeSettingsRulesTests
    {
        private Type rulesType;
        private LocalStore storage;

        [SetUp]
        public void FindRulesWithoutTouchingTheUsersSave()
        {
            rulesType = typeof(PlayerProgress).Assembly.GetType("BeachBoxing.BeachNativeSettingsRules");
            Assert.That(rulesType, Is.Not.Null, "Native settings rules are missing from the runtime assembly.");
            storage = new LocalStore("native-settings-test-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveOnlyThisTestsPrivateKeys()
        {
            if (storage == null) return;
            storage.Delete("MusicVolume");
            storage.Delete("SFXVolume");
            storage.Delete("beachBoxing.progress");
            for (int i = 0; i < 9; i++) storage.Delete("Keybind" + i);
            storage.Save();
        }

        [TestCase(0f, -80f)]
        [TestCase(0.5f, -40f)]
        [TestCase(1f, 0f)]
        [TestCase(1.5f, 10f)]
        [TestCase(2f, 20f)]
        public void VolumeUsesTheTwoOriginalLinearSegments(float volume, float decibels)
        {
            Assert.That(Call<float>(null, "MapVolumeToDecibels", volume), Is.EqualTo(decibels).Within(0.0001f));
        }

        [Test]
        public void FreshSettingsPersistTheNineOriginalRoleDefaults()
        {
            object rules = Create();
            var expected = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space,
                KeyCode.Escape, KeyCode.M, KeyCode.I, KeyCode.E };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(Call<KeyCode>(rules, "GetKey", i), Is.EqualTo(expected[i]), "Role " + i);
                Assert.That(storage.GetString("Keybind" + i), Is.EqualTo(expected[i].ToString()));
            }
            Assert.That(Property<float>(rules, "SfxVolume"), Is.EqualTo(1));
            Assert.That(Property<float>(rules, "MusicVolume"), Is.EqualTo(1));
        }

        [Test]
        public void MissingValuesAreInitializedWithoutReplacingOldSettingsOrProgress()
        {
            storage.SetFloat("SFXVolume", 0);
            storage.SetFloat("MusicVolume", 1.75f);
            storage.SetString("Keybind0", "Q");
            storage.SetString("Keybind5", "Mouse1");
            storage.SetString("beachBoxing.progress", "preserve-this-progress");
            object rules = Create();
            Assert.That(Property<float>(rules, "SfxVolume"), Is.Zero);
            Assert.That(Property<float>(rules, "MusicVolume"), Is.EqualTo(1.75f));
            Assert.That(Call<KeyCode>(rules, "GetKey", 0), Is.EqualTo(KeyCode.Q));
            Assert.That(Call<KeyCode>(rules, "GetKey", 5), Is.EqualTo(KeyCode.Mouse1));
            Assert.That(Call<KeyCode>(rules, "GetKey", 8), Is.EqualTo(KeyCode.E));
            Assert.That(storage.GetString("beachBoxing.progress"), Is.EqualTo("preserve-this-progress"));
        }

        [Test]
        public void RebindingRoundTripsEveryRoleAndAllowsDuplicatesMouseAndEscape()
        {
            object rules = Create();
            var saved = new[] { KeyCode.Mouse0, KeyCode.Mouse0, KeyCode.Escape, KeyCode.RightArrow,
                KeyCode.F, KeyCode.Tab, KeyCode.M, KeyCode.I, KeyCode.Return };
            for (int i = 0; i < saved.Length; i++) Call<object>(rules, "Rebind", i, saved[i]);
            object reloaded = Create();
            for (int i = 0; i < saved.Length; i++)
                Assert.That(Call<KeyCode>(reloaded, "GetKey", i), Is.EqualTo(saved[i]), "Reloaded role " + i);
        }

        [Test]
        public void BothVolumeKeysRoundTripIncludingMuteAndTheUpperEndpoint()
        {
            object rules = Create();
            Call<object>(rules, "SetVolumes", 0f, 2f);
            object reloaded = Create();
            Assert.That(Property<float>(reloaded, "SfxVolume"), Is.Zero);
            Assert.That(Property<float>(reloaded, "MusicVolume"), Is.EqualTo(2));
            Assert.That(storage.GetFloat("SFXVolume", -1), Is.Zero);
            Assert.That(storage.GetFloat("MusicVolume", -1), Is.EqualTo(2));
        }

        [TestCase(" B0 4 1 5 3 Z ", "boaises")]
        [TestCase("\tBAD\n", "\tbad\n")]
        public void FilterNormalizationUsesOnlyTheOriginalOrderedSubstitutions(string value, string normalized)
        {
            Assert.That(Call<string>(null, "NormalizeNameForFilter", value), Is.EqualTo(normalized));
        }

        [Test]
        public void FilteringRejectsNormalizedSubstringsButAllowsEmptyAndUnlimitedNames()
        {
            object rules = Create(new[] { "bad", "ass" });
            Assert.That(Call<bool>(rules, "IsAllowedName", "A B4D Runner"), Is.False);
            Assert.That(Call<bool>(rules, "IsAllowedName", "cl4ssic"), Is.False);
            Assert.That(Call<bool>(rules, "IsAllowedName", "Kind Runner"), Is.True);
            Assert.That(Call<bool>(rules, "IsAllowedName", ""), Is.True);
            Assert.That(Call<bool>(rules, "IsAllowedName", "   "), Is.True);
            Assert.That(Call<bool>(rules, "IsAllowedName", new string('大', 4096)), Is.True);
        }

        [Test]
        public void TheCompleteNativeFilterRemainsAnAuthoredDependency()
        {
            var audit = JsonUtility.FromJson<NativeAudit>(File.ReadAllText(
                Path.Combine(Application.dataPath, "../docs/beach-boxing/evidence/native-settings-2026-11-20.json")));
            Assert.That(audit.name_filter.profanity_words.Length, Is.EqualTo(293));
            object rules = Create(audit.name_filter.profanity_words);
            Assert.That(Call<bool>(rules, "IsAllowedName", "f u c k"), Is.False);
            Assert.That(Call<bool>(rules, "IsAllowedName", "Kind Runner"), Is.True);
        }

        private object Create(string[] words = null)
        {
            var constructor = rulesType.GetConstructor(new[] { typeof(LocalStore), typeof(string[]) });
            Assert.That(constructor, Is.Not.Null, "Settings must accept the existing LocalStore and editor-bound native filter.");
            return constructor.Invoke(new object[] { storage, words ?? new string[0] });
        }

        private T Call<T>(object instance, string name, params object[] args)
        {
            var method = rulesType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "Missing settings behavior " + name);
            object result = method.Invoke(instance, args);
            return result == null ? default(T) : (T)result;
        }

        private T Property<T>(object instance, string name)
        {
            var property = rulesType.GetProperty(name);
            Assert.That(property, Is.Not.Null, "Missing settings value " + name);
            return (T)property.GetValue(instance);
        }

        [Serializable] private sealed class NativeAudit { public NativeFilter name_filter; }
        [Serializable] private sealed class NativeFilter { public string[] profanity_words; }
    }
}
