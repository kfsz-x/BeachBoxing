using System;
using UnityCore;
using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Original independent settings keys, supplied with the authored name filter.</summary>
    public sealed class BeachNativeSettingsRules
    {
        private static readonly KeyCode[] Defaults =
        {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space,
            KeyCode.Escape, KeyCode.M, KeyCode.I, KeyCode.E
        };

        private readonly LocalStore storage;
        private readonly string[] profanityWords;
        private readonly KeyCode[] keys = new KeyCode[9];

        public float SfxVolume { get; private set; }
        public float MusicVolume { get; private set; }

        public BeachNativeSettingsRules(LocalStore storage, string[] profanityWords)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.profanityWords = profanityWords == null ? Array.Empty<string>() : (string[])profanityWords.Clone();
            bool changed = false;
            SfxVolume = ReadVolume("SFXVolume", ref changed);
            MusicVolume = ReadVolume("MusicVolume", ref changed);
            for (int i = 0; i < keys.Length; i++)
            {
                string value = storage.GetString("Keybind" + i, "\u0001missing-native-key");
                if (value == "\u0001missing-native-key")
                {
                    value = Defaults[i].ToString();
                    storage.SetString("Keybind" + i, value);
                    changed = true;
                }
                keys[i] = Enum.TryParse(value, out KeyCode code) && Enum.IsDefined(typeof(KeyCode), code)
                    ? code : Defaults[i];
            }
            if (changed) storage.Save();
        }

        private float ReadVolume(string key, ref bool changed)
        {
            float value = storage.GetFloat(key, float.MinValue);
            if (value != float.MinValue) return value;
            storage.SetFloat(key, 1);
            changed = true;
            return 1;
        }

        public KeyCode GetKey(int index)
        {
            if (index < 0 || index >= keys.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return keys[index];
        }

        public void Rebind(int index, KeyCode key)
        {
            if (index < 0 || index >= keys.Length) throw new ArgumentOutOfRangeException(nameof(index));
            keys[index] = key;
            storage.SetString("Keybind" + index, key.ToString());
            storage.Save();
        }

        public void SetVolumes(float sfx, float music)
        {
            SfxVolume = Mathf.Clamp(sfx, 0, 2);
            MusicVolume = Mathf.Clamp(music, 0, 2);
            storage.SetFloat("SFXVolume", SfxVolume);
            storage.SetFloat("MusicVolume", MusicVolume);
            storage.Save();
        }

        public static float MapVolumeToDecibels(float volume) => volume <= 1
            ? -80 + 80 * Mathf.Clamp01(volume)
            : 20 * Mathf.Clamp01(volume - 1);

        public static string NormalizeNameForFilter(string value) => (value ?? string.Empty).ToLower()
            .Replace(" ", string.Empty).Replace("0", "o").Replace("4", "a").Replace("1", "i")
            .Replace("5", "s").Replace("3", "e").Replace("z", "s");

        public bool IsAllowedName(string value)
        {
            string normalized = NormalizeNameForFilter(value);
            foreach (string forbidden in profanityWords)
                if (!string.IsNullOrEmpty(forbidden) && normalized.Contains(forbidden)) return false;
            return true;
        }
    }
}
