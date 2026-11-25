using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Editor
{
    public static class BeachColliderRestore
    {
        private const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        private const string DataPath = "Assets/BeachBoxing/OriginalArt/World/NativeColliderData.json";
        private const string SlipperyPath = "Assets/BeachBoxing/OriginalArt/PhysicMaterial/Slippery.physicMaterial";
        private const string CombatWorldPath = "Arena/Original Combat world";
        private const string UndoLabel = "Restore Beach Boxing colliders";

        [MenuItem("Beach Boxing/Restore Native Colliders")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before restoring the saved colliders.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded)
                throw new InvalidOperationException("Open the populated Beach Boxing scene first.");

            Transform root = null;
            foreach (var candidate in scene.GetRootGameObjects())
            {
                if (candidate.GetComponent<BeachGameFlow>())
                {
                    root = candidate.transform;
                    break;
                }
            }
            if (!root || !root.Find(CombatWorldPath))
                throw new InvalidOperationException("The populated Beach Boxing worlds are required.");

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            var material = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(SlipperyPath);
            if (!asset || !material)
                throw new InvalidOperationException("Import NativeColliderData.json and the Slippery physics material first.");

            var entries = JsonUtility.FromJson<ColliderData>(asset.text)?.entries;
            if (entries == null || entries.Length == 0)
                throw new InvalidOperationException("NativeColliderData.json has no collider records.");
            var byPath = IndexColliders(root);
            var colliders = new Collider[entries.Length];
            var assigned = new HashSet<Collider>();
            for (int index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var collider = FindCollider(byPath, entry);
                if (collider && !assigned.Add(collider))
                    throw new InvalidOperationException($"Multiple native records map to {entry.targetPath}.");
                colliders[index] = collider;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                for (int index = 0; index < entries.Length; index++)
                {
                    var entry = entries[index];
                    var collider = colliders[index] ? colliders[index] : CreateWall(root, entry, material);
                    SetGeometry(collider, entry);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity could not save the restored collider geometry.");
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }

            Debug.Log($"Restored {entries.Length} native world colliders in {scene.path}.");
        }

        private static Dictionary<string, List<Collider>> IndexColliders(Transform root)
        {
            var index = new Dictionary<string, List<Collider>>();
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                string path = collider.name;
                for (var parent = collider.transform.parent; parent && parent != root; parent = parent.parent)
                    path = parent.name + "/" + path;
                if (!index.TryGetValue(path, out var colliders))
                {
                    colliders = new List<Collider>();
                    index.Add(path, colliders);
                }
                colliders.Add(collider);
            }
            return index;
        }

        private static Collider FindCollider(Dictionary<string, List<Collider>> byPath, ColliderEntry entry)
        {
            if (byPath.TryGetValue(entry.targetPath, out var colliders))
            {
                Collider siblingMatch = null;
                foreach (var collider in colliders)
                {
                    if (collider.GetType().Name != entry.type) continue;
                    if (colliders.Count == 1 || GlobalObjectId.GetGlobalObjectIdSlow(collider).targetObjectId == (ulong)entry.targetComponentId)
                        return collider;
                    if (collider.transform.GetSiblingIndex() == entry.siblingIndex)
                        siblingMatch = collider;
                }
                if (siblingMatch) return siblingMatch;
            }
            else if (entry.createWall) return null;

            throw new InvalidOperationException($"Could not map {entry.nativeScene}:{entry.nativeComponentId} to {entry.targetPath}.");
        }

        private static BoxCollider CreateWall(Transform root, ColliderEntry entry, PhysicMaterial material)
        {
            var world = root.Find(CombatWorldPath);
            var walls = world.Find("InvisWalls");
            if (!walls) walls = CreateChild(world, "InvisWalls");

            string name = entry.targetPath.Substring(entry.targetPath.LastIndexOf('/') + 1);
            var wall = walls.Find(name);
            if (!wall)
            {
                wall = CreateChild(walls, name);
                Undo.RecordObject(wall, UndoLabel);
                // This parent applies the same .356 normalization and translation as the original combat world.
                wall.localPosition = entry.localPosition;
                wall.localRotation = entry.localRotation;
                wall.localScale = entry.localScale;
            }

            var collider = Undo.AddComponent<BoxCollider>(wall.gameObject);
            Undo.RecordObject(collider, UndoLabel);
            collider.sharedMaterial = material;
            return collider;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            if (child.scene != parent.gameObject.scene)
                SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child, UndoLabel);
            return child.transform;
        }

        private static void SetGeometry(Collider collider, ColliderEntry entry)
        {
            Undo.RecordObjects(new UnityEngine.Object[] { collider, collider.gameObject }, UndoLabel);
            collider.gameObject.layer = entry.layer;
            collider.enabled = entry.enabled;
            if (collider.isTrigger != entry.isTrigger)
                collider.isTrigger = entry.isTrigger;
            switch (collider)
            {
                case BoxCollider box:
                    box.size = entry.size;
                    box.center = entry.center;
                    break;
                case SphereCollider sphere:
                    sphere.radius = entry.radius;
                    sphere.center = entry.center;
                    break;
                case CapsuleCollider capsule:
                    if (capsule.direction != entry.direction) capsule.direction = entry.direction;
                    if (capsule.radius != entry.radius) capsule.radius = entry.radius;
                    if (capsule.height != entry.height) capsule.height = entry.height;
                    if (!capsule.center.Equals(entry.center)) capsule.center = entry.center;
                    break;
            }
        }

#pragma warning disable CS0649 // Deserialized by JsonUtility.
        [Serializable]
        private sealed class ColliderData
        {
            public ColliderEntry[] entries;
        }

        [Serializable]
        private sealed class ColliderEntry
        {
            public string nativeScene;
            public int nativeComponentId;
            public string targetPath;
            public long targetComponentId;
            public string type;
            public int siblingIndex;
            public int layer;
            public bool enabled;
            public bool isTrigger;
            public Vector3 size;
            public Vector3 center;
            public float radius;
            public float height;
            public int direction;
            public bool createWall;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
        }
#pragma warning restore CS0649
    }
}
