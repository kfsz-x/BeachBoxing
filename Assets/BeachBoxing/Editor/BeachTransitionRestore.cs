using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    /// <summary>Explicit migration only: preplaces source artwork and binds source animation curves.</summary>
    public static class BeachTransitionRestore
    {
        private const string OriginalRoot = "Assets/BeachBoxing/OriginalArt/";
        private const string SourceRoot = "C:/Users/Administrator/AppData/Local/Temp/BeachBoxing-reference/ripped/ExportedProject/Assets";
        [Serializable] private sealed class Evidence { public Clip opening, result, fadeIn, fadeOut; public IntroCamera introCamera; }
        [Serializable] private sealed class IntroCamera { public Vector3 pivot; public BeachPresentationCubicCurve[] curves; }
        [Serializable] private sealed class Clip { public Scale[] scales; public FloatTrack[] floats; }
        [Serializable] private sealed class Scale { public string path; public Key[] keys; }
        [Serializable] private sealed class Key
        {
            public float time, x, y, z;
            public string inX, inY, inZ, outX, outY, outZ;
        }
        [Serializable] private sealed class FloatTrack { public FloatKey[] keys; }
        [Serializable] private sealed class FloatKey { public float time, value; public string inTangent, outTangent; }
        private static Dictionary<string, string> sourceAssets;

        [MenuItem("Beach Boxing/Restore original countdown and transitions")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before restoring saved transition objects.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated BeachBoxing scene first.");
            string evidencePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/beach-boxing/reference/presentation-timeline.json"));
            var evidence = JsonUtility.FromJson<Evidence>(File.ReadAllText(evidencePath));
            if (evidence == null) throw new InvalidOperationException("The original presentation timeline evidence is missing.");
            ValidateScaleTracks(evidence.opening, false);
            ValidateScaleTracks(evidence.result, true);
            var fadeIn = FloatCurve(evidence.fadeIn);
            var fadeOut = FloatCurve(evidence.fadeOut);
            var data = new SerializedObject(flow);
            var hud = data.FindProperty("hud");
            var safe = Ref<RectTransform>(hud, "safe");
            var font = Ref<Font>(hud, "font");
            if (!safe || !font) throw new InvalidOperationException("Restore the existing original HUD/font first.");
            var canvas = safe.GetComponentInParent<Canvas>();
            var result = Ref<GameObject>(hud, "result");
            var victoryTitle = Ref<GameObject>(hud, "victoryTitle");
            var victoryCard = Ref<GameObject>(hud, "victoryCard");
            var homeButton = Ref<Button>(hud, "homeButton");
            if (!canvas || !result || !victoryTitle || !victoryCard || !homeButton)
                throw new InvalidOperationException("Bind the original HUD canvas and victory targets before restoring transitions.");
            var stage = data.FindProperty("stage");
            var presentation = data.FindProperty("presentation");
            // Serialized edits stay local until ApplyModifiedPropertiesWithoutUndo below.
            WriteIntroCamera(stage, presentation.FindPropertyRelative("introCamera"), evidence.introCamera, flow.transform);
            var glow = OriginalSprite("FadeCircle");
            var circle = OriginalSprite("AllAxis_Ridged");
            const string defeatAudioPath = OriginalRoot + "Resources/BeachOriginal/Audio/Defeat.mp3";
            AssetDatabase.ImportAsset(defeatAudioPath);
            var defeatAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(defeatAudioPath);
            if (!defeatAudio) throw new InvalidOperationException("Import the recovered original Debuff Downgrade 37 as Defeat.mp3 first.");
            Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, "Restore original presentation timeline");
            var sounds = stage.FindPropertyRelative("soundClips");
            if (sounds.arraySize < 7) sounds.arraySize = 7;
            sounds.GetArrayElementAtIndex(6).objectReferenceValue = defeatAudio;
            var previousRoot = flow.transform.Find("Original fight countdown");
            if (!previousRoot) previousRoot = safe.Find("Original fight countdown");
            var root = previousRoot ? (RectTransform)previousRoot : Rect(flow.transform, "Original fight countdown");
            root.SetParent(flow.transform, false);
            root.SetAsFirstSibling();
            Stretch(root);
            var openingCanvas = root.GetComponent<Canvas>();
            if (!openingCanvas) openingCanvas = root.gameObject.AddComponent<Canvas>();
            openingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            openingCanvas.sortingOrder = canvas.sortingOrder;
            var openingScaler = root.GetComponent<CanvasScaler>();
            if (!openingScaler) openingScaler = root.gameObject.AddComponent<CanvasScaler>();
            openingScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            openingScaler.referenceResolution = new Vector2(800, 600);
            openingScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            openingScaler.matchWidthOrHeight = 0;
            presentation.FindPropertyRelative("openingRoot").objectReferenceValue = root.gameObject;
            var legacy = Ref<Text>(hud, "countdown");
            presentation.FindPropertyRelative("legacyCountdown").objectReferenceValue = legacy;
            if (legacy) legacy.gameObject.SetActive(false);

            var opening = new List<Transform>();
            foreach (var track in evidence.opening.scales)
            {
                var item = Rect(root, track.path);
                Fixed(item, new Vector2(200, 147.10771f), Vector2.zero, Vector3.one);
                bool fight = track.path == "FIGHT";
                Color glowColor = fight ? new Color(.054901961f, 1, 0, 1) : track.path == "1" ?
                    new Color(.980392158f, 1, 0, 1) : track.path == "2" ? new Color(1, .643137276f, 0, 1) : new Color(1, .215686277f, 0, 1);
                Color circleColor = fight ? new Color(.053106308f, 1, 0, 1) : track.path == "1" ?
                    new Color(.980179906f, 1, 0, 1) : track.path == "2" ? new Color(1, .644330204f, 0, 1) : new Color(1, .214346379f, 0, 1);
                Picture(item, "Original glow", glow, glowColor,
                    new Vector3(3.622306f, 3.622306f, 3.622306f), new Vector2(0, -6.100052f));
                Picture(item, "Original ridged circle", circle, circleColor,
                    Vector3.one * 1.4710771f, Vector2.zero);
                if (fight) Picture(item, "Original black fight banner", glow, Color.black,
                    new Vector3(11.198575f, 2.1698387f, 1.4710771f), Vector2.zero);
                var labelRect = Rect(item, "Original Lilita text");
                Fixed(labelRect, new Vector2(fight ? 469 : 200, 50), Vector2.zero, Vector3.one);
                var text = labelRect.GetComponent<Text>();
                if (!text) text = labelRect.gameObject.AddComponent<Text>();
                text.font = font;
                var outline = AssetDatabase.LoadAssetAtPath<Material>("Assets/BeachBoxing/OriginalFont/LilitaOneOriginalOutline.mat");
                if (!outline) outline = font.material;
                text.material = outline;
                text.fontSize = fight ? 97 : 116;
                labelRect.localScale = Vector3.one * ((fight ? 96.6f : 115.78f) / text.fontSize);
                text.text = fight ? "FIGHT!" : track.path;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;
                opening.Add(item);
            }
            WriteTracks(presentation.FindPropertyRelative("openingTracks"), evidence.opening.scales, opening.ToArray(), false);
            root.gameObject.SetActive(false);

            var victory = new Transform[evidence.result.scales.Length];
            var defeat = new Transform[evidence.result.scales.Length];
            var defeatPanel = Ref<GameObject>(hud, "defeatPanel");
            for (int index = 0; index < evidence.result.scales.Length; index++)
            {
                switch (evidence.result.scales[index].path)
                {
                    case "": victory[index] = result.transform; defeat[index] = defeatPanel ? defeatPanel.transform : null; break;
                    case "MENU/Title": victory[index] = victoryTitle.transform; break;
                    case "MENU/MiddleBit": victory[index] = victoryCard.transform; break;
                    case "MENU/AcceptBTN": victory[index] = homeButton.transform; break;
                    // Original Defeat children are Titled / AcceptBTNd / BallsButton:
                    // they do not match this clip's paths and follow only the root animation.
                    // Original Victory has no MENU/HomeButton either.
                }
            }
            WriteTracks(presentation.FindPropertyRelative("victoryTracks"), evidence.result.scales, victory, true);
            WriteTracks(presentation.FindPropertyRelative("defeatTracks"), evidence.result.scales, defeat, true);

            var fadeRect = Rect(canvas.transform, "Original scene fade");
            Stretch(fadeRect);
            fadeRect.SetAsLastSibling();
            var fade = fadeRect.GetComponent<Image>();
            if (!fade) fade = fadeRect.gameObject.AddComponent<Image>();
            fade.color = Color.clear;
            fade.raycastTarget = false;
            fadeRect.gameObject.SetActive(false);
            presentation.FindPropertyRelative("fade").objectReferenceValue = fade;
            presentation.FindPropertyRelative("fadeIn").animationCurveValue = fadeIn;
            presentation.FindPropertyRelative("fadeOut").animationCurveValue = fadeOut;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Preplaced original countdown/glow/FIGHT and fade. Bound source XYZ curves to saved result objects; save scene after review.");
        }

        private static void WriteIntroCamera(SerializedProperty stage, SerializedProperty intro, IntroCamera source, Transform flow)
        {
            if (source == null || source.curves == null || source.curves.Length != 9 ||
                !IsFinite(source.pivot.x) || !IsFinite(source.pivot.y) || !IsFinite(source.pivot.z))
                throw new InvalidOperationException("The original EntranceCameraAnim streamed curve evidence is missing.");
            foreach (var curve in source.curves)
            {
                if (curve?.keys == null || curve.keys.Length == 0)
                    throw new InvalidOperationException("An original EntranceCameraAnim curve is missing its keys.");
                float previous = -1;
                foreach (var key in curve.keys)
                {
                    if (key == null || !IsFinite(key.time) || key.time < 0 || key.time <= previous ||
                        !IsFinite(key.a) || !IsFinite(key.b) || !IsFinite(key.c) || !IsFinite(key.d))
                        throw new InvalidOperationException("An original EntranceCameraAnim curve has an invalid key.");
                    previous = key.time;
                }
            }
            var target = BeachNativeCameraRestore.ResolveBasePose(stage);
            var world = Ref<Transform>(stage, "combatWorld");
            if (!world) world = flow.Find("Arena/Original Combat world");
            if (!target || !world) throw new InvalidOperationException("Bind the existing camera and original combat world before restoring the intro orbit.");
            intro.FindPropertyRelative("target").objectReferenceValue = target;
            intro.FindPropertyRelative("reference").objectReferenceValue = world;
            intro.FindPropertyRelative("pivot").vector3Value = source.pivot;
            var curves = intro.FindPropertyRelative("curves");
            curves.arraySize = 9;
            string[] fields = { "time", "a", "b", "c", "d" };
            for (int axis = 0; axis < curves.arraySize; axis++)
            {
                var keys = curves.GetArrayElementAtIndex(axis).FindPropertyRelative("keys");
                keys.arraySize = source.curves[axis].keys.Length;
                for (int index = 0; index < keys.arraySize; index++)
                {
                    var key = source.curves[axis].keys[index];
                    float[] values = { key.time, key.a, key.b, key.c, key.d };
                    for (int field = 0; field < fields.Length; field++)
                        keys.GetArrayElementAtIndex(index).FindPropertyRelative(fields[field]).floatValue = values[field];
                }
            }
        }

        private static void ValidateScaleTracks(Clip source, bool normalize)
        {
            if (source?.scales == null || source.scales.Length == 0)
                throw new InvalidOperationException("An original presentation scale clip is missing its tracks.");
            foreach (var track in source.scales)
            {
                if (track?.path == null || track.keys == null || track.keys.Length == 0)
                    throw new InvalidOperationException("An original presentation scale track is missing its path or keys.");
                foreach (var key in track.keys)
                {
                    if (key == null || !IsFinite(key.time) || !IsFinite(key.x) || !IsFinite(key.y) || !IsFinite(key.z))
                        throw new InvalidOperationException("An original presentation scale track has an invalid key.");
                    Slope(key.inX); Slope(key.inY); Slope(key.inZ);
                    Slope(key.outX); Slope(key.outY); Slope(key.outZ);
                }
                var last = track.keys[track.keys.Length - 1];
                if (normalize && (last.x == 0 || last.y == 0 || last.z == 0))
                    throw new InvalidOperationException("An original result scale track has a zero resting scale.");
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static void WriteTracks(SerializedProperty array, Scale[] source, Transform[] targets, bool normalize)
        {
            var canonicalRest = new Dictionary<Transform, Vector3>();
            if (normalize)
            {
                // A previous preview may have left a target at an intermediate animation scale.
                // Reuse the last binding's resting curve values rather than capturing that frame.
                for (int index = 0; index < array.arraySize; index++)
                {
                    var previous = array.GetArrayElementAtIndex(index);
                    var target = previous.FindPropertyRelative("target").objectReferenceValue as Transform;
                    if (!target) continue;
                    canonicalRest[target] = new Vector3(
                        LastValue(previous.FindPropertyRelative("x").animationCurveValue, target.localScale.x),
                        LastValue(previous.FindPropertyRelative("y").animationCurveValue, target.localScale.y),
                        LastValue(previous.FindPropertyRelative("z").animationCurveValue, target.localScale.z));
                    // Restore even a target whose obsolete binding is about to be removed.
                    target.localScale = NonZeroRest(canonicalRest[target]);
                }
            }
            array.arraySize = source.Length;
            for (int index = 0; index < source.Length; index++)
            {
                var item = array.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("target").objectReferenceValue = targets[index];
                Vector3 rest = targets[index] ? targets[index].localScale : Vector3.one;
                if (targets[index] && canonicalRest.TryGetValue(targets[index], out Vector3 previousRest)) rest = previousRest;
                if (normalize)
                {
                    rest = NonZeroRest(rest);
                    if (targets[index]) targets[index].localScale = rest;
                }
                var last = source[index].keys[source[index].keys.Length - 1];
                item.FindPropertyRelative("x").animationCurveValue = Curve(source[index], 0, normalize ? rest.x / last.x : 1);
                item.FindPropertyRelative("y").animationCurveValue = Curve(source[index], 1, normalize ? rest.y / last.y : 1);
                item.FindPropertyRelative("z").animationCurveValue = Curve(source[index], 2, normalize ? rest.z / last.z : 1);
            }
        }

        private static float LastValue(AnimationCurve curve, float fallback) =>
            curve != null && curve.length > 0 ? curve.keys[curve.length - 1].value : fallback;

        private static Vector3 NonZeroRest(Vector3 rest) => new Vector3(
            Mathf.Abs(rest.x) < .0001f ? 1 : rest.x,
            Mathf.Abs(rest.y) < .0001f ? 1 : rest.y,
            Mathf.Abs(rest.z) < .0001f ? 1 : rest.z);

        private static AnimationCurve Curve(Scale source, int axis, float multiplier)
        {
            var keys = new Keyframe[source.keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var key = source.keys[i];
                float value = axis == 0 ? key.x : axis == 1 ? key.y : key.z;
                string input = axis == 0 ? key.inX : axis == 1 ? key.inY : key.inZ;
                string output = axis == 0 ? key.outX : axis == 1 ? key.outY : key.outZ;
                keys[i] = new Keyframe(key.time, value * multiplier, Slope(input) * multiplier, Slope(output) * multiplier);
            }
            return new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        private static AnimationCurve FloatCurve(Clip source)
        {
            var tracks = source?.floats;
            if (tracks == null || tracks.Length != 1 || tracks[0]?.keys == null || tracks[0].keys.Length == 0)
                throw new InvalidOperationException("An original presentation fade curve is missing its keys.");
            var sourceKeys = tracks[0].keys;
            var keys = new Keyframe[sourceKeys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var key = sourceKeys[i];
                if (key == null || !IsFinite(key.time) || !IsFinite(key.value))
                    throw new InvalidOperationException("An original presentation fade curve has an invalid key.");
                keys[i] = new Keyframe(key.time, key.value, Slope(key.inTangent), Slope(key.outTangent));
            }
            return new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
        }

        private static float Slope(string value)
        {
            if (value == "inf" || value == "Infinity") return float.PositiveInfinity;
            if (value == "-inf" || value == "-Infinity") return float.NegativeInfinity;
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var existing = parent.Find(name);
            var rect = existing ? (RectTransform)existing : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
        private static void Fixed(RectTransform rect, Vector2 size, Vector2 position, Vector3 scale)
        { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = scale; }
        private static void Picture(Transform parent, string name, Sprite sprite, Color color, Vector3 scale, Vector2 position)
        {
            var rect = Rect(parent, name);
            Fixed(rect, Vector2.one * 100, position, scale);
            var image = rect.GetComponent<Image>();
            if (!image) image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.color = color; image.raycastTarget = false;
        }
        private static T Ref<T>(SerializedProperty parent, string name) where T : UnityEngine.Object =>
            parent.FindPropertyRelative(name).objectReferenceValue as T;

        private static Sprite OriginalSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OriginalRoot + "Resources/BeachOriginal/UI/" + name + ".asset");
            if (!sprite) sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OriginalRoot + "Sprite/" + name + ".asset");
            if (sprite) return sprite;
            if (sourceAssets == null)
            {
                sourceAssets = new Dictionary<string, string>();
                foreach (string meta in Directory.GetFiles(SourceRoot, "*.meta", SearchOption.AllDirectories))
                {
                    var match = Regex.Match(File.ReadAllText(meta), @"guid: ([a-f0-9]{32})");
                    if (match.Success) sourceAssets[match.Groups[1].Value] = meta.Substring(0, meta.Length - 5);
                }
            }
            CopyOriginal(Path.Combine(SourceRoot, "Sprite", name + ".asset"), new HashSet<string>());
            AssetDatabase.Refresh();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OriginalRoot + "Sprite/" + name + ".asset");
            if (!sprite) throw new InvalidOperationException("Original sprite could not be imported: " + name);
            return sprite;
        }
        private static void CopyOriginal(string source, HashSet<string> copied)
        {
            string meta = File.ReadAllText(source + ".meta");
            string guid = Regex.Match(meta, @"guid: ([a-f0-9]{32})").Groups[1].Value;
            if (!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) || !copied.Add(guid)) return;
            string relative = source.Substring(SourceRoot.Length).TrimStart('/', '\\').Replace('\\', '/');
            string target = OriginalRoot + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target)) throw new InvalidOperationException("Refusing to replace existing original asset: " + target);
            File.Copy(source, target);
            File.Copy(source + ".meta", target + ".meta");
            if (Path.GetExtension(source) != ".asset" && Path.GetExtension(source) != ".mat") return;
            foreach (Match dependency in Regex.Matches(File.ReadAllText(source), @"guid: ([a-f0-9]{32})"))
                if (sourceAssets.TryGetValue(dependency.Groups[1].Value, out string path)) CopyOriginal(path, copied);
        }
    }
}
