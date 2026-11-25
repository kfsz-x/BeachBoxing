using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class OriginalWorldRestore
    {
        public const float Scale = 0.356f;
        public static void Apply()
        {
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            var root = flow.transform;
            var beach = root.Find("Training beach");
            if (beach.Find("Original Workout world")) throw new InvalidOperationException("Original world is already present. Edit its scene objects directly.");
            var state = new SerializedObject(flow);
            var stage = state.FindProperty("stage");
            var world = PlaceWorld("Workout", beach, new Vector3(14, 10.9f, 72));
            string[] paths = {
                "LEVEL0/LVL1/WorkoutArea/GameObject/ChestPressMachineInteractable",
                "LEVEL0/LVL1/WorkoutArea (1)/MachineHolder/ArmsMachineInteractable",
                "LEVEL0/LVL1/WorkoutArea (3)/GameObject/ArmsMachineInteractable (1)",
                "LEVEL0/LVL1/WorkoutArea (2)/GameObject (1)/ChestPressMachineInteractable (2)"
            };
            string[] names = { "Chest", "Arms", "Back", "Legs" };
            for (int i = 0; i < paths.Length; i++)
            {
                var machine = world.Find(paths[i]);
                var station = beach.Find(names[i]);
                for (int child = station.childCount - 1; child >= 0; child--) UnityEngine.Object.DestroyImmediate(station.GetChild(child).gameObject);
                station.SetPositionAndRotation(machine.position, machine.rotation);
                machine.SetParent(station, true);
                machine.name = "Original equipment";
                stage.FindPropertyRelative("equipment").GetArrayElementAtIndex(i).objectReferenceValue = machine;
            }
            var entry = world.Find("LEVEL0/LVL1/SM_FX_Glow_Ring_01");
            stage.FindPropertyRelative("ringEntry").objectReferenceValue = entry;
            var spawn = (Transform)stage.FindPropertyRelative("beachSpawn").objectReferenceValue;
            spawn.position = Vector3.zero;
            spawn.rotation = Quaternion.Euler(0, 180, 0);
            var bounds = (Transform)stage.FindPropertyRelative("beachBounds").objectReferenceValue;
            bounds.position = new Vector3(0, 0, -5);
            bounds.localScale = new Vector3(48, 1, 44);
            Remove(beach, "Sand", "Original decor", "Original sea", "Ring entry", "Preview ring");
            var arena = root.Find("Arena");
            PlaceWorld("Combat", arena, new Vector3(14, 6.9f, -4.9f));
            Remove(arena, "Sand", "Original decor", "Original sea", "Original ring");
            var enemySpawn = (Transform)stage.FindPropertyRelative("enemySpawn").objectReferenceValue;
            var playerSpawn = (Transform)stage.FindPropertyRelative("playerSpawn").objectReferenceValue;
            playerSpawn.position = arena.position;
            enemySpawn.position = arena.position + (new Vector3(9.26f, 6.9f, -14.25f) - new Vector3(14, 6.9f, -4.9f)) * Scale;
            var ringBounds = (Transform)stage.FindPropertyRelative("ringBounds").objectReferenceValue;
            ringBounds.position = (playerSpawn.position + enemySpawn.position) / 2;
            ringBounds.localScale = new Vector3(11.5f, 1, 11.5f);
            stage.FindPropertyRelative("beachCameraOffset").vector3Value = new Vector3(0, 13.37f, 21.44f) * Scale;
            state.ApplyModifiedPropertiesWithoutUndo();
            root.Find("Player").SetPositionAndRotation(spawn.position, spawn.rotation);
            var camera = root.Find("Gameplay camera").GetComponent<Camera>();
            camera.fieldOfView = 60;
            camera.transform.position = spawn.position + new Vector3(0, 13.37f, 21.44f) * Scale;
            camera.transform.rotation = new Quaternion(0, -0.97316575f, 0.23010516f, 0);
            camera.farClipPlane = 600;
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            EditorSceneManager.SaveScene(flow.gameObject.scene);
        }

        private static Transform PlaceWorld(string name, Transform parent, Vector3 origin)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BeachBoxing/OriginalArt/World/" + name + "World.prefab");
            var world = UnityEngine.Object.Instantiate(source, parent, false).transform;
            world.name = "Original " + name + " world";
            world.localScale = Vector3.one * Scale;
            world.localPosition = -origin * Scale;
            return world;
        }

        private static void Remove(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                var child = root.Find(name);
                if (child) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
