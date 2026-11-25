using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Editor
{
    public static class BeachMuscleRestore
    {
        private const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        private const string DataPath = "Assets/BeachBoxing/OriginalArt/World/NativeMuscleMorph.json";
        private const string UndoLabel = "Bind Beach Boxing muscle growth";

        [Serializable]
        private sealed class MorphData
        {
            public MorphEntry[] chest;
            public MorphEntry[] arms;
            public MorphEntry[] back;
            public MorphEntry[] legs;
        }

        [Serializable]
        private sealed class MorphEntry
        {
            public string path;
            public int nativeTransformId;
            public int occurrence;
            public int matchCount;
            public BeachMuscleView.Pose start;
            public BeachMuscleView.Pose end;
        }

        [MenuItem("Beach Boxing/Bind Muscle Growth")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before binding muscle growth.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                throw new InvalidOperationException("Open the populated Beach Boxing scene first.");

            BeachGameFlow flow = null;
            foreach (var candidate in scene.GetRootGameObjects())
                if (candidate.TryGetComponent(out flow)) break;
            if (!flow) throw new InvalidOperationException("The Beach Boxing scene has no game flow.");

            var player = new SerializedObject(flow).FindProperty("stage")?.FindPropertyRelative("Player");
            var root = ReadTransform(player, "Root");
            var boxing = ReadTransform(player, "model");
            var training = ReadTransform(player, "trainingModel");
            if (!root || !boxing || !training || boxing == training || !boxing.IsChildOf(root) || !training.IsChildOf(root))
                throw new InvalidOperationException("The player must have two separate models attached to its root.");

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (!asset) throw new InvalidOperationException($"Import {DataPath} first.");
            var data = JsonUtility.FromJson<MorphData>(asset.text);
            var trainingParts = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
            var boxingParts = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
            IndexChildren(training, string.Empty, trainingParts);
            IndexChildren(boxing, string.Empty, boxingParts);

            var assigned = new HashSet<Renderer>();
            var chest = BindGroup(data.chest, 10, trainingParts, boxingParts, assigned);
            var arms = BindGroup(data.arms, 10, trainingParts, boxingParts, assigned);
            var back = BindGroup(data.back, 12, trainingParts, boxingParts, assigned);
            var legs = BindGroup(data.legs, 10, trainingParts, boxingParts, assigned);

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                Undo.RegisterFullObjectHierarchyUndo(root.gameObject, UndoLabel);
                var view = root.GetComponent<BeachMuscleView>();
                if (!view) view = Undo.AddComponent<BeachMuscleView>(root.gameObject);
                view.Bind(chest, arms, back, legs);
                view.Apply(new PlayerProgress(), 1);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }

            Debug.Log($"Bound {assigned.Count} muscle renderers on the training and boxing player models.");
        }

        private static Transform ReadTransform(SerializedProperty owner, string field) =>
            owner?.FindPropertyRelative(field)?.objectReferenceValue as Transform;

        private static void IndexChildren(Transform parent, string prefix, Dictionary<string, List<Transform>> index)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                string path = prefix + child.name;
                if (!index.TryGetValue(path, out var matches))
                {
                    matches = new List<Transform>();
                    index.Add(path, matches);
                }
                matches.Add(child);
                IndexChildren(child, path + "/", index);
            }
        }

        private static BeachMuscleView.Binding[] BindGroup(MorphEntry[] entries, int count,
            Dictionary<string, List<Transform>> training, Dictionary<string, List<Transform>> boxing,
            HashSet<Renderer> assigned)
        {
            if (entries == null || entries.Length != count)
                throw new InvalidOperationException($"Expected {count} native muscle records in the group.");

            var bindings = new BeachMuscleView.Binding[2 * count];
            for (int i = 0; i < count; i++)
            {
                bindings[2 * i] = FindBinding(entries[i], training, assigned);
                bindings[2 * i + 1] = FindBinding(entries[i], boxing, assigned);
            }
            return bindings;
        }

        private static BeachMuscleView.Binding FindBinding(MorphEntry entry,
            Dictionary<string, List<Transform>> index, HashSet<Renderer> assigned)
        {
            if (!index.TryGetValue(entry.path, out var matches) || matches.Count != entry.matchCount ||
                entry.occurrence < 0 || entry.occurrence >= matches.Count)
                throw new InvalidOperationException($"Cannot bind native transform {entry.nativeTransformId} at {entry.path}.");

            var renderer = matches[entry.occurrence].GetComponent<Renderer>();
            if (!renderer || !assigned.Add(renderer))
                throw new InvalidOperationException($"Native transform {entry.nativeTransformId} needs a unique renderer at {entry.path}.");

            return new BeachMuscleView.Binding { renderer = renderer, start = entry.start, end = entry.end };
        }
    }
}
