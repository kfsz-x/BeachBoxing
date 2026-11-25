using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    /// <summary>Explicit saved-scene migration. No InitializeOnLoad or runtime creation.</summary>
    public static class BeachFontParityRestore
    {
        private const string Root = "Assets/BeachBoxing/OriginalFont/";
        [Serializable] private sealed class Profiles { public LabelProfile[] profiles; }
        [Serializable] private sealed class LabelProfile
        {
            public string targetSuffix, matchText, hudField, sourceAsset, sourcePath;
            public int sourceId, fontId;
            public float sourcePointSize, pointSizeMultiplier, lineSpacingMultiplier;
            public bool applyPointSize, disableBestFit;
            public Color sourceColor;
            public NativeShadow shadow;
        }
        [Serializable] private sealed class NativeShadow
        {
            public int sourceId, fontId;
            public Color color;
            public Vector2 offset;
        }
        private sealed class Binding
        {
            public Component owner;
            public string path;
            public int previous;
        }

        private sealed class LabelState
        {
            public GameObject owner;
            public int previous;
            public Font font;
            public string text;
            public int size, minimum, maximum;
            public float spacing;
            public Color color;
            public bool enabled, raycast, maskable, rich, bestFit, geometry, outlined;
            public TextAnchor alignment;
            public HorizontalWrapMode horizontal;
            public VerticalWrapMode vertical;
            public MaskableGraphic.CullStateChangedEvent cullEvent;

            public LabelState(Text label)
            {
                owner = label.gameObject; previous = label.GetInstanceID(); font = label.font;
                text = label.text; size = label.fontSize;
                minimum = label.resizeTextMinSize; maximum = label.resizeTextMaxSize;
                spacing = label.lineSpacing; color = label.color;
                enabled = label.enabled; raycast = label.raycastTarget; maskable = label.maskable;
                rich = label.supportRichText; bestFit = label.resizeTextForBestFit; geometry = label.alignByGeometry;
                alignment = label.alignment; horizontal = label.horizontalOverflow; vertical = label.verticalOverflow;
                cullEvent = label.onCullStateChanged;
                outlined = label.material && label.material.HasProperty("_OutlineWidth") && label.material.GetFloat("_OutlineWidth") > 0;
            }
        }

        [MenuItem("Beach Boxing/Restore native fractional SDF fonts")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before migrating fonts.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated BeachBoxing scene first.");
            var nativeMetrics = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "LilitaOneNativeMetrics.json");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "LilitaOneOriginalSDF.png");
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "LilitaOneOriginal.fontsettings");
            var shader = Shader.Find("BeachBoxing/Native Lilita SDF");
            if (!nativeMetrics || !atlas || !font || !shader) throw new InvalidOperationException("Import native font metrics, atlas and shader first.");
            var plain = RestoreMaterial("LilitaOneNativeRegular.mat", shader, atlas, 0);
            var outline = RestoreMaterial("LilitaOneNativeOutline.mat", shader, atlas, 0.453f);
            var profilesAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "LilitaOneNativeUiProfiles.json");
            if (!profilesAsset) throw new InvalidOperationException("Import the native UI font profile evidence first.");
            var profiles = JsonUtility.FromJson<Profiles>(profilesAsset.text).profiles;
            var scene = flow.gameObject.scene;
            var labels = new List<Text>();
            var components = new List<Component>();
            foreach (var root in scene.GetRootGameObjects())
            {
                components.AddRange(root.GetComponentsInChildren<Component>(true));
                foreach (var label in root.GetComponentsInChildren<Text>(true))
                    if (label.font == font) labels.Add(label);
            }
            var oldIds = new HashSet<int>();
            foreach (var label in labels) if (!(label is OriginalSdfText)) oldIds.Add(label.GetInstanceID());
            // Record every reference BEFORE destroying Text. This includes nested HUD arrays,
            // Selectable.targetGraphic and UnityEvent object arguments throughout the scene.
            var bindings = new List<Binding>();
            foreach (var component in components)
            {
                if (!component || oldIds.Contains(component.GetInstanceID())) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    int reference = property.objectReferenceInstanceIDValue;
                    if (oldIds.Contains(reference)) bindings.Add(new Binding { owner = component, path = property.propertyPath, previous = reference });
                }
            }
            var replacements = new Dictionary<int, OriginalSdfText>();
            int shadowCount = 0;
            foreach (var label in labels)
            {
                bool outlined = label.material && label.material.HasProperty("_OutlineWidth") && label.material.GetFloat("_OutlineWidth") > 0;
                var profile = FindProfile(label, flow, profiles);
                if (profile != null) outlined = profile.fontId == 3372;
                var replacement = label as OriginalSdfText;
                if (!replacement)
                {
                    var state = new LabelState(label);
                    Undo.DestroyObjectImmediate(label);
                    replacement = Undo.AddComponent<OriginalSdfText>(state.owner);
                    replacement.font = state.font; replacement.text = state.text;
                    replacement.fontSize = state.size; replacement.lineSpacing = state.spacing;
                    replacement.color = state.color; replacement.enabled = state.enabled;
                    replacement.raycastTarget = state.raycast; replacement.maskable = state.maskable;
                    replacement.supportRichText = state.rich; replacement.resizeTextForBestFit = state.bestFit;
                    replacement.resizeTextMinSize = state.minimum; replacement.resizeTextMaxSize = state.maximum;
                    replacement.alignByGeometry = state.geometry; replacement.alignment = state.alignment;
                    replacement.horizontalOverflow = state.horizontal; replacement.verticalOverflow = state.vertical;
                    replacement.onCullStateChanged = state.cullEvent;
                    replacements.Add(state.previous, replacement);
                }
                Undo.RecordObject(replacement, "Restore native SDF font");
                replacement.sourceMetrics = nativeMetrics; replacement.sourceAtlas = atlas;
                replacement.fontStyle = FontStyle.Normal; // All351 shipped TMP labels serialize FontStyles.Normal.
                replacement.material = outlined ? outline : plain;
                // Only explicitly verified profiles apply native sizes. Other calibrated
                // labels keep their existing sizes, including the matched world title.
                if (profile != null && profile.applyPointSize)
                {
                    float multiplier = profile.pointSizeMultiplier > 0 ? profile.pointSizeMultiplier : 1;
                    replacement.nativePointSize = profile.sourcePointSize * multiplier;
                    if (profile.disableBestFit) replacement.resizeTextForBestFit = false;
                    if (profile.lineSpacingMultiplier > 0) replacement.lineSpacing = profile.lineSpacingMultiplier;
                }
                if (profile != null && profile.shadow != null && profile.shadow.sourceId != 0)
                {
                    replacement.color = profile.sourceColor;
                    AddNativeShadow(replacement, profile.shadow, plain, outline);
                    shadowCount++;
                }
                // Original font materials have no underlay keyword. Legacy UI shadow components
                // were generated by our earlier HUD builder, not present in the native labels.
                foreach (var shadow in replacement.GetComponents<Shadow>()) Undo.DestroyObjectImmediate(shadow);
                replacement.SetAllDirty();
                EditorUtility.SetDirty(replacement);
            }
            foreach (var binding in bindings)
            {
                if (!binding.owner || !replacements.TryGetValue(binding.previous, out var replacement)) continue;
                Undo.RecordObject(binding.owner, "Rebind original font label");
                var serialized = new SerializedObject(binding.owner);
                var property = serialized.FindProperty(binding.path);
                if (property == null) throw new InvalidOperationException("Missing serialized font binding: " + binding.path);
                property.objectReferenceValue = replacement;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(binding.owner);
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    Undo.RecordObject(canvas, "Enable original SDF vertex channels");
                    canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal;
                    EditorUtility.SetDirty(canvas);
                }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Native fractional SDF: {replacements.Count} Text components replaced, {bindings.Count} scene references rebound, {labels.Count} native labels configured, {shadowCount} source duplicate captions bound. Review and save the scene.");
        }

        private static void AddNativeShadow(OriginalSdfText white, NativeShadow source, Material plain, Material outline)
        {
            var parent = white.transform.parent;
            string name = "Original native font shadow - " + white.name;
            var found = parent.Find(name);
            OriginalSdfText shadow;
            if (found)
            {
                shadow = found.GetComponent<OriginalSdfText>();
                if (!shadow) throw new InvalidOperationException("Expected original SDF shadow at " + name);
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(OriginalSdfText));
                Undo.RegisterCreatedObjectUndo(go, "Restore original duplicate caption");
                Undo.SetTransformParent(go.transform, parent, "Parent original duplicate caption");
                shadow = go.GetComponent<OriginalSdfText>();
            }
            Undo.RecordObject(shadow, "Configure original duplicate caption");
            Undo.RecordObject(shadow.rectTransform, "Place original duplicate caption");
            Undo.RecordObject(parent, "Order original duplicate caption");
            var rect = shadow.rectTransform;
            var primary = white.rectTransform;
            rect.anchorMin = primary.anchorMin; rect.anchorMax = primary.anchorMax;
            rect.pivot = primary.pivot; rect.sizeDelta = primary.sizeDelta;
            rect.localRotation = primary.localRotation; rect.localScale = primary.localScale;
            rect.anchoredPosition3D = primary.anchoredPosition3D + new Vector3(source.offset.x, source.offset.y, 0);
            int whiteIndex = primary.GetSiblingIndex();
            rect.SetSiblingIndex(rect.GetSiblingIndex() < whiteIndex ? whiteIndex - 1 : whiteIndex);
            shadow.font = white.font; shadow.text = white.text; shadow.fontSize = white.fontSize;
            shadow.nativePointSize = white.nativePointSize;
            shadow.sourceMetrics = white.sourceMetrics; shadow.sourceAtlas = white.sourceAtlas;
            shadow.material = source.fontId == 3372 ? outline : plain;
            shadow.fontStyle = FontStyle.Normal; shadow.color = source.color;
            shadow.alignment = white.alignment; shadow.alignByGeometry = white.alignByGeometry;
            shadow.lineSpacing = white.lineSpacing; shadow.resizeTextForBestFit = false;
            shadow.horizontalOverflow = white.horizontalOverflow; shadow.verticalOverflow = white.verticalOverflow;
            shadow.supportRichText = white.supportRichText; shadow.maskable = white.maskable;
            shadow.raycastTarget = false; shadow.enabled = white.enabled;
            shadow.gameObject.SetActive(white.gameObject.activeSelf);
            shadow.SetAllDirty();
            EditorUtility.SetDirty(shadow);
        }

        private static LabelProfile FindProfile(Text label, BeachGameFlow flow, LabelProfile[] profiles)
        {
            var path = label.name;
            for (var parent = label.transform.parent; parent; parent = parent.parent) path = parent.name + "/" + path;
            foreach (var profile in profiles)
            {
                if (!string.IsNullOrEmpty(profile.hudField))
                {
                    var property = new SerializedObject(flow).FindProperty("hud").FindPropertyRelative(profile.hudField);
                    if (property != null && property.objectReferenceValue == label) return profile;
                }
                else if (path.EndsWith(profile.targetSuffix, StringComparison.Ordinal) &&
                         (string.IsNullOrEmpty(profile.matchText) || label.text == profile.matchText)) return profile;
            }
            return null;
        }

        private static Material RestoreMaterial(string filename, Shader shader, Texture2D atlas, float outline)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + filename);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Root + filename);
            }
            material.shader = shader; material.mainTexture = atlas;
            material.SetColor("_FaceColor", Color.white); material.SetColor("_OutlineColor", Color.black);
            material.SetFloat("_OutlineWidth", outline); material.SetFloat("_OutlineSoftness", 0);
            material.SetFloat("_FaceDilate", 0); material.SetFloat("_WeightNormal", 0); material.SetFloat("_WeightBold", 0.75f);
            material.SetFloat("_GradientScale", 6); material.SetFloat("_ScaleRatioA", 0.8333333f);
            material.SetFloat("_PerspectiveFilter", 0.875f); material.SetFloat("_Sharpness", 0);
            material.SetFloat("_ScaleX", 1); material.SetFloat("_ScaleY", 1);
            material.shaderKeywords = Array.Empty<string>();
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    [CustomEditor(typeof(OriginalSdfText)), CanEditMultipleObjects]
    public sealed class OriginalSdfTextEditor : UnityEditor.UI.TextEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceMetrics"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceAtlas"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nativePointSize"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
