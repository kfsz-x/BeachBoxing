using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityCore
{
    public sealed class LocalStore
    {
        private readonly string _prefix;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int UC_StorageAvailable();
        [DllImport("__Internal")] private static extern int UC_StorageSetString(string key, string value);
        [DllImport("__Internal")] private static extern string UC_StorageGetString(string key, string fallback);
        [DllImport("__Internal")] private static extern void UC_StorageDeleteKey(string key);

        private static bool UseWebStorage => UC_StorageAvailable() != 0;
#else
        private static bool UseWebStorage => false;
#endif

        public LocalStore(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentException("A storage prefix is required.", nameof(prefix));

            // Include the prefix length so delimiters in a prefix or key cannot cause collisions.
            _prefix = prefix.Length.ToString(CultureInfo.InvariantCulture) + ":" + prefix + ":";
        }

        public int GetInt(string key, int fallback = 0)
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage)
            {
                string raw = UC_StorageGetString(k, "");
                return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : fallback;
            }
#endif
            return UnityEngine.PlayerPrefs.GetInt(k, fallback);
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage)
            {
                string raw = UC_StorageGetString(k, "");
                return float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback;
            }
#endif
            return UnityEngine.PlayerPrefs.GetFloat(k, fallback);
        }

        public string GetString(string key, string fallback = "")
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage)
                return UC_StorageGetString(k, fallback ?? "");
#endif
            return UnityEngine.PlayerPrefs.GetString(k, fallback);
        }

        public bool GetBool(string key, bool fallback = false) => GetInt(key, fallback ? 1 : 0) != 0;

        public void SetInt(string key, int value)
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage && UC_StorageSetString(k, value.ToString(CultureInfo.InvariantCulture)) != 0)
                return;
#endif
            UnityEngine.PlayerPrefs.SetInt(k, value);
        }

        public void SetFloat(string key, float value)
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage && UC_StorageSetString(k, value.ToString("R", CultureInfo.InvariantCulture)) != 0)
                return;
#endif
            UnityEngine.PlayerPrefs.SetFloat(k, value);
        }

        public void SetString(string key, string value)
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage && UC_StorageSetString(k, value ?? "") != 0)
                return;
#endif
            UnityEngine.PlayerPrefs.SetString(k, value);
        }

        public void SetBool(string key, bool value) => SetInt(key, value ? 1 : 0);

        public void Delete(string key)
        {
            string k = Key(key);
#if UNITY_WEBGL && !UNITY_EDITOR
            if (UseWebStorage)
            {
                UC_StorageDeleteKey(k);
                return;
            }
#endif
            UnityEngine.PlayerPrefs.DeleteKey(k);
        }

        public void Save()
        {
            if (!UseWebStorage)
                UnityEngine.PlayerPrefs.Save();
        }

        private string Key(string key) => _prefix + (key ?? throw new ArgumentNullException(nameof(key)));
    }
}
