using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BeachBoxing.Editor
{
    public static class BeachUiRedesignAuthoring
    {
        private const string UiRoot = "Assets/BeachBoxing/Art/UI/";
        private const string FontPath = "Assets/BeachBoxing/Fonts/NotoSansSC-Regular.otf";
        private const string MixerPath = "Assets/BeachBoxing/OriginalArt/AudioMixerController/Main.mixer";
        private const string ButtonClickClipPath = "Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/Audio/Click.mp3";
        private const string CommonPrefabPath = "Assets/BeachBoxing/Resources/BeachBoxing/CommonUI.prefab";
        private const string InitScenePath = "Assets/GameFrame/Init.unity";
        private const string LegacyMenuScenePath = "Assets/GameFrame/Game.unity";
        private const string LegacyPlayScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        private const string GameScenePath = "Assets/BeachBoxing/Game.unity";

        private static Font cachedFont;

        [MenuItem("Beach Boxing/1. Import UI Sprites")]
        public static void ImportAllUiSprites()
        {
            if (!Directory.Exists(UiRoot))
                throw new InvalidOperationException("Missing UI art folder: " + UiRoot);

            var files = Directory.GetFiles(UiRoot, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace("\\", "/"))
                .ToArray();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in files)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    bool isJpg = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = !isJpg;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.maxTextureSize = 2048;
                    importer.textureCompression = isJpg
                        ? TextureImporterCompression.CompressedHQ
                        : TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            Debug.Log($"[BeachUiRedesign] Imported {files.Length} UI sprites.");
        }

        [MenuItem("Beach Boxing/2. Apply Full UI Redesign & Dual Scene Flow")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring scenes.");

            cachedFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (cachedFont == null)
                throw new InvalidOperationException("Missing Chinese font at " + FontPath);

            ImportAllUiSprites();
            MigrateScenesAndBuildSettings();
            BuildCommonUiPrefab();
            BuildInitScene();
            BuildGameScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BeachUiRedesign] Completed 1:1 UI authoring for Init, CommonUI, and Game.");
        }

        private static void MigrateScenesAndBuildSettings()
        {
            if (!File.Exists(GameScenePath) && File.Exists(LegacyPlayScenePath))
            {
                string err = AssetDatabase.MoveAsset(LegacyPlayScenePath, GameScenePath);
                if (!string.IsNullOrEmpty(err))
                    throw new InvalidOperationException("Failed to move BeachBoxing.unity -> Game.unity: " + err);
            }

            if (File.Exists(LegacyMenuScenePath))
                AssetDatabase.DeleteAsset(LegacyMenuScenePath);

            if (Directory.Exists("Assets/BeachBoxing/Scenes") &&
                Directory.GetFiles("Assets/BeachBoxing/Scenes").All(f => f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
                AssetDatabase.DeleteAsset("Assets/BeachBoxing/Scenes");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(InitScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static void BuildCommonUiPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CommonPrefabPath));
            var root = new GameObject("BeachCommonUI");
            try
            {
                var common = root.AddComponent<BeachCommonUI>();
                var uiSfx = root.AddComponent<AudioSource>();
                uiSfx.playOnAwake = false;
                uiSfx.loop = false;
                uiSfx.spatialBlend = 0f;
                uiSfx.ignoreListenerPause = true;
                common.uiSfxSource = uiSfx;
                common.buttonClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ButtonClickClipPath);

                // EventSystem (owned by CommonUI across Init and Game)
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(BeachTouchModule));
                esGo.transform.SetParent(root.transform, false);

                // 1. LoadingPanel (1:1 加载.psb)
                common.Loading = BuildLoadingPanel(root.transform);

                // 2. SettingPanel (1:1 设置.psb)
                common.Settings = BuildSettingsPanel(root.transform);

                // 3. ToastPanel
                common.Toast = BuildToastPanel(root.transform);

                PrefabUtility.SaveAsPrefabAsset(root, CommonPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static BeachLoadingView BuildLoadingPanel(Transform parent)
        {
            var canvasRect = CreateCanvas(parent, "LoadingPanel", 200);
            var view = canvasRect.gameObject.AddComponent<BeachLoadingView>();
            var root = CreateStretch(canvasRect, "Root");
            view.root = root.gameObject;

            var bg = CreateStretchImage(root, "Background", LoadSprite("Loading/Background.jpg"), raycast: true);
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 1080f;

            var bottom = CreateAnchorContainer(root, "BottomGroup", new Vector2(0.5f, 0f));

            // LoadingTitle: bbox (702, 794, 1183, 919)
            PlaceBottomCenterImage(bottom, "LoadingTitle", LoadSprite("Loading/LoadingTitle.png"), 702, 794, 1183, 919);

            // ProgressTrack: bbox (399, 931, 1485, 1037)
            PlaceBottomCenterImage(bottom, "ProgressTrack", LoadSprite("Loading/ProgressTrack.png"), 399, 931, 1485, 1037);

            // ProgressFill: bbox (434, 951, 1454, 1009)
            var fillImg = PlaceBottomCenterImage(bottom, "ProgressFill", LoadSprite("Loading/ProgressFill.png"), 434, 951, 1454, 1009);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            view.progressFill = fillImg;

            root.gameObject.SetActive(false);
            return view;
        }

        private static BeachSettingsPanel BuildSettingsPanel(Transform parent)
        {
            var canvasRect = CreateCanvas(parent, "SettingPanel", 150);
            var panel = canvasRect.gameObject.AddComponent<BeachSettingsPanel>();
            panel.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            panel.toggleOnSprite = LoadSprite("Settings/ToggleOn.png");
            panel.toggleOffSprite = LoadSprite("Settings/ToggleOff.png");

            var root = CreateStretch(canvasRect, "Root");
            panel.root = root.gameObject;

            var dim = CreateStretchImage(root, "DimOverlay", null, raycast: true);
            dim.color = new Color(0f, 0f, 0f, 0.5f);

            var modal = CreateCenterContainer(root, "ModalWindow");

            // PanelBg: bbox (556, 282, 1346, 901)
            PlaceCenterImage(modal, "PanelBg", LoadSprite("Settings/PanelBg.png"), 556, 282, 1346, 901, raycast: true);

            // TitleArt: bbox (681, 142, 1159, 357)
            PlaceCenterImage(modal, "TitleArt", LoadSprite("Settings/TitleArt.png"), 681, 142, 1159, 357);

            // CloseBtn: bbox (1230, 251, 1365, 381)
            panel.closeButton = PlaceCenterButton(modal, "CloseBtn", LoadSprite("Common/CloseBtn.png"), 1230, 251, 1365, 381);

            // MusicRow: bbox (655, 411, 1248, 582)
            var musicRow = CreateCenterContainer(modal, "MusicRow");
            PlaceCenterImage(musicRow, "RowBg", LoadSprite("Settings/RowBg.png"), 655, 411, 1248, 582);
            PlaceCenterImage(musicRow, "MusicIcon", LoadSprite("Settings/MusicIcon.png"), 703, 448, 805, 546);
            PlaceCenterText(musicRow, "MusicLabel", "音乐", 64, Hex("2A3964"), 815, 445, 995, 548, TextAnchor.MiddleCenter);
            panel.musicToggleButton = PlaceCenterButton(musicRow, "MusicToggleBtn", panel.toggleOnSprite, 1036, 446, 1223, 548);
            panel.musicToggleImage = panel.musicToggleButton.GetComponent<Image>();

            // SfxRow: bbox (655, 623, 1248, 794)
            var sfxRow = CreateCenterContainer(modal, "SfxRow");
            PlaceCenterImage(sfxRow, "RowBg", LoadSprite("Settings/RowBg.png"), 655, 623, 1248, 794);
            PlaceCenterImage(sfxRow, "SfxIcon", LoadSprite("Settings/SfxIcon.png"), 688, 644, 810, 773);
            PlaceCenterText(sfxRow, "SfxLabel", "音效", 64, Hex("2A3964"), 815, 657, 995, 760, TextAnchor.MiddleCenter);
            panel.sfxToggleButton = PlaceCenterButton(sfxRow, "SfxToggleBtn", panel.toggleOnSprite, 1036, 658, 1223, 760);
            panel.sfxToggleImage = panel.sfxToggleButton.GetComponent<Image>();

            root.gameObject.SetActive(false);
            return panel;
        }

        private static BeachToastView BuildToastPanel(Transform parent)
        {
            var canvasRect = CreateCanvas(parent, "ToastPanel", 250);
            var toast = canvasRect.gameObject.AddComponent<BeachToastView>();
            var root = CreateCenterRect(canvasRect, "Root", new Vector2(0f, 180f), new Vector2(540f, 92f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.12f, 0.24f, 0.92f);
            bg.raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var labelRect = CreateStretch(root, "MessageText");
            var label = CreateText(labelRect, "请接入SDK", 38, Color.white, TextAnchor.MiddleCenter);
            AddOutline(label, Hex("021936"), 2f);

            toast.root = root.gameObject;
            toast.messageLabel = label;
            toast.group = group;
            root.gameObject.SetActive(false);
            return toast;
        }

        private static void BuildInitScene()
        {
            var scene = EditorSceneManager.OpenScene(InitScenePath, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            // Clean Main Camera + AudioListener
            var camGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.14f, 0.25f, 1f);
            cam.orthographic = true;

            // HomePanel Canvas (1:1 主界面.psb)
            var canvasRect = CreateCanvas(null, "HomePanel", 0);
            var home = canvasRect.gameObject.AddComponent<BeachHomePanel>();
            var root = CreateStretch(canvasRect, "Root");
            home.root = root.gameObject;

            var bg = CreateStretchImage(root, "Background", LoadSprite("Home/Background.jpg"), raycast: false);
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1920f / 1080f;

            // StartBtn: bbox (531, 658, 1389, 990)
            var bottomGroup = CreateAnchorContainer(root, "BottomActions", new Vector2(0.5f, 0f));
            home.startButton = PlaceBottomCenterButton(bottomGroup, "StartBtn", LoadSprite("Home/StartBtn.png"), 531, 658, 1389, 990);
            home.startButtonVisual = (RectTransform)home.startButton.transform;

            // Left channel column (anchor TopLeft)
            var leftGroup = CreateAnchorContainer(root, "LeftChannelButtons", new Vector2(0f, 1f));
            home.subscribeButton = BuildHomeCircleButton(leftGroup, "SubscribeBtn", LoadSprite("Home/SubscribeIcon.png"), "订阅", 30,
                41, 128, 189, 279, 71, 141, 160, 238, 46, 231, 184, 279, hasSeparateLabelPill: true, topLeftAnchor: true);
            home.addDesktopButton = BuildHomeCircleButton(leftGroup, "AddDesktopBtn", LoadSprite("Home/AddDesktopIcon.png"), "添加桌面", 26,
                41, 326, 189, 477, 80, 344, 162, 441, 46, 429, 184, 477, hasSeparateLabelPill: true, topLeftAnchor: true);
            home.shareButton = BuildHomeCircleButton(leftGroup, "ShareBtn", LoadSprite("Home/ShareIcon.png"), "分享", 30,
                41, 540, 189, 691, 62, 562, 162, 649, 46, 643, 184, 691, hasSeparateLabelPill: true, topLeftAnchor: true);

            // Right utility column (anchor TopRight)
            var rightGroup = CreateAnchorContainer(root, "RightUtilityButtons", new Vector2(1f, 1f));
            home.settingsButton = BuildHomeCircleButton(rightGroup, "SettingsBtn", LoadSprite("Home/SettingsIcon.png"), "设置", 30,
                1730, 220, 1878, 371, 1773, 255, 1837, 316, 1735, 323, 1873, 371, hasSeparateLabelPill: false, topLeftAnchor: false);
            home.entryRewardButton = BuildHomeCircleButton(rightGroup, "EntryRewardBtn", LoadSprite("Home/EntryRewardIcon.png"), "入口有奖", 26,
                1730, 430, 1878, 581, 1756, 449, 1853, 543, 1735, 533, 1873, 581, hasSeparateLabelPill: true, topLeftAnchor: false);
            var alertImg = PlaceInParentRelative(home.entryRewardButton.transform, "AlertBadge", LoadSprite("Common/AlertBadge.png"),
                1730, 430, 1878, 581, 1836, 428, 1880, 471);
            home.entryRewardAlert = alertImg.gameObject;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, InitScenePath);
        }

        private static Button BuildHomeCircleButton(
            Transform parent, string name, Sprite iconSprite, string caption, int fontSize,
            int bx0, int by0, int bx1, int by1,
            int ix0, int iy0, int ix1, int iy1,
            int px0, int py0, int px1, int py1,
            bool hasSeparateLabelPill, bool topLeftAnchor)
        {
            var btn = topLeftAnchor
                ? PlaceTopLeftButton(parent, name, LoadSprite("Home/CircleBtnBg.png"), bx0, by0, bx1, by1)
                : PlaceTopRightButton(parent, name, LoadSprite("Home/CircleBtnBg.png"), bx0, by0, bx1, by1);

            PlaceInParentRelative(btn.transform, "Icon", iconSprite, bx0, by0, bx1, by1, ix0, iy0, ix1, iy1);
            if (hasSeparateLabelPill)
                PlaceInParentRelative(btn.transform, "LabelBg", LoadSprite("Home/CircleLabelBg.png"), bx0, by0, bx1, by1, px0, py0, px1, py1);

            var lblRect = PlaceRectInParentRelative(btn.transform, "Label", bx0, by0, bx1, by1, px0, py0, px1, py1);
            var lbl = CreateText(lblRect, caption, fontSize, Color.white, TextAnchor.MiddleCenter);
            AddOutline(lbl, Hex("0A2866"), 1.5f);
            return btn;
        }

        private static void BuildGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var flow = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<BeachGameFlow>(true))
                .SingleOrDefault();
            if (flow == null)
                throw new InvalidOperationException("BeachGameFlow not found in " + GameScenePath);

            // Remove duplicate EventSystem in Game scene (CommonUI provides the single persistent EventSystem)
            foreach (var es in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).ToArray())
                Object.DestroyImmediate(es.gameObject);

            // Remove old legacy UI canvases & legacy UI components from BeachGameFlow
            DestroyChildIfExists(flow.transform, "Original peripheral canvas");
            DestroyChildIfExists(flow.transform, "Beach UI");
            DestroyChildIfExists(flow.transform, "GameUI");

            RemoveComponentIfExists<BeachPeripheralView>(flow.gameObject);
            RemoveComponentIfExists<BeachNativeSettingsView>(flow.gameObject);
            RemoveComponentIfExists<BeachNativeTrainingView>(flow.gameObject);
            RemoveComponentIfExists<BeachNativeTutorialView>(flow.gameObject);
            RemoveComponentIfExists<BeachNativeOpponentView>(flow.gameObject);
            RemoveComponentIfExists<BeachNativeMobileControls>(flow.gameObject);
            RemoveComponentIfExists<BeachChineseUi>(flow.gameObject);

            var canvasRect = CreateCanvas(flow.transform, "GameUI", 10);
            var ui = canvasRect.gameObject.AddComponent<BeachGameUi>();
            ui.canvas = canvasRect.GetComponent<Canvas>();
            var safeRoot = CreateStretch(canvasRect, "SafeArea");
            ui.safeRoot = safeRoot;

            // Screen fade image for BeachPresentationView
            var fadeImg = CreateStretchImage(canvasRect, "SceneFadeOverlay", null, raycast: false);
            fadeImg.color = Color.clear;
            fadeImg.gameObject.SetActive(false);

            // Combat hit / low HP / heal overlay
            var combatFx = CreateStretchImage(canvasRect, "CombatScreenOverlay", null, raycast: false);
            combatFx.color = Color.clear;
            combatFx.enabled = false;
            combatFx.transform.SetAsFirstSibling();
            ui.combatScreenOverlay = combatFx;

            BuildHudPanel(safeRoot, ui);
            BuildRoamingControls(safeRoot, ui);
            BuildWorkoutPanel(safeRoot, ui);
            BuildCombatPanel(safeRoot, ui);
            BuildCountdownPanel(safeRoot, ui);
            BuildChallengePanel(safeRoot, ui);
            BuildBoxerPanel(safeRoot, ui);
            BuildSignInPanel(safeRoot, ui);
            BuildResultPanel(safeRoot, ui);

            // Restyle world-space Player nameplate and Player/Opponent overhead combat bars (1:1 操作.psb & 玩法.psb)
            var flowSO = new SerializedObject(flow);
            var stageProp = flowSO.FindProperty("stage");
            var playerRoot = stageProp.FindPropertyRelative("Player").FindPropertyRelative("Root").objectReferenceValue as Transform;
            var enemyRoot = stageProp.FindPropertyRelative("Enemy").FindPropertyRelative("Root").objectReferenceValue as Transform;
            var stageCamera = stageProp.FindPropertyRelative("camera").objectReferenceValue as Camera ?? Camera.main;

            if (playerRoot != null)
            {
                ui.playerTitleRoot = BuildWorldPlayerTitle(playerRoot, stageCamera);
                BuildWorldFighterBars(playerRoot, stageCamera, "PlayerCombatBars", out ui.playerBarsRoot,
                    out ui.playerHealthFill, out ui.playerHealthText, out ui.playerStaminaFill, out ui.playerStaminaText);
            }
            if (enemyRoot != null)
            {
                BuildWorldFighterBars(enemyRoot, stageCamera, "OpponentCombatBars", out ui.enemyBarsRoot,
                    out ui.enemyHealthFill, out ui.enemyHealthText, out ui.enemyStaminaFill, out ui.enemyStaminaText);
            }

            // Bind BeachGameUi & SceneFadeOverlay on BeachGameFlow
            flowSO.FindProperty("gameUi").objectReferenceValue = ui;
            var presProp = flowSO.FindProperty("presentation");
            presProp.FindPropertyRelative("fade").objectReferenceValue = fadeImg;
            presProp.FindPropertyRelative("openingRoot").objectReferenceValue = null;
            presProp.FindPropertyRelative("legacyCountdown").objectReferenceValue = null;
            // Clear legacy BeachHud sprite references so old UI sprites can be removed cleanly
            var hudProp = flowSO.FindProperty("hud");
            if (hudProp != null)
            {
                var icons = hudProp.FindPropertyRelative("muscleIcons");
                if (icons != null && icons.isArray) icons.arraySize = 0;
                var portraits = hudProp.FindPropertyRelative("portraits");
                if (portraits != null && portraits.isArray) portraits.arraySize = 0;
            }
            flowSO.ApplyModifiedPropertiesWithoutUndo();

            SanitizeNpcFakeNames(flow);
            RestyleAiAndTrainerNameplates(flow);
            ReplaceWorldLegacySprites(scene.GetRootGameObjects().Select(g => g.transform));
            ReplaceWorkoutWorldPrefabSprites();

            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static readonly Dictionary<string, string> WorldSpriteReplacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "aRmwad 1", "Common/StatChestIcon.png" },
            { "aRmwad", "Common/StatArmsIcon.png" },
            { "aRmwad 2", "Common/StatLegsIcon.png" },
            { "aRmwad 3", "Common/StatBackIcon.png" },
            { "Heart", "Gameplay/HealthIcon.png" },
            { "EnergyIcon", "Gameplay/StaminaIcon.png" },
            { "PunchIcon", "Gameplay/PunchBtn.png" },
            { "ShieldIcon", "Gameplay/BlockBtn.png" },
            { "Logo_", "Common/Logo.png" },
        };

        public static int ReplaceWorldLegacySprites(IEnumerable<Transform> roots)
        {
            int replaced = 0;
            var toDestroy = new List<GameObject>();
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == null || sr.sprite == null) continue;
                    string oldName = sr.sprite.name;
                    if (!WorldSpriteReplacements.TryGetValue(oldName, out var newRelPath)) continue;

                    // Old ShieldIcon used a second tinted overlay child ("Circle (3)") for outline.
                    // The new BlockBtn sprite already includes its own crisp border, so drop the duplicate overlay.
                    if (oldName == "ShieldIcon" && sr.name == "Circle (3)")
                    {
                        sr.sprite = null;
                        toDestroy.Add(sr.gameObject);
                        replaced++;
                        continue;
                    }

                    var newSprite = LoadSprite(newRelPath);
                    if (newSprite == null) continue;

                    Vector2 oldSize = sr.sprite.bounds.size;
                    Vector2 newSize = newSprite.bounds.size;
                    Vector3 scale = sr.transform.localScale;

                    if (oldName == "Logo_")
                    {
                        float rx = oldSize.x / Mathf.Max(newSize.x, 0.0001f);
                        float ry = oldSize.y / Mathf.Max(newSize.y, 0.0001f);
                        sr.transform.localScale = new Vector3(scale.x * rx, scale.y * ry, scale.z);
                    }
                    else
                    {
                        float oldMax = Mathf.Max(oldSize.x * Mathf.Abs(scale.x), oldSize.y * Mathf.Abs(scale.y));
                        float newMax = Mathf.Max(newSize.x, newSize.y);
                        float absScale = oldMax / Mathf.Max(newMax, 0.0001f);
                        sr.transform.localScale = new Vector3(
                            scale.x < 0f ? -absScale : absScale,
                            scale.y < 0f ? -absScale : absScale,
                            scale.z < 0f ? -absScale : absScale);
                    }

                    sr.sprite = newSprite;
                    if (oldName == "ShieldIcon") sr.color = Color.white;
                    EditorUtility.SetDirty(sr);
                    EditorUtility.SetDirty(sr.transform);
                    replaced++;
                }
            }
            foreach (var go in toDestroy)
                if (go != null) Object.DestroyImmediate(go);
            return replaced;
        }

        public static int ReplaceWorkoutWorldPrefabSprites()
        {
            const string prefabPath = "Assets/BeachBoxing/OriginalArt/World/WorkoutWorld.prefab";
            if (!File.Exists(prefabPath)) return 0;
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int count = ReplaceWorldLegacySprites(new[] { contents.transform });
                if (count > 0) PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                return count;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        public static void SanitizeNpcFakeNames(BeachGameFlow flow)
        {
            if (flow == null) return;
            var npcManager = flow.GetComponent<BeachNativeNpcManager>();
            if (npcManager == null) return;
            var so = new SerializedObject(npcManager);
            var prop = so.FindProperty("fakeNames");
            if (prop == null || !prop.isArray) return;
            var cleaned = new List<string>(prop.arraySize);
            for (int i = 0; i < prop.arraySize; i++)
            {
                string raw = prop.GetArrayElementAtIndex(i).stringValue;
                if (!BeachNativeNpcManager.IsUsableFakeName(raw)) continue;
                string normalized = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
                cleaned.Add(normalized);
            }
            prop.arraySize = cleaned.Count;
            for (int i = 0; i < cleaned.Count; i++)
                prop.GetArrayElementAtIndex(i).stringValue = cleaned[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(npcManager);
        }

        public static int RestyleAiAndTrainerNameplates(BeachGameFlow flow)
        {
            if (flow == null) return 0;
            if (cachedFont == null) cachedFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            int styled = 0;
            Color textColor = Hex("FFE03F");
            Color outlineColor = Hex("113363");

            // 1. Ambient AI NPCs (3 pooled BeachNativeNpcActor instances)
            foreach (var actor in flow.GetComponentsInChildren<BeachNativeNpcActor>(true))
            {
                var tc = actor.transform.Find("TitleCanvas");
                if (tc == null) continue;
                var bb = tc.GetComponent<BeachNameBillboard>();
                if (bb != null)
                {
                    bb.HeightAboveHead = 0.48f;
                    EditorUtility.SetDirty(bb);
                }
                foreach (var label in tc.GetComponentsInChildren<Text>(true))
                {
                    ApplyPlayerNameplateStyle(label, cachedFont, textColor, outlineColor, 2.0f);
                    if (label.name == "NameText") label.rectTransform.anchoredPosition = new Vector2(0f, 64f);
                    else if (label.name == "LevelText") label.rectTransform.anchoredPosition = new Vector2(0f, 36f);
                    else if (label.name == "Title") label.rectTransform.anchoredPosition = new Vector2(0f, 8f);
                    EditorUtility.SetDirty(label);
                    EditorUtility.SetDirty(label.rectTransform);
                    styled++;
                }
            }

            // 2. Trainer Followers (6 trainers under Original trainers)
            var trainersRoot = flow.transform.Find("Original trainers");
            if (trainersRoot != null)
            {
                foreach (var canvas in trainersRoot.GetComponentsInChildren<Canvas>(true))
                {
                    foreach (var label in canvas.GetComponentsInChildren<Text>(true))
                    {
                        ApplyPlayerNameplateStyle(label, cachedFont, textColor, outlineColor, 2.0f);
                        if (label.name == "NameText") label.rectTransform.anchoredPosition = new Vector2(0f, 32f);
                        else if (label.name == "Title") label.rectTransform.anchoredPosition = new Vector2(0f, 2f);
                        EditorUtility.SetDirty(label);
                        EditorUtility.SetDirty(label.rectTransform);
                        styled++;
                    }
                }
            }

            return styled;
        }

        private static void ApplyPlayerNameplateStyle(Text label, Font font, Color fillColor, Color outlineColor, float outlineDistance)
        {
            if (label == null) return;
            if (font != null) label.font = font;
            label.fontStyle = FontStyle.Bold;
            label.color = fillColor;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            foreach (var shadow in label.GetComponents<Shadow>())
                Object.DestroyImmediate(shadow);

            AddThickOutline(label, outlineColor, outlineDistance);
        }

        private static void BuildHudPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var hud = CreateStretch(safeRoot, "HudPanel");
            ui.hudRoot = hud.gameObject;

            // Top-left 4 stat rows (1:1 锻炼.psb with progress bars)
            BuildHudStatsPanel(hud, ui);

            // Top-right currencies & side menu (1:1 操作.psb)
            var topRight = CreateAnchorContainer(hud, "TopRightPanel", new Vector2(1f, 1f));

            // CoinBar: bbox (1158, 41, 1482, 155) + AdBadge (1446, 42, 1485, 71)
            ui.coinAdButton = PlaceTopRightButton(topRight, "CoinBarBtn", LoadSprite("Common/CoinBarBg.png"), 1158, 41, 1482, 155);
            PlaceInParentRelative(ui.coinAdButton.transform, "AdBadge", LoadSprite("Common/AdBadge.png"), 1158, 41, 1482, 155, 1446, 42, 1485, 71);
            var coinTxtRect = PlaceRectInParentRelative(ui.coinAdButton.transform, "CoinValueText", 1158, 41, 1482, 155, 1255, 64, 1408, 116);
            ui.coinValueLabel = CreateText(coinTxtRect, "0", 46, Color.white, TextAnchor.MiddleCenter);
            ui.coinValueLabel.resizeTextForBestFit = true;
            ui.coinValueLabel.resizeTextMinSize = 24;
            ui.coinValueLabel.resizeTextMaxSize = 46;

            // StarBar: bbox (1540, 39, 1861, 151) + AdBadge (1825, 42, 1864, 71)
            ui.starAdButton = PlaceTopRightButton(topRight, "StarBarBtn", LoadSprite("Common/StarBarBg.png"), 1540, 39, 1861, 151);
            PlaceInParentRelative(ui.starAdButton.transform, "AdBadge", LoadSprite("Common/AdBadge.png"), 1540, 39, 1861, 151, 1825, 42, 1864, 71);
            var starTxtRect = PlaceRectInParentRelative(ui.starAdButton.transform, "StarValueText", 1540, 39, 1861, 151, 1635, 64, 1788, 116);
            ui.starValueLabel = CreateText(starTxtRect, "0", 46, Color.white, TextAnchor.MiddleCenter);
            ui.starValueLabel.resizeTextForBestFit = true;
            ui.starValueLabel.resizeTextMinSize = 24;
            ui.starValueLabel.resizeTextMaxSize = 46;

            // Right side buttons: SettingsBtn, BoxerBtn (+NewBadge), SignInBtn (+AlertBadge)
            ui.settingsButton = BuildSideMenuButton(topRight, "SettingsBtn", LoadSprite("Common/SideSettingsIcon.png"), "设置",
                1616, 190, 1859, 292, 1654, 208, 1718, 269, 1726, 210, 1835, 268);

            ui.boxerButton = BuildSideMenuButton(topRight, "BoxerBtn", LoadSprite("Common/SideBoxerIcon.png"), "拳手",
                1616, 326, 1859, 428, 1614, 321, 1727, 425, 1726, 346, 1835, 404);
            ui.boxerNewBadge = PlaceInParentRelative(ui.boxerButton.transform, "NewBadge", LoadSprite("Common/NewBadge.png"),
                1616, 326, 1859, 428, 1789, 302, 1891, 366).gameObject;

            ui.signInButton = BuildSideMenuButton(topRight, "SignInBtn", LoadSprite("Common/SideSignInIcon.png"), "签到",
                1616, 467, 1859, 569, 1650, 478, 1720, 548, 1726, 487, 1835, 545);
            ui.signInAlertBadge = PlaceInParentRelative(ui.signInButton.transform, "AlertBadge", LoadSprite("Common/AlertBadge.png"),
                1616, 467, 1859, 569, 1827, 459, 1871, 502).gameObject;

            // Center-top TipBanner (1:1 操作.psb bt group)
            var tipGroup = CreateAnchorContainer(hud, "TipBanner", new Vector2(0.5f, 1f));
            ui.tipBannerRoot = tipGroup.gameObject;
            var tipCanvasGroup = tipGroup.gameObject.AddComponent<CanvasGroup>();
            tipCanvasGroup.blocksRaycasts = false;
            tipCanvasGroup.interactable = false;
            ui.tipBannerGroup = tipCanvasGroup;
            PlaceTopCenterImage(tipGroup, "BannerBg", LoadSprite("Gameplay/TipBannerBg.png"), 537, 155, 1414, 314);
            PlaceTopCenterImage(tipGroup, "BannerMascot", LoadSprite("Gameplay/TipBannerMascot.png"), 514, 132, 699, 324);
            ui.tipBannerText = PlaceTopCenterText(tipGroup, "BannerText", "持续锻炼，击败更强的对手", 48, Color.white, 710, 212, 1375, 286, TextAnchor.MiddleCenter);
            AddThickOutline(ui.tipBannerText, Hex("113363"), 3f);
        }

        public static void BuildHudStatsPanel(Transform hud, BeachGameUi ui)
        {
            DestroyChildIfExists(hud, "StatsPanel");
            var statsGroup = CreateAnchorContainer(hud, "StatsPanel", new Vector2(0f, 1f));
            var trackSprite = LoadSprite("Common/StatProgressTrack.png");
            var fillSprite = LoadSprite("Common/StatProgressFill.png");
            var statDefs = new[]
            {
                ("ChestStat", "胸部", "Common/StatChestIcon.png", 67, 40, 380, 141, 48, 47, 141, 139, 150, 44, 232, 96, 238, 44, 368, 96, 144, 96, 351, 122, 146, 96, 349, 121),
                ("ArmsStat",  "手臂", "Common/StatArmsIcon.png",  66, 160, 379, 261, 49, 165, 144, 256, 150, 164, 232, 216, 238, 164, 368, 216, 144, 216, 351, 242, 146, 216, 349, 241),
                ("BackStat",  "背部", "Common/StatBackIcon.png",  66, 279, 379, 380, 45, 286, 143, 373, 150, 283, 232, 335, 238, 283, 368, 335, 144, 335, 351, 361, 146, 335, 349, 360),
                ("LegsStat",  "腿部", "Common/StatLegsIcon.png",  66, 398, 379, 499, 44, 404, 125, 501, 150, 402, 232, 454, 238, 402, 368, 454, 144, 454, 351, 480, 146, 454, 349, 479),
            };
            ui.statValueLabels = new Text[4];
            ui.statProgressFills = new Image[4];
            for (int i = 0; i < statDefs.Length; i++)
            {
                var d = statDefs[i];
                var row = CreateAnchorContainer(statsGroup, d.Item1, new Vector2(0f, 1f));
                PlaceTopLeftImage(row, "BarBg", LoadSprite("Common/StatBarBg.png"), d.Item4, d.Item5, d.Item6, d.Item7);
                PlaceTopLeftImage(row, "Icon", LoadSprite(d.Item3), d.Item8, d.Item9, d.Item10, d.Item11);
                var titleTxt = PlaceTopLeftText(row, "TitleText", d.Item2, 34, Hex("113363"), d.Item12, d.Item13, d.Item14, d.Item15, TextAnchor.MiddleLeft);
                titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
                titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                var valTxt = PlaceTopLeftText(row, "ValueText", "0", 34, Hex("FCE11E"), d.Item16, d.Item17, d.Item18, d.Item19, TextAnchor.MiddleLeft);
                valTxt.verticalOverflow = VerticalWrapMode.Overflow;
                valTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                AddThickOutline(valTxt, Hex("113363"), 2.5f);
                ui.statValueLabels[i] = valTxt;

                PlaceTopLeftImage(row, "ProgressTrack", trackSprite, d.Item20, d.Item21, d.Item22, d.Item23);
                var fill = PlaceTopLeftImage(row, "ProgressFill", fillSprite, d.Item24, d.Item25, d.Item26, d.Item27);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = 0f;
                ui.statProgressFills[i] = fill;
            }
        }

        private static Button BuildSideMenuButton(
            Transform parent, string name, Sprite icon, string caption,
            int bx0, int by0, int bx1, int by1,
            int ix0, int iy0, int ix1, int iy1,
            int tx0, int ty0, int tx1, int ty1)
        {
            var btn = PlaceTopRightButton(parent, name, LoadSprite("Common/SideMenuBtnBg.png"), bx0, by0, bx1, by1);
            PlaceInParentRelative(btn.transform, "Icon", icon, bx0, by0, bx1, by1, ix0, iy0, ix1, iy1);
            var lblRect = PlaceRectInParentRelative(btn.transform, "Label", bx0, by0, bx1, by1, tx0, ty0, tx1, ty1);
            CreateText(lblRect, caption, 46, Hex("113363"), TextAnchor.MiddleCenter);
            return btn;
        }

        private static void BuildRoamingControls(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateStretch(safeRoot, "RoamingControls");
            ui.roamingControlsRoot = root.gameObject;
            ui.roamingJoystick = BuildJoystick(root, "RoamingJoystick", ui.canvas);
            BuildRoamingJumpButton(root, ui);
        }

        public static void BuildRoamingJumpButton(RectTransform roamingControls, BeachGameUi ui)
        {
            if (roamingControls == null || ui == null) return;
            DestroyChildIfExists(roamingControls, "RoamingActionButtons");
            var rightActions = CreateAnchorContainer(roamingControls, "RoamingActionButtons", new Vector2(1f, 0f));
            // JumpBtn (操作.psb): bbox (1408, 622, 1691, 910)
            var jumpImg = PlaceBottomRightImage(rightActions, "JumpBtn", LoadSprite("Gameplay/JumpBtn.png"), 1408, 622, 1691, 910, raycast: true);
            ui.roamingJumpHold = jumpImg.gameObject.AddComponent<BeachHoldButton>();
        }

        private static BeachMobileStick BuildJoystick(Transform parent, string name, Canvas canvas)
        {
            // fx1 bbox: (312, 598, 643, 927), fx2 bbox: (392, 678, 564, 848)
            var holder = CreateAnchorContainer(parent, name, new Vector2(0f, 0f));
            var baseImg = PlaceBottomLeftImage(holder, "JoystickBase", LoadSprite("Common/JoystickBase.png"), 312, 598, 643, 927, raycast: true);
            var knobImg = PlaceInParentRelative(baseImg.transform, "JoystickKnob", LoadSprite("Common/JoystickKnob.png"),
                312, 598, 643, 927, 392, 678, 564, 848);

            var stick = baseImg.gameObject.AddComponent<BeachMobileStick>();
            var so = new SerializedObject(stick);
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.FindProperty("baseRing").objectReferenceValue = baseImg.rectTransform;
            so.FindProperty("knob").objectReferenceValue = knobImg.rectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return stick;
        }

        private static void BuildWorkoutPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateAnchorContainer(safeRoot, "WorkoutPanel", new Vector2(0.5f, 0f));
            ui.workoutRoot = root.gameObject;

            // DoubleBtn (2倍): bbox (1129, 570, 1327, 636) + AdBadge (1289, 560, 1328, 589)
            ui.doubleWorkoutButton = PlaceBottomCenterButton(root, "DoubleWorkoutBtn", LoadSprite("Gameplay/WorkoutDoubleBtnBg.png"), 1129, 570, 1327, 636);
            ui.doubleWorkoutAdBadge = PlaceInParentRelative(ui.doubleWorkoutButton.transform, "AdBadge", LoadSprite("Common/AdBadge.png"),
                1129, 570, 1327, 636, 1289, 560, 1328, 589).gameObject;
            var dblRect = CreateStretch(ui.doubleWorkoutButton.transform, "Label");
            ui.doubleWorkoutLabel = CreateText(dblRect, "2倍", 36, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(ui.doubleWorkoutLabel, Hex("0342A1"), 2.5f);

            // QuickWorkoutBtn (快速锻炼): bbox (623, 650, 1023, 787)
            var quickImg = PlaceBottomCenterImage(root, "QuickWorkoutBtn", LoadSprite("Gameplay/WorkoutQuickBtnBg.png"), 623, 650, 1023, 787, raycast: true);
            ui.quickWorkoutHold = quickImg.gameObject.AddComponent<BeachHoldButton>();
            PlaceInParentRelative(quickImg.transform, "LightningIcon", LoadSprite("Gameplay/WorkoutLightningIcon.png"),
                623, 650, 1023, 787, 670, 679, 728, 752);
            var quickLblRect = PlaceRectInParentRelative(quickImg.transform, "Label", 623, 650, 1023, 787, 730, 678, 990, 754);
            var quickLbl = CreateText(quickLblRect, "快速锻炼", 60, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(quickLbl, Hex("0F5C06"), 2.5f);

            // ExitWorkoutBtn (退出): bbox (1037, 665, 1361, 776)
            ui.exitWorkoutButton = PlaceBottomCenterButton(root, "ExitWorkoutBtn", LoadSprite("Gameplay/WorkoutExitBtnBg.png"), 1037, 665, 1361, 776);
            PlaceInParentRelative(ui.exitWorkoutButton.transform, "ExitIcon", LoadSprite("Gameplay/WorkoutExitIcon.png"),
                1037, 665, 1361, 776, 1105, 689, 1172, 750);
            var exitLblRect = PlaceRectInParentRelative(ui.exitWorkoutButton.transform, "Label", 1037, 665, 1361, 776, 1178, 686, 1310, 750);
            var exitLbl = CreateText(exitLblRect, "退出", 56, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(exitLbl, Hex("A20807"), 2.5f);

            // Progress bar: Track (417, 831, 1503, 944), Fill (452, 851, 1472, 909)
            PlaceBottomCenterImage(root, "ProgressTrack", LoadSprite("Gameplay/WorkoutProgressTrack.png"), 417, 831, 1503, 944);
            var fill = PlaceBottomCenterImage(root, "ProgressFill", LoadSprite("Gameplay/WorkoutProgressFill.png"), 452, 851, 1472, 909);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            ui.workoutProgressFill = fill;
        }

        private static void BuildCombatPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateStretch(safeRoot, "CombatPanel");
            ui.combatRoot = root.gameObject;

            var mobileControls = CreateStretch(root, "CombatMobileControls");
            ui.combatMobileControlsRoot = mobileControls.gameObject;
            ui.combatJoystick = BuildJoystick(mobileControls, "CombatJoystick", ui.canvas);

            var rightActions = CreateAnchorContainer(mobileControls, "CombatActionButtons", new Vector2(1f, 0f));
            // PunchBtn (qj): bbox (1465, 664, 1774, 973)
            var punchImg = PlaceBottomRightImage(rightActions, "PunchBtn", LoadSprite("Gameplay/PunchBtn.png"), 1465, 664, 1774, 973, raycast: true);
            ui.punchHold = punchImg.gameObject.AddComponent<BeachHoldButton>();

            // BlockBtn (fy): bbox (1199, 689, 1440, 930)
            var blockImg = PlaceBottomRightImage(rightActions, "BlockBtn", LoadSprite("Gameplay/BlockBtn.png"), 1199, 689, 1440, 930, raycast: true);
            ui.blockHold = blockImg.gameObject.AddComponent<BeachHoldButton>();
        }

        private static void BuildCountdownPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateCenterContainer(safeRoot, "CountdownPanel");
            ui.countdownRoot = root.gameObject;

            // Badge (db): bbox (705, 389, 1185, 747), Number '3': (903, 488, 995, 627)
            var badge = PlaceCenterImage(root, "CountdownBadge", LoadSprite("Gameplay/CountdownBadge.png"), 705, 389, 1185, 747);
            ui.countdownBadge = badge.rectTransform;
            var numRect = PlaceRectInParentRelative(badge.transform, "CountdownText", 705, 389, 1185, 747, 840, 450, 1050, 665);
            ui.countdownText = CreateText(numRect, "3", 180, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(ui.countdownText, Hex("021936"), 4f);
        }

        private static void BuildChallengePanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateStretch(safeRoot, "ChallengePanel");
            ui.challengeRoot = root.gameObject;

            var dim = CreateStretchImage(root, "DimOverlay", null, raycast: true);
            dim.color = new Color(0f, 0f, 0f, 0.5f);

            var modal = CreateCenterContainer(root, "ModalWindow");

            // Main frame & header & close (1:1 挑战.psb)
            PlaceCenterImage(modal, "PanelBg", LoadSprite("Challenge/PanelBg.png"), 371, 242, 1586, 1001, raycast: true);
            PlaceCenterImage(modal, "TitleArt", LoadSprite("Challenge/TitleArt.png"), 635, 51, 1383, 331);
            ui.challengeCloseButton = PlaceCenterButton(modal, "CloseBtn", LoadSprite("Common/CloseBtn.png"), 1486, 184, 1621, 314);

            // Left Stats column (组 22)
            var statsCol = CreateCenterContainer(modal, "StatsSection");
            PlaceCenterImage(statsCol, "StatsBoxBg", LoadSprite("Challenge/StatsBoxBg.png"), 441, 317, 730, 853);
            PlaceCenterImage(statsCol, "StatsHeaderBg", LoadSprite("Challenge/StatsHeaderBg.png"), 484, 295, 688, 369);
            var statsHdr = PlaceCenterText(statsCol, "StatsHeaderText", "属性", 40, Color.white, 484, 295, 688, 369, TextAnchor.MiddleCenter);
            AddThickOutline(statsHdr, Hex("113363"), 2.5f);

            var oppStats = new[]
            {
                ("ChestRow", "Common/StatChestIcon.png", 472, 388, 711, 462, 464, 388, 540, 463, 550, 398, 700, 452),
                ("ArmsRow", "Common/StatArmsIcon.png", 472, 496, 711, 570, 468, 495, 546, 569, 550, 506, 700, 560),
                ("BackRow", "Common/StatBackIcon.png", 472, 616, 711, 690, 462, 620, 541, 691, 550, 626, 700, 680),
                ("LegsRow", "Common/StatLegsIcon.png", 472, 730, 711, 804, 461, 731, 544, 817, 550, 740, 700, 794),
            };
            ui.challengeStatTexts = new Text[4];
            for (int i = 0; i < oppStats.Length; i++)
            {
                var s = oppStats[i];
                var row = CreateCenterContainer(statsCol, s.Item1);
                PlaceCenterImage(row, "RowBg", LoadSprite("Challenge/StatRowBg.png"), s.Item3, s.Item4, s.Item5, s.Item6);
                PlaceCenterImage(row, "Icon", LoadSprite(s.Item2), s.Item7, s.Item8, s.Item9, s.Item10);
                var val = PlaceCenterText(row, "ValueText", "0", 40, Hex("FCE11E"), s.Item11, s.Item12, s.Item13, s.Item14, TextAnchor.MiddleCenter);
                AddThickOutline(val, Hex("113363"), 2.5f);
                ui.challengeStatTexts[i] = val;
            }

            // Center Stage + Rewards column (组 23)
            var centerCol = CreateCenterContainer(modal, "OpponentSection");
            PlaceCenterImage(centerCol, "CenterBoxBg", LoadSprite("Challenge/CenterBoxBg.png"), 762, 315, 1278, 789);

            // Masked rounded stage preview
            var stageMask = PlaceCenterImage(centerCol, "StageMask", LoadSprite("Challenge/StageMask.png"), 762, 315, 1278, 659);
            var maskComp = stageMask.gameObject.AddComponent<Mask>();
            maskComp.showMaskGraphic = false;

            ui.challengeStageBackground = CreateStretchImage(stageMask.transform, "StageBackground", null);
            ui.challengeStageBackground.color = new Color(0.14f, 0.25f, 0.45f, 1f);
            ui.challengePortraitImage = CreateStretchImage(stageMask.transform, "OpponentPortrait", null);
            ui.challengePortraitImage.preserveAspect = true;
            var portraitRect = ui.challengePortraitImage.rectTransform;
            portraitRect.offsetMin = new Vector2(40f, 10f);
            portraitRect.offsetMax = new Vector2(-40f, -30f);

            PlaceCenterImage(centerCol, "StageBorder", LoadSprite("Challenge/StageBorder.png"), 762, 315, 1278, 659);
            PlaceCenterImage(centerCol, "NameHeaderBg", LoadSprite("Challenge/NameHeaderBg.png"), 876, 295, 1164, 369);
            ui.challengeNameText = PlaceCenterText(centerCol, "OpponentNameText", "对手", 40, Color.white, 876, 295, 1164, 369, TextAnchor.MiddleCenter);
            AddThickOutline(ui.challengeNameText, Hex("113363"), 2.5f);

            ui.challengePrevButton = PlaceCenterButton(centerCol, "PrevOpponentBtn", LoadSprite("Challenge/ArrowLeft.png"), 798, 451, 855, 523);
            ui.challengeNextButton = PlaceCenterButton(centerCol, "NextOpponentBtn", LoadSprite("Challenge/ArrowRight.png"), 1186, 451, 1243, 523);

            // Rewards bar (56b3931f...)
            PlaceCenterImage(centerCol, "RewardBoxBg", LoadSprite("Challenge/RewardBoxBg.png"), 811, 644, 1240, 774);
            PlaceCenterImage(centerCol, "CoinIcon", LoadSprite("Common/CoinIcon.png"), 854, 699, 916, 760);
            ui.challengeCoinRewardText = PlaceCenterText(centerCol, "CoinRewardText", "x50", 40, Hex("FCE11E"), 918, 700, 1020, 756, TextAnchor.MiddleLeft);
            AddThickOutline(ui.challengeCoinRewardText, Color.black, 2.5f);

            PlaceCenterImage(centerCol, "StarIcon", LoadSprite("Common/StarIcon.png"), 1065, 699, 1129, 758);
            ui.challengeStarRewardText = PlaceCenterText(centerCol, "StarRewardText", "x3", 40, Color.white, 1131, 700, 1225, 756, TextAnchor.MiddleLeft);
            AddThickOutline(ui.challengeStarRewardText, Color.black, 2.5f);

            // Right Progress column (组 21)
            var progCol = CreateCenterContainer(modal, "ProgressSection");
            PlaceCenterImage(progCol, "ProgressBoxBg", LoadSprite("Challenge/ProgressBoxBg.png"), 1316, 317, 1512, 853);
            PlaceCenterImage(progCol, "ProgressHeaderBg", LoadSprite("Challenge/ProgressHeaderBg.png"), 1327, 295, 1501, 369);
            var progHdr = PlaceCenterText(progCol, "ProgressHeaderText", "进度", 40, Color.white, 1327, 295, 1501, 369, TextAnchor.MiddleCenter);
            AddThickOutline(progHdr, Hex("113363"), 2.5f);
            PlaceCenterImage(progCol, "ProgressLine", LoadSprite("Challenge/ProgressLine.png"), 1403, 419, 1424, 804);

            ui.challengeNodeActiveSprite = LoadSprite("Challenge/ProgressNodeActive.png");
            ui.challengeNodePendingSprite = LoadSprite("Challenge/ProgressNodePending.png");
            ui.challengeNodeCrownSprite = LoadSprite("Challenge/ProgressCrown.png");
            ui.challengeProgressNodes = new Image[5];
            // Index 0..3 bottom-to-top nodes, index 4 = top crown
            ui.challengeProgressNodes[0] = PlaceCenterImage(progCol, "ProgressNode1", ui.challengeNodePendingSprite, 1389, 764, 1440, 815);
            ui.challengeProgressNodes[1] = PlaceCenterImage(progCol, "ProgressNode2", ui.challengeNodePendingSprite, 1389, 674, 1440, 725);
            ui.challengeProgressNodes[2] = PlaceCenterImage(progCol, "ProgressNode3", ui.challengeNodePendingSprite, 1389, 584, 1440, 635);
            ui.challengeProgressNodes[3] = PlaceCenterImage(progCol, "ProgressNode4", ui.challengeNodeActiveSprite, 1386, 487, 1444, 545);
            ui.challengeProgressNodes[4] = PlaceCenterImage(progCol, "ProgressCrown", ui.challengeNodeCrownSprite, 1372, 387, 1457, 448);

            // Start Challenge button (icon group)
            ui.challengeStartButton = PlaceCenterButton(modal, "StartChallengeBtn", LoadSprite("Common/ActionYellowBtn.png"), 798, 818, 1226, 924);
            PlaceInParentRelative(ui.challengeStartButton.transform, "SparkAccent", LoadSprite("Challenge/FightBtnSpark.png"),
                798, 818, 1226, 924, 1177, 790, 1256, 887);
            var startLbl = CreateText(CreateStretch(ui.challengeStartButton.transform, "Label"), "开始挑战", 53, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(startLbl, Hex("001445"), 4f);
        }

        private static void BuildBoxerPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateStretch(safeRoot, "BoxerPanel");
            ui.boxerRoot = root.gameObject;

            var dim = CreateStretchImage(root, "DimOverlay", null, raycast: true);
            dim.color = new Color(0f, 0f, 0f, 0.5f);

            var modal = CreateCenterContainer(root, "ModalWindow");

            // Frame, title, subtitle, close (1:1 选角.psb)
            PlaceCenterImage(modal, "PanelBg", LoadSprite("Boxer/PanelBg.png"), 281, 242, 1621, 1001, raycast: true);
            PlaceCenterImage(modal, "SubtitleBanner", LoadSprite("Boxer/SubtitleBanner.png"), 674, 247, 1236, 328);
            PlaceCenterImage(modal, "TitleArt", LoadSprite("Boxer/TitleArt.png"), 659, 80, 1256, 277);
            ui.boxerCloseButton = PlaceCenterButton(modal, "CloseBtn", LoadSprite("Common/CloseBtn.png"), 1505, 213, 1640, 343);

            // Left 2x3 card grid
            var gridSection = CreateCenterContainer(modal, "RosterGridSection");
            PlaceCenterImage(gridSection, "GridBoxBg", LoadSprite("Boxer/GridBoxBg.png"), 339, 297, 1038, 927);

            ui.boxerCardEquippedSprite = LoadSprite("Boxer/CardEquippedBg.png");
            ui.boxerCardNormalSprite = LoadSprite("Boxer/CardNormalBg.png");
            ui.boxerPortraitSprites = new[]
            {
                LoadSprite("Boxer/BoxerPortraitCody.png"),
                LoadSprite("Boxer/BoxerPortraitDion.png"),
                LoadSprite("Boxer/BoxerPortraitNova.png"),
                LoadSprite("Boxer/BoxerPortraitZara.png"),
                LoadSprite("Boxer/BoxerPortraitSerena.png"),
                LoadSprite("Boxer/BoxerPortraitYui.png"),
            };
            ui.boxerFullSprites = new[]
            {
                LoadSprite("Boxer/BoxerFullCody.png"),
                LoadSprite("Boxer/BoxerFullDion.png"),
                LoadSprite("Boxer/BoxerFullNova.png"),
                LoadSprite("Boxer/BoxerFullZara.png"),
                LoadSprite("Boxer/BoxerFullSerena.png"),
                LoadSprite("Boxer/BoxerFullYui.png"),
            };
            var redFull = ui.boxerFullSprites[0];

            var cardBoxes = new[]
            {
                (358, 327, 573, 598),
                (581, 327, 796, 598),
                (803, 327, 1018, 598),
                (358, 625, 573, 896),
                (581, 625, 796, 896),
                (803, 625, 1018, 896),
            };
            ui.boxerCardButtons = new Button[6];
            ui.boxerCardBackgrounds = new Image[6];
            ui.boxerCardPortraits = new Image[6];
            ui.boxerCardNames = new Text[6];
            ui.boxerCardCheckmarks = new GameObject[6];

            for (int i = 0; i < 6; i++)
            {
                var (x0, y0, x1, y1) = cardBoxes[i];
                var btn = PlaceCenterButton(gridSection, $"BoxerCard{i + 1}", ui.boxerCardNormalSprite, x0, y0, x1, y1);
                ui.boxerCardButtons[i] = btn;
                ui.boxerCardBackgrounds[i] = btn.GetComponent<Image>();

                var portrait = PlaceInParentRelative(btn.transform, "Portrait", ui.boxerPortraitSprites[i],
                    x0, y0, x1, y1, x0 + 18, y0 + 18, x1 - 18, y1 - 65);
                portrait.preserveAspect = true;
                ui.boxerCardPortraits[i] = portrait;

                PlaceInParentRelative(btn.transform, "NamePill", LoadSprite("Boxer/CardNamePill.png"),
                    x0, y0, x1, y1, x0 + 30, y1 - 74, x1 - 29, y1 - 26);
                var nameRect = PlaceRectInParentRelative(btn.transform, "NameText",
                    x0, y0, x1, y1, x0 + 30, y1 - 74, x1 - 29, y1 - 26);
                ui.boxerCardNames[i] = CreateText(nameRect, BeachChineseText.Translate(BeachPeripheralRules.GetTrainerName(i)), 34, Color.white, TextAnchor.MiddleCenter);

                var check = PlaceInParentRelative(btn.transform, "EquippedCheck", LoadSprite("Common/CheckmarkBadge.png"),
                    x0, y0, x1, y1, x1 - 43, y0 - 6, x1 + 5, y0 + 43);
                ui.boxerCardCheckmarks[i] = check.gameObject;
            }

            // Selection corner brackets
            var selFrame = PlaceCenterImage(gridSection, "SelectionFrame", LoadSprite("Boxer/CardSelectFrame.png"), 578, 329, 799, 599);
            ui.boxerSelectionFrame = selFrame.rectTransform;

            // Right preview column
            var previewSection = CreateCenterContainer(modal, "PreviewSection");
            PlaceCenterImage(previewSection, "PreviewPanelBg", LoadSprite("Boxer/PreviewPanelBg.png"), 1053, 301, 1566, 929);
            ui.boxerPreviewImage = PlaceCenterImage(previewSection, "PreviewCharacter", redFull, 1185, 333, 1450, 797);
            ui.boxerPreviewImage.preserveAspect = true;
            PlaceCenterImage(previewSection, "PreviewSparkle", LoadSprite("Boxer/BoxerFullSparkle.png"), 1234, 392, 1424, 479);

            // Name badge (组 14)
            var nameBadge = CreateCenterContainer(previewSection, "NameBadge");
            PlaceCenterImage(nameBadge, "BadgeBg", LoadSprite("Boxer/PreviewNameBadgeBg.png"), 1386, 345, 1555, 432);
            ui.boxerPreviewNameText = PlaceCenterText(nameBadge, "BoxerNameText", "科迪", 40, Color.white, 1386, 345, 1555, 400, TextAnchor.MiddleCenter);
            AddThickOutline(ui.boxerPreviewNameText, Hex("113363"), 3f);
            ui.boxerPreviewTypeText = PlaceCenterText(nameBadge, "BoxerTypeText", "力量型", 18, Hex("113363"), 1396, 398, 1545, 428, TextAnchor.MiddleCenter);

            // Stat bonuses card (组 37)
            var bonusSection = CreateCenterContainer(previewSection, "BonusSection");
            PlaceCenterImage(bonusSection, "BonusPanelBg", LoadSprite("Boxer/BonusPanelBg.png"), 1076, 657, 1543, 822);
            var bonusDefs = new[]
            {
                ("AttackBonus", "攻击", "Boxer/BonusAttackIcon.png", 1118, 702, 1164, 747, 1088, 748, 1190, 778, 1088, 780, 1190, 810),
                ("SpeedBonus", "速度", "Boxer/BonusSpeedIcon.png", 1240, 706, 1276, 750, 1210, 748, 1312, 778, 1210, 780, 1312, 810),
                ("DefenseBonus", "防御", "Boxer/BonusDefenseIcon.png", 1348, 706, 1396, 749, 1322, 748, 1424, 778, 1322, 780, 1424, 810),
                ("HealthBonus", "生命", "Boxer/BonusHealthIcon.png", 1461, 703, 1506, 748, 1434, 748, 1536, 778, 1434, 780, 1536, 810),
            };
            ui.boxerBonusTexts = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var b = bonusDefs[i];
                var item = CreateCenterContainer(bonusSection, b.Item1);
                PlaceCenterImage(item, "Icon", LoadSprite(b.Item3), b.Item4, b.Item5, b.Item6, b.Item7);
                PlaceCenterText(item, "Label", b.Item2, 24, Hex("113363"), b.Item8, b.Item9, b.Item10, b.Item11, TextAnchor.MiddleCenter);
                ui.boxerBonusTexts[i] = PlaceCenterText(item, "ValueText", "+5%", 24, Hex("113363"), b.Item12, b.Item13, b.Item14, b.Item15, TextAnchor.MiddleCenter);
            }

            // Bottom action button (选择出战)
            ui.boxerActionButton = PlaceCenterButton(previewSection, "SelectBoxerBtn", LoadSprite("Common/ActionYellowBtn.png"), 1103, 841, 1531, 947);
            ui.boxerActionLabel = CreateText(CreateStretch(ui.boxerActionButton.transform, "Label"), "选择出战", 53, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(ui.boxerActionLabel, Hex("001445"), 4f);
        }

        private static void BuildSignInPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateStretch(safeRoot, "SignInPanel");
            ui.signInRoot = root.gameObject;

            var dim = CreateStretchImage(root, "DimOverlay", null, raycast: true);
            dim.color = new Color(0f, 0f, 0f, 0.5f);

            var modal = CreateCenterContainer(root, "ModalWindow");
            PlaceCenterImage(modal, "PanelBg", LoadSprite("SignIn/PanelBg.png"), 291, 216, 1626, 975, raycast: true);
            PlaceCenterImage(modal, "TitleArt", LoadSprite("SignIn/TitleArt.png"), 546, 21, 1361, 317);
            ui.signInCloseButton = PlaceCenterButton(modal, "CloseBtn", LoadSprite("Common/CloseBtn.png"), 1495, 187, 1630, 317);

            ui.signInCardClaimedSprite = LoadSprite("SignIn/CardClaimedBg.png");
            ui.signInCardNormalSprite = LoadSprite("SignIn/CardNormalBg.png");
            var coinsPile = LoadSprite("SignIn/CoinsPileIcon.png");
            var starsPile = LoadSprite("SignIn/StarsPileIcon.png");
            var pinkBust = LoadSprite("Boxer/BoxerPortraitPink.png");
            var lockOverlaySprite = LoadSprite("SignIn/CardLockedOverlay.png");

            ui.signInDayButtons = new Button[7];
            ui.signInDayBackgrounds = new Image[7];
            ui.signInDayCheckmarks = new GameObject[7];
            ui.signInDayLockOverlays = new GameObject[7];
            ui.signInDayTitleTexts = new Text[7];
            ui.signInDayRewardTexts = new Text[7];

            var dayCards = new[]
            {
                (365, 290, 600, 582, "第一天", "拳手体验", pinkBust, 402, 366, 578, 554),
                (632, 290, 867, 582, "第二天", "3.00k", coinsPile, 686, 386, 818, 491),
                (899, 290, 1134, 582, "第三天", "60", starsPile, 933, 386, 1100, 498),
                (365, 601, 600, 893, "第四天", "10.00k", coinsPile, 419, 697, 551, 802),
                (632, 601, 867, 893, "第五天", "100", starsPile, 666, 697, 833, 809),
                (899, 601, 1134, 893, "第六天", "1000k", coinsPile, 953, 697, 1085, 802),
            };

            var daysGroup = CreateCenterContainer(modal, "DaysGrid");
            for (int i = 0; i < 6; i++)
            {
                var d = dayCards[i];
                var btn = PlaceCenterButton(daysGroup, $"DayCard{i + 1}", ui.signInCardNormalSprite, d.Item1, d.Item2, d.Item3, d.Item4);
                ui.signInDayButtons[i] = btn;
                ui.signInDayBackgrounds[i] = btn.GetComponent<Image>();

                var titleRect = PlaceRectInParentRelative(btn.transform, "DayTitleText", d.Item1, d.Item2, d.Item3, d.Item4,
                    d.Item1 + 18, d.Item2 + 20, d.Item3 - 17, d.Item2 + 76);
                ui.signInDayTitleTexts[i] = CreateText(titleRect, d.Item5, 34, i == 0 ? Hex("6C1B0D") : Hex("043B8A"), TextAnchor.MiddleCenter);

                var iconImg = PlaceInParentRelative(btn.transform, "RewardIcon", d.Item7, d.Item1, d.Item2, d.Item3, d.Item4,
                    d.Item8, d.Item9, d.Item10, d.Item11);
                iconImg.preserveAspect = true;

                var rewardRect = PlaceRectInParentRelative(btn.transform, "RewardText", d.Item1, d.Item2, d.Item3, d.Item4,
                    d.Item1 + 18, d.Item4 - 76, d.Item3 - 17, d.Item4 - 20);
                var rewTxt = CreateText(rewardRect, d.Item6, 34, i == 0 ? Hex("B45D4E") : Color.white, TextAnchor.MiddleCenter);
                if (i == 0) AddOutline(rewTxt, Color.white, 1.5f);
                ui.signInDayRewardTexts[i] = rewTxt;

                var lockImg = PlaceInParentRelative(btn.transform, "LockedOverlay", lockOverlaySprite, d.Item1, d.Item2, d.Item3, d.Item4,
                    d.Item1 + 10, d.Item2 + 10, d.Item3 - 10, d.Item4 - 10);
                ui.signInDayLockOverlays[i] = lockImg.gameObject;

                var check = PlaceInParentRelative(btn.transform, "ClaimedCheck", LoadSprite("Common/CheckmarkBadge.png"), d.Item1, d.Item2, d.Item3, d.Item4,
                    d.Item3 - 41, d.Item2 - 4, d.Item3 + 7, d.Item2 + 45);
                ui.signInDayCheckmarks[i] = check.gameObject;
            }

            // Day 7 grand prize card: bbox (1165, 319, 1564, 883)
            var day7Btn = PlaceCenterButton(daysGroup, "DayCard7", LoadSprite("SignIn/Day7CardBg.png"), 1165, 319, 1564, 883);
            ui.signInDayButtons[6] = day7Btn;
            ui.signInDayBackgrounds[6] = day7Btn.GetComponent<Image>();

            var d7TitleRect = PlaceRectInParentRelative(day7Btn.transform, "DayTitleText", 1165, 319, 1564, 883, 1210, 340, 1520, 422);
            ui.signInDayTitleTexts[6] = CreateText(d7TitleRect, "第七天", 60, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(ui.signInDayTitleTexts[6], Hex("64251E"), 4f);

            PlaceInParentRelative(day7Btn.transform, "GrandPrizeCharacter", LoadSprite("SignIn/Day7Character.png"),
                1165, 319, 1564, 883, 1285, 426, 1466, 781);
            PlaceInParentRelative(day7Btn.transform, "GrandPrizeRibbon", LoadSprite("SignIn/Day7Ribbon.png"),
                1165, 319, 1564, 883, 1178, 720, 1552, 837);
            ui.signInDayLockOverlays[6] = CreateStretchImage(day7Btn.transform, "LockedOverlay", LoadSprite("SignIn/Day7LockedOverlay.png")).gameObject;
            ui.signInDayCheckmarks[6] = PlaceInParentRelative(day7Btn.transform, "ClaimedCheck", LoadSprite("Common/CheckmarkBadge.png"),
                1165, 319, 1564, 883, 1510, 310, 1574, 374).gameObject;

            // Selection bracket frame
            ui.signInSelectionFrame = PlaceCenterImage(daysGroup, "SelectionFrame", LoadSprite("SignIn/CardSelectFrame.png"), 628, 290, 874, 591).rectTransform;

            // Bottom buttons: Claim (领取) & DoubleClaim (双倍领取 + AdBadge)
            ui.signInClaimButton = PlaceCenterButton(modal, "ClaimBtn", LoadSprite("Common/YellowBtn.png"), 555, 908, 936, 1036);
            var claimLbl = CreateText(CreateStretch(ui.signInClaimButton.transform, "Label"), "领取", 60, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(claimLbl, Hex("6D2701"), 4f);

            ui.signInDoubleClaimButton = PlaceCenterButton(modal, "DoubleClaimBtn", LoadSprite("Common/BlueBtn.png"), 981, 908, 1362, 1036);
            var dblClaimLbl = CreateText(CreateStretch(ui.signInDoubleClaimButton.transform, "Label"), "双倍领取", 60, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(dblClaimLbl, Hex("0342A1"), 4f);
            PlaceInParentRelative(ui.signInDoubleClaimButton.transform, "AdBadge", LoadSprite("Common/AdBadge.png"),
                981, 908, 1362, 1036, 1311, 904, 1350, 933);
        }

        private static void BuildResultPanel(RectTransform safeRoot, BeachGameUi ui)
        {
            var root = CreateStretch(safeRoot, "ResultPanel");
            ui.resultRoot = root.gameObject;

            var dim = CreateStretchImage(root, "DimOverlay", null, raycast: true);
            dim.color = new Color(0f, 0f, 0f, 0.5f);

            var modal = CreateCenterContainer(root, "ModalWindow");

            ui.victoryPanelSprite = LoadSprite("Result/VictoryPanelBg.png");
            ui.defeatPanelSprite = LoadSprite("Result/DefeatPanelBg.png");
            ui.victoryTitleSprite = LoadSprite("Result/VictoryTitleArt.png");
            ui.defeatTitleSprite = LoadSprite("Result/DefeatTitleArt.png");
            ui.victoryPodiumSprite = LoadSprite("Result/PodiumVictory.png");
            ui.defeatPodiumSprite = LoadSprite("Result/PodiumDefeat.png");
            ui.victoryStatusSprite = LoadSprite("Result/VictoryStatusBadge.png");
            ui.defeatStatusSprite = LoadSprite("Result/DefeatStatusBadge.png");
            ui.yellowBtnSprite = LoadSprite("Common/YellowBtn.png");
            ui.blueBtnSprite = LoadSprite("Common/BlueBtn.png");

            // Left podium + boxer + confetti (组 33)
            var podiumGroup = CreateCenterContainer(modal, "PodiumSection");
            ui.resultPodiumImage = PlaceCenterImage(podiumGroup, "PodiumBase", ui.victoryPodiumSprite, 135, 600, 769, 1023);
            PlaceCenterImage(podiumGroup, "BoxerPose", LoadSprite("Result/BoxerResultPose.png"), 296, 232, 585, 760);
            ui.resultConfetti = PlaceCenterImage(podiumGroup, "VictoryConfetti", LoadSprite("Result/VictoryConfetti.png"), 228, 192, 587, 350).gameObject;

            // Right result card (1:1 胜利.psb / 失败.psb)
            ui.resultPanelBg = PlaceCenterImage(modal, "ResultCardBg", ui.victoryPanelSprite, 660, 200, 1665, 959, raycast: true);
            ui.resultTitleArt = PlaceCenterImage(modal, "TitleArt", ui.victoryTitleSprite, 714, 20, 1584, 294);
            ui.resultTitleArt.preserveAspect = true;
            ui.resultCloseButton = PlaceCenterButton(modal, "CloseBtn", LoadSprite("Common/CloseBtn.png"), 1559, 163, 1694, 293);

            // Record section (组 29)
            var recordGroup = CreateCenterContainer(modal, "RecordSection");
            PlaceCenterImage(recordGroup, "RecordBoxBg", LoadSprite("Result/RecordBoxBg.png"), 763, 322, 1564, 574);
            PlaceCenterImage(recordGroup, "RecordHeaderBg", LoadSprite("Result/SectionHeaderBg.png"), 1017, 303, 1307, 378);
            PlaceCenterText(recordGroup, "RecordHeaderText", "战绩", 46, Color.white, 1017, 303, 1307, 378, TextAnchor.MiddleCenter);

            var avatarFrame = PlaceCenterImage(recordGroup, "OpponentAvatarFrame", LoadSprite("Result/AvatarFrame.png"), 825, 409, 943, 528);
            var avatarImg = CreateStretchImage(avatarFrame.transform, "OpponentAvatar", null);
            avatarImg.preserveAspect = true;
            avatarImg.rectTransform.offsetMin = new Vector2(8f, 8f);
            avatarImg.rectTransform.offsetMax = new Vector2(-8f, -8f);
            ui.resultOpponentAvatar = avatarImg;

            PlaceCenterImage(recordGroup, "DividerLine", LoadSprite("Result/DividerLine.png"), 975, 404, 979, 523);
            ui.resultOpponentLabel = PlaceCenterText(recordGroup, "OpponentNameText", "击败对手：拉梅兹", 40, Hex("37383C"), 1000, 396, 1510, 454, TextAnchor.MiddleCenter);
            ui.resultStatusBadge = PlaceCenterImage(recordGroup, "StatusBadge", ui.victoryStatusSprite, 1084, 462, 1341, 526);

            // Reward section (组 30)
            var rewardGroup = CreateCenterContainer(modal, "RewardSection");
            ui.resultRewardSection = rewardGroup.gameObject;
            PlaceCenterImage(rewardGroup, "RewardBoxBg", LoadSprite("Result/RewardBoxBg.png"), 763, 612, 1564, 814);
            PlaceCenterImage(rewardGroup, "RewardHeaderBg", LoadSprite("Result/SectionHeaderBg.png"), 1017, 593, 1307, 668);
            PlaceCenterText(rewardGroup, "RewardHeaderText", "奖励", 46, Color.white, 1017, 593, 1307, 668, TextAnchor.MiddleCenter);

            PlaceCenterImage(rewardGroup, "CoinIcon", LoadSprite("Common/CoinIcon.png"), 876, 678, 970, 771);
            ui.resultCoinValueText = PlaceCenterText(rewardGroup, "CoinValueText", "x500", 56, Hex("37383C"), 984, 686, 1210, 762, TextAnchor.MiddleLeft);

            PlaceCenterImage(rewardGroup, "StarIcon", LoadSprite("Common/StarIcon.png"), 1228, 680, 1325, 769);
            ui.resultStarValueText = PlaceCenterText(rewardGroup, "StarValueText", "x3", 56, Hex("37383C"), 1332, 686, 1520, 762, TextAnchor.MiddleLeft);

            // Bottom buttons: ContinueBtn (继续游戏) + RestartBtn (重新开始 / 重新游戏)
            ui.resultContinueButton = PlaceCenterButton(modal, "ContinueBtn", LoadSprite("Common/YellowBtn.png"), 759, 853, 1140, 981);
            var contLbl = CreateText(CreateStretch(ui.resultContinueButton.transform, "Label"), "继续游戏", 60, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(contLbl, Hex("6D2701"), 4f);

            ui.resultRestartButton = PlaceCenterButton(modal, "RestartBtn", LoadSprite("Common/BlueBtn.png"), 1185, 853, 1566, 981);
            ui.resultRestartImage = ui.resultRestartButton.GetComponent<Image>();
            ui.resultRestartRect = (RectTransform)ui.resultRestartButton.transform;
            ui.resultRestartLabel = CreateText(CreateStretch(ui.resultRestartButton.transform, "Label"), "重新开始", 60, Color.white, TextAnchor.MiddleCenter);
            AddThickOutline(ui.resultRestartLabel, Hex("0342A1"), 4f);
            ui.victoryRestartPosition = CenterPos(1185, 853, 1566, 981);
            ui.defeatRestartPosition = CenterPos(972, 853, 1353, 981);
        }

        private static GameObject BuildWorldPlayerTitle(Transform playerRoot, Camera cam)
        {
            DestroyChildIfExists(playerRoot, "Original title canvas");
            DestroyChildIfExists(playerRoot, "PlayerTitleCanvas");

            var go = new GameObject("PlayerTitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(BeachNameBillboard));
            go.transform.SetParent(playerRoot, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            var rect = (RectTransform)go.transform;
            rect.localPosition = new Vector3(0f, 2.70f, 0f);
            rect.localScale = Vector3.one * 0.011f;
            rect.sizeDelta = new Vector2(360f, 140f);

            var bb = go.GetComponent<BeachNameBillboard>();
            bb.View = cam;

            var lvRect = CreateCenterRect(rect, "LevelText", new Vector2(0f, 26f), new Vector2(340f, 52f));
            var lv = CreateText(lvRect, "lv1", 38, Hex("FFE03F"), TextAnchor.MiddleCenter);
            AddThickOutline(lv, Hex("113363"), 3f);

            var nameRect = CreateCenterRect(rect, "NameText", new Vector2(0f, -22f), new Vector2(340f, 56f));
            var nm = CreateText(nameRect, "夏洛克", 38, Hex("FFE03F"), TextAnchor.MiddleCenter);
            AddThickOutline(nm, Hex("113363"), 3f);
            return go;
        }

        private static void BuildWorldFighterBars(
            Transform fighterRoot, Camera cam, string name,
            out GameObject barsRoot, out Image hpFill, out Text hpText, out Image stFill, out Text stText)
        {
            DestroyChildIfExists(fighterRoot, "Original combat bars");
            DestroyChildIfExists(fighterRoot, name);

            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(BeachNameBillboard));
            go.transform.SetParent(fighterRoot, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            var rect = (RectTransform)go.transform;
            rect.localPosition = new Vector3(0f, 2.35f, 0f);
            rect.localScale = Vector3.one * 0.0052f;
            rect.sizeDelta = new Vector2(430f, 130f);

            var bb = go.GetComponent<BeachNameBillboard>();
            bb.View = cam;

            // Health bar row (1:1 玩法.psb smz)
            var hpRow = CreateCenterRect(rect, "HealthBar", new Vector2(0f, 30f), new Vector2(408f, 60f));
            var hpTrack = CreateCenterRect(hpRow, "HealthTrack", new Vector2(26f, 0f), new Vector2(356f, 49f)).gameObject.AddComponent<Image>();
            hpTrack.sprite = LoadSprite("Gameplay/HealthTrack.png");
            hpTrack.raycastTarget = false;
            hpFill = CreateCenterRect(hpTrack.transform, "HealthFill", Vector2.zero, new Vector2(339f, 35f)).gameObject.AddComponent<Image>();
            hpFill.sprite = LoadSprite("Gameplay/HealthFill.png");
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFill.fillAmount = 1f;
            hpFill.raycastTarget = false;
            hpText = CreateText(CreateStretch(hpTrack.transform, "HealthValueText"), "100/100", 28, Color.white, TextAnchor.MiddleCenter);
            AddOutline(hpText, Hex("113363"), 1.5f);
            var hpIcon = CreateCenterRect(hpRow, "HealthIcon", new Vector2(-176f, 0f), new Vector2(56f, 60f)).gameObject.AddComponent<Image>();
            hpIcon.sprite = LoadSprite("Gameplay/HealthIcon.png");
            hpIcon.preserveAspect = true;
            hpIcon.raycastTarget = false;

            // Stamina bar row (1:1 玩法.psb tl)
            var stRow = CreateCenterRect(rect, "StaminaBar", new Vector2(0f, -30f), new Vector2(408f, 62f));
            var stTrack = CreateCenterRect(stRow, "StaminaTrack", new Vector2(26f, 0f), new Vector2(356f, 49f)).gameObject.AddComponent<Image>();
            stTrack.sprite = LoadSprite("Gameplay/StaminaTrack.png");
            stTrack.raycastTarget = false;
            stFill = CreateCenterRect(stTrack.transform, "StaminaFill", Vector2.zero, new Vector2(340f, 34f)).gameObject.AddComponent<Image>();
            stFill.sprite = LoadSprite("Gameplay/StaminaFill.png");
            stFill.type = Image.Type.Filled;
            stFill.fillMethod = Image.FillMethod.Horizontal;
            stFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            stFill.fillAmount = 1f;
            stFill.raycastTarget = false;
            stText = CreateText(CreateStretch(stTrack.transform, "StaminaValueText"), "100/100", 28, Color.white, TextAnchor.MiddleCenter);
            AddOutline(stText, Hex("113363"), 1.5f);
            var stIcon = CreateCenterRect(stRow, "StaminaIcon", new Vector2(-176f, 0f), new Vector2(52f, 62f)).gameObject.AddComponent<Image>();
            stIcon.sprite = LoadSprite("Gameplay/StaminaIcon.png");
            stIcon.preserveAspect = true;
            stIcon.raycastTarget = false;

            go.SetActive(false);
            barsRoot = go;
        }

        // ==================== Helpers ====================

        private static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        private static RectTransform CreateStretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateAnchorContainer(Transform parent, string name, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateCenterContainer(Transform parent, string name)
            => CreateAnchorContainer(parent, name, new Vector2(0.5f, 0.5f));

        private static RectTransform CreateCenterRect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateStretchImage(Transform parent, string name, Sprite sprite, bool raycast = false)
        {
            var rect = CreateStretch(parent, name);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = raycast;
            return img;
        }

        private static Vector2 CenterPos(int x0, int y0, int x1, int y1)
            => new Vector2((x0 + x1) * 0.5f - 960f, 540f - (y0 + y1) * 0.5f);

        private static RectTransform PlaceByBBox(Transform parent, string name, Vector2 anchor, int x0, int y0, int x1, int y1)
        {
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            float w = x1 - x0;
            float h = y1 - y0;
            float refX = anchor.x * 1920f;
            float refY = (1f - anchor.y) * 1080f;
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(cx - refX, refY - cy);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static RectTransform PlaceRectInParentRelative(Transform parent, string name,
            int px0, int py0, int px1, int py1, int x0, int y0, int x1, int y1)
        {
            float pcx = (px0 + px1) * 0.5f;
            float pcy = (py0 + py1) * 0.5f;
            float cx = (x0 + x1) * 0.5f;
            float cy = (y0 + y1) * 0.5f;
            return CreateCenterRect(parent, name, new Vector2(cx - pcx, pcy - cy), new Vector2(x1 - x0, y1 - y0));
        }

        private static Image PlaceInParentRelative(Transform parent, string name, Sprite sprite,
            int px0, int py0, int px1, int py1, int x0, int y0, int x1, int y1, bool raycast = false)
        {
            var rect = PlaceRectInParentRelative(parent, name, px0, py0, px1, py1, x0, y0, x1, y1);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = raycast;
            return img;
        }

        private static Image PlaceImageAnchored(Transform parent, string name, Sprite sprite, Vector2 anchor, int x0, int y0, int x1, int y1, bool raycast = false)
        {
            var rect = PlaceByBBox(parent, name, anchor, x0, y0, x1, y1);
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.raycastTarget = raycast;
            return img;
        }

        private static Button PlaceButtonAnchored(Transform parent, string name, Sprite sprite, Vector2 anchor, int x0, int y0, int x1, int y1)
        {
            var img = PlaceImageAnchored(parent, name, sprite, anchor, x0, y0, x1, y1, raycast: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static Text PlaceTextAnchored(Transform parent, string name, string value, int fontSize, Color color, Vector2 anchor, int x0, int y0, int x1, int y1, TextAnchor align)
        {
            var rect = PlaceByBBox(parent, name, anchor, x0, y0, x1, y1);
            return CreateText(rect, value, fontSize, color, align);
        }

        private static Image PlaceCenterImage(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1, bool raycast = false)
            => PlaceImageAnchored(p, n, s, new Vector2(0.5f, 0.5f), x0, y0, x1, y1, raycast);
        private static Button PlaceCenterButton(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1)
            => PlaceButtonAnchored(p, n, s, new Vector2(0.5f, 0.5f), x0, y0, x1, y1);
        private static Text PlaceCenterText(Transform p, string n, string v, int fs, Color c, int x0, int y0, int x1, int y1, TextAnchor a)
            => PlaceTextAnchored(p, n, v, fs, c, new Vector2(0.5f, 0.5f), x0, y0, x1, y1, a);

        private static Image PlaceTopLeftImage(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1, bool raycast = false)
            => PlaceImageAnchored(p, n, s, new Vector2(0f, 1f), x0, y0, x1, y1, raycast);
        private static Button PlaceTopLeftButton(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1)
            => PlaceButtonAnchored(p, n, s, new Vector2(0f, 1f), x0, y0, x1, y1);
        private static Text PlaceTopLeftText(Transform p, string n, string v, int fs, Color c, int x0, int y0, int x1, int y1, TextAnchor a)
            => PlaceTextAnchored(p, n, v, fs, c, new Vector2(0f, 1f), x0, y0, x1, y1, a);

        private static Button PlaceTopRightButton(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1)
            => PlaceButtonAnchored(p, n, s, new Vector2(1f, 1f), x0, y0, x1, y1);

        private static Image PlaceTopCenterImage(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1, bool raycast = false)
            => PlaceImageAnchored(p, n, s, new Vector2(0.5f, 1f), x0, y0, x1, y1, raycast);
        private static Text PlaceTopCenterText(Transform p, string n, string v, int fs, Color c, int x0, int y0, int x1, int y1, TextAnchor a)
            => PlaceTextAnchored(p, n, v, fs, c, new Vector2(0.5f, 1f), x0, y0, x1, y1, a);

        private static Image PlaceBottomCenterImage(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1, bool raycast = false)
            => PlaceImageAnchored(p, n, s, new Vector2(0.5f, 0f), x0, y0, x1, y1, raycast);
        private static Button PlaceBottomCenterButton(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1)
            => PlaceButtonAnchored(p, n, s, new Vector2(0.5f, 0f), x0, y0, x1, y1);

        private static Image PlaceBottomLeftImage(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1, bool raycast = false)
            => PlaceImageAnchored(p, n, s, new Vector2(0f, 0f), x0, y0, x1, y1, raycast);
        private static Image PlaceBottomRightImage(Transform p, string n, Sprite s, int x0, int y0, int x1, int y1, bool raycast = false)
            => PlaceImageAnchored(p, n, s, new Vector2(1f, 0f), x0, y0, x1, y1, raycast);

        private static Text CreateText(RectTransform rect, string value, int fontSize, Color color, TextAnchor alignment)
        {
            if (cachedFont == null) cachedFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var txt = rect.gameObject.AddComponent<Text>();
            txt.font = cachedFont;
            txt.fontStyle = FontStyle.Bold;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.text = value;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private static void AddOutline(Text text, Color color, float distance)
        {
            var o = text.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(distance, -distance);
        }

        private static void AddThickOutline(Text text, Color color, float distance)
        {
            AddOutline(text, color, distance);
            var o2 = text.gameObject.AddComponent<Outline>();
            o2.effectColor = color;
            o2.effectDistance = new Vector2(distance * 0.7f, distance * 0.7f);
        }

        private static Sprite LoadSprite(string relPath)
        {
            string full = UiRoot + relPath;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(full);
            if (sprite == null)
                throw new InvalidOperationException("Missing UI sprite: " + full);
            return sprite;
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) return c;
            return Color.white;
        }

        private static void DestroyChildIfExists(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void RemoveComponentIfExists<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp != null) Object.DestroyImmediate(comp);
        }
    }
}
