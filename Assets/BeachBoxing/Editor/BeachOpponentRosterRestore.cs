using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachOpponentRosterRestore
    {
        const string Art = "Assets/BeachBoxing/OriginalOpponents/";
        const string UndoLabel = "Restore native opponent roster";
        [Serializable] sealed class AssetsData
        {
            public string[] prefabs, portraits, gauntletBackgrounds;
            public string comingSoonPortrait;
        }
        [Serializable] sealed class MorphData { public MorphRecord[] records; }
        [Serializable] sealed class MorphRecord { public int index; public MorphPart[] muscles; }
        [Serializable] sealed class MorphPart
        {
            public string group, name;
            public int[] childIndices;
            public BeachMuscleView.Pose start, end;
        }

        [MenuItem("Beach Boxing/Restore all 45 native opponents")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before restoring the opponent roster.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene first.");
            var enemyRoot = new SerializedObject(flow).FindProperty("stage")?.FindPropertyRelative("Enemy")?.FindPropertyRelative("Root")?.objectReferenceValue as Transform;
            if (!enemyRoot) throw new InvalidOperationException("The scene needs its existing opponent actor root.");
            var source = JsonUtility.FromJson<AssetsData>(Required<TextAsset>(Art + "NativeOpponentAssets.json").text);
            var morphs = JsonUtility.FromJson<MorphData>(Required<TextAsset>(Art + "NativeOpponentMorphs.json").text);
            var profiles = OpponentProfile.RookieRoster();
            var prefabs = new GameObject[NativeOpponentCatalog.PlayableCount];
            var portraits = new Sprite[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
            {
                prefabs[i] = Required<GameObject>(source.prefabs[i]);
                portraits[i] = Required<Sprite>(source.portraits[i]);
                if (!prefabs[i].GetComponentInChildren<Animator>(true))
                    throw new InvalidOperationException("Missing original opponent animator " + i);
                foreach (var renderer in prefabs[i].GetComponentsInChildren<Renderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                        if (!material || !material.shader)
                            throw new InvalidOperationException("Missing original opponent material/shader " + i);
            }
            var backgrounds = new Sprite[source.gauntletBackgrounds.Length];
            for (int i = 0; i < backgrounds.Length; i++) backgrounds[i] = Required<Sprite>(source.gauntletBackgrounds[i]);
            var comingSoon = Required<Sprite>(source.comingSoonPortrait);
            var slippery = Required<PhysicMaterial>("Assets/BeachBoxing/OriginalArt/PhysicMaterial/Slippery.physicMaterial");
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                var actorCapsule = enemyRoot.GetComponent<CapsuleCollider>();
                if (!actorCapsule) actorCapsule = Undo.AddComponent<CapsuleCollider>(enemyRoot.gameObject);
                Undo.RecordObject(actorCapsule, UndoLabel);
                actorCapsule.sharedMaterial = slippery; // source sharedassets0.assets:391 on live and dead capsules.
                FightRules.ConfigureEnemyBody(enemyRoot, profiles[0].geometry, FightRules.ReferenceWorldScale, false);
                var holder = enemyRoot.Find("Original opponents");
                if (!holder)
                {
                    holder = new GameObject("Original opponents").transform;
                    Undo.RegisterCreatedObjectUndo(holder.gameObject, UndoLabel);
                    holder.SetParent(enemyRoot, false);
                }
                Undo.RegisterFullObjectHierarchyUndo(holder.gameObject, UndoLabel);
                var models = new Transform[prefabs.Length];
                for (int i = 0; i < models.Length; i++)
                {
                    string name = "Original " + profiles[i].modelName;
                    var model = holder.Find(name);
                    if (!model)
                    {
                        model = ((GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], holder)).transform;
                        Undo.RegisterCreatedObjectUndo(model.gameObject, UndoLabel);
                        model.name = name;
                        model.localPosition = Vector3.zero;
                        model.localRotation = Quaternion.identity;
                        model.localScale = Vector3.one * (FightRules.ReferenceWorldScale *
                            BeachProgressionRules.RootScale(profiles[i].prestige));
                        BakeMuscles(model, morphs.records[i], profiles[i]);
                    }
                    // Keep all later hand edits on repeated Apply calls.
                    foreach (var animator in model.GetComponentsInChildren<Animator>(true))
                    {
                        animator.applyRootMotion = false;
                        animator.fireEvents = animator.GetComponent<BeachNativeBoxingAudio>() != null;
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                    }
                    foreach (var canvas in model.GetComponentsInChildren<Canvas>(true))
                    {
                        canvas.gameObject.SetActive(false);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(canvas.gameObject);
                    }
                    foreach (var audio in model.GetComponentsInChildren<AudioSource>(true))
                    {
                        audio.playOnAwake = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
                    }
                    model.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(model.gameObject);
                    models[i] = model;
                }
                var catalog = flow.GetComponent<NativeOpponentCatalog>();
                if (!catalog) catalog = Undo.AddComponent<NativeOpponentCatalog>(flow.gameObject);
                Undo.RecordObject(catalog, UndoLabel);
                catalog.Bind(profiles, models, portraits, comingSoon, backgrounds);
                EditorUtility.SetDirty(catalog);
                EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo); throw; }
            Debug.Log("Preplaced 45 original opponents and bound their exact profiles, portraits and COMING SOON boundary. Review and save the scene.");
        }

        static void BakeMuscles(Transform model, MorphRecord record, OpponentProfile profile)
        {
            if (record.index != profile.sourceIndex || record.muscles.Length != 42)
                throw new InvalidOperationException("Misordered native muscle record " + profile.sourceIndex);
            foreach (var part in record.muscles)
            {
                var target = model;
                foreach (int child in part.childIndices) target = target.GetChild(child);
                if (target.name != part.name) throw new InvalidOperationException("Native muscle hierarchy mismatch " + part.name);
                int stat = part.group == "chest" ? profile.chest : part.group == "arms" ? profile.arms :
                    part.group == "back" ? profile.back : profile.legs;
                float fraction = BeachProgressionRules.Fraction(stat, profile.prestige);
                target.localPosition = Vector3.Lerp(part.start.position, part.end.position, fraction);
                target.localRotation = Quaternion.Lerp(part.start.rotation, part.end.rotation, fraction);
                target.localScale = Vector3.Lerp(part.start.scale, part.end.scale, fraction);
                var renderer = target.GetComponent<Renderer>();
                if (!renderer) throw new InvalidOperationException("Missing native muscle renderer " + part.name);
                renderer.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        static T Required<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing native opponent asset " + path);
    }
}
