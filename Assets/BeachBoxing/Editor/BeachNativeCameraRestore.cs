using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachNativeCameraRestore
    {
        private const string StationPath = "Assets/BeachBoxing/OriginalArt/World/NativePlayerStationCameras.json";
        private const string PunchPath = "Assets/BeachBoxing/OriginalArt/NativeCombatAudio/NativeCameraTracks.json";
        private static readonly int[] StationSourceIds = { 4697, 4718, 4877, 4918 };

        [Serializable] private sealed class Data { public Track[] tracks; }
        [Serializable] private sealed class Track { public string name; public float duration; public Curve fov, pitch, yaw; }
        [Serializable] private sealed class Curve { public Key[] keys; }
        [Serializable] private sealed class Key
        {
            public float time, value, inSlope, outSlope, inWeight, outWeight;
            public bool inConstant, outConstant;
            public int weightedMode;
        }
        [Serializable] private sealed class StationData { public Station[] stations; }
        [Serializable] private sealed class Station
        {
            public int muscle, sourceComponent;
            public Vector3 position, positionLevelTen, euler;
        }

        [MenuItem("Beach Boxing/Bind native additive camera feedback")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var stations = ReadStations();
            var punch = ReadAsset<Data>(PunchPath)?.tracks;
            if (punch == null || punch.Length != 2 || punch[0]?.name != "Cam_Punch1" || punch[1]?.name != "Cam_Punch2" ||
                !IsFinite(punch[0].duration) || punch[0].duration <= 0 || punch[1].duration != punch[0].duration)
                throw new InvalidOperationException("Expected original Cam_Punch1 and Cam_Punch2 tracks with a shared duration.");
            float duration = punch[0].duration;
            var fov = Restore(punch[0].fov, duration);
            var pitchLeft = Restore(punch[0].pitch, duration);
            var yawLeft = Restore(punch[0].yaw, duration);
            Restore(punch[1].fov, duration); // Both source tracks carry the same FOV curve.
            var pitchRight = Restore(punch[1].pitch, duration);
            var yawRight = Restore(punch[1].yaw, duration);

            var serialized = new SerializedObject(flow);
            var stage = serialized.FindProperty("stage");
            var camera = Ref<Camera>(stage, "camera");
            var rig = Ref<Transform>(stage, "cameraPose");
            var player = Ref<Transform>(stage?.FindPropertyRelative("Player"), "Root");
            var enemy = Ref<Transform>(stage?.FindPropertyRelative("Enemy"), "Root");
            var arena = Ref<Transform>(stage, "arena");
            var trainingWorld = Ref<Transform>(stage, "trainingWorld");
            var intro = serialized.FindProperty("presentation")?.FindPropertyRelative("introCamera");
            if (!camera || !player || !enemy || !arena || !trainingWorld || intro == null)
                throw new InvalidOperationException("Bind the camera, both fighters, arena, original training world and presentation before restoring native cameras.");
            if (rig && (rig == camera.transform || rig.IsChildOf(camera.transform)))
                throw new InvalidOperationException("The gameplay camera base pose must be outside the camera hierarchy.");
            var target = player.Find("Source camera target");

            if (!rig)
            {
                rig = new GameObject("Gameplay camera base pose").transform;
                Undo.RegisterCreatedObjectUndo(rig.gameObject, "Preplace additive camera rig");
                rig.SetParent(camera.transform.parent, false);
                rig.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            }
            if (camera.transform.parent != rig)
            {
                Undo.SetTransformParent(camera.transform, rig, "Keep gameplay camera pose");
                Undo.RecordObject(camera.transform, "Place gameplay camera on its base pose");
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
            }
            Undo.RecordObject(camera, "Set source gameplay camera clip planes");
            camera.nearClipPlane = .01f * FightRules.ReferenceWorldScale;
            camera.farClipPlane = 2000f * FightRules.ReferenceWorldScale;
            var feedback = camera.GetComponent<BeachNativeCameraFeedback>();
            if (!feedback) feedback = Undo.AddComponent<BeachNativeCameraFeedback>(camera.gameObject);
            Undo.RecordObject(feedback, "Bind original camera punch curves");
            feedback.view = camera;
            feedback.fov = fov;
            feedback.pitchLeft = pitchLeft;
            feedback.yawLeft = yawLeft;
            feedback.pitchRight = pitchRight;
            feedback.yawRight = yawRight;
            feedback.punchDuration = duration;
            stage.FindPropertyRelative("cameraPose").objectReferenceValue = rig;
            var sourceCamera = flow.GetComponent<BeachNativeCombatCamera>();
            if (!sourceCamera) sourceCamera = Undo.AddComponent<BeachNativeCombatCamera>(flow.gameObject);
            Undo.RecordObject(sourceCamera, "Bind original gameplay camera follow");
            sourceCamera.player = player;
            sourceCamera.enemy = enemy;
            sourceCamera.view = rig;
            sourceCamera.arena = arena;
            if (!target)
            {
                target = new GameObject("Source camera target").transform;
                Undo.RegisterCreatedObjectUndo(target.gameObject, "Preplace original camera target");
                Undo.SetTransformParent(target, player, "Bind original camera target parent");
                target.localPosition = Vector3.zero;
                target.localRotation = Quaternion.identity;
                target.localScale = Vector3.one;
            }
            sourceCamera.target = target;
            stage.FindPropertyRelative("nativeFightCamera").objectReferenceValue = sourceCamera;
            EditorUtility.SetDirty(sourceCamera);
            var beachCamera = flow.GetComponent<BeachNativeBeachCamera>();
            if (!beachCamera) beachCamera = Undo.AddComponent<BeachNativeBeachCamera>(flow.gameObject);
            Undo.RecordObject(beachCamera, "Bind original beach and workout camera");
            beachCamera.player = player;
            beachCamera.target = target;
            beachCamera.view = rig;
            beachCamera.trainingWorld = trainingWorld;
            stage.FindPropertyRelative("nativeBeachCamera").objectReferenceValue = beachCamera;
            stage.FindPropertyRelative("workoutCameraPositions").arraySize = stations.Length;
            stage.FindPropertyRelative("workoutCameraPositionsLevelTen").arraySize = stations.Length;
            stage.FindPropertyRelative("workoutCameraAngles").arraySize = stations.Length;
            foreach (var row in stations)
            {
                stage.FindPropertyRelative("workoutCameraPositions").GetArrayElementAtIndex(row.muscle).vector3Value = row.position * FightRules.ReferenceWorldScale;
                stage.FindPropertyRelative("workoutCameraPositionsLevelTen").GetArrayElementAtIndex(row.muscle).vector3Value = row.positionLevelTen * FightRules.ReferenceWorldScale;
                stage.FindPropertyRelative("workoutCameraAngles").GetArrayElementAtIndex(row.muscle).vector3Value = row.euler;
            }
            EditorUtility.SetDirty(beachCamera);
            intro.FindPropertyRelative("target").objectReferenceValue = ResolveBasePose(stage);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(feedback);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }

        internal static Transform ResolveBasePose(SerializedProperty stage)
        {
            var rig = Ref<Transform>(stage, "cameraPose");
            var camera = Ref<Camera>(stage, "camera");
            return rig ? rig : camera ? camera.transform : null;
        }

        private static Station[] ReadStations()
        {
            var rows = ReadAsset<StationData>(StationPath)?.stations;
            if (rows == null || rows.Length != StationSourceIds.Length)
                throw new InvalidOperationException("Expected the four original player station camera records.");
            var stations = new Station[StationSourceIds.Length];
            foreach (var row in rows)
            {
                if (row == null || row.muscle < 0 || row.muscle >= stations.Length || stations[row.muscle] != null ||
                    row.sourceComponent != StationSourceIds[row.muscle] || !IsFinite(row.position) ||
                    !IsFinite(row.positionLevelTen) || !IsFinite(row.euler))
                    throw new InvalidOperationException("Expected station camera source IDs 4697, 4718, 4877, and 4918 for muscles 0 through 3.");
                stations[row.muscle] = row;
            }
            return stations;
        }

        private static T ReadAsset<T>(string path) where T : class
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (!asset) throw new InvalidOperationException("Missing original camera data at " + path);
            try { return JsonUtility.FromJson<T>(asset.text); }
            catch (ArgumentException error) { throw new InvalidOperationException("Invalid original camera data at " + path, error); }
        }

        private static T Ref<T>(SerializedProperty parent, string name) where T : UnityEngine.Object =>
            parent?.FindPropertyRelative(name)?.objectReferenceValue as T;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        private static AnimationCurve Restore(Curve curve, float duration)
        {
            if (curve?.keys == null || curve.keys.Length < 2)
                throw new InvalidOperationException("An original camera punch curve is missing its keys.");
            var keys = new Keyframe[curve.keys.Length];
            float previous = -1;
            for (int index = 0; index < keys.Length; index++)
            {
                var key = curve.keys[index];
                if (key == null || !IsFinite(key.time) || key.time < 0 || key.time <= previous || key.time > duration ||
                    !IsFinite(key.value) || (!key.inConstant && !IsFinite(key.inSlope)) ||
                    (!key.outConstant && !IsFinite(key.outSlope)) || !IsFinite(key.inWeight) ||
                    !IsFinite(key.outWeight) || key.weightedMode < 0 || key.weightedMode > (int)WeightedMode.Both)
                    throw new InvalidOperationException("An original camera punch curve has an invalid key.");
                keys[index] = new Keyframe(key.time, key.value, key.inConstant ? float.PositiveInfinity : key.inSlope,
                    key.outConstant ? float.PositiveInfinity : key.outSlope, key.inWeight, key.outWeight)
                    { weightedMode = (WeightedMode)key.weightedMode };
                previous = key.time;
            }
            return new AnimationCurve(keys);
        }
    }
}
