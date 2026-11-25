using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class DualSceneAndUiFlowTests
    {
        private const string CaptureDir = "Temp/UiVerify";

        private static IEnumerator Snap(string file)
        {
            Directory.CreateDirectory(CaptureDir);
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(CaptureDir, file), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        [UnityTest]
        public IEnumerator InitToGameDualSceneAndNewUiModalsWorkEndToEnd()
        {
            yield return SceneManager.LoadSceneAsync(BeachCommonUI.InitScenePath, LoadSceneMode.Single);
            yield return null;
            yield return Snap("01_home.png");

            var home = Object.FindObjectOfType<BeachHomePanel>();
            Assert.That(home, Is.Not.Null);
            var common = BeachCommonUI.Ensure();
            Assert.That(common, Is.Not.Null);

            // Open and toggle Settings on Init
            home.settingsButton.onClick.Invoke();
            yield return null;
            Assert.That(common.Settings.IsOpen, Is.True);
            yield return Snap("02_settings.png");

            bool prevMusic = CoreRoot.Current.Audio.MusicEnabled;
            common.Settings.musicToggleButton.onClick.Invoke();
            Assert.That(CoreRoot.Current.Audio.MusicEnabled, Is.EqualTo(!prevMusic));
            common.Settings.musicToggleButton.onClick.Invoke();
            Assert.That(CoreRoot.Current.Audio.MusicEnabled, Is.EqualTo(prevMusic));
            common.Settings.closeButton.onClick.Invoke();
            yield return null;
            Assert.That(common.Settings.IsOpen, Is.False);

            // Preview LoadingPanel
            common.Loading.Show(0.65f);
            yield return null;
            yield return Snap("03_loading.png");
            common.Loading.Hide();

            // Channel button triggers SDK toast
            home.shareButton.onClick.Invoke();
            yield return null;
            Assert.That(common.Toast.root.activeSelf, Is.True);
            Assert.That(common.Toast.messageLabel.text, Is.EqualTo("请接入SDK"));

            // Start game -> Loading -> Game scene
            home.startButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 15f;
            while (SceneManager.GetActiveScene().name != "Game" || BeachCommonUI.IsTransitioning)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Timed out entering Game scene");
                yield return null;
            }
            yield return null;

            var flow = Object.FindObjectOfType<BeachGameFlow>();
            Assert.That(flow, Is.Not.Null);
            var gameUi = Object.FindObjectOfType<BeachGameUi>();
            Assert.That(gameUi, Is.Not.Null);
            Assert.That(gameUi.hudRoot.activeInHierarchy, Is.True);

            // TipBanner shows at start, starts fading after 3s, and hides after hold + fade
            Assert.That(gameUi.tipBannerRoot.activeInHierarchy, Is.True);
            Assert.That(gameUi.tipBannerGroup, Is.Not.Null);
            Assert.That(gameUi.tipBannerGroup.alpha, Is.EqualTo(1f).Within(0.01f));

            // Rewarded ad buttons default to granting rewards and firing analytics events
            var adEvents = new List<string>();
            void OnAdEvent(string evt, Dictionary<string, object> data)
            {
                if (evt == BeachAdService.RewardAdEventName)
                    adEvents.Add($"{data[evt]}:{data["status"]}");
            }
            BeachAdService.EventReported += OnAdEvent;
            try
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                Assert.That(es, Is.Not.Null);
                Assert.That(es.sendNavigationEvents, Is.False);

                var pField = typeof(BeachGameFlow).GetField("progress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var p = (PlayerProgress)pField.GetValue(flow);
                int coinsBefore = p.coins;
                int starsBefore = p.stars;

                es.SetSelectedGameObject(gameUi.coinAdButton.gameObject);
                gameUi.coinAdButton.onClick.Invoke();
                Assert.That(es.currentSelectedGameObject, Is.Null, "Coin ad button must clear EventSystem selection after click.");

                es.SetSelectedGameObject(gameUi.starAdButton.gameObject);
                gameUi.starAdButton.onClick.Invoke();
                Assert.That(es.currentSelectedGameObject, Is.Null, "Star ad button must clear EventSystem selection after click.");

                Assert.That(p.coins, Is.EqualTo(coinsBefore + 500));
                Assert.That(p.stars, Is.EqualTo(starsBefore + 10));
                Assert.That(adEvents, Is.EqualTo(new[]
                {
                    "hud_coins:点击", "hud_coins:成功",
                    "hud_stars:点击", "hud_stars:成功"
                }));
            }
            finally
            {
                BeachAdService.EventReported -= OnAdEvent;
            }

            // PC mode hides mobile joystick & jump; Mobile mode shows mobile joystick & jump
            gameUi.SetUseTouch(false);
            yield return null;
            Assert.That(gameUi.roamingControlsRoot.activeInHierarchy, Is.False);
            Assert.That(gameUi.roamingJumpHold.gameObject.activeInHierarchy, Is.False);
            gameUi.SetUseTouch(true);
            yield return null;
            Assert.That(gameUi.roamingControlsRoot.activeInHierarchy, Is.True);
            Assert.That(gameUi.roamingJumpHold.gameObject.activeInHierarchy, Is.True);
            yield return Snap("04_roaming_mobile.png");

            // Simulate tapping the mobile Jump button and verify it pulses press & release
            var jumpPointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                pointerId = 99,
                button = UnityEngine.EventSystems.PointerEventData.InputButton.Left
            };
            gameUi.roamingJumpHold.OnPointerDown(jumpPointer);
            Assert.That(gameUi.roamingJumpHold.Held, Is.True);
            yield return null;
            gameUi.roamingJumpHold.OnPointerUp(jumpPointer);
            Assert.That(gameUi.roamingJumpHold.Held, Is.False);
            yield return null;

            // Open Boxer modal
            gameUi.boxerButton.onClick.Invoke();
            yield return null;
            Assert.That(gameUi.boxerRoot.activeInHierarchy, Is.True);
            yield return Snap("05_boxer.png");
            gameUi.boxerCloseButton.onClick.Invoke();
            yield return null;
            Assert.That(gameUi.boxerRoot.activeInHierarchy, Is.False);

            // Open 7-Day Sign-In modal
            gameUi.signInButton.onClick.Invoke();
            yield return null;
            Assert.That(gameUi.signInRoot.activeInHierarchy, Is.True);
            yield return Snap("06_signin.png");
            gameUi.signInCloseButton.onClick.Invoke();
            yield return null;
            Assert.That(gameUi.signInRoot.activeInHierarchy, Is.False);

            // Preview Workout, Challenge, Countdown, Combat, Victory, Defeat directly via BeachGameUi
            var fakeProgress = new PlayerProgress { chest = 888, arms = 500, back = 1000, legs = 250, coins = 23500, stars = 23500 };
            var catalog = flow.GetComponent<NativeOpponentCatalog>();
            var sampleOpp = catalog != null && catalog.Profiles.Length > 0 ? catalog.Profiles[0] : OpponentProfile.RookieRoster()[0];

            flow.enabled = false;
            gameUi.Render(BeachPhase.Training, fakeProgress, true, 0.72f, false, 0, sampleOpp, 0, 1, null, false, false);
            Assert.That(gameUi.statProgressFills[0].fillAmount, Is.EqualTo(0.888f).Within(0.005f));
            Assert.That(gameUi.statProgressFills[1].fillAmount, Is.EqualTo(0.500f).Within(0.005f));
            Assert.That(gameUi.statProgressFills[2].fillAmount, Is.EqualTo(1.000f).Within(0.005f));
            Assert.That(gameUi.statProgressFills[3].fillAmount, Is.EqualTo(0.250f).Within(0.005f));
            yield return Snap("07_workout.png");

            gameUi.Render(BeachPhase.OpponentSelect, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 4, null, false, false);
            yield return Snap("08_challenge.png");

            var dummyPlayer = new GameObject("DummyP").transform;
            var dummyEnemy = new GameObject("DummyE").transform;
            var fakeFight = new FightController(dummyPlayer, dummyEnemy, fakeProgress, sampleOpp, new Rect(-3, -3, 6, 6), 1);
            gameUi.Render(BeachPhase.Fight, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 1, fakeFight, false, false);
            yield return Snap("09_countdown.png");

            // Advance countdown to 0 for Combat HUD
            for (int i = 0; i < 160; i++) fakeFight.Tick(0.05f, default);
            gameUi.SetUseTouch(true);
            gameUi.Render(BeachPhase.Fight, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 1, fakeFight, false, false);
            yield return Snap("10_combat_mobile.png");

            // Victory & Defeat preview via reflection on FightController outcome
            // First verify red combat overlay activates on low HP/damage and clears immediately on Result and Training
            gameUi.combatScreenOverlay.color = new Color(1f, 0.13f, 0.1f, 0.5f);
            gameUi.combatScreenOverlay.enabled = true;
            var outcomeProp = typeof(FightController).GetProperty("Outcome");
            outcomeProp?.SetValue(fakeFight, FightOutcome.Victory);
            gameUi.Render(BeachPhase.Result, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 1, fakeFight, true, false);
            Assert.That(gameUi.combatScreenOverlay.enabled, Is.False, "Red combat screen overlay must clear on Result.");
            Assert.That(gameUi.combatScreenOverlay.color.a, Is.EqualTo(0f).Within(0.001f));
            yield return Snap("11_victory.png");

            outcomeProp?.SetValue(fakeFight, FightOutcome.Defeat);
            gameUi.Render(BeachPhase.Result, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 1, fakeFight, false, false);
            Assert.That(gameUi.combatScreenOverlay.enabled, Is.False, "Red combat screen overlay must stay cleared on Defeat.");
            yield return Snap("12_defeat.png");

            gameUi.combatScreenOverlay.color = new Color(1f, 0.13f, 0.1f, 0.5f);
            gameUi.combatScreenOverlay.enabled = true;
            gameUi.Render(BeachPhase.Training, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 1, null, false, false);
            Assert.That(gameUi.combatScreenOverlay.enabled, Is.False, "Red combat screen overlay must clear back on Beach.");

            Object.Destroy(dummyPlayer.gameObject);
            Object.Destroy(dummyEnemy.gameObject);
            gameUi.SetUseTouch(false);

            // Verify player boxing armature height resets cleanly after Boxing_Die (both Beach->Ring and direct Rematch)
            // Keep flow disabled during direct stage ring assertions so Training Update does not switch back to workout model.
            var stageField = typeof(BeachGameFlow).GetField("stage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var liveStage = stageField.GetValue(flow);
            var enterRingMethod = liveStage.GetType().GetMethod("EnterRing");
            var enterBeachMethod = liveStage.GetType().GetMethod("EnterBeach");
            var frameFightMethod = liveStage.GetType().GetMethod("FrameFight");
            var playerBoxerField = liveStage.GetType().GetField("Player");
            var playerBoxer = playerBoxerField.GetValue(liveStage);
            var playerRootT = (Transform)playerBoxer.GetType().GetField("Root").GetValue(playerBoxer);
            var boxingModelT = playerRootT.Find("Original Player");
            Assert.That(boxingModelT, Is.Not.Null);
            Assert.That(boxingModelT.localPosition.y, Is.EqualTo(0.052f).Within(0.005f),
                "Original Player boxing model should sit flush with the visual ring mat.");
            var boxingArmature = boxingModelT.Find("Armature");
            var boxingAnim = boxingArmature.GetComponent<Animator>();

            enterRingMethod.Invoke(liveStage, new object[] { sampleOpp.modelName });
            yield return null;
            Assert.That(boxingModelT.gameObject.activeInHierarchy, Is.True);
            Assert.That(boxingArmature.localPosition.y, Is.EqualTo(1.9611f).Within(0.01f));
            Assert.That(boxingAnim.GetCurrentAnimatorStateInfo(0).IsName("Boxing_Idle"), Is.True);

            // Simulate Boxing_Die dropping Armature to (0, 0.59, -2.99) then returning to Beach and entering Ring again
            boxingArmature.localPosition = new Vector3(0f, 0.59f, -2.99f);
            enterBeachMethod.Invoke(liveStage, new object[] { 1 });
            yield return null;
            enterRingMethod.Invoke(liveStage, new object[] { sampleOpp.modelName });
            yield return null;
            Assert.That(boxingArmature.localPosition.y, Is.EqualTo(1.9611f).Within(0.01f),
                "Player boxing Armature Y must reset after returning from a KO fight.");
            Assert.That(boxingArmature.localPosition.z, Is.EqualTo(0f).Within(0.01f));
            Assert.That(boxingAnim.GetCurrentAnimatorStateInfo(0).IsName("Boxing_Idle"), Is.True);

            // Simulate direct Rematch while still in Ring after KO, let physics settle onto ring mat, and capture screenshot
            boxingArmature.localPosition = new Vector3(0f, 0.59f, -2.99f);
            enterRingMethod.Invoke(liveStage, new object[] { sampleOpp.modelName });
            for (int f = 0; f < 25; f++)
            {
                frameFightMethod.Invoke(liveStage, new object[] { 0f, true });
                yield return new WaitForFixedUpdate();
            }
            yield return null;
            Assert.That(boxingArmature.localPosition.y, Is.EqualTo(1.9611f).Within(0.01f),
                "Player boxing Armature Y must reset on direct Rematch.");
            Assert.That(boxingArmature.localPosition.z, Is.EqualTo(0f).Within(0.01f));
            Assert.That(boxingAnim.GetCurrentAnimatorStateInfo(0).IsName("Boxing_Idle"), Is.True);
            yield return Snap("13_ring_idle_after_ko.png");

            // Verify mobile combat camera keeps BOTH player and enemy clearly inside viewport
            var setPhoneMethod = liveStage.GetType().GetMethod("SetPhone");
            var enemyBoxer = liveStage.GetType().GetField("Enemy").GetValue(liveStage);
            var enemyRootT = (Transform)enemyBoxer.GetType().GetField("Root").GetValue(enemyBoxer);
            setPhoneMethod.Invoke(liveStage, new object[] { true });
            gameUi.SetUseTouch(true);
            enterRingMethod.Invoke(liveStage, new object[] { sampleOpp.modelName });
            var liveMobileFight = new FightController(playerRootT, enemyRootT, fakeProgress, sampleOpp, new Rect(-3, -3, 6, 6), 1);
            var cdField = typeof(FightController).GetField("countdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cdField?.SetValue(liveMobileFight, 0f);
            for (int f = 0; f < 40; f++)
            {
                frameFightMethod.Invoke(liveStage, new object[] { 0f, true });
                yield return null;
            }
            gameUi.Render(BeachPhase.Fight, fakeProgress, false, 0f, false, 0, sampleOpp, 0, 1, liveMobileFight, false, false);
            yield return null;
            yield return Snap("14_combat_mobile_camera.png");
            var mainCam = Camera.main ?? Object.FindObjectOfType<Camera>();
            Assert.That(mainCam, Is.Not.Null);
            var playerVp = mainCam.WorldToViewportPoint(playerRootT.position + Vector3.up * 0.8f);
            var enemyVp = mainCam.WorldToViewportPoint(enemyRootT.position + Vector3.up * 0.8f);
            Assert.That(playerVp.z, Is.GreaterThan(0f), "Player must be in front of mobile combat camera.");
            Assert.That(playerVp.x, Is.InRange(0.10f, 0.90f), $"Player X out of mobile combat viewport: {playerVp}");
            Assert.That(playerVp.y, Is.InRange(0.10f, 0.90f), $"Player Y out of mobile combat viewport: {playerVp}");
            Assert.That(enemyVp.z, Is.GreaterThan(0f), "Enemy must be in front of mobile combat camera.");
            Assert.That(enemyVp.x, Is.InRange(0.10f, 0.90f), $"Enemy X out of mobile combat viewport: {enemyVp}");
            Assert.That(enemyVp.y, Is.InRange(0.10f, 0.90f), $"Enemy Y out of mobile combat viewport: {enemyVp}");
            setPhoneMethod.Invoke(liveStage, new object[] { false });
            gameUi.SetUseTouch(false);

            enterBeachMethod.Invoke(liveStage, new object[] { 1 });
            flow.enabled = true;
            yield return null;

            // Verify completing a live workout rep does not throw (regression test for legacy BeachHud.TrainingGain NRE)
            var stationsField = typeof(BeachGameFlow).GetField("stations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var activeStationField = typeof(BeachGameFlow).GetField("activeStation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressField = typeof(BeachGameFlow).GetField("progress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var workoutField = typeof(BeachGameFlow).GetField("workout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var advanceWorkoutMethod = typeof(BeachGameFlow).GetMethod("AdvanceWorkout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var stations = (TrainingStation[])stationsField.GetValue(flow);
            var liveProgress = (PlayerProgress)progressField.GetValue(flow);
            var workoutCycle = (TrainingCycle)workoutField.GetValue(flow);
            int armsBefore = liveProgress.arms;
            int levelBefore = liveProgress.prestigeLevel;
            workoutCycle.Reset();
            activeStationField.SetValue(flow, stations[0]);
            bool completedRep = false;
            float workoutDeadline = Time.realtimeSinceStartup + 4f;
            float prevWorkoutProgress = 0f;
            while (Time.realtimeSinceStartup < workoutDeadline && !completedRep)
            {
                advanceWorkoutMethod.Invoke(flow, new object[] { new InputFrame(Vector2.zero, false, false, false, false, true, false, true, false) });
                if (liveProgress.arms != armsBefore || liveProgress.prestigeLevel != levelBefore || workoutCycle.Progress < prevWorkoutProgress)
                    completedRep = true;
                prevWorkoutProgress = workoutCycle.Progress;
                yield return null;
            }
            activeStationField.SetValue(flow, null);
            Assert.That(completedRep, Is.True, "Expected at least one workout rep to complete cleanly.");

            // Advance TipBanner past 3s hold + 0.6s fade and verify it fades out and hides
            gameUi.ResetTipBanner();
            gameUi.AdvanceTipBanner(2.9f);
            Assert.That(gameUi.tipBannerRoot.activeSelf, Is.True);
            Assert.That(gameUi.tipBannerGroup.alpha, Is.EqualTo(1f).Within(0.01f));
            gameUi.AdvanceTipBanner(0.4f); // 3.3s -> mid-fade
            Assert.That(gameUi.tipBannerRoot.activeSelf, Is.True);
            Assert.That(gameUi.tipBannerGroup.alpha, Is.GreaterThan(0.05f).And.LessThan(0.95f));
            gameUi.AdvanceTipBanner(0.5f); // 3.8s -> hidden
            Assert.That(gameUi.tipBannerRoot.activeSelf, Is.False);
            Assert.That(gameUi.tipBannerGroup.alpha, Is.EqualTo(0f).Within(0.01f));

            // Open shared Settings in Game
            gameUi.settingsButton.onClick.Invoke();
            yield return null;
            Assert.That(common.Settings.IsOpen, Is.True);
            common.Settings.closeButton.onClick.Invoke();
            yield return null;
            Assert.That(common.Settings.IsOpen, Is.False);
        }
    }
}
