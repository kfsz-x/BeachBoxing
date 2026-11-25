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
    public static class BeachOriginalNpcLifecycleRestore
    {
        private const string OriginalArt = "Assets/BeachBoxing/OriginalArt/";
        private const string OriginalFont = "Assets/BeachBoxing/OriginalFont/";
        private const string UndoLabel = "Bind original desktop NPC lifecycle";

        [Serializable]
        private sealed class Source
        {
            public string[] fakeNames, adjectives, nouns;
            public Marker[] markers;
            public Station[] stations;
            public MusclePose[] muscles;
            public RectPose titleCanvas;
            public Label[] titleLabels;
            public Alert[] alerts;
            public AudioChannel[] audio;
        }

        [Serializable]
        private sealed class Marker
        {
            public int id;
            public string field, sourcePath;
            public Vector3 position;
        }

        [Serializable]
        private sealed class Station
        {
            public int id;
            public string sourcePath, playerMuscle, playerPos, playerExitPos, holder, pivotObj, idle, move;
            public bool npcCandidate, available;
        }

        [Serializable]
        private sealed class MusclePose
        {
            public string group, target;
            public int[] occurrences;
            public BeachMuscleView.Pose start, end;
        }

        [Serializable]
        private sealed class RectPose
        {
            public string name;
            public Vector2 anchorsMin, anchorsMax, pivot, size;
            public Vector3 anchoredPosition, scale;
            public Quaternion rotation;
        }

        [Serializable]
        private sealed class Label
        {
            public RectPose rect;
            public string text;
            public int fontId;
            public float pointSize;
            public Color color;
        }

        [Serializable]
        private sealed class Alert
        {
            public RectPose rect, image;
            public Color imageColor;
            public string spritePath;
            public Label[] labels;
        }

        [Serializable]
        private sealed class AudioChannel
        {
            public string name;
            public int clipId, rolloff;
            public float volume, pitch, spatial, minDistance, maxDistance;
        }

        private sealed class ActorBinding
        {
            public Transform root;
            public Animator animator;
            public BeachAmbientNpc legacy;
            public Transform[] audioHolders;
            public BeachNativeNpcActor.MusclePose[] muscles;
        }

        private sealed class Arts
        {
            public Font font;
            public Material plain, outline;
            public Texture2D atlas;
            public TextAsset metrics;
            public Sprite[] alertSprites;
            public AudioMixerGroup sfx;
            public AudioClip[] channels;
        }

        [MenuItem("Beach Boxing/Bind original desktop NPC lifecycle")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before binding NPCs.");
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var beach = Find(flow.transform, "Training beach");
            var world = Find(beach, "Original Workout world");
            var ambient = Find(beach, "Original ambient NPCs");
            var ui = Find(flow.transform, "Original peripheral canvas");
            var canvas = Require(ui.GetComponent<Canvas>(), "Original peripheral canvas component");
            var stage = new SerializedObject(flow).FindProperty("stage");
            var camera = Require(stage.FindPropertyRelative("camera").objectReferenceValue as Camera, "prebound stage camera");
            if (!UnityEditorInternal.InternalEditorUtility.tags.Contains("NPC"))
                throw new InvalidOperationException("Restore the original NPC tag before binding the desktop lifecycle.");
            var source = JsonUtility.FromJson<Source>(Asset<TextAsset>(OriginalArt + "World/NativeNpcLifecycle.json").text);
            ValidateSource(source);
            var art = LoadArt(source);
            var stations = source.stations.Select(s => BindStation(s, world, stage)).ToArray();
            var pool = new[] { "Chest", "Arms", "Back" }.Select(name => ReadActor(Find(ambient, "Original NPC " + name), source)).ToArray();
            var arena = Find(world, source.markers.Single(marker => marker.id == 1583).sourcePath);
            var newSpawnSource = source.markers.Single(marker => marker.id == 1597);
            var oldSpawnSources = source.markers.Where(marker => marker.field == "possibleOldNPCPoints").ToArray();
            ValidateRect(ambient, "Source NPC spawns", false);
            var existingMarkers = ambient.Find("Source NPC spawns");
            if (existingMarkers)
            {
                ValidateRect(existingMarkers, MarkerName(newSpawnSource), false);
                foreach (var marker in oldSpawnSources) ValidateRect(existingMarkers, MarkerName(marker), false);
            }
            foreach (var actor in pool)
            {
                ValidateRect(actor.root, source.titleCanvas.name);
                var title = actor.root.Find(source.titleCanvas.name);
                if (title)
                    foreach (var label in source.titleLabels) ValidateRect(title, label.rect.name);
            }
            foreach (var alert in source.alerts)
            {
                ValidateRect(ui, alert.rect.name);
                var root = ui.Find(alert.rect.name);
                if (!root) continue;
                ValidateRect(root, alert.image.name);
                foreach (var label in alert.labels) ValidateRect(root, label.rect.name);
            }

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                var actors = pool.Select(r => BindActor(r, source, art, camera)).ToArray();
                var markerGroup = Place(ambient, "Source NPC spawns", false);
                var newSpawn = BindMarker(markerGroup, world, newSpawnSource);
                var oldSpawns = oldSpawnSources.Select(marker => BindMarker(markerGroup, world, marker)).ToArray();
                Undo.RecordObject(canvas, UndoLabel);
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal;
                var connected = BindAlert(ui, source.alerts[0], art.alertSprites[0], art);
                var disconnected = BindAlert(ui, source.alerts[1], art.alertSprites[1], art);
                var manager = Component<BeachNativeNpcManager>(flow.gameObject);
                Undo.RecordObject(manager, UndoLabel);
                manager.Bind(actors, stations, oldSpawns, newSpawn, arena, source.fakeNames, source.adjectives,
                    source.nouns, connected, disconnected);
                Record(manager);
                EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
                Undo.CollapseUndoOperations(undo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undo);
                throw;
            }
        }

        private static void ValidateSource(Source source)
        {
            if (source == null || source.fakeNames?.Length != 73 || source.adjectives?.Length != 44 ||
                source.nouns?.Length != 44 || source.muscles?.Length != 42 || source.stations?.Length != 8 ||
                source.stations.Count(s => s.npcCandidate) != 7 || source.stations.Count(s => !string.IsNullOrEmpty(s.playerMuscle)) != 4 ||
                source.markers?.Length != 16 || source.markers.Count(m => m.field == "possibleOldNPCPoints") != 14 ||
                source.alerts?.Length != 2 || source.titleLabels?.Length != 3 || source.audio?.Length != 3)
                throw new InvalidOperationException("Incomplete NPC native source bindings.");
        }

        private static Arts LoadArt(Source source)
        {
            var mixer = Asset<AudioMixer>(OriginalArt + "AudioMixerController/Main.mixer");
            return new Arts
            {
                font = Asset<Font>(OriginalFont + "LilitaOneOriginal.fontsettings"),
                plain = Asset<Material>(OriginalFont + "LilitaOneNativeRegular.mat"),
                outline = Asset<Material>(OriginalFont + "LilitaOneNativeOutline.mat"),
                atlas = Asset<Texture2D>(OriginalFont + "LilitaOneOriginalSDF.png"),
                metrics = Asset<TextAsset>(OriginalFont + "LilitaOneNativeMetrics.json"),
                alertSprites = source.alerts.Select(a => Asset<Sprite>(a.spritePath)).ToArray(),
                sfx = mixer.FindMatchingGroups("SFX").Single(g => g.name == "SFX"),
                channels = source.audio.Select(a => Asset<AudioClip>(OriginalArt + "NativeCombatAudio/native-combat-0-" + a.clipId + ".mp3")).ToArray()
            };
        }

        private static BeachNativeNpcManager.Station BindStation(Station source, Transform world, SerializedProperty stage)
        {
            int muscle = string.IsNullOrEmpty(source.playerMuscle) ? -1 : (int)Enum.Parse(typeof(Muscle), source.playerMuscle);
            Transform machine, holder;
            Vector3 holderOffset = Vector3.zero;
            if (muscle >= 0)
            {
                machine = Require(stage.FindPropertyRelative("equipment").GetArrayElementAtIndex(muscle).objectReferenceValue as Transform,
                    "prebound player equipment " + source.playerMuscle);
                holder = Require(machine.parent, source.playerMuscle + " group");
                // The player equipment moved out of the native world during the original world restore.
                // Preserve the actual holder pivot, including the legs holder's offset from its machine.
                var originalHolder = MatchingPaths(world, source.holder)
                    .OrderBy(t => (t.position - holder.position).sqrMagnitude).FirstOrDefault();
                Require(originalHolder, source.holder);
                holderOffset = holder.parent.InverseTransformPoint(originalHolder.position) - holder.localPosition;
            }
            else
            {
                machine = Require(MatchingPaths(world, source.sourcePath).SingleOrDefault(), source.sourcePath);
                holder = Require(machine.parent, source.holder);
                if (holder.name != source.holder.Split('/').Last())
                    throw new InvalidOperationException("Native holder mismatch for " + source.sourcePath);
            }

            return new BeachNativeNpcManager.Station
            {
                sourceId = source.id,
                playerMuscle = muscle,
                playerOnly = !source.npcCandidate,
                unavailableAtStart = !source.available,
                target = machine,
                playerPos = UnderEquipment(machine, source.sourcePath, source.playerPos),
                exitPos = UnderEquipment(machine, source.sourcePath, source.playerExitPos),
                pivot = string.IsNullOrEmpty(source.pivotObj) ? null : UnderEquipment(machine, source.sourcePath, source.pivotObj).gameObject,
                holder = holder,
                holderRestScale = holder.localScale,
                holderRestPosition = holder.localPosition,
                holderPivotOffset = holderOffset,
                idle = source.idle,
                move = source.move
            };
        }

        private static Transform UnderEquipment(Transform root, string parent, string sourcePath)
        {
            if (sourcePath == parent) return root;
            if (!sourcePath.StartsWith(parent + "/", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected a native anchor beneath " + parent + ", got " + sourcePath);
            return Find(root, sourcePath.Substring(parent.Length + 1));
        }

        private static ActorBinding ReadActor(Transform root, Source source)
        {
            var animator = Require(root.GetComponentInChildren<Animator>(true), root.name + " Armature");
            Require(animator.runtimeAnimatorController, root.name + " Animator controller");
            if (animator.GetComponents<Animator>().Length != 1)
                throw new InvalidOperationException("Unexpected native NPC Animator count on " + root.name);
            var bound = new ActorBinding
            {
                root = root,
                animator = animator,
                legacy = Require(root.GetComponent<BeachAmbientNpc>(), root.name + " original static setup"),
                audioHolders = source.audio.Select(audio => Find(animator.transform, audio.name)).ToArray(),
                muscles = source.muscles.Select(m => new BeachNativeNpcActor.MusclePose
                {
                    group = (Muscle)Enum.Parse(typeof(Muscle), m.group),
                    renderer = Require(FindOccurrence(root, m.target, m.occurrences).GetComponent<Renderer>(), m.target + " renderer"),
                    start = m.start,
                    end = m.end
                }).ToArray()
            };
            if (bound.muscles.Select(m => m.renderer).Distinct().Count() != bound.muscles.Length)
                throw new InvalidOperationException("The preplaced NPC muscle hierarchy does not match the native prefab.");
            return bound;
        }

        private static BeachNativeNpcActor BindActor(ActorBinding data, Source source, Arts art, Camera camera)
        {
            var root = data.root;
            Undo.RecordObject(data.legacy, UndoLabel);
            data.legacy.enabled = false;
            if (data.legacy.StaticPivot)
            {
                Undo.RecordObject(data.legacy.StaticPivot, UndoLabel);
                data.legacy.StaticPivot.SetActive(true);
                Record(data.legacy.StaticPivot);
            }
            Record(data.legacy);
            Undo.RecordObject(root.gameObject, UndoLabel);
            Undo.RecordObject(root, UndoLabel);
            root.gameObject.SetActive(false);
            root.gameObject.tag = "NPC";
            root.localScale = Vector3.one * BeachNativeNpcManager.WorldScale;
            Undo.RecordObject(data.animator, UndoLabel);
            data.animator.applyRootMotion = false;
            data.animator.fireEvents = true;
            var audio = Component<BeachNativeBoxingAudio>(data.animator.gameObject);
            Undo.RecordObject(audio, UndoLabel);
            audio.step1 = BindAudio(data.audioHolders[0], source.audio[0], art.channels[0], art.sfx);
            audio.step2 = BindAudio(data.audioHolders[1], source.audio[1], art.channels[1], art.sfx);
            audio.jump = BindAudio(data.audioHolders[2], source.audio[2], art.channels[2], art.sfx);
            Record(audio);
            Record(data.animator);

            var title = BindRect(root, source.titleCanvas);
            var canvas = Component<Canvas>(title.gameObject);
            Undo.RecordObject(canvas, UndoLabel);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal;
            var labels = source.titleLabels.Select(label => BindLabel(title, label, art)).ToArray();
            var actor = Component<BeachNativeNpcActor>(root.gameObject);
            Undo.RecordObject(actor, UndoLabel);
            Undo.RecordObject(root.GetComponent<Rigidbody>(), UndoLabel);
            Undo.RecordObject(root.GetComponent<CapsuleCollider>(), UndoLabel);
            actor.ConfigureBody();
            actor.Bind(data.animator, labels[0], labels[1], labels[2], data.muscles);
            Undo.RecordObject(data.animator.transform.parent, UndoLabel);
            actor.AlignModelToFloor();
            Record(data.animator.transform.parent);
            Record(actor);
            Record(root.GetComponent<Rigidbody>());
            Record(root.GetComponent<CapsuleCollider>());
            Record(root);
            Record(root.gameObject);
            return actor;
        }

        private static AudioSource BindAudio(Transform target, AudioChannel source, AudioClip clip, AudioMixerGroup group)
        {
            var audio = Component<AudioSource>(target.gameObject);
            Undo.RecordObject(audio, UndoLabel);
            audio.clip = clip;
            audio.outputAudioMixerGroup = group;
            audio.playOnAwake = false;
            audio.loop = false;
            audio.volume = source.volume;
            audio.pitch = source.pitch;
            audio.spatialBlend = source.spatial;
            audio.minDistance = source.minDistance * BeachNativeNpcManager.WorldScale;
            audio.maxDistance = source.maxDistance * BeachNativeNpcManager.WorldScale;
            audio.rolloffMode = (AudioRolloffMode)source.rolloff;
            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0, 1, 1, 0));
            Record(audio);
            return audio;
        }

        private static string MarkerName(Marker source) => "Source NPC transform " + source.id;

        private static Transform BindMarker(Transform group, Transform world, Marker source)
        {
            var marker = Place(group, MarkerName(source), false);
            Undo.RecordObject(marker, UndoLabel);
            marker.SetPositionAndRotation(world.TransformPoint(source.position), Quaternion.identity);
            marker.localScale = Vector3.one;
            return marker;
        }

        private static GameObject BindAlert(Transform parent, Alert source, Sprite sprite, Arts art)
        {
            var root = BindRect(parent, source.rect);
            var imageRect = BindRect(root, source.image);
            var image = Component<Image>(imageRect.gameObject);
            Undo.RecordObject(image, UndoLabel);
            image.sprite = sprite;
            image.color = source.imageColor;
            image.raycastTarget = false;
            foreach (var label in source.labels) BindLabel(root, label, art);
            Undo.RecordObject(root.gameObject, UndoLabel);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static OriginalSdfText BindLabel(Transform parent, Label source, Arts art)
        {
            var rect = BindRect(parent, source.rect);
            var label = Component<OriginalSdfText>(rect.gameObject);
            Undo.RecordObject(label, UndoLabel);
            label.font = art.font;
            label.fontSize = Mathf.RoundToInt(source.pointSize);
            label.nativePointSize = source.pointSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.sourceAtlas = art.atlas;
            label.sourceMetrics = art.metrics;
            label.material = source.fontId == 3372 ? art.outline : art.plain;
            label.text = source.text;
            label.color = source.color;
            Record(label);
            return label;
        }

        private static RectTransform BindRect(Transform parent, RectPose source)
        {
            var rect = (RectTransform)Place(parent, source.name, true);
            Undo.RecordObject(rect, UndoLabel);
            rect.anchorMin = source.anchorsMin;
            rect.anchorMax = source.anchorsMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.size;
            rect.anchoredPosition3D = source.anchoredPosition;
            rect.localRotation = source.rotation;
            rect.localScale = source.scale;
            Record(rect);
            return rect;
        }

        private static void ValidateRect(Transform parent, string name, bool rect = true)
        {
            var target = parent.Find(name);
            if (target && (target is RectTransform) != rect)
                throw new InvalidOperationException("Incompatible native NPC transform " + name);
        }

        private static Transform Place(Transform parent, string name, bool rect)
        {
            var existing = parent.Find(name);
            if (existing) return existing;
            var item = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            Undo.RegisterCreatedObjectUndo(item, UndoLabel);
            Undo.SetTransformParent(item.transform, parent, UndoLabel);
            return item.transform;
        }

        private static Transform FindOccurrence(Transform root, string path, int[] occurrences)
        {
            var names = path.Split('/');
            if (occurrences == null || occurrences.Length != names.Length)
                throw new InvalidOperationException("Invalid source hierarchy address for " + path);
            for (int i = 0; i < names.Length; i++)
            {
                int occurrence = occurrences[i];
                Transform next = null;
                foreach (Transform child in root)
                {
                    if (child.name != names[i]) continue;
                    if (occurrence-- != 0) continue;
                    next = child;
                    break;
                }
                root = Require(next, path);
            }
            return root;
        }

        private static IEnumerable<Transform> MatchingPaths(Transform root, string path)
        {
            return path.Split('/').Aggregate(new[] { root }.AsEnumerable(),
                (parents, name) => parents.SelectMany(parent => parent.Cast<Transform>().Where(child => child.name == name)));
        }

        private static Transform Find(Transform root, string path) => Require(root.Find(path), path);
        private static T Asset<T>(string path) where T : Object => Require(AssetDatabase.LoadAssetAtPath<T>(path), path);

        private static T Require<T>(T value, string description) where T : Object
        {
            if (!value) throw new InvalidOperationException("Missing original NPC binding " + description);
            return value;
        }

        private static T Component<T>(GameObject owner) where T : UnityEngine.Component
        {
            var component = owner.GetComponent<T>();
            return component ? component : Undo.AddComponent<T>(owner);
        }

        private static void Record(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }
    }
}
