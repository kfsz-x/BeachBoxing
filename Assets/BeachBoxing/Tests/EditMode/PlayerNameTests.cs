using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class PlayerNameTests
    {
        [Test]
        public void GeneratedNameJoinsTheConfiguredWordsWithOneSpace()
        {
            var namesType = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachPlayerNames");
            Assert.That(namesType, Is.Not.Null, "The gameplay scene needs a configured name generator.");

            var root = new GameObject("Player name test");
            var previousRandom = Random.state;
            try
            {
                var names = root.AddComponent(namesType);
                var data = new SerializedObject(names);
                SetSingleWord(data, "adjectives", "Brave");
                SetSingleWord(data, "nouns", "Goose");
                data.ApplyModifiedPropertiesWithoutUndo();

                var result = namesType.GetMethod("Generate").Invoke(names, null);
                Assert.That(result, Is.EqualTo("Brave Goose"));
            }
            finally
            {
                Random.state = previousRandom;
                Object.DestroyImmediate(root);
            }
        }

        private static void SetSingleWord(SerializedObject data, string field, string word)
        {
            var words = data.FindProperty(field);
            words.arraySize = 1;
            words.GetArrayElementAtIndex(0).stringValue = word;
        }
    }
}
