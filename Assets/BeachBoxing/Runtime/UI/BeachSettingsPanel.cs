using System;
using UnityCore;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing
{
    public sealed class BeachSettingsPanel : MonoBehaviour
    {
        public GameObject root;
        public Button closeButton;
        public Button musicToggleButton;
        public Button sfxToggleButton;
        public Image musicToggleImage;
        public Image sfxToggleImage;
        public Sprite toggleOnSprite;
        public Sprite toggleOffSprite;
        public AudioMixer mixer;
        public string musicParameter = "mposwoH";
        public string sfxParameter = "tuRmmTJ";

        public event Action AudioChanged;
        public event Action Closed;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            BindButton(closeButton, () =>
            {
                BeachCommonUI.PlayButtonClick();
                Close();
            });
            BindButton(musicToggleButton, () =>
            {
                BeachCommonUI.PlayButtonClick();
                ToggleMusic();
            });
            BindButton(sfxToggleButton, () =>
            {
                ToggleSfx();
                BeachCommonUI.PlayButtonClick();
            });
            if (root != null) root.SetActive(false);
        }

        private static void BindButton(Button button, Action action)
        {
            if (button == null) return;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var es = EventSystem.current;
                if (es != null && es.currentSelectedGameObject == button.gameObject)
                    es.SetSelectedGameObject(null);
                action?.Invoke();
            });
        }

        public void SyncMixerFromSettings()
        {
            var audio = CoreRoot.GetOrCreate().Audio;
            ApplyMixer(audio.MusicEnabled, audio.SfxEnabled);
            RefreshVisuals(audio.MusicEnabled, audio.SfxEnabled);
        }

        public void Open()
        {
            bool wasOpen = IsOpen;
            var audio = CoreRoot.GetOrCreate().Audio;
            RefreshVisuals(audio.MusicEnabled, audio.SfxEnabled);
            ApplyMixer(audio.MusicEnabled, audio.SfxEnabled);
            if (root != null) root.SetActive(true);
            if (!wasOpen) BeachCommonUI.PlayPopupOpen();
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (root != null) root.SetActive(false);
            Closed?.Invoke();
        }

        public void HideImmediately()
        {
            if (root != null) root.SetActive(false);
        }

        public void ToggleMusic()
        {
            var audio = CoreRoot.GetOrCreate().Audio;
            audio.MusicEnabled = !audio.MusicEnabled;
            RefreshVisuals(audio.MusicEnabled, audio.SfxEnabled);
            ApplyMixer(audio.MusicEnabled, audio.SfxEnabled);
            AudioChanged?.Invoke();
        }

        public void ToggleSfx()
        {
            var audio = CoreRoot.GetOrCreate().Audio;
            audio.SfxEnabled = !audio.SfxEnabled;
            RefreshVisuals(audio.MusicEnabled, audio.SfxEnabled);
            ApplyMixer(audio.MusicEnabled, audio.SfxEnabled);
            AudioChanged?.Invoke();
        }

        private void RefreshVisuals(bool musicOn, bool sfxOn)
        {
            if (musicToggleImage != null)
                musicToggleImage.sprite = musicOn ? toggleOnSprite : toggleOffSprite;
            if (sfxToggleImage != null)
                sfxToggleImage.sprite = sfxOn ? toggleOnSprite : toggleOffSprite;
        }

        private void ApplyMixer(bool musicOn, bool sfxOn)
        {
            if (mixer == null) return;
            mixer.SetFloat(musicParameter, musicOn ? 0f : -80f);
            mixer.SetFloat(sfxParameter, sfxOn ? 0f : -80f);
        }
    }
}

