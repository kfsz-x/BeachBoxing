using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    public static partial class BeachNativeCombatFeedbackRestore
    {
        private const string UiArt = "Assets/BeachBoxing/OriginalArt/UI/NativeCombat/";
        private const string FontArt = "Assets/BeachBoxing/OriginalFont/";
        private const string Operation = "Restore source combat feedback";

        [Serializable] private sealed class Data
        {
            public Node[] overlayNodes, markerNodes;
            public SpriteBinding[] sprites;
            public Track damageAlpha, lowHealthAlpha, markerAnchoredY, markerAlpha;
            public ScaleData outOfStaminaScale, blockHintScale, markerScale;
            public ClipData[] boxingClips;
            public AudioData[] playerAudio;
        }

        [Serializable] private sealed class Node
        {
            public int sourceId, parentId, siblingIndex;
            public string name;
            public bool active;
            public Vector2 anchorMin, anchorMax, pivot, sizeDelta;
            public Vector3 anchoredPosition, scale;
            public Quaternion rotation;
            public ImageData image;
            public LabelData label;
        }

        [Serializable] private sealed class SpriteBinding { public int sourceId; public string path; }
        [Serializable] private sealed class SourceSprite { public int id; }
        [Serializable] private sealed class ImageData
        {
            public bool enabled, preserveAspect, raycast, maskable, fillCenter, fillClockwise;
            public SourceSprite sourceSprite;
            public int componentId, imageType, fillMethod, fillOrigin;
            public float fillAmount, pixelsPerUnitMultiplier;
            public Color color;
        }

        [Serializable] private sealed class LabelData
        {
            public string text;
            public float pointSize, minSize, maxSize;
            public Color color;
            public int componentId, horizontal, vertical, fontStyle;
            public bool enabled, wordWrap, autoSize, richText, raycast;
        }

        [Serializable] private sealed class ScaleData { public Track x, y, z; }
        [Serializable] private sealed class Track { public KeyData[] keys; }
        [Serializable] private sealed class KeyData
        {
            public float time, value, inSlope, outSlope, inWeight, outWeight;
            public bool inConstant, outConstant;
            public int weightedMode;
        }

        [MenuItem("Beach Boxing/Restore source combat overlays, hit markers and audio")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before restoring combat feedback.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var catalog = flow.GetComponent<NativeOpponentCatalog>();
            if (!catalog || catalog.Models.Length != NativeOpponentCatalog.PlayableCount)
                throw new InvalidOperationException("Preplace all 45 original opponents before restoring feedback.");
            var canvas = Need(flow.transform.Find("Original peripheral canvas"), "Original peripheral canvas");
            var scaler = Need(canvas.GetComponent<CanvasScaler>(), "Original width-match CanvasScaler");
            if (scaler.referenceResolution != new Vector2(800, 600) ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight ||
                scaler.matchWidthOrHeight != 0)
                throw new InvalidOperationException("The original peripheral canvas must use 800 × 600 width matching.");

            var data = JsonUtility.FromJson<Data>(Asset<TextAsset>(UiArt + "NativeCombatPresentation.json").text);
            var sprites = data.sprites.ToDictionary(item => item.sourceId, item => Asset<Sprite>(item.path));
            var font = Asset<Font>(FontArt + "LilitaOneOriginal.fontsettings");
            var atlas = Asset<Texture2D>(FontArt + "LilitaOneOriginalSDF.png");
            var metrics = Asset<TextAsset>(FontArt + "LilitaOneNativeMetrics.json");
            // Source material sharedassets0:2 has _OutlineWidth=.453 and _GradientScale=6.
            var material = Asset<Material>(FontArt + "LilitaOneNativeOutline.mat");
            var markerSpawns = catalog.Models.Select((model, index) =>
                Need(Need(model, "scene opponent " + index).Find("HitMarkerSpawn"), model.name + "/HitMarkerSpawn")).ToArray();
            var audio = PrepareAudio(flow, catalog, data);
            var writtenAssets = new List<string>();

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(Operation);
            try
            {
                ApplyScene(flow, canvas, data, sprites, font, atlas, metrics, material, markerSpawns, audio, writtenAssets);
                Undo.CollapseUndoOperations(group);
            }
            catch (Exception error)
            {
                Undo.RevertAllDownToGroup(group);
                string assets = writtenAssets.Count == 0
                    ? "No asset writes completed."
                    : "Asset writes completed before the failure: " + string.Join(", ", writtenAssets);
                throw new InvalidOperationException(Operation + " stopped. Scene changes were reverted. " + assets +
                    " " + error.Message, error);
            }
            Debug.Log("Restored five source combat overlays, eight world-space HitMarkers, and native combat audio bindings. Save the scene after review.");
        }

        private static void ApplyScene(BeachGameFlow flow, Transform canvas, Data data, Dictionary<int, Sprite> sprites,
            Font font, Texture2D atlas, TextAsset metrics, Material material, Transform[] markerSpawns,
            AudioPreparation audio, List<string> writtenAssets)
        {
            Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, Operation);
            var feedback = Component<BeachNativeCombatFeedback>(flow.gameObject);
            var screen = Rect(canvas, "Native combat UIScreen");
            screen.anchorMin = Vector2.zero;
            screen.anchorMax = Vector2.one;
            screen.pivot = Vector2.one * .5f;
            screen.offsetMin = screen.offsetMax = Vector2.zero;
            screen.localRotation = Quaternion.identity;
            screen.localScale = Vector3.one;
            screen.SetAsFirstSibling();

            var overlays = new Dictionary<int, RectTransform> { [37] = screen };
            foreach (var node in data.overlayNodes)
                overlays.Add(node.sourceId, Place(node, overlays[node.parentId], sprites, font, atlas, metrics, material));

            feedback.screen = screen.gameObject;
            feedback.takeDamage = overlays[260].gameObject;
            feedback.lowHealth = overlays[637].gameObject;
            feedback.healing = overlays[107].gameObject;
            feedback.outOfStamina = overlays[494].gameObject;
            feedback.blockHint = overlays[321].gameObject;
            feedback.blockHintPulse = overlays[578];
            feedback.damageImage = overlays[260].GetComponent<Image>();
            feedback.lowHealthImage = overlays[637].GetComponent<Image>();
            feedback.healingImage = overlays[107].GetComponent<Image>();
            feedback.damageAlpha = Curve(data.damageAlpha);
            feedback.lowHealthAlpha = Curve(data.lowHealthAlpha);
            feedback.outOfStaminaScale = Scale(data.outOfStaminaScale);
            feedback.blockHintScale = Scale(data.blockHintScale);
            feedback.markerScale = Scale(data.markerScale);
            feedback.markerAnchoredY = Curve(data.markerAnchoredY);
            feedback.markerAlpha = Curve(data.markerAlpha);

            const int capacity = 8;
            var scene = flow.gameObject.scene;
            var pool = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "Native combat HitMarker pool");
            if (!pool)
            {
                pool = new GameObject("Native combat HitMarker pool");
                SceneManager.MoveGameObjectToScene(pool, scene);
                Undo.RegisterCreatedObjectUndo(pool, Operation);
            }
            else Undo.RegisterFullObjectHierarchyUndo(pool, Operation);

            feedback.markerRoots = new Transform[capacity];
            feedback.markerLabels = new Text[capacity];
            var markerNode = data.markerNodes.Single(node => node.sourceId == 1836);
            var labelNode = data.markerNodes.Single(node => node.sourceId == 6820);
            for (int slot = 0; slot < capacity; slot++)
            {
                var marker = Rect(pool.transform, "HitMarker " + (slot + 1));
                ApplyRect(marker, markerNode);
                marker.anchoredPosition3D = Vector3.zero;
                marker.localRotation = Quaternion.identity;
                marker.localScale = Vector3.one * (3f * FightRules.ReferenceWorldScale);
                var markerCanvas = Component<Canvas>(marker.gameObject);
                markerCanvas.renderMode = RenderMode.WorldSpace;
                markerCanvas.additionalShaderChannels = (AdditionalCanvasShaderChannels)25;
                markerCanvas.sortingOrder = 0;
                var markerScaler = Component<CanvasScaler>(marker.gameObject);
                markerScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                markerScaler.referencePixelsPerUnit = 100;
                markerScaler.dynamicPixelsPerUnit = 1;
                var label = Place(labelNode, marker, sprites, font, atlas, metrics, material);
                feedback.markerRoots[slot] = marker;
                feedback.markerLabels[slot] = label.GetComponent<OriginalSdfText>();
                marker.gameObject.SetActive(false);
            }

            feedback.markerSpawns = markerSpawns;
            RestoreAudio(flow, feedback, audio, writtenAssets);
            feedback.ResetEffects();
            EditorUtility.SetDirty(feedback);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static RectTransform Place(Node node, Transform parent, Dictionary<int, Sprite> sprites,
            Font font, Texture2D atlas, TextAsset metrics, Material material)
        {
            var rect = Rect(parent, node.name + " [" + node.sourceId + "]");
            ApplyRect(rect, node);
            rect.SetAsLastSibling();
            if (node.image != null && node.image.componentId != 0)
            {
                var source = node.image;
                var image = Component<Image>(rect.gameObject);
                image.sprite = sprites[source.sourceSprite.id];
                image.material = null;
                image.color = source.color;
                image.type = (Image.Type)source.imageType;
                image.preserveAspect = source.preserveAspect;
                image.raycastTarget = source.raycast;
                image.maskable = source.maskable;
                image.fillCenter = source.fillCenter;
                image.fillMethod = (Image.FillMethod)source.fillMethod;
                image.fillAmount = source.fillAmount;
                image.fillOrigin = source.fillOrigin;
                image.fillClockwise = source.fillClockwise;
                image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
                image.enabled = source.enabled;
            }
            if (node.label != null && node.label.componentId != 0)
            {
                var source = node.label;
                var label = Component<OriginalSdfText>(rect.gameObject);
                label.font = font;
                label.material = material;
                label.sourceAtlas = atlas;
                label.sourceMetrics = metrics;
                label.nativePointSize = source.pointSize;
                label.fontSize = Mathf.RoundToInt(source.pointSize);
                label.fontStyle = (FontStyle)source.fontStyle;
                label.text = source.text;
                label.color = source.color;
                label.raycastTarget = source.raycast;
                label.supportRichText = source.richText;
                label.horizontalOverflow = source.wordWrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                label.alignment = (TextAnchor)((source.vertical == 256 ? 0 : 3) + (source.horizontal == 1 ? 0 : 1));
                label.resizeTextForBestFit = source.autoSize;
                label.resizeTextMinSize = Mathf.RoundToInt(source.minSize);
                label.resizeTextMaxSize = Mathf.RoundToInt(source.maxSize);
                label.enabled = source.enabled;
                label.SetAllDirty();
            }
            rect.gameObject.SetActive(node.active);
            return rect;
        }

        private static void ApplyRect(RectTransform rect, Node node)
        {
            rect.anchorMin = node.anchorMin;
            rect.anchorMax = node.anchorMax;
            rect.pivot = node.pivot;
            rect.sizeDelta = node.sizeDelta;
            rect.anchoredPosition3D = node.anchoredPosition;
            rect.localRotation = node.rotation;
            rect.localScale = node.scale;
        }

        private static AnimationCurve Curve(Track track) => new AnimationCurve(track.keys.Select(key =>
            new Keyframe(key.time, key.value, key.inConstant ? float.PositiveInfinity : key.inSlope,
                key.outConstant ? float.PositiveInfinity : key.outSlope, key.inWeight, key.outWeight)
            { weightedMode = (WeightedMode)key.weightedMode }).ToArray());

        private static BeachNativeCombatFeedback.ScaleTrack Scale(ScaleData data)
            => new BeachNativeCombatFeedback.ScaleTrack { x = Curve(data.x), y = Curve(data.y), z = Curve(data.z) };

        private static RectTransform Rect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing) return (RectTransform)existing;
            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, Operation);
            child.transform.SetParent(parent, false);
            return (RectTransform)child.transform;
        }

        private static T Component<T>(GameObject owner) where T : Component
        {
            var current = owner.GetComponent<T>();
            return current ? current : Undo.AddComponent<T>(owner);
        }

        private static T Asset<T>(string path) where T : UnityEngine.Object
            => Need(AssetDatabase.LoadAssetAtPath<T>(path), path);

        private static T Need<T>(T obj, string name) where T : UnityEngine.Object
            => obj ? obj : throw new InvalidOperationException("Missing combat binding " + name);
    }
}

