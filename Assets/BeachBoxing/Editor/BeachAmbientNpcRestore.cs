using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachAmbientNpcRestore
    {
        private const string PrefabPath = "Assets/BeachBoxing/OriginalArt/World/NPC.prefab";

        [MenuItem("Beach Boxing/Restore preplaced original ambient NPCs")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before restoring saved ambient NPCs.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated BeachBoxing scene first.");
            var beach = flow.transform.Find("Training beach");
            var world = beach ? beach.Find("Original Workout world") : null;
            if (!world) throw new InvalidOperationException("Restore the original Workout world first.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new InvalidOperationException("The sanitized native NPC prefab is missing.");

            var group = beach.Find("Original ambient NPCs");
            if (!group)
            {
                group = new GameObject("Original ambient NPCs").transform;
                Undo.RegisterCreatedObjectUndo(group.gameObject, "Restore ambient NPCs");
                group.SetParent(beach, false);
            }

            // These are the three unused stations in NPCSpawner.workoutStations, not player stations.
            Place(prefab, group, world, "Chest", "LEVEL0/LVL1/WorkoutArea/GameObject/ChestPressMachineInteractable (1)",
                "", "ChestPressMachine (1)/Pivot");
            Place(prefab, group, world, "Arms", "LEVEL0/LVL1/WorkoutArea (1)/GameObject/ArmsMachineInteractable (1)", "", "");
            Place(prefab, group, world, "Back", "LEVEL0/LVL1/WorkoutArea (3)/GameObject/ArmsMachineInteractable (2)", "", "Pivot");
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            Debug.Log("Restored preplaced native NPCs at three original vacant workout stations. Existing NPC placements were preserved; save the scene after review.");
        }

        private static void Place(GameObject prefab, Transform group, Transform world, string workout,
            string stationPath, string anchorPath, string pivotPath)
        {
            string name = "Original NPC " + workout;
            if (group.Find(name)) return;
            var station = world.Find(stationPath);
            if (!station) throw new InvalidOperationException("Missing original NPC station: " + stationPath);
            var anchor = string.IsNullOrEmpty(anchorPath) ? station : station.Find(anchorPath);
            if (!anchor) throw new InvalidOperationException("Missing original NPC playerPos: " + stationPath + "/" + anchorPath);

            var actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
            Undo.RegisterCreatedObjectUndo(actor, "Restore ambient NPCs");
            actor.name = name;
            // Source NPC.InteractWithWorkoutStation sets position to playerPos and world Euler angles to zero.
            actor.transform.SetPositionAndRotation(anchor.position, Quaternion.identity);
            actor.transform.localScale = Vector3.one * OriginalWorldRestore.Scale;
            var presentation = Undo.AddComponent<BeachAmbientNpc>(actor);
            presentation.Rig = actor.GetComponentInChildren<Animator>(true);
            presentation.Workout = workout;
            var pivot = string.IsNullOrEmpty(pivotPath) ? null : station.Find(pivotPath);
            if (pivot)
            {
                Undo.RecordObject(pivot.gameObject, "Hide static NPC equipment pivot");
                presentation.StaticPivot = pivot.gameObject;
                pivot.gameObject.SetActive(false);
            }
            if (presentation.Rig)
            {
                presentation.Rig.applyRootMotion = false;
                string clipName = "Player_" + workout + "Workout";
                foreach (var clip in presentation.Rig.runtimeAnimatorController.animationClips)
                    if (clip && clip.name == clipName)
                    {
                        clip.SampleAnimation(presentation.Rig.gameObject, clip.length * 0.25f);
                        break;
                    }
            }
            EditorUtility.SetDirty(presentation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
        }
    }
}
