using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Editor
{
    public static class BeachTrainerRestore
    {
        private const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        private const string PrefabPath = "Assets/BeachBoxing/OriginalArt/World/NativeTrainers/GameObject/";
        private const string FontPath = "Assets/BeachBoxing/OriginalFont/";
        private const string GroupName = "Original trainers";
        private const string UndoLabel = "Bind Beach Boxing trainers";

        [MenuItem("Beach Boxing/Bind Trainer Followers")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before binding the trainer followers.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.isLoaded) throw new InvalidOperationException("Open the populated Beach Boxing scene first.");
            BeachGameFlow flow = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.TryGetComponent(out flow)) break;
            if (!flow) throw new InvalidOperationException("The Beach Boxing scene has no game flow.");

            var stage = new SerializedObject(flow).FindProperty("stage");
            var camera = stage?.FindPropertyRelative("camera")?.objectReferenceValue as Camera;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath + "LilitaOneOriginal.fontsettings");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(FontPath + "LilitaOneOriginalSDF.png");
            var metrics = AssetDatabase.LoadAssetAtPath<TextAsset>(FontPath + "LilitaOneNativeMetrics.json");
            var material = AssetDatabase.LoadAssetAtPath<Material>(FontPath + "LilitaOneNativeRegular.mat");
            if (!camera || !font || !atlas || !metrics || !material)
                throw new InvalidOperationException("The gameplay camera and imported Lilita font are required.");

            var prefabs = new GameObject[BeachPeripheralRules.TrainerCount];
            for (int i = 0; i < prefabs.Length; i++)
            {
                string path = PrefabPath + "Trainer " + BeachPeripheralRules.GetTrainerName(i) + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var animator = prefab ? prefab.GetComponentInChildren<Animator>(true) : null;
                if (!animator || !animator.runtimeAnimatorController || !prefab.transform.Find("TitleCanvas/Title") ||
                    !prefab.transform.Find("TitleCanvas/NameText"))
                    throw new InvalidOperationException($"Import the sanitized trainer model and dependencies from {path}.");
                prefabs[i] = prefab;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                var holder = flow.transform.Find(GroupName);
                if (!holder)
                {
                    holder = new GameObject(GroupName).transform;
                    Undo.RegisterCreatedObjectUndo(holder.gameObject, UndoLabel);
                    holder.SetParent(flow.transform, false);
                }
                Undo.RegisterFullObjectHierarchyUndo(holder.gameObject, UndoLabel);
                var models = new BeachTrainerFollowers.Model[prefabs.Length];
                for (int i = 0; i < prefabs.Length; i++)
                {
                    var prefab = prefabs[i];
                    var actor = holder.Find(prefab.name);
                    if (!actor)
                    {
                        actor = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, holder)).transform;
                        Undo.RegisterCreatedObjectUndo(actor.gameObject, UndoLabel);
                    }
                    var canvas = actor.Find("TitleCanvas").GetComponent<Canvas>();
                    canvas.worldCamera = camera;
                    canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal;
                    var level = BindLabel(canvas.transform.Find("Title"), "LVL 1 Trainer",
                        new Color(0.993672f, 1f, 0f, 1f), font, atlas, metrics, material);
                    BindLabel(canvas.transform.Find("NameText"), BeachPeripheralRules.GetTrainerName(i),
                        Color.white, font, atlas, metrics, material);
                    var animator = actor.GetComponentInChildren<Animator>(true);
                    animator.applyRootMotion = false;
                    var body = actor.GetComponent<Rigidbody>();
                    if (body)
                    {
                        body.useGravity = false;
                        body.isKinematic = true;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(body);
                    }
                    actor.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(canvas);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(actor.gameObject);
                    models[i] = new BeachTrainerFollowers.Model
                    {
                        root = actor, animator = animator, title = canvas.transform, levelLabel = level
                    };
                }

                var followers = flow.GetComponent<BeachTrainerFollowers>();
                if (!followers) followers = Undo.AddComponent<BeachTrainerFollowers>(flow.gameObject);
                Undo.RecordObject(followers, UndoLabel);
                followers.Bind(models, camera);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }

            Debug.Log($"Bound {prefabs.Length} trainer followers in the populated Beach Boxing scene.");
        }

        private static OriginalSdfText BindLabel(Transform target, string value, Color color, Font font,
            Texture2D atlas, TextAsset metrics, Material material)
        {
            var label = target.GetComponent<OriginalSdfText>();
            if (!label) label = Undo.AddComponent<OriginalSdfText>(target.gameObject);
            label.font = font;
            label.fontSize = 25;
            label.nativePointSize = 25f;
            label.sourceAtlas = atlas;
            label.sourceMetrics = metrics;
            label.material = material;
            label.alignment = TextAnchor.MiddleCenter;
            label.text = value;
            label.color = color;
            label.raycastTarget = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            return label;
        }
    }
}
