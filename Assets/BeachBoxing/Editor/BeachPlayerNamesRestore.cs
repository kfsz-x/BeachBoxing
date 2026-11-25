using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachPlayerNamesRestore
    {
        private const string DataPath = "Assets/BeachBoxing/OriginalArt/World/NativePlayerNames.json";

        [MenuItem("Beach Boxing/Restore player name word lists")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before restoring the player name lists.");

            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow || !flow.transform.Find("Player"))
                throw new InvalidOperationException("Open the populated Beach Boxing scene first.");

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            var lists = asset ? JsonUtility.FromJson<WordLists>(asset.text) : null;
            if (lists?.adjectives == null || lists.adjectives.Length == 0 || lists.nouns == null || lists.nouns.Length == 0)
                throw new InvalidOperationException("Import both player name word lists before applying the migration.");

            var names = flow.GetComponent<BeachPlayerNames>();
            if (!names) names = Undo.AddComponent<BeachPlayerNames>(flow.gameObject);
            var serialized = new SerializedObject(names);
            WriteWords(serialized.FindProperty("adjectives"), lists.adjectives);
            WriteWords(serialized.FindProperty("nouns"), lists.nouns);
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }

        private static void WriteWords(SerializedProperty property, string[] words)
        {
            property.arraySize = words.Length;
            for (int i = 0; i < words.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = words[i];
        }

#pragma warning disable CS0649 // Deserialized by JsonUtility.
        [Serializable]
        private sealed class WordLists
        {
            public string[] adjectives;
            public string[] nouns;
        }
#pragma warning restore CS0649
    }
}
