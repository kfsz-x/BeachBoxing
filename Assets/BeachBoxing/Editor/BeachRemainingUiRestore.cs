using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BeachBoxing.Editor
{
    public static class BeachRemainingUiRestore
    {
        [MenuItem("Beach Boxing/Append remaining original UI")]
        public static void Append() => BeachPeripheralRestore.AppendRemaining();
        public static AnimationClip Clip(int sourceId) => BeachPeripheralRestore.RemainingClip(sourceId);
        public static NativeUiMotionBinding[] MotionBindings() => BeachPeripheralRestore.RemainingBindings();
    }

    [Serializable]
    public sealed class NativeUiMotionBinding
    {
        public int sourceAnimatorId, targetId, controllerId, defaultClipId, updateMode;
        public string sourcePath, controllerName;
        public bool loop, enabled;
        public NativeUiMotionState[] states;
    }
    [Serializable] public sealed class NativeUiMotionState
    {
        public string name;
        public int clipId;
        public bool isDefault, loop;
        public float speed;
    }

    public static partial class BeachPeripheralRestore
    {
        private const string RemainingPath = ArtRoot + "NativeRemainingUi.json";
        private const string MotionPath = ArtRoot + "NativeUiMotion.json";
        private const string AnimationRoot = ArtRoot + "NativeAnimations/";

        [Serializable] private sealed class SliderData : SelectableData
        {
            public int fillId, handleId, direction;
            public float minValue, maxValue, value;
            public bool wholeNumbers;
        }
        [Serializable] private sealed class InputData : SelectableData
        {
            public int viewportId, textId, placeholderId, verticalBarId, contentType, inputType, asteriskChar,
                keyboardType, lineType, characterValidation, characterLimit, caretWidth, lineLimit, fontId;
            public float pointSize, caretBlinkRate, scrollSensitivity;
            public bool hideMobileInput, hideSoftKeyboard, customCaretColor, readOnly, richText,
                selectAllOnFocus, resetOnDeactivation, restoreOriginalTextOnEscape, richTextEditingAllowed;
            public string regexValue, text;
            public Color caretColor, selectionColor;
        }
        [Serializable] private sealed class RectMaskData : ComponentData
        { public Vector4 padding; public Vector2Int softness; }
        [Serializable] private sealed class LayoutData : ComponentData { public LayoutValues sourceData; }
        [Serializable] private sealed class LayoutValues
        {
            public bool m_IgnoreLayout;
            public float m_MinWidth, m_MinHeight, m_PreferredWidth, m_PreferredHeight, m_FlexibleWidth, m_FlexibleHeight;
            public int m_LayoutPriority;
        }
        [Serializable] private sealed class NativeNameAudit { public NativeNameFilter name_filter; }
        [Serializable] private sealed class NativeNameFilter { public string[] profanity_words; }
        [Serializable] private sealed class MotionSource
        { public MotionClipData[] clips; public NativeUiMotionBinding[] bindings; }
        [Serializable] private sealed class MotionClipData
        {
            public int sourceId, wrapMode;
            public string name;
            public float frameRate, duration;
            public bool loop;
            public MotionCurveData[] curves;
            public MotionEventData[] events;
        }
        [Serializable] private sealed class MotionCurveData
        {
            public string path, property, targetKind, sourceProperty;
            public bool discrete;
            public MotionKeyData[] keys;
        }
        [Serializable] private sealed class MotionKeyData
        {
            public float time, value, inSlope, outSlope, inWeight, outWeight;
            public bool inConstant, outConstant;
            public int weightedMode;
        }
        [Serializable] private sealed class MotionEventData
        {
            public float time, floatParameter;
            public int intParameter, messageOptions;
            public string functionName, stringParameter;
        }

        public static void AppendRemaining()
        {
            var scene = SceneManager.GetActiveScene();
            var flow = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BeachGameFlow>(true)).SingleOrDefault();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !flow)
                throw new InvalidOperationException("Open the Beach Boxing gameplay scene in Edit Mode.");
            var view = flow.GetComponent<BeachPeripheralView>();
            var canvas = flow.transform.Find(CanvasName) as RectTransform;
            if (!view || !canvas) throw new InvalidOperationException("Restore the initial peripheral UI before appending.");
            var source = JsonUtility.FromJson<NativeUi>(Required<TextAsset>(RemainingPath).text);
            PrepareSprites(source.sprites);
            RestoreRemainingClips();
            var font = Required<Font>(FontRoot + "LilitaOneOriginal.fontsettings");
            var atlas = Required<Texture2D>(FontRoot + "LilitaOneOriginalSDF.png");
            var metrics = Required<TextAsset>(FontRoot + "LilitaOneNativeMetrics.json");
            var plain = Required<Material>(FontRoot + "LilitaOneNativeRegular.mat");
            var outline = Required<Material>(FontRoot + "LilitaOneNativeOutline.mat");
            var mixer = Required<AudioMixer>("Assets/BeachBoxing/OriginalArt/AudioMixerController/Main.mixer");
            var sfxGroup = mixer.FindMatchingGroups("SFX").Single(group => group.name == "SFX");
            var nodes = view.nodes.Where(n => n != null && n.transform).ToDictionary(n => n.sourceId);
            var rows = source.nodes.Where(row => !IsLandscapeControlNode(row.sourceId) || nodes.ContainsKey(row.sourceId)).ToArray();
            var added = new HashSet<int>();
            var sibling = new Dictionary<int, int>();

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Append remaining original UI");
            try
            {
                foreach (var row in rows)
                {
                    sibling.TryGetValue(row.parentId, out int index);
                    sibling[row.parentId] = index + 1;
                    if (nodes.ContainsKey(row.sourceId)) continue;
                    var parent = row.parentId == source.canvasId ? canvas : nodes[row.parentId].transform;
                    var node = RestoreNode(row, parent, index, null);
                    nodes.Add(row.sourceId, node);
                    added.Add(row.sourceId);
                    if (node.label) RestoreLabel((OriginalSdfText)node.label, row.label, font, atlas, metrics,
                        row.label.fontId == 3372 ? outline : plain);
                    if (row.audio?.componentId > 0) RestoreAudio(node, row.audio, sfxGroup);
                }
                // All nodes now exist before cross-component references are restored.
                foreach (var row in rows)
                {
                    var node = nodes[row.sourceId];
                    if (added.Contains(row.sourceId)) RestoreWiring(node, row, nodes);
                    AppendControls(node, row, nodes);
                }
                Undo.RecordObject(view, "Append original UI references");
                // Preserve existing node instances and any scene-only additions outside this source file.
                var ordered = rows.Select(row => nodes[row.sourceId]).ToList();
                ordered.AddRange(view.nodes.Where(n => n != null && n.transform && !ordered.Contains(n)));
                view.nodes = ordered.ToArray();
                var portraitTargets = new HashSet<int>(view.portraits.Where(p => p != null && p.target)
                    .Select(p => nodes.Values.FirstOrDefault(n => n.transform == p.target)?.sourceId ?? 0));
                var portraitList = view.portraits.ToList();
                foreach (var p in source.portraits)
                    if (portraitTargets.Add(p.targetId)) portraitList.Add(RestorePortrait(canvas, p, nodes));
                view.portraits = portraitList.ToArray();
                EditorUtility.SetDirty(view);
                BindSettings(flow, view, nodes, mixer);
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("Appended " + added.Count + " native UI nodes; preserved existing transforms and components.");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static void AppendControls(NativeUiNode node, NodeData row, Dictionary<int, NativeUiNode> nodes)
        {
            if (row.slider?.componentId > 0 && !node.transform.GetComponent<Slider>())
            {
                var d = row.slider;
                var control = Component<Slider>(node.transform.gameObject);
                RestoreSelectable(control, d, nodes);
                control.fillRect = nodes[d.fillId].transform;
                control.handleRect = nodes[d.handleId].transform;
                control.direction = (Slider.Direction)d.direction;
                control.minValue = d.minValue;
                control.maxValue = d.maxValue;
                control.wholeNumbers = d.wholeNumbers;
                control.SetValueWithoutNotify(d.value);
            }
            if (row.input?.componentId > 0 && !node.transform.GetComponent<InputField>())
            {
                var d = row.input;
                var control = Component<InputField>(node.transform.gameObject);
                RestoreSelectable(control, d, nodes);
                control.textComponent = nodes[d.textId].label;
                control.placeholder = nodes[d.placeholderId].label;
                control.contentType = (InputField.ContentType)d.contentType;
                control.inputType = (InputField.InputType)d.inputType;
                control.asteriskChar = (char)d.asteriskChar;
                control.keyboardType = (TouchScreenKeyboardType)d.keyboardType;
                control.lineType = (InputField.LineType)d.lineType;
                control.characterValidation = (InputField.CharacterValidation)d.characterValidation;
                control.characterLimit = d.characterLimit;
                control.shouldHideMobileInput = d.hideMobileInput;
                control.customCaretColor = d.customCaretColor;
                control.caretColor = d.caretColor;
                control.selectionColor = d.selectionColor;
                control.caretBlinkRate = d.caretBlinkRate;
                control.caretWidth = d.caretWidth;
                control.readOnly = d.readOnly;
                control.textComponent.supportRichText = d.richText;
                control.SetTextWithoutNotify(d.text ?? string.Empty);
            }
            if (row.rectMask?.componentId > 0 && !node.transform.GetComponent<RectMask2D>())
            {
                var mask = Component<RectMask2D>(node.transform.gameObject);
                mask.padding = row.rectMask.padding;
                mask.softness = row.rectMask.softness;
                mask.enabled = row.rectMask.enabled;
            }
            if (row.layoutElement?.componentId > 0 && !node.transform.GetComponent<LayoutElement>())
            {
                var control = Component<LayoutElement>(node.transform.gameObject);
                var d = row.layoutElement.sourceData;
                control.ignoreLayout = d.m_IgnoreLayout;
                control.minWidth = d.m_MinWidth; control.minHeight = d.m_MinHeight;
                control.preferredWidth = d.m_PreferredWidth; control.preferredHeight = d.m_PreferredHeight;
                control.flexibleWidth = d.m_FlexibleWidth; control.flexibleHeight = d.m_FlexibleHeight;
                control.layoutPriority = d.m_LayoutPriority;
                control.enabled = row.layoutElement.enabled;
            }
        }

        private static void BindSettings(BeachGameFlow flow, BeachPeripheralView peripheral,
            Dictionary<int, NativeUiNode> nodes, AudioMixer mixer)
        {
            bool firstBinding = !flow.GetComponent<BeachNativeSettingsView>();
            var settings = Component<BeachNativeSettingsView>(flow.gameObject);
            Undo.RecordObject(settings, "Bind original settings controls");
            settings.peripheral = peripheral;
            settings.sfxSlider = nodes[705].transform.GetComponent<Slider>();
            settings.musicSlider = nodes[166].transform.GetComponent<Slider>();
            settings.sfxText = nodes[733].label;
            settings.musicText = nodes[593].label;
            settings.nameInput = nodes[458].transform.GetComponent<InputField>();
            var keyboard = Component<BeachNativeNameKeyboard>(nodes[458].transform.gameObject);
            keyboard.owner = settings;
            EditorUtility.SetDirty(keyboard);
            settings.mixer = mixer;
            settings.musicParameter = MixerParameter(mixer, "Music");
            settings.sfxParameter = MixerParameter(mixer, "SFX");
            var audit = JsonUtility.FromJson<NativeNameAudit>(File.ReadAllText(
                "docs/beach-boxing/evidence/native-settings-2026-11-20.json"));
            settings.profanityWords = audit.name_filter.profanity_words;
            settings.menuOpen = RemainingClip(329);
            settings.menuClose = RemainingClip(327);
            settings.profanityClip = RemainingClip(344);
            if (!settings.musicGroup) settings.trainingMusicPosition = nodes[653].transform.anchoredPosition3D;
            if (!settings.sfxGroup) settings.trainingSfxPosition = nodes[171].transform.anchoredPosition3D;
            settings.musicGroup = nodes[653].transform;
            settings.sfxGroup = nodes[171].transform;
            settings.fightMusicPosition = new Vector3(0, -159.49998474121094f, 0);
            settings.fightSfxPosition = new Vector3(0, -33.067745208740234f, 0);
            if (firstBinding)
            {
                settings.SetFightContext(false);
                nodes[80].transform.gameObject.SetActive(false);
                nodes[933].transform.gameObject.SetActive(false);
            }
            EditorUtility.SetDirty(settings);
        }

        private static string MixerParameter(AudioMixer mixer, string groupName)
        {
            // AssetRipper names exposed parameters from their hashes; match the actual group volume GUID.
            string text = File.ReadAllText(AssetDatabase.GetAssetPath(mixer));
            var blocks = text.Split(new[] { "--- !u!" }, StringSplitOptions.None);
            string group = blocks.Single(b => b.StartsWith("243 ") &&
                System.Text.RegularExpressions.Regex.IsMatch(b, @"(?m)^  m_Name: " + groupName + @"\r?$"));
            var volume = System.Text.RegularExpressions.Regex.Match(group, @"m_Volume: (\w+)").Groups[1].Value;
            var pattern = @"guid: " + volume + @"\s+name: ([^\r\n]+)";
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern);
            if (!match.Success) throw new InvalidOperationException("No exposed native mixer parameter for " + groupName);
            return match.Groups[1].Value.Trim();
        }

        public static NativeUiMotionBinding[] RemainingBindings() =>
            JsonUtility.FromJson<MotionSource>(Required<TextAsset>(MotionPath).text).bindings;

        public static AnimationClip RemainingClip(int sourceId)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "Native-" + sourceId + ".anim");
            if (!clip) throw new InvalidOperationException("Append/import native UI clips before requesting clip " + sourceId);
            return clip;
        }

        private static void RestoreRemainingClips()
        {
            if (!AssetDatabase.IsValidFolder(AnimationRoot.TrimEnd('/')))
                AssetDatabase.CreateFolder(ArtRoot.TrimEnd('/'), "NativeAnimations");
            var source = JsonUtility.FromJson<MotionSource>(Required<TextAsset>(MotionPath).text);
            foreach (var row in source.clips)
            {
                string path = AnimationRoot + "Native-" + row.sourceId + ".anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path)) continue;
                var clip = new AnimationClip { name = row.name, frameRate = row.frameRate, wrapMode = (WrapMode)row.wrapMode };
                foreach (var track in row.curves)
                {
                    var keys = track.keys.Select(k => new Keyframe(k.time, k.value,
                        k.inConstant ? float.PositiveInfinity : k.inSlope,
                        k.outConstant ? float.PositiveInfinity : k.outSlope,
                        k.inWeight, k.outWeight) { weightedMode = (WeightedMode)k.weightedMode }).ToArray();
                    var type = track.targetKind == "Graphic"
                        ? (track.sourceProperty != null && track.sourceProperty.StartsWith("m_fontColor")
                            ? typeof(OriginalSdfText) : typeof(Image))
                        : MotionType(track.targetKind);
                    var binding = track.discrete ? EditorCurveBinding.DiscreteCurve(track.path, type, track.property)
                        : EditorCurveBinding.FloatCurve(track.path, type, track.property);
                    AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(keys));
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = row.loop;
                settings.startTime = 0;
                settings.stopTime = row.duration;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AnimationUtility.SetAnimationEvents(clip, row.events.Select(e => new AnimationEvent
                {
                    time = e.time, functionName = e.functionName, stringParameter = e.stringParameter,
                    floatParameter = e.floatParameter, intParameter = e.intParameter,
                    messageOptions = (SendMessageOptions)e.messageOptions
                }).ToArray());
                AssetDatabase.CreateAsset(clip, path);
            }
        }

        private static Type MotionType(string kind)
        {
            switch (kind)
            {
                case "GameObject": return typeof(GameObject);
                case "Transform": return typeof(Transform);
                case "RectTransform": return typeof(RectTransform);
                case "Graphic": return typeof(Graphic);
                case "OriginalSdfText": return typeof(OriginalSdfText);
                case "Image": return typeof(Image);
                case "CanvasGroup": return typeof(CanvasGroup);
                default: throw new InvalidOperationException("Unknown original UI animation target " + kind);
            }
        }
    }
}
