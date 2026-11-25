using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeSettingsFlowTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator NativeMenuUsesBeachClockAndBlocksUnderlyingActions()
        {
            yield return WithScene(flow => Check(flow, false));
        }

        [UnityTest]
        public IEnumerator FightMenuPausesAndReturnsToPreplacedBeach()
        {
            yield return WithScene(flow => Check(flow, true));
        }

        [UnityTest]
        public IEnumerator AcceptedEmptyNameSurvivesSceneReload()
        {
            yield return WithScene(CheckEmptyName);
        }

        private static IEnumerator CheckEmptyName(BeachGameFlow flow)
        {
            var view = flow.GetComponent<BeachNativeSettingsView>();
            Call(flow, "TogglePause");
            view.nameInput.text = "";
            view.nameInput.onEndEdit.Invoke("");
            Assert.That(((PlayerProgress)Field(flow, "progress")).playerName, Is.Empty);
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var reopened = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            Assert.That(((PlayerProgress)Field(reopened, "progress")).playerName, Is.Empty,
                "Original GeneratedName guard must preserve an accepted empty name.");
        }

        private static IEnumerator Check(BeachGameFlow flow, bool fighting)
        {
            if (fighting) Call(flow, "EnterFight");
            var view = flow.GetComponent<BeachNativeSettingsView>();
            var stage = Field(flow, "stage");
            var boxer = stage.GetType().GetField("Player").GetValue(stage);
            var root = (Transform)boxer.GetType().GetField("Root").GetValue(boxer);
            var body = root.GetComponent<Rigidbody>();
            var momentum = new Vector3(1.25f, 2.5f, -3.75f);
            body.velocity = momentum;
            Call(flow, "TogglePause");
            Assert.That(view.IsOpen, Is.True, "Game menu must use source GO80, not legacy pause UI.");
            Assert.That(Time.timeScale, Is.EqualTo(fighting ? 0 : 1));
            Assert.That(body.velocity, Is.EqualTo(fighting ? momentum : Vector3.zero),
                "Boxing pause preserves physical momentum; beach EnterMenu explicitly clears its body.");
            Assert.That((bool)typeof(BeachGameFlow).GetProperty("CanAcceptGameplayAction", Flags).GetValue(flow), Is.False);
            Assert.That(view.peripheral.Node(1000517).transform.gameObject.activeSelf, Is.EqualTo(fighting));
            yield return new WaitForSecondsRealtime(0.36f);
            if (fighting)
            {
                view.peripheral.Node(1000517).button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.6f);
                Assert.That(Field(flow, "phase"), Is.EqualTo(BeachPhase.Training));
                Assert.That(Field(flow, "fight"), Is.Null);
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            else
            {
                view.nameInput.text = "Source Name  ";
                view.nameInput.onEndEdit.Invoke(view.nameInput.text);
                Assert.That(((PlayerProgress)Field(flow, "progress")).playerName, Is.EqualTo("Source Name  "));
                view.peripheral.Node(1089).button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(view.IsOpen, Is.False);
                Assert.That((bool)typeof(BeachGameFlow).GetProperty("CanAcceptGameplayAction", Flags).GetValue(flow), Is.True);
            }
        }

        private static IEnumerator WithScene(Func<BeachGameFlow, IEnumerator> run)
        {
            var core = CoreRoot.GetOrCreate();
            var property = typeof(CoreRoot).GetProperty(nameof(CoreRoot.Storage));
            var original = core.Storage;
            var storage = new LocalStore("BeachBoxing.SettingsFlow." + Guid.NewGuid());
            BeachGameFlow flow = null;
            try
            {
                property.SetValue(core, storage);
                new ProgressStore(storage).Save(new PlayerProgress { playerName = "Settings Test", unlockedOpponent = 1 });
                yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
                yield return null;
                flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
                typeof(BeachGameFlow).GetField("focused", Flags).SetValue(flow, true);
                yield return run(flow);
            }
            finally
            {
                property.SetValue(core, original);
                if (flow) UnityEngine.Object.Destroy(flow.gameObject);
                foreach (string key in new[] { "beachBoxing.progress", "beachBoxing.progress.backup", "FirstTimeInGame", "FirstTimeStarted", "MusicVolume", "SFXVolume", "Sensitivity" }) storage.Delete(key);
                for (int i = 0; i < 9; i++) storage.Delete("Keybind" + i);
                storage.Save();
                Time.timeScale = 1;
            }
        }

        private static object Field(BeachGameFlow flow, string name) => typeof(BeachGameFlow).GetField(name, Flags).GetValue(flow);
        private static void Call(BeachGameFlow flow, string name) => typeof(BeachGameFlow).GetMethod(name, Flags).Invoke(flow, null);
    }
}
