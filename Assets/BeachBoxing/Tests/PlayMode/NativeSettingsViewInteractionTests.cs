using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    // Root may copy this uniquely named draft into Tests/PlayMode after scene append/integration.
    public sealed class NativeSettingsViewInteractionTests
    {
        [UnityTest]
        public IEnumerator SettingsCanBeClosedByClickingAfterTheOpeningAnimation()
        {
            yield return WithScene(Exercise);

            IEnumerator Exercise(BeachNativeSettingsView view, BeachPeripheralView native, LocalStore store, Callbacks state)
            {
                var button = native.Node(1089).button;
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    view.Open();
                    yield return new WaitForSecondsRealtime(view.menuOpen.length + 0.05f);
                    Assert.That(button.isActiveAndEnabled, Is.True, "The close button must become available after opening.");

                    Canvas.ForceUpdateCanvases();
                    var pointer = new PointerEventData(EventSystem.current)
                    {
                        button = PointerEventData.InputButton.Left,
                        position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
                    };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits, Is.Not.Empty);
                    var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                    Assert.That(handler, Is.EqualTo(button.gameObject));
                    ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
                    yield return new WaitForSecondsRealtime(view.menuClose.length + 0.05f);

                    Assert.That(view.IsOpen, Is.False);
                    Assert.That(state.closed, Is.EqualTo(attempt));
                }
            }
        }

        [UnityTest]
        public IEnumerator NativeVolumeAndEndEditNameCallbacksUseSavedControls()
        {
            yield return WithScene((view, native, store, state) => Exercise(view, native, store, state));
            IEnumerator Exercise(BeachNativeSettingsView view, BeachPeripheralView native, LocalStore store, Callbacks state)
            {
                view.Open();
                yield return new WaitForSecondsRealtime(0.36f);
                view.sfxSlider.value = 0;
                view.musicSlider.value = 2;
                Assert.That(store.GetFloat("SFXVolume"), Is.EqualTo(1), "Native volume callback waits 0.03 seconds.");
                yield return new WaitForSecondsRealtime(0.05f);
                Assert.That(store.GetFloat("SFXVolume"), Is.Zero);
                Assert.That(store.GetFloat("MusicVolume"), Is.EqualTo(2));
                Assert.That(view.sfxText.text, Is.EqualTo("0.0"));
                Assert.That(view.musicText.text, Is.EqualTo("2.0"));
                Assert.That(view.mixer.GetFloat(view.sfxParameter, out float sfx), Is.True);
                Assert.That(sfx, Is.EqualTo(-80).Within(0.0001f));
                Assert.That(view.mixer.GetFloat(view.musicParameter, out float music), Is.True);
                Assert.That(music, Is.EqualTo(20).Within(0.0001f));
                // Continual dragging must keep the first scheduled deadline, not debounce indefinitely.
                view.sfxSlider.value = 0.2f;
                Tick(view, 0.02f);
                Assert.That(store.GetFloat("SFXVolume"), Is.Zero);
                view.sfxSlider.value = 0.4f;
                Tick(view, 0.02f);
                Assert.That(store.GetFloat("SFXVolume"), Is.EqualTo(0.4f).Within(0.0001f), "A held drag still applies native volume changes.");
                view.nameInput.text = "Kind Runner  ";
                view.nameInput.onEndEdit.Invoke(view.nameInput.text);
                Assert.That(state.name, Is.EqualTo("Kind Runner  "), "End-edit saves original whitespace.");
                view.nameInput.text = "f u c k";
                view.nameInput.onEndEdit.Invoke(view.nameInput.text);
                Assert.That(state.name, Is.EqualTo("Kind Runner  "));
                Assert.That(view.nameInput.text, Is.EqualTo("Kind Runner  "));
                Assert.That(native.Node(933).transform.gameObject.activeSelf, Is.True);
                view.nameInput.ActivateInputField();
                yield return null;
                Assert.That(view.IsEditingText, Is.True);
                native.Node(857).button.onClick.Invoke();
                native.Node(358).button.onClick.Invoke();
                Poll(view, key => key == KeyCode.Mouse1);
                Assert.That(view.GetKey(0), Is.EqualTo(KeyCode.W), "Text focus gates keyboard capture as well as gameplay.");
                view.nameInput.DeactivateInputField();
            }
        }

        [UnityTest]
        public IEnumerator HiddenPendingRebindResumesAndFocusLossDoesNotCapture()
        {
            yield return WithScene((view, native, store, state) => Exercise(view, native, state));
            IEnumerator Exercise(BeachNativeSettingsView view, BeachPeripheralView native, Callbacks state)
            {
                view.Open();
                yield return new WaitForSecondsRealtime(0.36f);
                native.Node(857).button.onClick.Invoke();
                native.Node(358).button.onClick.Invoke();
                Assert.That(view.IsListening, Is.True);
                view.HideImmediately();
                Poll(view, key => key == KeyCode.Mouse0);
                Assert.That(view.IsListening, Is.False);
                Assert.That(view.GetKey(0), Is.EqualTo(KeyCode.W));
                view.Open();
                Assert.That(view.IsListening, Is.True, "Native pending picking survives hiding.");
                Focus(view, false);
                Poll(view, key => key == KeyCode.Mouse0);
                Assert.That(view.GetKey(0), Is.EqualTo(KeyCode.W));
                Focus(view, true);
                Poll(view, key => key == KeyCode.Mouse0);
                Assert.That(view.GetKey(0), Is.EqualTo(KeyCode.Mouse0));
                Assert.That(view.IsListening, Is.False);
                Assert.That(native.Node(118).label.text, Is.EqualTo("Mouse0"));
                view.Close();
                Assert.That(view.IsOpen, Is.True);
                Assert.That(view.AcceptsActions, Is.False);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(view.IsOpen, Is.False);
                Assert.That(state.closed, Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator FightVariantHidesRenameAndUsesNativeBeachHomeWhilePaused()
        {
            yield return WithScene((view, native, store, state) => Exercise(view, native, state));
            IEnumerator Exercise(BeachNativeSettingsView view, BeachPeripheralView native, Callbacks state)
            {
                view.SetFightContext(false);
                Assert.That(native.Node(1000517).transform.gameObject.activeSelf, Is.False);
                view.SetFightContext(true);
                Assert.That(native.Node(458).transform.gameObject.activeSelf, Is.False);
                Assert.That(native.Node(205).transform.gameObject.activeSelf, Is.False);
                Assert.That(native.Node(547).transform.gameObject.activeSelf, Is.False);
                Assert.That(view.musicGroup.anchoredPosition.y, Is.EqualTo(-159.4999847f).Within(0.0001f));
                Assert.That(view.sfxGroup.anchoredPosition.y, Is.EqualTo(-33.0677452f).Within(0.0001f));
                Time.timeScale = 0;
                view.Open();
                yield return new WaitForSecondsRealtime(0.36f);
                Assert.That(native.Node(1000517).button.gameObject.activeInHierarchy, Is.True);
                native.Node(1000517).button.onClick.Invoke();
                Assert.That(state.beachHome, Is.EqualTo(1));
                Assert.That(view.IsOpen, Is.False);
                view.SetFightContext(false);
                Assert.That(native.Node(458).transform.gameObject.activeSelf, Is.True);
            }
        }

        private sealed class Callbacks
        {
            public string name = "Kind Runner";
            public int closed, beachHome;
        }

        private static IEnumerator WithScene(Func<BeachNativeSettingsView, BeachPeripheralView, LocalStore, Callbacks, IEnumerator> run)
        {
            yield return NativePlayModeScene.WithScene(false, Exercise);

            IEnumerator Exercise(BeachGameFlow flow, LocalStore store)
            {
                var view = flow.GetComponent<BeachNativeSettingsView>();
                Assert.That(view, Is.Not.Null);
                var native = flow.GetComponent<BeachPeripheralView>();
                var state = new Callbacks();
                float music = 0, sfx = 0;
                bool hadMusic = view.mixer && view.mixer.GetFloat(view.musicParameter, out music);
                bool hadSfx = view.mixer && view.mixer.GetFloat(view.sfxParameter, out sfx);
                try
                {
                    view.Bind(store, () => state.name, value => state.name = value,
                        () => state.closed++, () => state.beachHome++);
                    yield return run(view, native, store, state);
                }
                finally
                {
                    view.HideImmediately();
                    if (hadMusic) view.mixer.SetFloat(view.musicParameter, music);
                    if (hadSfx) view.mixer.SetFloat(view.sfxParameter, sfx);
                }
            }
        }

        private static void Poll(BeachNativeSettingsView view, Func<KeyCode, bool> held) =>
            typeof(BeachNativeSettingsView).GetMethod("PollHeldKeys", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { held });
        private static void Focus(BeachNativeSettingsView view, bool focused) =>
            typeof(BeachNativeSettingsView).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { focused });
        private static void Tick(BeachNativeSettingsView view, float delta) =>
            typeof(BeachNativeSettingsView).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(view, new object[] { delta });
    }
}
