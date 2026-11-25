using System;
using System.Collections;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class PeripheralMenuTests
    {
        private const string ScenePath = "Assets/BeachBoxing/Scenes/BeachBoxing.unity";
        private const string SaveKey = "beachBoxing.progress";

        [UnityTest]
        public IEnumerator HighStatCaptionsRemainVisibleAfterValueChangesAndMenus()
        {
            return WithProgress(new PlayerProgress { unlockedOpponent = 1 }, CheckRenderedStatCaptions);
        }

        private static IEnumerator CheckRenderedStatCaptions(BeachPeripheralView view, LocalStore storage)
        {
            var flow = view.GetComponent<BeachGameFlow>();
            var field = typeof(BeachGameFlow).GetField("progress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var progress = (PlayerProgress)field.GetValue(flow);
            foreach (int value in new[] { 999, 1000, 4240, 3520, 3000 })
            {
                progress.chest = progress.arms = progress.back = value;
                view.Render(progress, BeachPhase.Training, false, false);
                yield return null;
                yield return new WaitForEndOfFrame();
                var pixels = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    foreach (int id in new[] { 802, 716, 1051 })
                    {
                        var label = view.Node(id).label;
                        var center = label.rectTransform.TransformPoint(label.rectTransform.rect.center);
                        int white = 0;
                        int left = Mathf.Max(0, Mathf.RoundToInt(center.x - 70));
                        int right = Mathf.Min(pixels.width, Mathf.RoundToInt(center.x + 70));
                        int bottom = Mathf.Max(0, Mathf.RoundToInt(center.y - 25));
                        int top = Mathf.Min(pixels.height, Mathf.RoundToInt(center.y + 25));
                        for (int y = bottom; y < top; y++)
                            for (int x = left; x < right; x++)
                            {
                                var color = pixels.GetPixel(x, y);
                                if (color.r > .94f && color.g > .94f && color.b > .94f) white++;
                            }
                        Assert.That(white, Is.GreaterThan(80), $"Rendered stat {id} ({label.text}) must contain visible white glyphs, not only a valid text property.");
                    }
                }
                finally { UnityEngine.Object.Destroy(pixels); }
                Click(view, 855);
                yield return Until(() => Active(view, 1146), "Trainer panel should finish opening.");
                yield return Close(view, 1146);
            }
        }

        [UnityTest]
        public IEnumerator ARepeatedPrestigeConfirmationCannotSkipTheNextPreview()
        {
            return WithProgress(new PlayerProgress { chest = 3000, arms = 3000, back = 3000, legs = 3000 }, CheckDoubleConfirm);
        }

        private static IEnumerator CheckDoubleConfirm(BeachPeripheralView view, LocalStore storage)
        {
            Click(view, 129);
            yield return Until(() => Active(view, 882), "The next level preview should open.");
            var confirm = view.Node(54).button;
            Click(view, 54);
            confirm.onClick.Invoke();
            Assert.That(Load(storage).prestigeLevel, Is.EqualTo(2), "A second event from the closing preview must not grant another level.");
        }

        [UnityTest]
        public IEnumerator FreshTutorialKeepsTrainerAndDailyUnavailable()
        {
            return WithProgress(new PlayerProgress(), CheckTutorialGate);
        }

        [UnityTest]
        public IEnumerator PausingBlocksNativeInteractionAndNestedMenus()
        {
            return WithProgress(new PlayerProgress { unlockedOpponent = 1 }, CheckPauseGate);
        }

        private static IEnumerator CheckTutorialGate(BeachPeripheralView view, LocalStore storage)
        {
            Assert.That(Active(view, 855), Is.False);
            Assert.That(Active(view, 510), Is.False);
            view.Node(855).button.onClick.Invoke();
            Assert.That(view.IsOpen, Is.False);
            yield return null;
        }

        private static IEnumerator CheckPauseGate(BeachPeripheralView view, LocalStore storage)
        {
            var station = view.transform.Find("Training beach/Arms/Original equipment");
            view.transform.Find("Player").position = station.position;
            Physics.SyncTransforms();
            yield return Until(() => Active(view, 1057) || Active(view, 1110), "Station control should be available before pausing.");
            Click(view, 760);
            Assert.That(Active(view, 855), Is.False);
            Assert.That(Active(view, 510), Is.False);
            Assert.That(Active(view, 1057), Is.False);
            Assert.That(Active(view, 1110), Is.False);
            view.Node(855).button.onClick.Invoke();
            view.Node(1057).button.onClick.Invoke();
            Assert.That(view.IsOpen, Is.False);
            var flow = view.GetComponent<BeachGameFlow>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            Assert.That(typeof(BeachGameFlow).GetField("activeStation", flags).GetValue(flow), Is.Null);
            typeof(BeachGameFlow).GetMethod("TogglePause", flags).Invoke(flow, null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DailyClaimUnlocksCodyAndHisBonusReachesARepetition()
        {
            return WithProgress(new PlayerProgress { prestigeLevel = 4, unlockedOpponent = 1 }, DailyClaimAndWorkout, phone: true);
        }

        [UnityTest]
        public IEnumerator TrainerPurchaseEquipAndUpgradeSurviveClosingAndReloading()
        {
            return WithProgress(new PlayerProgress { stars = 50, coins = 16, unlockedOpponent = 1 }, TrainerPurchaseAndReload);
        }

        [UnityTest]
        public IEnumerator FourFullBarsEnableManualLevelUpAndPreserveTheSavedProgress()
        {
            return WithProgress(new PlayerProgress
            {
                chest = 1250, arms = 1000, back = 1000, legs = 500,
                coins = 910, stars = 70, unlockedOpponent = 3
            }, LevelUp);
        }

        private static IEnumerator DailyClaimAndWorkout(BeachPeripheralView view, LocalStore storage)
        {
            int placedObjects = view.GetComponentsInChildren<Transform>(true).Length;
            var flow = view.GetComponent<BeachGameFlow>();
            var joystick = flow.GetComponent<BeachNativeMobileControls>().beachJoystick;
            var origin = new Vector2(Screen.width * .15f, Screen.height * .2f);
            var pointer = NativePlayModeScene.Pointer(22, origin);
            Assert.That(joystick.isActiveAndEnabled, Is.True);
            try
            {
                ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                NativePlayModeScene.Drag(joystick, pointer, Vector2.right);
                Assert.That(joystick.background.gameObject.activeInHierarchy, Is.True);
                Assert.That(joystick.Direction.x, Is.EqualTo(1f).Within(.00001f));

                Assert.That(Active(view, 628), Is.True, "A fresh local date has a daily reward waiting.");
                Click(view, 510);
                Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero));
                Assert.That(joystick.background.gameObject.activeSelf, Is.False);
                ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.dragHandler);
                Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero));
            }
            finally { ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.pointerUpHandler); }
            yield return Until(() => Active(view, 145), "The daily close button should appear after the opening animation.");
            Assert.That(Active(view, 141), Is.True);
            Assert.That(view.Node(364).button.IsInteractable(), Is.False);

            Click(view, 427);
            Assert.That(Load(storage).peripheral.trainerLevels[0], Is.EqualTo(1));
            Assert.That(Load(storage).peripheral.equippedTrainer, Is.EqualTo(-1));
            Assert.That(Active(view, 1096), Is.True);
            Assert.That(Active(view, 141), Is.False);
            Assert.That(view.Node(427).button.IsInteractable(), Is.False);
            view.Node(427).button.onClick.Invoke();
            view.Node(364).button.onClick.Invoke();
            Assert.That(Load(storage).peripheral.claimedDays, Is.EqualTo(1));
            Assert.That(Load(storage).coins, Is.Zero);
            yield return Close(view, 145);
            Assert.That(Active(view, 628), Is.False);
            Assert.That(Active(view, 299), Is.True);
            yield return null;
            ExecuteEvents.Execute(joystick.gameObject, pointer, ExecuteEvents.dragHandler);
            Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero), "Closing must discard the original gesture.");
            var fresh = NativePlayModeScene.Pointer(24, origin);
            try
            {
                ExecuteEvents.Execute(joystick.gameObject, fresh, ExecuteEvents.pointerDownHandler);
                NativePlayModeScene.Drag(joystick, fresh, Vector2.right);
                Assert.That(joystick.background.gameObject.activeInHierarchy, Is.True);
                Assert.That(joystick.Direction.x, Is.EqualTo(1f).Within(.00001f), "Closing must allow a fresh gesture.");
            }
            finally { ExecuteEvents.Execute(joystick.gameObject, fresh, ExecuteEvents.pointerUpHandler); }

            Click(view, 855);
            yield return Until(() => Active(view, 1146), "The trainer close button should appear after the opening animation.");
            Click(view, 901);
            yield return null;
            Click(view, 569);
            var equipped = Load(storage);
            Assert.That(equipped.peripheral.equippedTrainer, Is.Zero);
            Assert.That(equipped.peripheral.appliedModifiers, Is.EqualTo(new[] { 6, 6, 6, 6 }));
            yield return Close(view, 1146);
            Assert.That(Active(view, 299), Is.False);
            Assert.That(joystick.background.gameObject.activeSelf, Is.False);
            Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero), "Closing a menu must require a fresh movement gesture.");

            // Reachability has separate tests. Start within the arms detector to isolate the gain through the UI.
            var equipment = view.transform.Find("Training beach/Arms/Original equipment");
            view.transform.Find("Player").position = equipment.position;
            Physics.SyncTransforms();
            yield return Until(() => Active(view, 1057), "The native interact button should appear beside the arms station.");
            Assert.That(Active(view, 1110), Is.False);
            Click(view, 1057);
            yield return null;
            Assert.That(Active(view, 855), Is.False, "The trainer menu entry is unavailable during a workout.");
            view.Node(855).button.onClick.Invoke();
            Assert.That(view.IsOpen, Is.False);
            var pulse = view.Node(804).transform.gameObject;
            Assert.That(pulse.activeInHierarchy, Is.True);
            var repetition = NativePlayModeScene.Pointer(23, NativePlayModeScene.ScreenCenter(pulse.transform));
            ExecuteEvents.Execute(pulse, repetition, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(pulse, repetition, ExecuteEvents.pointerUpHandler);
            yield return null;
            var cycle = NativePlayModeScene.Field<TrainingCycle>(flow, "workout");
            float expectedSpeed = 5f + (.5f - 5f) * (Time.deltaTime * 16f);
            Assert.That(cycle.Speed, Is.EqualTo(expectedSpeed).Within(.00001f), "The source phone pulse must use its own speed curve.");
            yield return Until(() => Load(storage).arms > 0, "A completed repetition should be saved.");
            Assert.That(Load(storage).arms, Is.EqualTo(21), "A level-four repetition gives 20 base strength with Cody's 6% applied.");
            Assert.That(view.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placedObjects));
        }

        private static IEnumerator TrainerPurchaseAndReload(BeachPeripheralView view, LocalStore storage)
        {
            int placedObjects = view.GetComponentsInChildren<Transform>(true).Length;
            var menuHolder = view.Node(333).transform;
            var phoneMenu = Array.Find(view.portraits, binding => binding.target == menuHolder);
            Assert.That(phoneMenu, Is.Not.Null, "The source scales the menu owner separately from its pause-panel pose target.");
            Assert.That(phoneMenu.portrait.localScale, Is.EqualTo(Vector3.one * 1.5f));
            Assert.That(phoneMenu.retainPhoneScaleInLandscape, Is.True);
            Click(view, 855);
            yield return Until(() => Active(view, 1146), "The trainer menu must open from its saved button.");
            var scroll = view.Node(41).transform.GetComponent<UnityEngine.UI.ScrollRect>();
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(.001f),
                "Opening scale animation must not scroll away the top row of trainers.");
            var close = view.Node(1146).button;
            var down = new PointerEventData(EventSystem.current) { pointerId = 43, button = PointerEventData.InputButton.Left };
            close.OnPointerDown(down);
            for (int frame = 0; frame < 5; frame++) yield return null;
            var pointerDown = typeof(UnityEngine.UI.Selectable).GetProperty("isPointerDown",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(pointerDown.GetValue(close, null), Is.EqualTo(true),
                "An idle open menu must not keep disabling and re-enabling its close button.");
            close.OnPointerUp(down);
            Click(view, 975);
            yield return null;
            Assert.That(view.Node(1169).image.sprite, Is.SameAs(view.Node(977).image.sprite));
            Click(view, 227);
            var purchased = Load(storage);
            Assert.That(purchased.stars, Is.Zero);
            Assert.That(purchased.coins, Is.EqualTo(16));
            Assert.That(purchased.peripheral.trainerLevels[1], Is.EqualTo(1));
            Assert.That(purchased.peripheral.equippedTrainer, Is.EqualTo(-1));
            Assert.That(Active(view, 227), Is.False);

            Click(view, 569);
            Assert.That(Load(storage).peripheral.equippedTrainer, Is.EqualTo(1));
            Assert.That(view.Node(966).label.text, Is.EqualTo("16"));
            Click(view, 618);
            var upgraded = Load(storage);
            Assert.That(upgraded.coins, Is.Zero);
            Assert.That(upgraded.peripheral.trainerLevels[1], Is.EqualTo(2));
            Assert.That(upgraded.peripheral.appliedModifiers, Is.EqualTo(new[] { 11, 11, 11, 11 }));
            Assert.That(view.Node(667).label.text, Is.EqualTo("+12%"));
            Assert.That(view.Node(966).label.text, Is.EqualTo("24"));

            yield return Close(view, 1146);
            Click(view, 855);
            yield return Until(() => Active(view, 1146), "The same close button must be ready after reopening.");
            Assert.That(view.Selection, Is.EqualTo(1));
            Assert.That(Active(view, 618), Is.True);
            Assert.That(view.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placedObjects));
            yield return Close(view, 1146);

            yield return Reload(storage);
            view = FindView();
            Assert.That(view.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(placedObjects));
            Assert.That(Load(storage).peripheral.appliedModifiers, Is.EqualTo(new[] { 12, 12, 12, 12 }));
            Click(view, 855);
            yield return Until(() => Active(view, 1146), "The reloaded trainer menu must bind its saved buttons.");
            Assert.That(view.Selection, Is.EqualTo(1));
            Assert.That(view.Node(1085).label.text, Is.EqualTo("等级 2"));
            yield return Close(view, 1146);
        }

        private static IEnumerator LevelUp(BeachPeripheralView view, LocalStore storage)
        {
            Assert.That(view.Node(293).label.text, Is.EqualTo("1.25k"));
            Assert.That(view.Node(802).label.text, Is.EqualTo("1.25k"));
            Assert.That(view.Node(293).label.color, Is.EqualTo(Color.black));
            Assert.That(view.Node(802).label.color, Is.EqualTo(Color.white));
            Assert.That(view.Node(223).label.text, Is.EqualTo("500"));
            Assert.That(view.Node(480).image.sprite.rect.size, Is.EqualTo(new Vector2(256, 256)));
            Assert.That(view.Node(480).image.fillAmount, Is.EqualTo(.5f).Within(.001f));
            Assert.That(Active(view, 324), Is.True);
            Assert.That(Active(view, 1029), Is.False);
            Assert.That(Active(view, 129), Is.False, "One incomplete muscle must keep the prestige entry hidden.");

            var progress = Load(storage);
            progress.legs = 1000;
            new ProgressStore(storage).Save(progress);
            yield return Reload(storage);
            view = FindView();
            Assert.That(Load(storage).prestigeLevel, Is.EqualTo(1), "Full bars should only enable the explicit level-up action.");
            foreach (int id in new[] { 762, 1083, 120, 480 }) Assert.That(view.Node(id).image.fillAmount, Is.EqualTo(1));
            foreach (int id in new[] { 324, 251, 1087, 1029 }) Assert.That(Active(view, id), Is.True);

            Click(view, 129);
            yield return Until(() => Active(view, 882), "The prestige confirmation should appear.");
            Click(view, 54);
            yield return Until(() => !view.IsOpen, "Confirming the level should close its menu.");
            var levelled = Load(storage);
            Assert.That(levelled.prestigeLevel, Is.EqualTo(2));
            Assert.That(new[] { levelled.chest, levelled.arms, levelled.back, levelled.legs },
                Is.EqualTo(new[] { 1250, 1000, 1000, 1000 }));
            Assert.That(levelled.coins, Is.EqualTo(910));
            Assert.That(levelled.stars, Is.EqualTo(70));
            Assert.That(levelled.unlockedOpponent, Is.EqualTo(3));
            Assert.That(view.Node(762).image.fillAmount, Is.EqualTo(.125f).Within(.001f));
            foreach (int id in new[] { 1083, 120, 480 }) Assert.That(view.Node(id).image.fillAmount, Is.Zero);
        }

        private static IEnumerator WithProgress(PlayerProgress initial, Func<BeachPeripheralView, LocalStore, IEnumerator> run, bool phone = false)
        {
            var storage = new LocalStore("BeachBoxing.PeripheralMenuTests." + Guid.NewGuid());
            float previousTimeScale = Time.timeScale;
            float previousDeltaTime = Time.captureDeltaTime;
            bool sceneLoaded = false;
            BeachGameFlow phoneFlow = null;
            bool previousFocus = true;
            try
            {
                new ProgressStore(storage).Save(initial);
                Time.timeScale = 1;
                Time.captureDeltaTime = 1f / 60;
                yield return Reload(storage);
                sceneLoaded = true;
                var view = FindView();
                if (phone)
                {
                    phoneFlow = view.GetComponent<BeachGameFlow>();
                    previousFocus = NativePlayModeScene.Field<bool>(phoneFlow, "focused");
                    NativePlayModeScene.SetField(phoneFlow, "focused", true);
                    NativePlayModeScene.ConfigurePhone(phoneFlow, storage, true);
                    yield return null;
                }
                yield return run(view, storage);
            }
            finally
            {
                if (phoneFlow) NativePlayModeScene.SetField(phoneFlow, "focused", previousFocus);
                if (sceneLoaded)
                {
                    var activeView = UnityEngine.Object.FindObjectOfType<BeachPeripheralView>();
                    if (activeView) UnityEngine.Object.Destroy(activeView.gameObject);
                }
                storage.Delete(SaveKey);
                storage.Delete(SaveKey + ".backup");
                storage.Save();
                Time.timeScale = previousTimeScale;
                Time.captureDeltaTime = previousDeltaTime;
            }
        }

        private static IEnumerator Reload(LocalStore storage)
        {
            var core = CoreRoot.GetOrCreate();
            var storageProperty = typeof(CoreRoot).GetProperty(nameof(CoreRoot.Storage));
            var savedStorage = core.Storage;
            try
            {
                storageProperty.SetValue(core, storage);
                yield return SceneManager.LoadSceneAsync(ScenePath);
                yield return null;
                yield return null;
            }
            finally
            {
                storageProperty.SetValue(core, savedStorage);
            }
        }

        private static BeachPeripheralView FindView()
        {
            var view = UnityEngine.Object.FindObjectOfType<BeachPeripheralView>();
            Assert.That(view, Is.Not.Null, "The scene must contain the preplaced peripheral view.");
            Assert.That(view.IsBound, Is.True);
            return view;
        }

        private static IEnumerator Close(BeachPeripheralView view, int id)
        {
            Click(view, id);
            yield return Until(() => !view.IsOpen && !Active(view, id) && Active(view, 855),
                "Closing should restore the same beach controls.");
        }

        private static IEnumerator Until(Func<bool> result, string message)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!result() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(result(), Is.True, message);
        }

        private static void Click(BeachPeripheralView view, int id)
        {
            var button = view.Node(id)?.button;
            Assert.That(button, Is.Not.Null, $"Source node {id} must have its preplaced button.");
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True,
                $"Source button {id} must be available to the player.");
            button.onClick.Invoke();
        }

        private static bool Active(BeachPeripheralView view, int id)
        {
            var node = view.Node(id);
            return node?.transform && node.transform.gameObject.activeInHierarchy;
        }

        private static PlayerProgress Load(LocalStore storage) => new ProgressStore(storage).Load();
    }
}
