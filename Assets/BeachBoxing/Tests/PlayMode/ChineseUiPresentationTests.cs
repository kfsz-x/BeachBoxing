using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class ChineseUiPresentationTests
    {
        [UnityTest]
        public IEnumerator SavedSceneShowsOneHudAndRendersChineseAfterDynamicUpdates()
        {
            yield return NativePlayModeScene.WithScene(true, (flow, store) => Exercise(flow));
        }

        private static IEnumerator Exercise(BeachGameFlow flow)
        {
            var progress = NativePlayModeScene.Field<PlayerProgress>(flow, "progress");
            progress.playerName = "Cozy Undies";
            progress.firstMove = progress.completedTutorial = progress.unlockedOpponent = 1;
            NativePlayModeScene.Call(NativePlayModeScene.Field<object>(flow, "stage"), "ApplyProgress", progress);
            flow.GetComponent<BeachNativeTutorialView>().BeachEntered();
            flow.GetComponent<BeachNativeTutorialView>().ObserveMove(Vector2.up);
            yield return null;
            Assert.That(Object.FindObjectsOfType<BeachGameFlow>().Length, Is.EqualTo(1));
            var hud = NativePlayModeScene.Field<object>(flow, "hud");
            foreach (var name in new[] { "stats", "menu", "workout", "picker", "move" })
                Assert.That(NativePlayModeScene.Field<GameObject>(hud, name).activeInHierarchy, Is.False, name);
            Assert.That(NativePlayModeScene.Field<GameObject>(hud, "trainingTitle").activeInHierarchy, Is.True);
            var title = flow.transform.Find("Player/Original title canvas/NameText").GetComponent<Text>();
            Canvas.ForceUpdateCanvases();
            Assert.That(title.text, Is.EqualTo("惬意的内裤"));
            Assert.That(title.cachedTextGenerator.vertexCount, Is.GreaterThan(0));
            var view = flow.GetComponent<BeachPeripheralView>();
            Assert.That(view.Node(760).button.isActiveAndEnabled, Is.True);
            yield return Capture("chinese-home.png");
            var text = view.Node(205).label as OriginalSdfText;
            Assert.That(text, Is.Not.Null);
            Assert.That(text.useLocalizedFont, Is.True);
            text.text = "Change Name";
            Assert.That(text.text, Is.EqualTo("修改昵称"));
            Assert.That(text.mainTexture, Is.EqualTo(text.font.material.mainTexture));
            Assert.That("修改昵称训练商店签到胜利".All(text.font.HasCharacter), Is.True);
            NativePlayModeScene.Click(view, 760);
            yield return new WaitForSecondsRealtime(.4f);
            Canvas.ForceUpdateCanvases();
            Assert.That(text.cachedTextGenerator.vertexCount, Is.GreaterThan(0));
            yield return Capture("chinese-settings.png");
            var input = flow.GetComponent<BeachNativeSettingsView>().nameInput;
            Assert.That(input.textComponent.text, Is.EqualTo("惬意的内裤"));
            input.text = "我的昵称 MyName";
            Assert.That(input.textComponent.text, Is.EqualTo("我的昵称 MyName"));
            input.text = "Back";
            input.onEndEdit.Invoke(input.text);
            var nameLabel = flow.transform.Find("Player/Original title canvas/NameText").GetComponent<Text>();
            Assert.That(nameLabel.text, Is.EqualTo("Back"), "Custom names must remain literal outside the input field too.");
            Assert.That(NativePlayModeScene.Field<PlayerProgress>(flow, "progress").customPlayerName, Is.True);
            var saved = NativePlayModeScene.Field<ProgressStore>(flow, "store").Load();
            Assert.That(saved.customPlayerName, Is.True);
            Assert.That(saved.playerName, Is.EqualTo("Back"));
            Assert.That(BeachChineseText.Translate("LVL 4 Trainer"), Is.EqualTo("4级教练"));
            Assert.That(BeachChineseText.Translate("125kg"), Is.EqualTo("125公斤"));
            Assert.That(BeachChineseText.Translate("TIME LEFT: 120"), Is.EqualTo("剩余时间：120"));
            NativePlayModeScene.Click(view, 1089);
            yield return new WaitForSecondsRealtime(.25f);

            foreach (var panel in new[] { new[] { 855, 1146 }, new[] { 510, 145 } })
            {
                NativePlayModeScene.Click(view, panel[0]);
                yield return NativePlayModeScene.Until(() => NativePlayModeScene.Available(view.Node(panel[1]).button), 2f, "Peripheral close button must be ready.");
                yield return Capture(panel[0] == 855 ? "chinese-shop.png" : "chinese-daily.png");
                NativePlayModeScene.Click(view, panel[1]);
                yield return NativePlayModeScene.Until(() => !view.IsOpen, 2f, "Peripheral menu must close.");
            }

            var player = flow.transform.Find("Player");
            NativePlayModeScene.PlacePlayer(player, flow.transform.Find("Training beach/Arms/Original equipment").position);
            yield return NativePlayModeScene.Until(() => NativePlayModeScene.Available(view.Node(1057).button), 2f, "Training interaction must be ready.");
            NativePlayModeScene.Click(view, 1057);
            yield return null;
            Assert.That(NativePlayModeScene.Field<GameObject>(hud, "workout").activeInHierarchy, Is.False);
            Assert.That(flow.GetComponent<BeachNativeTrainingView>().IsOpen, Is.True);
            yield return Capture("chinese-training.png");
            NativePlayModeScene.Click(view, 377);
            NativePlayModeScene.PlacePlayer(player, flow.transform.Find(NativePlayModeScene.RingPath).position);
            yield return NativePlayModeScene.Until(() => NativePlayModeScene.Available(view.Node(806).button), 2f, "Opponent menu must be ready.");
            Assert.That(NativePlayModeScene.Field<GameObject>(hud, "picker").activeInHierarchy, Is.False);
            Assert.That(view.Node(332).label.text, Is.EqualTo("凯尔"));
            yield return NativePlayModeScene.Until(() => NativePlayModeScene.Available(view.Node(214).button), 2f, "Opponent opening animation must finish.");
            yield return Capture("chinese-opponents.png");
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../.superpowers/localization-1123"));
            System.IO.Directory.CreateDirectory(directory);
            var pixels = ScreenCapture.CaptureScreenshotAsTexture();
            try { System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name), pixels.EncodeToPNG()); }
            finally { Object.Destroy(pixels); }
        }
    }
}
