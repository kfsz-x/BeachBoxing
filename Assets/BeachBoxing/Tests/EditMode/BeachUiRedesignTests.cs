using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class BeachUiRedesignTests
    {
        private const string InitPath = "Assets/GameFrame/Init.unity";
        private const string GamePath = "Assets/BeachBoxing/Game.unity";
        private const string CommonPrefabPath = "Assets/BeachBoxing/Resources/BeachBoxing/CommonUI.prefab";
        private static readonly Regex EnglishName = new Regex(@"^[A-Za-z][A-Za-z0-9 _()\-]*$");

        [Test]
        public void BuildSettingsUsesDualSceneInitToGameFlow()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Assert.That(scenes, Is.EqualTo(new[] { InitPath, GamePath }));
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/GameFrame/Game.unity"), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/BeachBoxing/Scenes/BeachBoxing.unity"), Is.Null);
        }

        [Test]
        public void InitSceneAndCommonUiHaveEnglishNodesAndValidArt()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            GameObject common = null;
            try
            {
                var init = EditorSceneManager.OpenScene(InitPath, OpenSceneMode.Single);
                var home = init.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BeachHomePanel>(true)).Single();
                AssertEnglishHierarchy(home.transform);
                AssertNotNull(home, "root", "startButton", "settingsButton", "subscribeButton", "addDesktopButton", "shareButton", "entryRewardButton");
                AssertAllSpritesUnderNewUi(home.transform);

                common = PrefabUtility.LoadPrefabContents(CommonPrefabPath);
                AssertEnglishHierarchy(common.transform);
                var commonUi = common.GetComponent<BeachCommonUI>();
                Assert.That(commonUi, Is.Not.Null);
                Assert.That(commonUi.Loading, Is.Not.Null);
                Assert.That(commonUi.Settings, Is.Not.Null);
                Assert.That(commonUi.Toast, Is.Not.Null);
                Assert.That(commonUi.uiSfxSource, Is.Not.Null);
                Assert.That(commonUi.buttonClickClip, Is.Not.Null);
                Assert.That(commonUi.rewardClip, Is.Not.Null);
                AssertNotNull(commonUi.Loading, "root", "progressFill");
                AssertNotNull(commonUi.Settings, "root", "closeButton", "musicToggleButton", "sfxToggleButton", "toggleOnSprite", "toggleOffSprite", "mixer");
                AssertNotNull(commonUi.Toast, "root", "messageLabel", "group");
                AssertAllSpritesUnderNewUi(common.transform);
            }
            finally
            {
                if (common != null) PrefabUtility.UnloadPrefabContents(common);
                if (setup.Any(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        [Test]
        public void GameSceneHasEnglishGameUiAndNoLegacyCanvases()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var game = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Single);
                var flow = game.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BeachGameFlow>(true)).Single();
                Assert.That(flow.transform.Find("Original peripheral canvas"), Is.Null);
                Assert.That(flow.transform.Find("Beach UI"), Is.Null);

                var ui = flow.GetComponentInChildren<BeachGameUi>(true);
                Assert.That(ui, Is.Not.Null);
                var combatCam = flow.GetComponent<BeachNativeCombatCamera>();
                Assert.That(combatCam, Is.Not.Null);
                Assert.That(combatCam.phoneWorldOperand.x, Is.LessThan(0f), "Mobile combat camera must frame from player side, not positive X.");
                Assert.That(combatCam.phoneWorldOperand.y, Is.LessThan(10f), "Mobile combat camera height must stay at ring level.");
                Assert.That(System.IO.File.Exists("Assets/GameFrame/Plugins/WebGL/UnityCoreStorage.jslib"), Is.True,
                    "WebGL/MiniGame sync storage plugin must exist so progress persists when IndexedDB is unavailable.");
                AssertEnglishHierarchy(ui.transform);
                AssertAllSpritesUnderNewUi(ui.transform);
                AssertNotNull(ui,
                    "hudRoot", "coinValueLabel", "starValueLabel", "settingsButton", "boxerButton", "signInButton",
                    "roamingControlsRoot", "roamingJoystick", "roamingJumpHold",
                    "workoutRoot", "quickWorkoutHold", "exitWorkoutButton", "doubleWorkoutButton", "workoutProgressFill",
                    "combatRoot", "combatMobileControlsRoot", "combatJoystick", "punchHold", "blockHold",
                    "playerBarsRoot", "playerHealthFill", "playerStaminaFill", "enemyBarsRoot", "enemyHealthFill", "enemyStaminaFill", "playerTitleRoot",
                    "countdownRoot", "countdownBadge", "countdownText",
                    "challengeRoot", "challengeCloseButton", "challengeNameText", "challengePortraitImage", "challengeStartButton",
                    "boxerRoot", "boxerCloseButton", "boxerPreviewImage", "boxerActionButton",
                    "signInRoot", "signInCloseButton", "signInClaimButton", "signInDoubleClaimButton",
                    "resultRoot", "resultCloseButton", "resultContinueButton", "resultRestartButton");

                // Verify top-left 4 HUD stat progress bars and stat text fonts are bound
                Assert.That(ui.statProgressFills, Is.Not.Null.And.Length.EqualTo(4));
                for (int i = 0; i < 4; i++)
                {
                    Assert.That(ui.statValueLabels[i], Is.Not.Null, $"statValueLabels[{i}] is null");
                    Assert.That(ui.statValueLabels[i].font, Is.Not.Null, $"statValueLabels[{i}].font is null");
                    Assert.That(ui.statProgressFills[i], Is.Not.Null, $"statProgressFills[{i}] is null");
                    Assert.That(ui.statProgressFills[i].sprite, Is.Not.Null, $"statProgressFills[{i}].sprite is null");
                    Assert.That(ui.statProgressFills[i].type, Is.EqualTo(Image.Type.Filled));
                    Assert.That(ui.statProgressFills[i].fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
                }

                // Verify all 6 boxers have distinct, non-null portraits and full preview sprites
                Assert.That(ui.boxerPortraitSprites, Is.Not.Null.And.Length.EqualTo(6));
                Assert.That(ui.boxerFullSprites, Is.Not.Null.And.Length.EqualTo(6));
                Assert.That(ui.boxerPortraitSprites.Distinct().Count(), Is.EqualTo(6), "All 6 boxers must have distinct portrait icons.");
                Assert.That(ui.boxerFullSprites.Distinct().Count(), Is.EqualTo(6), "All 6 boxers must have distinct full preview sprites.");
                for (int i = 0; i < 6; i++)
                {
                    Assert.That(ui.boxerPortraitSprites[i], Is.Not.Null, $"boxerPortraitSprites[{i}] is null");
                    Assert.That(ui.boxerFullSprites[i], Is.Not.Null, $"boxerFullSprites[{i}] is null");
                    Assert.That(ui.boxerCardPortraits[i].sprite, Is.EqualTo(ui.boxerPortraitSprites[i]));
                }

                AssertEnglishHierarchy(ui.playerTitleRoot.transform);
                AssertEnglishHierarchy(ui.playerBarsRoot.transform);
                AssertEnglishHierarchy(ui.enemyBarsRoot.transform);

                // Verify NPC names and nameplates are clean, localized, and free of symbol spam
                var npcManager = flow.GetComponent<BeachNativeNpcManager>();
                Assert.That(npcManager, Is.Not.Null);
                var npcSo = new SerializedObject(npcManager);
                var fakeNamesProp = npcSo.FindProperty("fakeNames");
                Assert.That(fakeNamesProp.arraySize, Is.GreaterThan(0));
                for (int i = 0; i < fakeNamesProp.arraySize; i++)
                {
                    string raw = fakeNamesProp.GetArrayElementAtIndex(i).stringValue;
                    Assert.That(BeachNativeNpcManager.IsUsableFakeName(raw), Is.True, $"Invalid NPC fakeName[{i}]: '{raw}'");
                    string zh = BeachChineseText.Translate(raw);
                    Assert.That(zh, Is.Not.Null.And.Not.Empty);
                    Assert.That(zh.Length, Is.LessThanOrEqualTo(12), $"Overlong NPC name '{raw}' -> '{zh}'");
                    Assert.That(zh, Does.Not.Contain("___").And.Not.Contain("..."));
                }
                var actorsProp = npcSo.FindProperty("actors");
                Assert.That(actorsProp.arraySize, Is.EqualTo(3));
                for (int i = 0; i < actorsProp.arraySize; i++)
                {
                    var actor = actorsProp.GetArrayElementAtIndex(i).objectReferenceValue as BeachNativeNpcActor;
                    Assert.That(actor, Is.Not.Null);
                    var actorSo = new SerializedObject(actor);
                    var nameLabel = actorSo.FindProperty("nameLabel").objectReferenceValue as OriginalSdfText;
                    Assert.That(nameLabel, Is.Not.Null);
                    Assert.That(nameLabel.useLocalizedFont, Is.True);
                    Assert.That(nameLabel.translateText, Is.True);
                    Assert.That(nameLabel.font, Is.Not.Null);
                    var tc = actor.transform.Find("TitleCanvas");
                    Assert.That(tc, Is.Not.Null);
                    foreach (var lbl in tc.GetComponentsInChildren<Text>(true))
                    {
                        Assert.That(lbl.fontStyle, Is.EqualTo(FontStyle.Bold), $"NPC label {lbl.name} should be Bold");
                        Assert.That(lbl.GetComponents<Outline>().Length, Is.EqualTo(2), $"NPC label {lbl.name} should have thick outline");
                    }
                }

                // Verify all 6 trainer nameplates match player font style & thick outline
                var trainersRoot = flow.transform.Find("Original trainers");
                Assert.That(trainersRoot, Is.Not.Null);
                var trainerLabels = trainersRoot.GetComponentsInChildren<Text>(true);
                Assert.That(trainerLabels.Length, Is.GreaterThanOrEqualTo(12));
                foreach (var lbl in trainerLabels)
                {
                    Assert.That(lbl.fontStyle, Is.EqualTo(FontStyle.Bold), $"Trainer label {lbl.name} should be Bold");
                    Assert.That(lbl.GetComponents<Outline>().Length, Is.EqualTo(2), $"Trainer label {lbl.name} should have thick outline");
                }

                // Verify world-space Billboard, WorkoutArea, and Ring Logo sprites use new Art/UI assets
                var legacySpriteNames = new HashSet<string>
                {
                    "aRmwad", "aRmwad 1", "aRmwad 2", "aRmwad 3",
                    "Heart", "EnergyIcon", "PunchIcon", "ShieldIcon", "Logo_"
                };
                foreach (var sr in game.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)))
                {
                    if (sr.sprite == null) continue;
                    Assert.That(legacySpriteNames.Contains(sr.sprite.name), Is.False,
                        $"Legacy world sprite '{sr.sprite.name}' still referenced on '{sr.name}'.");
                }
            }
            finally
            {
                if (setup.Any(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        [Test]
        public void SevenDaySignInSupportsNormalAndDoubleClaim()
        {
            var progress = new PlayerProgress();
            progress.peripheral.dailyIndex = 7;

            Assert.That(BeachPeripheralRules.DailyRewardCount, Is.EqualTo(7));
            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, 0, 1), Is.True);
            Assert.That(progress.peripheral.trainerLevels[0], Is.EqualTo(1));

            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, 1, 2), Is.True);
            Assert.That(progress.coins, Is.EqualTo(6000));

            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, 2, 2), Is.True);
            Assert.That(progress.stars, Is.EqualTo(120));

            Assert.That(BeachPeripheralRules.TryClaimDaily(progress, progress.peripheral, 6, 1), Is.True);
            Assert.That(progress.peripheral.trainerLevels[3], Is.EqualTo(1));
        }

        [Test]
        public void RewardedAdsDefaultToGrantingRewardsAndReportAnalytics()
        {
            var events = new List<(string name, string placement, string status)>();
            void OnEvent(string name, Dictionary<string, object> data)
            {
                events.Add((name, data.TryGetValue(name, out var p) ? p?.ToString() : "", data.TryGetValue("status", out var s) ? s?.ToString() : ""));
            }

            BeachAdService.RegisterProvider(null);
            BeachAdService.EventReported += OnEvent;
            try
            {
                int rewardCount = 0;
                Assert.That(BeachAdService.ShowRewardedAd("hud_coins", () => rewardCount++), Is.True);
                Assert.That(rewardCount, Is.EqualTo(1));
                Assert.That(BeachAdService.IsShowing, Is.False);
                Assert.That(events, Is.EqualTo(new[]
                {
                    ("look_ad_id", "hud_coins", "点击"),
                    ("look_ad_id", "hud_coins", "成功"),
                }));

                events.Clear();
                BeachAdService.RequestSdkEntry("分享");
                Assert.That(events, Is.EqualTo(new[]
                {
                    ("DY_giftPack_id", "分享", "点击"),
                }));
            }
            finally
            {
                BeachAdService.EventReported -= OnEvent;
                BeachAdService.RegisterProvider(null);
            }
        }

        [Test]
        public void PersistenceUsesUnityPlayerPrefsWithoutSdkDependency()
        {
            string prefix = "BeachBoxingTest_" + System.Guid.NewGuid().ToString("N");
            var local = new LocalStore(prefix);
            var store = new ProgressStore(local);
            string expectedRawKey = prefix.Length + ":" + prefix + ":beachBoxing.progress";
            try
            {
                var saved = new PlayerProgress { chest = 123, arms = 456, coins = 789, stars = 12 };
                store.Save(saved);

                Assert.That(UnityEngine.PlayerPrefs.HasKey(expectedRawKey), Is.True);
                string rawJson = UnityEngine.PlayerPrefs.GetString(expectedRawKey, "");
                Assert.That(rawJson, Does.Contain("\"coins\":789"));

                var loaded = store.Load();
                Assert.That(loaded.chest, Is.EqualTo(123));
                Assert.That(loaded.arms, Is.EqualTo(456));
                Assert.That(loaded.coins, Is.EqualTo(789));
                Assert.That(loaded.stars, Is.EqualTo(12));
            }
            finally
            {
                local.Delete("beachBoxing.progress");
                local.Delete("beachBoxing.progress.backup");
                local.Save();
            }
        }

        [Test]
        public void EventSystemDisablesKeyboardNavigationAndAdButtonsDisableNavigation()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            GameObject host = null;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                host = new GameObject("UiSubmitGuardTest");
                BeachPlatformInput.EnsureEventSystem(host.transform);
                var es = host.GetComponentInChildren<EventSystem>(true);
                Assert.That(es, Is.Not.Null);
                Assert.That(es.sendNavigationEvents, Is.False,
                    "EventSystem must disable navigation/Submit so Spacebar jump never re-triggers clicked HUD buttons.");

                var ui = host.AddComponent<BeachGameUi>();
                var coinBtnGo = new GameObject("CoinAdBtn", typeof(RectTransform), typeof(Button));
                coinBtnGo.transform.SetParent(host.transform, false);
                var starBtnGo = new GameObject("StarAdBtn", typeof(RectTransform), typeof(Button));
                starBtnGo.transform.SetParent(host.transform, false);
                ui.coinAdButton = coinBtnGo.GetComponent<Button>();
                ui.starAdButton = starBtnGo.GetComponent<Button>();

                int coinClicks = 0;
                int starClicks = 0;
                ui.Bind(null, null, null, null, null,
                    () => coinClicks++, () => starClicks++,
                    null, null, null, null, null, null, null, null, null, null);

                Assert.That(ui.coinAdButton.navigation.mode, Is.EqualTo(Navigation.Mode.None));
                Assert.That(ui.starAdButton.navigation.mode, Is.EqualTo(Navigation.Mode.None));

                ui.coinAdButton.onClick.Invoke();
                ui.starAdButton.onClick.Invoke();
                Assert.That(coinClicks, Is.EqualTo(1));
                Assert.That(starClicks, Is.EqualTo(1));
            }
            finally
            {
                if (host != null) Object.DestroyImmediate(host);
                if (setup.Any(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        [Test]
        public void HoldButtonPreservesFastTapPulseOnTouchCancelAndAudioVolumesAreAudible()
        {
            var go = new GameObject("QuickWorkoutTapTest");
            try
            {
                var hold = go.AddComponent<BeachHoldButton>();
                hold.Bind(() => true);
                var data = new PointerEventData(null)
                {
                    pointerId = 7,
                    button = PointerEventData.InputButton.Left
                };
                hold.OnPointerDown(data);
                Assert.That(hold.Held, Is.True);

                // Simulate BeachTouchModule canceling the touch on the same frame as TouchPhase.Ended
                var cancelMethod = typeof(BeachHoldButton).GetMethod("OnPointerCanceled",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cancelMethod.Invoke(hold, new object[] { 7 });

                Assert.That(hold.Held, Is.False);
                Assert.That(hold.ConsumePress(), Is.True, "Fast mobile tap must preserve pressedPulse for AdvanceWorkout.");
                Assert.That(hold.ConsumeRelease(), Is.True);

                var stageType = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachStage");
                Assert.That(stageType, Is.Not.Null);
                Assert.That((float)stageType.GetField("CountdownSoundVolume").GetValue(null), Is.GreaterThanOrEqualTo(0.4f));
                Assert.That((float)stageType.GetField("VictorySoundVolume").GetValue(null), Is.GreaterThanOrEqualTo(0.7f));
                Assert.That((float)stageType.GetField("DefeatSoundVolume").GetValue(null), Is.GreaterThanOrEqualTo(0.7f));

                // Verify QuickWorkoutBtn tap-rhythm scale & shake pulse animates and cleanly resets to rest pose
                var ui = go.AddComponent<BeachGameUi>();
                var btnGo = new GameObject("QuickWorkoutBtn", typeof(RectTransform), typeof(BeachHoldButton));
                btnGo.transform.SetParent(go.transform, false);
                var btnRect = (RectTransform)btnGo.transform;
                btnRect.anchoredPosition = new Vector2(100f, 200f);
                btnRect.localScale = Vector3.one;
                ui.quickWorkoutHold = btnGo.GetComponent<BeachHoldButton>();

                ui.PulseQuickWorkoutButton();
                ui.AdvanceQuickWorkoutPulse(0.03f); // squash phase
                Assert.That(btnRect.localScale.x, Is.LessThan(0.98f));
                Assert.That(Mathf.Abs(btnRect.localRotation.z), Is.GreaterThan(0.001f));

                ui.AdvanceQuickWorkoutPulse(0.06f); // rebound pop phase (t=0.09)
                Assert.That(btnRect.localScale.x, Is.GreaterThan(1.03f));

                ui.AdvanceQuickWorkoutPulse(0.20f); // finished -> exact rest pose
                Assert.That(btnRect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(btnRect.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(btnRect.anchoredPosition, Is.EqualTo(new Vector2(100f, 200f)));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void AssertEnglishHierarchy(Transform root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                Assert.That(EnglishName.IsMatch(node.name), Is.True, "Node must have English name: " + node.name);
        }

        private static void AssertAllSpritesUnderNewUi(Transform root)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null) continue;
                string path = AssetDatabase.GetAssetPath(img.sprite);
                Assert.That(path, Does.StartWith("Assets/BeachBoxing/Art/UI/"), "Legacy sprite on " + img.name + ": " + path);
            }
        }

        private static void AssertNotNull(Object target, params string[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var field in fields)
            {
                var prop = so.FindProperty(field);
                Assert.That(prop, Is.Not.Null, $"{target.GetType().Name}.{field}");
                Assert.That(prop.objectReferenceValue, Is.Not.Null, $"{target.GetType().Name}.{field} is null");
            }
        }
    }
}
