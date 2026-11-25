using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BeachBoxing.Editor
{
    public static partial class BeachPeripheralRestore
    {
        private const string ArtRoot = "Assets/BeachBoxing/OriginalArt/UI/";
        private const string FontRoot = "Assets/BeachBoxing/OriginalFont/";
        private const string CanvasName = "Original peripheral canvas";
        private const string UndoName = "Restore original peripheral UI";

        [Serializable] private sealed class NativeUi
        {
            public int canvasId, additionalShaderChannels;
            public Vector2 canvasReferenceResolution;
            public float canvasWidthMatch, referencePixelsPerUnit;
            public NodeData[] nodes;
            public SpriteData[] sprites;
            public PortraitData[] portraits;
            public ClipData[] animations;
            public string[] unsupported;
        }

        [Serializable] private sealed class NodeData
        {
            public int sourceId, parentId, sourceSiblingIndex, pointerAudioId, clickAudioId;
            public string name;
            public bool active;
            public Vector2 anchorMin, anchorMax, pivot, sizeDelta;
            public Vector3 anchoredPosition, scale;
            public Quaternion rotation;
            public ImageData image;
            public LabelData label;
            public SelectableData button;
            public MaskData mask;
            public ScrollData scroll;
            public ScrollbarData scrollbar;
            public AudioData audio;
            public SliderData slider;
            public InputData input;
            public RectMaskData rectMask;
            public LayoutData layoutElement;
        }

        [Serializable] private class ComponentData { public int componentId; public bool enabled; }
        [Serializable] private sealed class ImageData : ComponentData
        {
            public string spritePath;
            public Color color;
            public int imageType, fillMethod, fillOrigin;
            public bool raycast, maskable, preserveAspect, fillCenter, fillClockwise, useSpriteMesh;
            public float fillAmount, pixelsPerUnitMultiplier;
        }

        [Serializable] private sealed class LabelData : ComponentData
        {
            public int fontId, horizontal, vertical;
            public string text;
            public Color color;
            public float pointSize, minSize, maxSize;
            public bool raycast, maskable, wordWrap, autoSize;
        }

        [Serializable] private class SelectableData : ComponentData
        {
            public int navigation, transition, targetId;
            public bool interactable;
            public Color normal, highlighted, pressed, selected, disabled;
            public float multiplier, fadeDuration;
            public string highlightedSprite, pressedSprite, selectedSprite, disabledSprite;
        }

        [Serializable] private sealed class MaskData : ComponentData { public bool showGraphic; }
        [Serializable] private sealed class ScrollData : ComponentData
        {
            public int contentId, viewportId, horizontalBarId, verticalBarId, movementType, horizontalVisibility, verticalVisibility;
            public bool horizontal, vertical, inertia;
            public float elasticity, decelerationRate, sensitivity, horizontalSpacing, verticalSpacing;
        }

        [Serializable] private sealed class ScrollbarData : SelectableData
        {
            public int handleId, direction, steps;
            public float value, size;
        }

        [Serializable] private sealed class AudioData : ComponentData
        {
            public string clipPath;
            public bool playOnAwake, loop;
            public float volume, pitch, spatialBlend;
        }

        [Serializable] private sealed class SpriteData
        {
            public string path;
            public float pixelsPerUnit;
            public Vector2 pivot;
            public Vector4 border;
            public SpriteMeshType meshType;
        }
        [Serializable] private sealed class PortraitData
        {
            public int targetId, portraitId, landscapeId;
            public bool applyPhoneScale;
            public bool retainPhoneScaleInLandscape;
            public Vector3 phoneScale;
        }

        [Serializable] private sealed class ClipData { public int sourceId; public string name; public float frameRate; public CurveData[] curves; }
        [Serializable] private sealed class CurveData { public string path, property; public bool gameObject, discrete; public KeyData[] keys; }
        [Serializable] private sealed class KeyData { public float time, value, inSlope, outSlope; }

        [MenuItem("Beach Boxing/Restore original peripheral UI")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            var flow = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BeachGameFlow>(true)).SingleOrDefault();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !flow)
                throw new InvalidOperationException("Open the Beach Boxing gameplay scene in Edit Mode before restoring its peripheral UI.");

            var source = JsonUtility.FromJson<NativeUi>(Required<TextAsset>(ArtRoot + "NativePeripheralUi.json").text);
            var font = Required<Font>(FontRoot + "LilitaOneOriginal.fontsettings");
            var atlas = Required<Texture2D>(FontRoot + "LilitaOneOriginalSDF.png");
            var metrics = Required<TextAsset>(FontRoot + "LilitaOneNativeMetrics.json");
            var plain = Required<Material>(FontRoot + "LilitaOneNativeRegular.mat");
            var outline = Required<Material>(FontRoot + "LilitaOneNativeOutline.mat");
            var mixer = Required<AudioMixer>("Assets/BeachBoxing/OriginalArt/AudioMixerController/Main.mixer");
            var sfx = mixer.FindMatchingGroups("SFX").Single(group => group.name == "SFX");
            PrepareSprites(source.sprites);
            var clips = source.animations.ToDictionary(clip => clip.sourceId, ImportClip);

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                var view = Component<BeachPeripheralView>(flow.gameObject);
                Component<BeachAutoTraining>(flow.gameObject);
                var canvas = RestoreCanvas(flow.transform, source);
                var oldNodes = view.nodes.Where(node => node != null && node.transform && node.transform.IsChildOf(canvas))
                    .ToDictionary(node => node.sourceId);
                var nodes = new Dictionary<int, NativeUiNode>();
                var siblingIndices = new Dictionary<int, int>();
                foreach (var row in source.nodes)
                {
                    var parent = row.parentId == source.canvasId ? canvas : nodes[row.parentId].transform;
                    siblingIndices.TryGetValue(row.parentId, out int index);
                    siblingIndices[row.parentId] = index + 1;
                    oldNodes.TryGetValue(row.sourceId, out var previous);
                    var node = RestoreNode(row, parent, index, previous);
                    nodes.Add(row.sourceId, node);
                    if (node.label)
                        RestoreLabel((OriginalSdfText)node.label, row.label, font, atlas, metrics, row.label.fontId == 3372 ? outline : plain);
                    if (row.audio?.componentId > 0) RestoreAudio(node, row.audio, sfx);
                }

                foreach (var row in source.nodes)
                    RestoreWiring(nodes[row.sourceId], row, nodes);

                view.nodes = source.nodes.Select(row => nodes[row.sourceId]).ToArray();
                view.portraits = source.portraits.Select(portrait => RestorePortrait(canvas, portrait, nodes)).ToArray();
                view.menuOpen = clips[329];
                view.menuClose = clips[327];
                view.alertClip = clips[344];
                EditorUtility.SetDirty(view);
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"Restored {nodes.Count} native peripheral UI objects and {view.portraits.Length} orientation bindings. " +
                    "Legacy components handled separately or omitted: " + string.Join(", ", source.unsupported) + ". Save the scene after review.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static RectTransform RestoreCanvas(Transform owner, NativeUi source)
        {
            var rect = FindOrPlace(owner, CanvasName);
            var canvas = Component<Canvas>(rect.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1;
            canvas.additionalShaderChannels = (AdditionalCanvasShaderChannels)source.additionalShaderChannels;
            var scaler = Component<CanvasScaler>(rect.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = source.canvasReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = source.canvasWidthMatch;
            scaler.referencePixelsPerUnit = source.referencePixelsPerUnit;
            Component<GraphicRaycaster>(rect.gameObject);
            return rect;
        }

        private static NativeUiNode RestoreNode(NodeData row, RectTransform parent, int index, NativeUiNode previous)
        {
            var rect = previous?.transform ? previous.transform : Place(parent, row.name);
            Undo.RecordObjects(new Object[] { rect, rect.gameObject, parent }, UndoName);
            if (rect.parent != parent) Undo.SetTransformParent(rect, parent, UndoName);
            rect.name = row.name;
            rect.SetSiblingIndex(index);
            rect.anchorMin = row.anchorMin;
            rect.anchorMax = row.anchorMax;
            rect.pivot = row.pivot;
            rect.sizeDelta = row.sizeDelta;
            rect.localRotation = row.rotation;
            rect.localScale = row.scale;
            rect.anchoredPosition3D = row.anchoredPosition;
            rect.gameObject.layer = LayerMask.NameToLayer("UI");
            rect.gameObject.SetActive(row.active);

            var node = new NativeUiNode { sourceId = row.sourceId, transform = rect };
            if (row.image?.componentId > 0)
            {
                node.image = Component<Image>(rect.gameObject);
                RestoreImage(node.image, row.image);
            }
            if (row.label?.componentId > 0) node.label = Component<OriginalSdfText>(rect.gameObject);
            if (row.button?.componentId > 0) node.button = Component<Button>(rect.gameObject);
            if (row.sourceId == 1110) Component<NativeStationTouch>(rect.gameObject);
            if (row.mask?.componentId > 0)
            {
                var mask = Component<Mask>(rect.gameObject);
                mask.showMaskGraphic = row.mask.showGraphic;
                mask.enabled = row.mask.enabled;
            }
            if (row.scrollbar?.componentId > 0) Component<Scrollbar>(rect.gameObject);
            if (row.scroll?.componentId > 0) Component<ScrollRect>(rect.gameObject);
            return node;
        }

        private static void RestoreImage(Image image, ImageData data)
        {
            image.sprite = SpriteAt(data.spritePath);
            image.color = data.color;
            image.type = (Image.Type)data.imageType;
            image.preserveAspect = data.preserveAspect;
            image.fillCenter = data.fillCenter;
            image.fillMethod = (Image.FillMethod)data.fillMethod;
            image.fillOrigin = data.fillOrigin;
            image.fillAmount = data.fillAmount;
            image.fillClockwise = data.fillClockwise;
            image.useSpriteMesh = data.useSpriteMesh;
            image.pixelsPerUnitMultiplier = data.pixelsPerUnitMultiplier;
            image.raycastTarget = data.raycast;
            image.maskable = data.maskable;
            image.enabled = data.enabled;
        }

        private static void RestoreLabel(OriginalSdfText label, LabelData data, Font font, Texture2D atlas, TextAsset metrics, Material material)
        {
            label.font = font;
            label.material = material;
            label.sourceAtlas = atlas;
            label.sourceMetrics = metrics;
            label.nativePointSize = data.pointSize;
            label.fontSize = Mathf.RoundToInt(data.pointSize);
            label.text = data.text;
            label.color = data.color;
            label.raycastTarget = data.raycast;
            label.maskable = data.maskable;
            label.horizontalOverflow = data.wordWrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.alignment = (TextAnchor)((data.vertical == 256 ? 0 : 3) + (data.horizontal == 1 ? 0 : 1));
            label.resizeTextForBestFit = data.autoSize;
            label.resizeTextMinSize = Mathf.RoundToInt(data.minSize);
            label.resizeTextMaxSize = Mathf.RoundToInt(data.maxSize);
            label.enabled = data.enabled;
            label.SetAllDirty();
        }

        private static void RestoreWiring(NativeUiNode node, NodeData row, Dictionary<int, NativeUiNode> nodes)
        {
            if (node.button) RestoreSelectable(node.button, row.button, nodes);
            RestoreAudioEvents(node, row, nodes);
            if (row.scrollbar?.componentId > 0)
            {
                var bar = node.transform.GetComponent<Scrollbar>();
                RestoreSelectable(bar, row.scrollbar, nodes);
                bar.handleRect = nodes[row.scrollbar.handleId].transform;
                bar.direction = (Scrollbar.Direction)row.scrollbar.direction;
                bar.numberOfSteps = row.scrollbar.steps;
                bar.size = row.scrollbar.size;
                bar.SetValueWithoutNotify(row.scrollbar.value);
            }
            if (row.scroll?.componentId > 0)
            {
                var rect = node.transform.GetComponent<ScrollRect>();
                var data = row.scroll;
                rect.content = nodes[data.contentId].transform;
                rect.viewport = nodes[data.viewportId].transform;
                rect.horizontal = data.horizontal;
                rect.vertical = data.vertical;
                rect.movementType = (ScrollRect.MovementType)data.movementType;
                rect.elasticity = data.elasticity;
                rect.inertia = data.inertia;
                rect.decelerationRate = data.decelerationRate;
                rect.scrollSensitivity = data.sensitivity;
                rect.horizontalScrollbar = data.horizontalBarId == 0 ? null : nodes[data.horizontalBarId].transform.GetComponent<Scrollbar>();
                rect.verticalScrollbar = data.verticalBarId == 0 ? null : nodes[data.verticalBarId].transform.GetComponent<Scrollbar>();
                rect.horizontalScrollbarVisibility = (ScrollRect.ScrollbarVisibility)data.horizontalVisibility;
                rect.verticalScrollbarVisibility = (ScrollRect.ScrollbarVisibility)data.verticalVisibility;
                rect.horizontalScrollbarSpacing = data.horizontalSpacing;
                rect.verticalScrollbarSpacing = data.verticalSpacing;
                rect.enabled = data.enabled;
            }
        }

        private static void RestoreAudio(NativeUiNode node, AudioData data, AudioMixerGroup mixer)
        {
            var audio = Component<AudioSource>(node.transform.gameObject);
            audio.clip = Required<AudioClip>(data.clipPath);
            audio.outputAudioMixerGroup = mixer;
            audio.playOnAwake = data.playOnAwake;
            audio.loop = data.loop;
            audio.volume = data.volume;
            audio.pitch = data.pitch;
            audio.spatialBlend = data.spatialBlend;
            audio.enabled = data.enabled;
        }

        private static void RestoreAudioEvents(NativeUiNode node, NodeData row, Dictionary<int, NativeUiNode> nodes)
        {
            if (row.pointerAudioId != 0)
            {
                var audio = nodes[row.pointerAudioId].transform.GetComponent<AudioSource>();
                var trigger = Component<EventTrigger>(node.transform.gameObject);
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                UnityEventTools.AddVoidPersistentListener(entry.callback, audio.Play);
                trigger.triggers = new List<EventTrigger.Entry> { entry };
            }

            if (node.button && row.clickAudioId != 0)
            {
                var audio = nodes[row.clickAudioId].transform.GetComponent<AudioSource>();
                var click = node.button.onClick;
                for (int i = click.GetPersistentEventCount() - 1; i >= 0; i--)
                    if (click.GetPersistentTarget(i) == audio && click.GetPersistentMethodName(i) == nameof(AudioSource.Play))
                        UnityEventTools.RemovePersistentListener(click, i);
                UnityEventTools.AddVoidPersistentListener(click, audio.Play);
            }
        }

        private static void RestoreSelectable(Selectable control, SelectableData data, Dictionary<int, NativeUiNode> nodes)
        {
            var navigation = control.navigation;
            navigation.mode = (Navigation.Mode)data.navigation;
            control.navigation = navigation;
            control.transition = (Selectable.Transition)data.transition;
            control.colors = new ColorBlock
            {
                normalColor = data.normal, highlightedColor = data.highlighted, pressedColor = data.pressed,
                selectedColor = data.selected, disabledColor = data.disabled, colorMultiplier = data.multiplier,
                fadeDuration = data.fadeDuration
            };
            control.spriteState = new SpriteState
            {
                highlightedSprite = SpriteAt(data.highlightedSprite), pressedSprite = SpriteAt(data.pressedSprite),
                selectedSprite = SpriteAt(data.selectedSprite), disabledSprite = SpriteAt(data.disabledSprite)
            };
            control.targetGraphic = data.targetId == 0 ? null : nodes[data.targetId].transform.GetComponent<Graphic>();
            control.interactable = data.interactable;
            control.enabled = data.enabled;
        }

        private static NativeUiPortrait RestorePortrait(RectTransform canvas, PortraitData data, Dictionary<int, NativeUiNode> nodes)
        {
            var target = nodes[data.targetId].transform;
            var portrait = nodes[data.portraitId].transform;
            var landscape = nodes[data.landscapeId].transform;
            // Source markers sometimes alias their target. Snapshot those poses before runtime changes the target.
            if (data.applyPhoneScale || portrait == target)
                portrait = SnapshotPose(canvas, $"Portrait pose {data.targetId}", portrait,
                    data.applyPhoneScale ? data.phoneScale : portrait.localScale);
            if (landscape == target)
                landscape = SnapshotPose(canvas, $"Landscape pose {data.targetId}", landscape, landscape.localScale);
            return new NativeUiPortrait { target = target, portrait = portrait, landscape = landscape,
                retainPhoneScaleInLandscape = data.retainPhoneScaleInLandscape };
        }

        private static RectTransform SnapshotPose(RectTransform canvas, string name, RectTransform source, Vector3 scale)
        {
            var pose = FindOrPlace(canvas, name);
            Undo.RecordObject(pose, UndoName);
            pose.anchorMin = source.anchorMin;
            pose.anchorMax = source.anchorMax;
            pose.pivot = source.pivot;
            pose.sizeDelta = source.sizeDelta;
            pose.anchoredPosition3D = source.anchoredPosition3D;
            pose.localScale = scale;
            return pose;
        }

        private static void PrepareSprites(SpriteData[] sprites)
        {
            foreach (var frame in sprites)
            {
                var importer = AssetImporter.GetAtPath(frame.path) as TextureImporter;
                if (!importer) throw new InvalidOperationException("Import native sprite texture " + frame.path + " first.");
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                    importer.spritePixelsPerUnit == frame.pixelsPerUnit && importer.spriteBorder == frame.border &&
                    settings.spriteAlignment == (int)SpriteAlignment.Custom && settings.spritePivot == frame.pivot &&
                    settings.spriteMeshType == frame.meshType)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = frame.pivot;
                settings.spriteMeshType = frame.meshType;
                importer.SetTextureSettings(settings);
                importer.spritePixelsPerUnit = frame.pixelsPerUnit;
                importer.spriteBorder = frame.border;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static AnimationClip ImportClip(ClipData source)
        {
            var clip = new AnimationClip { name = source.name, frameRate = source.frameRate };
            foreach (var track in source.curves)
            {
                var keys = new Keyframe[track.keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    var key = track.keys[i];
                    keys[i] = new Keyframe(key.time, key.value, key.inSlope, key.outSlope);
                }
                var type = track.gameObject ? typeof(GameObject) : typeof(Transform);
                var binding = track.discrete
                    ? EditorCurveBinding.DiscreteCurve(track.path, type, track.property)
                    : EditorCurveBinding.FloatCurve(track.path, type, track.property);
                AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys));
            }

            string path = ArtRoot + "Native" + source.name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!existing)
            {
                AssetDatabase.CreateAsset(clip, path);
                return clip;
            }

            EditorUtility.CopySerialized(clip, existing);
            Object.DestroyImmediate(clip);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static RectTransform FindOrPlace(Transform parent, string name) =>
            parent.Find(name) is RectTransform existing ? existing : Place(parent, name);

        private static RectTransform Place(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, UndoName);
            Undo.SetTransformParent(child.transform, parent, UndoName);
            return (RectTransform)child.transform;
        }

        private static T Component<T>(GameObject node) where T : Component
        {
            var value = node.GetComponent<T>();
            if (!value) value = Undo.AddComponent<T>(node);
            Undo.RecordObject(value, UndoName);
            return value;
        }

        private static Sprite SpriteAt(string path) => string.IsNullOrEmpty(path) ? null : Required<Sprite>(path);

        private static T Required<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) throw new InvalidOperationException("Missing original peripheral UI asset " + path + ".");
            return asset;
        }
    }
}
