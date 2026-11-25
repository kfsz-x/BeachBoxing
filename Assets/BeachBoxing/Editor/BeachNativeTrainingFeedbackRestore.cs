using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BeachBoxing.Editor
{
    /// <summary>Explicit Editor migration: all feedback objects and pool slots are saved in the scene.</summary>
    public static class BeachNativeTrainingFeedbackRestore
    {
        const string Root = "Assets/BeachBoxing/OriginalArt/NativeTrainingFeedback/";
        const string FontRoot = "Assets/BeachBoxing/OriginalFont/";
        const string UndoName = "Preplace native training feedback";
        [Serializable] class Data { public string[] faces; public PoolData[] pools; public ClipData clip; }
        [Serializable] class PoolData { public string name; public NodeData[] nodes; }
        [Serializable] class NodeData
        {
            public int id, horizontal, vertical, fontId, sourceFontMaterialId, siblingIndex;
            public string path, sprite, text;
            public bool active;
            public float fontSize;
            public Vector2 anchorMin, anchorMax, pivot, sizeDelta, position;
            public Vector3 scale;
            public Quaternion rotation;
            public Color color;
        }
        [Serializable] class ClipData { public string name; public float frameRate, duration; public bool loop; public CurveData[] curves; }
        [Serializable] class CurveData { public string path, property, targetKind, sourceProperty; public bool discrete; public KeyData[] keys; }
        [Serializable] class KeyData { public float time, value, inSlope, outSlope, inWeight, outWeight; public bool inConstant, outConstant; public int weightedMode; }
        [MenuItem("Beach Boxing/Preplace native training feedback")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode before preplacing feedback.");
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the saved gameplay scene first.");
            var flowData = new SerializedObject(flow);
            var stage = flowData.FindProperty("stage");
            var camera = stage.FindPropertyRelative("camera").objectReferenceValue as Camera;
            var playerRoot = stage.FindPropertyRelative("Player").FindPropertyRelative("Root").objectReferenceValue as Transform;
            var muscleView = playerRoot ? playerRoot.GetComponent<BeachMuscleView>() : null;
            var peripheral = flow.GetComponent<BeachPeripheralView>();
            if (!peripheral || !muscleView || !camera) throw new InvalidOperationException("Prebind source actor and peripheral UI first.");
            var canvas = peripheral.nodes.Select(n => n.transform ? n.transform.GetComponentInParent<Canvas>() : null).First(c => c);
            var source = JsonUtility.FromJson<Data>(Required<TextAsset>(Root + "NativeFeedbackData.json").text);
            var training = stage.FindPropertyRelative("Player").FindPropertyRelative("trainingModel").objectReferenceValue as Transform;
            if (!training) throw new InvalidOperationException("Bind the source training actor first.");
            var face = training.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "FACE DECAL");
            var sweat = stage.FindPropertyRelative("sweatEffect").objectReferenceValue as ParticleSystem;
            var fill = SourceNode(peripheral, 1119).image;
            if (!face || !sweat || !fill) throw new InvalidOperationException("Prebind source face, sweat3064 and fill1119 first.");
            var targetIds = new[] { 558, 383, 916, 1216 }; // WorkingOutManager Rect3948/3853/4143/4309 -> original GO IDs.
            var targets = targetIds.Select(id => SourceNode(peripheral, id).transform).ToArray();
            PreflightAssets(source);
            var particlePrefab = Required<GameObject>(Root + "NativeWorkoutParticles.prefab");
            var particleNames = new[] { "Particle System", "Particle System (1)", "Particle System (2)", "Particle System (3)" };
            foreach (var name in particleNames)
                if (!particlePrefab.transform.Find(name)?.GetComponent<ParticleSystem>())
                    throw new InvalidOperationException("Missing original completion particle " + name);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, UndoName);
                Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, UndoName);
                Undo.RegisterFullObjectHierarchyUndo(camera.gameObject, UndoName);
                Undo.RegisterFullObjectHierarchyUndo(playerRoot.gameObject, UndoName);
                var feedback = Component<BeachNativeTrainingFeedback>(flow.gameObject);
                feedback.face = face;
                feedback.normalFace = Required<Material>("Assets/BeachBoxing/OriginalArt/Material/FaceDecal 1.mat");
                feedback.angryFace = FaceMaterial(45, source.faces[0], feedback.normalFace);
                feedback.strugglingFace = FaceMaterial(46, source.faces[1], feedback.normalFace);
                feedback.sweat = sweat;
                var groups = new[] { "chest", "arms", "back", "legs" };
                var morph = new SerializedObject(muscleView);
                feedback.muscles = groups.Select((name, index) =>
                {
                    var values = morph.FindProperty(name); var renderers = new List<Renderer>();
                    for (int i = 0; i < values.arraySize; i++)
                    {
                        var renderer = values.GetArrayElementAtIndex(i).FindPropertyRelative("renderer").objectReferenceValue as Renderer;
                        if (renderer) renderers.Add(renderer);
                    }
                    return new BeachNativeTrainingFeedback.MuscleBinding { muscle = (Muscle)index, renderers = renderers.Distinct().ToArray() };
                }).ToArray();
                feedback.fillBar = fill;
                feedback.parentCanvas = canvas.transform;
                var spawn = canvas.transform.Find("ParticleSpawnPoint") as RectTransform;
                if (!spawn) spawn = NewRect(canvas.transform, "ParticleSpawnPoint");
                // Source level0 Rect4012: centered native UI spawn, no runtime canvas lookup.
                spawn.anchorMin = spawn.anchorMax = spawn.pivot = Vector2.one * .5f;
                spawn.anchoredPosition3D = Vector3.zero; spawn.sizeDelta = new Vector2(100,100);
                spawn.localRotation = Quaternion.identity; spawn.localScale = Vector3.one;
                feedback.particleSpawn = spawn;
                var sfx = flow.GetComponent<BeachNativeSettingsView>()?.mixer;
                var group = sfx ? sfx.FindMatchingGroups("SFX").FirstOrDefault() : null;
                var audioRoot = Child(flow.transform, "Native workout audio");
                feedback.workoutAudio = new AudioSource[5];
                for (int i=0;i<5;i++) feedback.workoutAudio[i] = Audio(Child(audioRoot, "Source workout " + i).gameObject, 355, .5279999971f, group);
                var particleRoot = camera.transform.Find("Native workout completion effects");
                if (!particleRoot)
                {
                    var prefab = Required<GameObject>(Root + "NativeWorkoutParticles.prefab");
                    var instance = PrefabUtility.InstantiatePrefab(prefab, camera.transform) as GameObject;
                    Undo.RegisterCreatedObjectUndo(instance, UndoName); particleRoot = instance.transform;
                    particleRoot.name = "Native workout completion effects";
                    particleRoot.localPosition *= .356f; particleRoot.localScale *= .356f;
                }
                feedback.workoutParticles = new[] { "Particle System", "Particle System (1)", "Particle System (2)", "Particle System (3)" }
                    .Select(name => particleRoot.Find(name).GetComponent<ParticleSystem>()).ToArray();
                foreach (var p in feedback.workoutParticles) { var main=p.main; main.playOnAwake=false; p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
                var clip = RestoreClip(source.clip);
                var container = Child(canvas.transform, "Native training rewards");
                feedback.pools = source.pools.Select(row =>
                {
                    Muscle muscle = row.name.StartsWith("Chest") ? Muscle.Chest : row.name.StartsWith("Arms") ? Muscle.Arms : row.name.StartsWith("Back") ? Muscle.Back : Muscle.Legs;
                    Transform target = targets[(int)muscle];
                    var slots = new BeachNativeRewardParticle[8];
                    for (int i=0;i<slots.Length;i++) slots[i] = PlaceSlot(container, row, i, target, clip, group);
                    return new BeachNativeTrainingFeedback.RewardPool { muscle=muscle,slots=slots };
                }).ToArray();
                feedback.face.sharedMaterial = feedback.normalFace;
                var emission = feedback.sweat.emission; emission.rateOverTime = 0;
                EditorUtility.SetDirty(feedback); EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log("Preplaced original training feedback: four body pools x8, exact clip322, five pulse audios; scene save remains explicit.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }
        static NativeUiNode SourceNode(BeachPeripheralView peripheral, int id)
        {
            var node = peripheral.nodes.FirstOrDefault(n => n != null && n.sourceId == id);
            if (node == null || !node.transform) throw new InvalidOperationException("Missing preplaced source UI node " + id);
            return node;
        }
        static void PreflightAssets(Data source)
        {
            Required<Font>(FontRoot+"LilitaOneOriginal.fontsettings");
            Required<Material>(FontRoot+"LilitaOneNativeRegular.mat");
            Required<Material>(FontRoot+"LilitaOneNativeOutline.mat");
            Required<Texture2D>(FontRoot+"LilitaOneOriginalSDF.png");
            Required<TextAsset>(FontRoot+"LilitaOneNativeMetrics.json");
            Required<Material>("Assets/BeachBoxing/OriginalArt/Material/FaceDecal 1.mat");
            foreach (var path in source.faces) Required<Texture2D>(path);
            foreach (int id in new[] {355,358,363}) Required<AudioClip>(Root+"native-feedback-"+id+".mp3");
            foreach (var pool in source.pools) foreach (var row in pool.nodes)
            {
                if (!string.IsNullOrEmpty(row.sprite)) Required<Sprite>(row.sprite);
                if (row.text != null && row.fontId != 3372 && row.fontId != 3373)
                    throw new InvalidOperationException("Unmapped original reward font " + row.fontId);
            }
        }
        static BeachNativeRewardParticle PlaceSlot(Transform parent, PoolData source, int index, Transform target, AnimationClip clip, AudioMixerGroup mixer)
        {
            var root = parent.Find(source.name+" "+index) as RectTransform;
            if (!root) root = NewRect(parent, source.name+" "+index);
            var nodes = new Dictionary<string,RectTransform> { [source.name]=root };
            var labels = new List<Text>();
            foreach (var row in source.nodes)
            {
                RectTransform rect;
                if (row.path==source.name) rect=root;
                else
                {
                    int slash=row.path.LastIndexOf('/'); var p=nodes[row.path.Substring(0,slash)];
                    rect=p.Find(row.path.Substring(slash+1)) as RectTransform;
                    if (!rect) rect=NewRect(p,row.path.Substring(slash+1));
                }
                nodes[row.path]=rect;
                rect.anchorMin=row.anchorMin; rect.anchorMax=row.anchorMax; rect.pivot=row.pivot;
                rect.sizeDelta=row.sizeDelta; rect.anchoredPosition=row.position;
                rect.localScale=row.scale; rect.localRotation=row.rotation;
                if (!string.IsNullOrEmpty(row.sprite))
                { var image=Component<Image>(rect.gameObject); image.sprite=Required<Sprite>(row.sprite); image.color=row.color; image.raycastTarget=false; }
                if (row.text!=null)
                {
                    var label=Component<OriginalSdfText>(rect.gameObject);
                    label.font=Required<Font>(FontRoot+"LilitaOneOriginal.fontsettings");
                    label.material=Required<Material>(FontRoot+(row.fontId==3372 ? "LilitaOneNativeOutline.mat" : "LilitaOneNativeRegular.mat"));
                    label.sourceAtlas=Required<Texture2D>(FontRoot+"LilitaOneOriginalSDF.png");
                    label.sourceMetrics=Required<TextAsset>(FontRoot+"LilitaOneNativeMetrics.json");
                    label.nativePointSize=row.fontSize; label.fontSize=Mathf.RoundToInt(row.fontSize);
                    label.text=row.text; label.color=row.color; label.raycastTarget=false;
                    label.horizontalOverflow=HorizontalWrapMode.Overflow;label.verticalOverflow=VerticalWrapMode.Overflow;
                    label.alignment=(TextAnchor)((row.vertical==256?0:3)+(row.horizontal==1?0:1)); labels.Add(label);
                }
                rect.gameObject.SetActive(row.active);
            }
            // Reapply original RectTransform.m_Children order after the complete subtree exists.
            // Construction order is topological; rendering order belongs to the source sibling indices.
            foreach (var row in source.nodes.OrderBy(n => n.siblingIndex))
            {
                if (row.path == source.name) continue;
                var rect = nodes[row.path];
                if (rect.GetSiblingIndex() == row.siblingIndex) continue;
                Undo.RecordObjects(new Object[] { rect, rect.parent }, UndoName);
                rect.SetSiblingIndex(row.siblingIndex);
            }
            var slot=Component<BeachNativeRewardParticle>(root.gameObject);
            slot.labels=labels.ToArray();slot.target=target;slot.clip=clip;
            slot.checkpointAudio=Audio(root.gameObject,358,1,mixer);
            slot.collectionAudio=Audio(nodes[source.name+"/CollectCoinSFX"].gameObject,363,.200000003f,mixer);
            slot.checkpointPitchMin=1.1f;slot.checkpointPitchMax=.9f;slot.collectionPitchMin=1.2f;slot.collectionPitchMax=.8f;
            slot.Cancel(); EditorUtility.SetDirty(slot); return slot;
        }
        static AnimationClip RestoreClip(ClipData row)
        {
            string path=Root+"NativeReward322.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip=new AnimationClip {name=row.name,frameRate=row.frameRate};AssetDatabase.CreateAsset(clip,path); }
            foreach (var c in row.curves)
            {
                Type type=c.targetKind=="GameObject"?typeof(GameObject):c.targetKind=="Transform"?typeof(Transform):c.targetKind=="RectTransform"?typeof(RectTransform):c.sourceProperty!=null&&c.sourceProperty.StartsWith("m_fontColor")?typeof(OriginalSdfText):typeof(Image);
                var keys=c.keys.Select(k=>new Keyframe(k.time,k.value,k.inConstant?float.PositiveInfinity:k.inSlope,k.outConstant?float.PositiveInfinity:k.outSlope,k.inWeight,k.outWeight){weightedMode=(WeightedMode)k.weightedMode}).ToArray();
                var binding=c.discrete?EditorCurveBinding.DiscreteCurve(c.path,type,c.property):EditorCurveBinding.FloatCurve(c.path,type,c.property);
                AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
            }
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=row.duration;settings.loopTime=row.loop;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());EditorUtility.SetDirty(clip);return clip;
        }
        static Material FaceMaterial(int id,string texture,Material normal)
        {
            string path=Root+"NativeFace"+id+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) {material=new Material(normal);AssetDatabase.CreateAsset(material,path);}
            material.shader=normal.shader;material.mainTexture=Required<Texture2D>(texture);EditorUtility.SetDirty(material);return material;
        }
        static AudioSource Audio(GameObject go,int clip,float volume,AudioMixerGroup mixer)
        { var a=Component<AudioSource>(go);a.clip=Required<AudioClip>(Root+"native-feedback-"+clip+".mp3");a.playOnAwake=false;a.loop=false;a.volume=volume;a.pitch=1;a.spatialBlend=0;a.outputAudioMixerGroup=mixer;return a; }
        static T Required<T>(string path) where T:Object { var value=AssetDatabase.LoadAssetAtPath<T>(path);if(!value)throw new InvalidOperationException("Missing source asset "+path);return value; }
        static T Component<T>(GameObject go) where T:Component {var value=go.GetComponent<T>();if(!value)value=Undo.AddComponent<T>(go);Undo.RecordObject(value,UndoName);return value;}
        static Transform Child(Transform parent,string name)
        {
            var found=parent.Find(name);if(found)return found;
            var go=new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go,UndoName);
            ParentNewObject(go.transform,parent);
            return go.transform;
        }
        static RectTransform NewRect(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go,UndoName);
            ParentNewObject(go.transform,parent);
            return (RectTransform)go.transform;
        }
        static void ParentNewObject(Transform child,Transform parent)
        {
            // Parent changes require a hierarchy Undo operation, not only object creation registration.
            Undo.SetTransformParent(child,parent,UndoName);
            Undo.RecordObject(child,UndoName);
            child.localPosition=Vector3.zero;
            child.localRotation=Quaternion.identity;
            child.localScale=Vector3.one;
        }
    }
}

