using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    /// <summary>Explicit, repeatable migration of the existing preplaced HUD. Never runs on load.</summary>
    public static class BeachOriginalHudRestore
    {
        private const string FontRoot = "Assets/BeachBoxing/OriginalFont/";
        private const string SpriteRoot = "Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/UI/";
        [Serializable] private sealed class Metrics { public int pointSize; public float ascent, lineHeight; public Glyph[] glyphs; }
        [Serializable] private sealed class Glyph { public int unicode, x, y, rectWidth, rectHeight; public float width, height, bearingX, bearingY, advance; }

        [MenuItem("Beach Boxing/Restore original HUD and Lilita font")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before restoring the saved HUD.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated BeachBoxing scene first.");
            var data = new SerializedObject(flow);
            var hud = data.FindProperty("hud");
            var safe = Ref<RectTransform>(hud, "safe");
            if (!safe) throw new InvalidOperationException("The scene must already contain its serialized HUD.");
            Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, "Restore original Beach Boxing HUD");
            var font = RestoreFont();
            var material = font.material;
            foreach (var text in safe.GetComponentInParent<Canvas>().GetComponentsInChildren<Text>(true))
            {
                text.font = font;
                text.material = material;
                text.fontStyle = FontStyle.Normal;
                text.resizeTextForBestFit = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                foreach (var shadow in text.GetComponents<Shadow>()) UnityEngine.Object.DestroyImmediate(shadow);
            }
            hud.FindPropertyRelative("font").objectReferenceValue = font;
            var scaler = safe.GetComponentInParent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 620);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.85f;

            var stats = Ref<GameObject>(hud, "stats");
            Fixed(stats.GetComponent<RectTransform>(), Vector2.up, new Vector2(5, -10), new Vector2(170, 200), Vector2.up);
            for (int index = 0; index < 6; index++)
            {
                var row = stats.transform.Find("Stat " + index);
                if (!row) continue;
                row.gameObject.SetActive(index < 4);
                if (index >= 4) continue;
                var rect = (RectTransform)row;
                Fixed(rect, Vector2.up, new Vector2(0, -index * 50), new Vector2(170, 48), Vector2.up);
                var bg = row.GetComponent<Image>() ?? row.gameObject.AddComponent<Image>();
                bg.sprite = Sprite("CountBG (4)");
                bg.color = new Color(0, 0, 0, 0.48f);
                bg.raycastTarget = false;
                var icon = row.Find("Icon").GetComponent<Image>();
                Fixed(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(32, 0), new Vector2(54, 54), new Vector2(0.5f, 0.5f));
                icon.preserveAspect = true;
                var value = hud.FindPropertyRelative("statsValues").GetArrayElementAtIndex(index).objectReferenceValue as Text;
                Fixed(value.rectTransform, new Vector2(0, 0.5f), new Vector2(107, 0), new Vector2(100, 46), new Vector2(0.5f, 0.5f));
                value.fontSize = 25;
                value.alignment = TextAnchor.MiddleCenter;
                value.material = OutlinedMaterial();
                if (string.IsNullOrEmpty(value.text)) value.text = "0";
            }
            var menu = Ref<GameObject>(hud, "menu");
            Fixed(menu.GetComponent<RectTransform>(), Vector2.one, new Vector2(-15, -14), new Vector2(75, 75), Vector2.one);
            menu.GetComponent<Image>().preserveAspect = true;
            foreach (var text in menu.GetComponentsInChildren<Text>(true)) text.gameObject.SetActive(false);
            // The original desktop roaming HUD has no written movement instruction.
            Ref<Text>(hud, "prompt").text = "";
            var move = Ref<GameObject>(hud, "move");
            var joystick = move.transform.Find("Icon");
            if (joystick)
            {
                Fixed((RectTransform)joystick, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(100, 100), Vector2.one * 0.5f);
                hud.FindPropertyRelative("joystickGraphic").objectReferenceValue = joystick.gameObject;
            }
            var fingerChild = safe.Find("Original movement finger");
            var finger = fingerChild ? fingerChild.GetComponent<Image>() : new GameObject("Original movement finger", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            finger.transform.SetParent(safe, false);
            finger.sprite = Sprite("Finger"); finger.preserveAspect = true; finger.raycastTarget = false;
            Fixed(finger.rectTransform, new Vector2(0.61f, 0.14f), Vector2.zero, new Vector2(120, 140), Vector2.one * 0.5f);
            finger.gameObject.SetActive(false);
            hud.FindPropertyRelative("tutorialFinger").objectReferenceValue = finger.gameObject;
            RestoreResult(hud, font);
            var actor = data.FindProperty("stage").FindPropertyRelative("Player").FindPropertyRelative("Root").objectReferenceValue as Transform;
            if (actor) RestoreName(actor, font, CameraFor(data));
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Original Lilita SDF font: 96 recovered characters (ASCII 32-126 and ellipsis). Existing HUD restyled; save the scene after review.");
        }

        private static Font RestoreFont()
        {
            var atlasPath = FontRoot + "LilitaOneOriginalSDF.png";
            var importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
            if (!importer) { AssetDatabase.ImportAsset(atlasPath); importer = AssetImporter.GetAtPath(atlasPath) as TextureImporter; }
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            var metrics = JsonUtility.FromJson<Metrics>(File.ReadAllText(FontRoot + "LilitaOneOriginalMetrics.json"));
            var material = AssetDatabase.LoadAssetAtPath<Material>(FontRoot + "LilitaOneOriginal.mat");
            if (!material) { material = new Material(Shader.Find("BeachBoxing/Original Lilita UI SDF")); AssetDatabase.CreateAsset(material, FontRoot + "LilitaOneOriginal.mat"); }
            material.mainTexture = atlas;
            material.SetFloat("_OutlineWidth", 0);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "LilitaOneOriginal.fontsettings");
            if (!font) { font = new Font { name = "Lilita One (original atlas)" }; AssetDatabase.CreateAsset(font, FontRoot + "LilitaOneOriginal.fontsettings"); }
            font.material = material;
            var characters = new CharacterInfo[metrics.glyphs.Length];
            for (int index = 0; index < characters.Length; index++)
            {
                var g = metrics.glyphs[index];
                int padding = g.rectWidth == 0 ? 0 : 5;
                float left = (g.x - padding) / (float)atlas.width, bottom = (g.y - padding) / (float)atlas.height;
                float right = (g.x + g.rectWidth + padding) / (float)atlas.width, top = (g.y + g.rectHeight + padding) / (float)atlas.height;
                characters[index] = new CharacterInfo
                {
                    index = g.unicode, size = 0, style = FontStyle.Normal,
                    advance = Mathf.RoundToInt(g.advance),
                    minX = Mathf.RoundToInt(g.bearingX) - padding,
                    maxX = Mathf.RoundToInt(g.bearingX) + g.rectWidth + padding,
                    minY = Mathf.RoundToInt(g.bearingY) - g.rectHeight - padding,
                    maxY = Mathf.RoundToInt(g.bearingY) + padding,
                    uvBottomLeft = new Vector2(left, bottom), uvBottomRight = new Vector2(right, bottom),
                    uvTopLeft = new Vector2(left, top), uvTopRight = new Vector2(right, top)
                };
            }
            font.characterInfo = characters;
            WriteFontMetrics(font, metrics);
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(material);
            return font;
        }

        [MenuItem("Beach Boxing/Repair Lilita nominal font size")]
        public static void RepairFontMetrics()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontRoot + "LilitaOneOriginal.fontsettings");
            if (!font) throw new InvalidOperationException("Restore the original font first.");
            WriteFontMetrics(font, JsonUtility.FromJson<Metrics>(File.ReadAllText(FontRoot + "LilitaOneOriginalMetrics.json")));
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
                if (text.font == font) { text.cachedTextGenerator.Invalidate(); text.SetAllDirty(); }
            Debug.Log("Lilita original nominal font size: " + font.fontSize);
        }

        private static void WriteFontMetrics(Font font, Metrics metrics)
        {
            var serialized = new SerializedObject(font);
            SetNumber(serialized, "m_FontSize", metrics.pointSize);
            SetNumber(serialized, "m_LineSpacing", metrics.lineHeight);
            SetNumber(serialized, "m_Ascent", metrics.ascent);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material OutlinedMaterial()
        {
            var path = FontRoot + "LilitaOneOriginalOutline.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(FontRoot + "LilitaOneOriginal.mat"));
                // Original TMP material: OutlineWidth .453, ScaleRatioA .8333333.
                material.SetFloat("_OutlineWidth", 0.453f * 0.8333333f * 0.5f);
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

        [MenuItem("Beach Boxing/Restore original pause panel")]
        public static void RestorePausePanel()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before restoring the saved pause panel.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated BeachBoxing scene first.");
            Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, "Restore original pause panel");
            var data = new SerializedObject(flow);
            var hud = data.FindProperty("hud");
            var safe = Ref<RectTransform>(hud, "safe");
            var font = Ref<Font>(hud, "font");
            if (!safe || !font) throw new InvalidOperationException("Restore the saved HUD and font first.");
            var existing = safe.Find("Original pause panel");
            var overlay = existing ? existing.GetComponent<Image>() : new GameObject("Original pause panel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            overlay.transform.SetParent(safe, false);
            Anchors(overlay.rectTransform, 0, 0, 1, 1);
            overlay.color = new Color(0, 0, 0, 0.45f);
            overlay.raycastTarget = true;
            overlay.transform.SetAsLastSibling();
            var cardChild = overlay.transform.Find("Pause card");
            var card = cardChild ? cardChild.GetComponent<Image>() : new GameObject("Pause card", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            card.transform.SetParent(overlay.transform, false);
            Anchors(card.rectTransform, 0.20f, 0.20f, 0.80f, 0.80f);
            card.sprite = Sprite("BG 1"); card.color = Color.white; card.raycastTarget = true;
            var title = ChildText(card.transform, "Pause title", font, "PAUSED", 48);
            Anchors(title.rectTransform, 0.05f, 0.73f, 0.95f, 0.96f);
            title.material = OutlinedMaterial();
            var resume = PauseButton(card.transform, font, "Resume", "RESUME", "Join Room BTN 1", 0.42f, 0.64f);
            var home = PauseButton(card.transform, font, "Return home", "RETURN HOME", "Join Room BTN 3", 0.13f, 0.35f);
            hud.FindPropertyRelative("pausePanel").objectReferenceValue = overlay.gameObject;
            hud.FindPropertyRelative("pauseResume").objectReferenceValue = resume;
            hud.FindPropertyRelative("pauseHome").objectReferenceValue = home;
            overlay.gameObject.SetActive(false);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            Debug.Log("Preplaced original-art pause panel. Bind its actions with hud.BindPause(resume, home), then save the scene.");
        }

        private static Button PauseButton(Transform parent, Font font, string name, string caption, string sprite, float bottom, float top)
        {
            var child = parent.Find(name);
            var button = child ? child.GetComponent<Button>() : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            Anchors((RectTransform)button.transform, 0.17f, bottom, 0.83f, top);
            var image = button.GetComponent<Image>();
            image.sprite = Sprite(sprite); image.color = Color.white; image.raycastTarget = true;
            button.targetGraphic = image;
            var text = ChildText(button.transform, "Label", font, caption, 29);
            Anchors(text.rectTransform, 0.03f, 0.05f, 0.97f, 0.95f);
            return button;
        }

        [MenuItem("Beach Boxing/Restore original world combat bars")]
        public static void RestoreCombatBars()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before restoring saved combat bars.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated BeachBoxing scene first.");
            Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, "Restore original world combat bars");
            var data = new SerializedObject(flow);
            var hud = data.FindProperty("hud");
            var stage = data.FindProperty("stage");
            var player = stage.FindPropertyRelative("Player").FindPropertyRelative("Root").objectReferenceValue as Transform;
            var enemy = stage.FindPropertyRelative("Enemy").FindPropertyRelative("Root").objectReferenceValue as Transform;
            if (!player || !enemy) throw new InvalidOperationException("Both preplaced fighter roots must be assigned.");
            var font = Ref<Font>(hud, "font");
            if (!font) throw new InvalidOperationException("Restore the original font first.");
            var camera = CameraFor(data);
            CombatCanvas(hud, player, camera, font, "player");
            CombatCanvas(hud, enemy, camera, font, "enemy");
            // Keep the status marker and its serialized reference for phase/test compatibility.
            // Its old screen-space decorations no longer represent a fighter's world position.
            var status = Ref<GameObject>(hud, "combat");
            foreach (Transform child in status.transform) child.gameObject.SetActive(false);
            var title = player.Find("Original title canvas");
            if (title) hud.FindPropertyRelative("trainingTitle").objectReferenceValue = title.gameObject;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Moved the six bound combat indicators into two preplaced fighter canvases. Save the scene after reviewing combat camera framing.");
        }

        private static void CombatCanvas(SerializedProperty hud, Transform fighter, Camera camera, Font font, string side)
        {
            string name = "Original combat bars";
            var existing = fighter.Find(name);
            var canvas = existing ? existing.GetComponent<Canvas>() : new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(BeachNameBillboard)).GetComponent<Canvas>();
            canvas.transform.SetParent(fighter, false);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.transform.localPosition = new Vector3(0, 0.55f, 0);
            canvas.transform.localScale = Vector3.one * 0.006f;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(300, 110);
            canvas.GetComponent<BeachNameBillboard>().View = camera;
            hud.FindPropertyRelative(side + "WorldBars").objectReferenceValue = canvas.gameObject;

            var health = Ref<Image>(hud, side + "Health");
            var stamina = Ref<Image>(hud, side + "Stamina");
            var guard = Ref<Image>(hud, side + "Guard");
            PlaceWorldBar(health, canvas.transform, new Vector2(15, 18), new Color(1, 0.13f, 0));
            PlaceWorldBar(stamina, canvas.transform, new Vector2(15, -19), new Color(1, 0.79f, 0));
            WorldIcon(canvas.transform, "Heart icon", "Heart", new Vector2(-147, 18), new Vector2(32, 32));
            WorldIcon(canvas.transform, "Energy icon", "EnergyIcon", new Vector2(-147, -19), new Vector2(30, 36));

            var healthText = ChildText(health.transform.parent, "Original health value", font, "50", 21);
            Fixed(healthText.rectTransform, new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(60, 30), new Vector2(1, 0.5f));
            healthText.alignment = TextAnchor.MiddleRight;
            var staminaText = ChildText(stamina.transform.parent, "Original stamina value", font, "50", 21);
            Fixed(staminaText.rectTransform, new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(60, 30), new Vector2(1, 0.5f));
            staminaText.alignment = TextAnchor.MiddleRight;
            hud.FindPropertyRelative(side + "HealthValue").objectReferenceValue = healthText;
            hud.FindPropertyRelative(side + "StaminaValue").objectReferenceValue = staminaText;

            var guardRect = guard.transform.parent as RectTransform;
            guardRect.SetParent(canvas.transform, false);
            Fixed(guardRect, Vector2.one * 0.5f, new Vector2(178, 65), new Vector2(78, 92), Vector2.one * 0.5f);
            var guardBackground = guardRect.GetComponent<Image>();
            guardBackground.sprite = Sprite("ShieldIcon");
            guardBackground.color = new Color(0.65f, 0.70f, 0.75f, 0.38f);
            guardBackground.type = Image.Type.Simple;
            guardBackground.preserveAspect = true;
            guardBackground.raycastTarget = false;
            Anchors(guard.rectTransform, 0, 0, 1, 1);
            guard.sprite = Sprite("ShieldIcon");
            guard.type = Image.Type.Simple;
            guard.preserveAspect = true;
            guard.color = new Color(1, 1, 1, 0.38f);
            guard.raycastTarget = false;
            var guardText = Ref<Text>(hud, side == "player" ? "playerValues" : "enemyValues");
            guardText.transform.SetParent(guardRect, false);
            Anchors(guardText.rectTransform, 0, 0, 1, 1);
            guardText.text = "20"; guardText.font = font; guardText.fontSize = 30;
            guardText.material = OutlinedMaterial();
            guardText.alignment = TextAnchor.MiddleCenter;
            guardText.resizeTextForBestFit = false;
            guardText.gameObject.SetActive(true);
            canvas.gameObject.SetActive(false);
        }

        private static void PlaceWorldBar(Image fill, Transform canvas, Vector2 position, Color color)
        {
            var rect = fill.transform.parent as RectTransform;
            rect.SetParent(canvas, false);
            Fixed(rect, Vector2.one * 0.5f, position, new Vector2(270, 27), Vector2.one * 0.5f);
            var background = rect.GetComponent<Image>();
            background.sprite = Sprite("CountBG Full");
            background.color = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            background.raycastTarget = false;
            Anchors(fill.rectTransform, 0, 0, 1, 1);
            fill.sprite = Sprite("CountBG Full");
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1;
            fill.raycastTarget = false;
            rect.gameObject.SetActive(true);
        }

        private static void WorldIcon(Transform canvas, string name, string sprite, Vector2 position, Vector2 size)
        {
            var child = canvas.Find(name);
            var image = child ? child.GetComponent<Image>() : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(canvas, false);
            image.sprite = Sprite(sprite); image.preserveAspect = true; image.raycastTarget = false;
            Fixed(image.rectTransform, Vector2.one * 0.5f, position, size, Vector2.one * 0.5f);
        }

        private static void RestoreResult(SerializedProperty hud, Font font)
        {
            var result = Ref<GameObject>(hud, "result");
            Anchors(result.GetComponent<RectTransform>(), 0.17f, 0.17f, 0.82f, 0.82f);
            var title = Ref<GameObject>(hud, "victoryTitle");
            Anchors(title.GetComponent<RectTransform>(), 0.06f, 0.79f, 0.94f, 1.12f);
            var titleText = title.GetComponentInChildren<Text>(true);
            titleText.fontSize = 62;
            titleText.material = OutlinedMaterial();
            var caption = ChildText(result.transform, "Original defeated caption", font, "You Defeated:", 20);
            Anchors(caption.rectTransform, 0.2f, 0.73f, 0.8f, 0.81f);
            hud.FindPropertyRelative("defeatedCaption").objectReferenceValue = caption.gameObject;
            var card = Ref<GameObject>(hud, "victoryCard");
            Anchors(card.GetComponent<RectTransform>(), 0.035f, 0.23f, 0.965f, 0.73f);
            var picture = Ref<Image>(hud, "resultPortrait");
            Anchors(picture.rectTransform, 0.035f, 0.07f, 0.26f, 0.95f);
            Anchors(Ref<GameObject>(hud, "goldReward").GetComponent<RectTransform>(), 0.29f, 0.39f, 0.53f, 0.58f);
            Anchors(Ref<GameObject>(hud, "starReward").GetComponent<RectTransform>(), 0.29f, 0.20f, 0.53f, 0.39f);
            var rewardCaption = ChildText(card.transform, "Original rewards caption", font, "REWARDS", 20);
            Anchors(rewardCaption.rectTransform, 0.29f, 0.55f, 0.96f, 0.65f);
            Fixed(Ref<Button>(hud, "homeButton").GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(160, 60), new Vector2(0.5f, 0));
            Ref<Text>(hud, "returnLabel").fontSize = 29;
        }

        private static void RestoreName(Transform actor, Font font, Camera camera)
        {
            var found = actor.Find("Original title canvas");
            var canvas = found ? found.GetComponent<Canvas>() : new GameObject("Original title canvas", typeof(RectTransform), typeof(Canvas), typeof(BeachNameBillboard)).GetComponent<Canvas>();
            canvas.transform.SetParent(actor, false);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localPosition = new Vector3(0, 2.3852f, 0);
            canvas.transform.localScale = Vector3.one * (0.048561f * OriginalWorldRestore.Scale);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(500, 200);
            canvas.GetComponent<BeachNameBillboard>().View = camera;
            var name = ChildText(canvas.transform, "NameText", font, "Smelly Pants", 24);
            Fixed(name.rectTransform, Vector2.one * 0.5f, new Vector2(0, 43.9f), new Vector2(217.59f, 50), Vector2.one * 0.5f);
            name.material = OutlinedMaterial();
            var level = ChildText(canvas.transform, "LevelText", font, "LVL 1", 24);
            Fixed(level.rectTransform, Vector2.one * 0.5f, new Vector2(0, 20.2f), new Vector2(217.59f, 50), Vector2.one * 0.5f);
            level.rectTransform.localScale = Vector3.one * 0.64645f;
            var rank = ChildText(canvas.transform, "RankText", font, "Rookie", 32);
            rank.color = Color.yellow;
            Fixed(rank.rectTransform, Vector2.one * 0.5f, new Vector2(0, 2.1f), new Vector2(217.59f, 50), Vector2.one * 0.5f);
            rank.rectTransform.localScale = Vector3.one * 0.8f;
        }

        private static Camera CameraFor(SerializedObject data)
        {
            var camera = data.FindProperty("stage").FindPropertyRelative("camera");
            return camera != null ? camera.objectReferenceValue as Camera : Camera.main;
        }
        private static Text ChildText(Transform parent, string name, Font font, string value, int size)
        {
            var child = parent.Find(name);
            var text = child ? child.GetComponent<Text>() : new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false); text.font = font; text.material = font.material;
            text.text = value; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
        private static T Ref<T>(SerializedProperty parent, string name) where T : UnityEngine.Object => parent.FindPropertyRelative(name).objectReferenceValue as T;
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + name + ".asset");
        private static void Fixed(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = Vector3.one; }
        private static void Anchors(RectTransform rect, float x, float y, float right, float top)
        { rect.anchorMin = new Vector2(x, y); rect.anchorMax = new Vector2(right, top); rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
        private static void SetNumber(SerializedObject target, string name, float value)
        {
            var property = target.FindProperty(name);
            if (property == null) return;
            if (property.propertyType == SerializedPropertyType.Float) property.floatValue = value;
            else if (property.propertyType == SerializedPropertyType.Integer) property.intValue = Mathf.RoundToInt(value);
            else throw new InvalidOperationException(name + " has unexpected type " + property.propertyType);
        }
    }
}
