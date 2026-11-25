using System;
using System.Globalization;
using UnityCore;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using QGMiniGame;
#endif

namespace BeachBoxing
{
    /// <summary>Owns the preplaced source settings UI. All art/filter/clip references are editor-bound.</summary>
    public sealed class BeachNativeSettingsView : MonoBehaviour
    {
        public BeachPeripheralView peripheral;
        public Slider sfxSlider, musicSlider;
        public Text sfxText, musicText;
        public InputField nameInput;
        public AudioMixer mixer;
        public string musicParameter = "mposwoH", sfxParameter = "tuRmmTJ";
        public string[] profanityWords = Array.Empty<string>();
        public AnimationClip menuOpen, menuClose, profanityClip;
        public RectTransform musicGroup, sfxGroup;
        public Vector3 trainingMusicPosition, fightMusicPosition, trainingSfxPosition, fightSfxPosition;

        private static readonly int[] KeyButtons = { 358, 641, 1105, 1212, 844, 920, 439, 809, 1183 };
        private static readonly int[] KeyLabels = { 118, 838, 1192, 241, 1188, 110, 583, 1152, 822 };
        private static readonly int[] KeyBackgrounds = { 35, 1041, 240, 254, 459, 447, 763, 450, 773 };
        private static readonly KeyCode[] EnumeratedKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        private BeachNativeSettingsRules rules;
        private Func<string> getName;
        private Action<string> rename;
        private Action closed, returnToBeach;
        private bool open, closing, focused = true, volumeDirty, profanityActive, fightContext;
        private float menuTime, volumeTime, profanityTime;
        private int picking = -1;
        private bool nativeKeyboardOpen, suppressEndEdit;
#if UNITY_WEBGL && !UNITY_EDITOR
        private string keyboardInputId, keyboardConfirmId, keyboardCompleteId;
#endif

        public bool IsOpen => open;
        public bool AcceptsActions => open && !closing;
        public bool IsEditingText => AcceptsActions && (nativeKeyboardOpen || nameInput && nameInput.isFocused);
        public bool IsListening => AcceptsActions && picking >= 0 && Object(498) && Object(498).activeInHierarchy;
        public KeyCode GetKey(int index) => rules != null ? rules.GetKey(index) : DefaultKey(index);

        private static KeyCode DefaultKey(int index)
        {
            var defaults = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space,
                KeyCode.Escape, KeyCode.M, KeyCode.I, KeyCode.E };
            if (index < 0 || index >= defaults.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return defaults[index];
        }

        public void Bind(LocalStore storage, Func<string> getName, Action<string> rename,
            Action closed, Action returnToBeach)
        {
            this.getName = getName ?? throw new ArgumentNullException(nameof(getName));
            this.rename = rename ?? throw new ArgumentNullException(nameof(rename));
            this.closed = closed;
            this.returnToBeach = returnToBeach;
            rules = new BeachNativeSettingsRules(storage, profanityWords);
            Hook(1089, Close);
            Hook(857, () => ShowKeys(true));
            Hook(683, () => ShowKeys(false));
            Hook(1000517, RequestReturnToBeach);
            for (int i = 0; i < KeyButtons.Length; i++)
            {
                int index = i;
                Hook(KeyButtons[i], () => BeginRebind(index));
            }
            if (sfxSlider)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(VolumeChanged);
            }
            if (musicSlider)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(VolumeChanged);
            }
            if (nameInput)
            {
                nameInput.onEndEdit.RemoveAllListeners();
                nameInput.onEndEdit.AddListener(CommitName);
            }
            RefreshValues();
            SetActive(80, false);
            SetActive(933, false);
            open = closing = profanityActive = false;
        }

        public void Open()
        {
            if (rules == null || open && !closing) return;
            open = true;
            closing = false;
            menuTime = 0;
            RefreshValues();
            SetActive(80, true);
            Sample(menuOpen, 80, 0);
            if (!menuOpen || menuOpen.length <= 0) SetActive(1089, true);
        }

        public void Close()
        {
            if (!AcceptsActions) return;
            EndNativeKeyboard(true);
            if (nameInput) nameInput.DeactivateInputField();
            FlushVolumes();
            closing = true;
            menuTime = 0;
            Sample(menuClose, 80, 0);
        }

        public void HideImmediately()
        {
            EndNativeKeyboard(true);
            if (nameInput) nameInput.DeactivateInputField();
            FlushVolumes();
            open = closing = false;
            SetActive(80, false);
            // Native KeybindsManager has no OnDisable reset: retain picking for reopening.
        }

        public void SetFightContext(bool fighting)
        {
            fightContext = fighting;
            SetActive(458, !fighting);
            SetActive(205, !fighting);
            SetActive(547, !fighting);
            SetActive(1000517, fighting);
            if (musicGroup) musicGroup.anchoredPosition3D = fighting ? fightMusicPosition : trainingMusicPosition;
            if (sfxGroup) sfxGroup.anchoredPosition3D = fighting ? fightSfxPosition : trainingSfxPosition;
        }

        private void RequestReturnToBeach()
        {
            if (!AcceptsActions || !fightContext) return;
            HideImmediately();
            returnToBeach?.Invoke();
        }

        private void RefreshValues()
        {
            if (sfxSlider) sfxSlider.SetValueWithoutNotify(rules.SfxVolume);
            if (musicSlider) musicSlider.SetValueWithoutNotify(rules.MusicVolume);
            if (nameInput) nameInput.SetTextWithoutNotify(getName() ?? string.Empty);
            for (int i = 0; i < KeyLabels.Length; i++)
                Label(KeyLabels[i], i == picking ? " " : rules.GetKey(i).ToString());
            RefreshVolumeLabels();
            ApplyMixer();
        }

        private void ShowKeys(bool visible)
        {
            if (!AcceptsActions) return;
            SetActive(1190, !visible);
            SetActive(498, visible);
        }

        private void BeginRebind(int index)
        {
            if (!AcceptsActions) return;
            picking = index;
            SetActive(KeyBackgrounds[index], true);
            Label(KeyLabels[index], " ");
        }

        private void PollHeldKeys(Func<KeyCode, bool> held)
        {
            if (!focused || !IsListening || IsEditingText) return;
            foreach (KeyCode key in EnumeratedKeys)
            {
                if (!held(key)) continue;
                int index = picking;
                picking = -1;
                rules.Rebind(index, key);
                Label(KeyLabels[index], key.ToString());
                SetActive(KeyBackgrounds[index], false);
                break;
            }
        }

        private void VolumeChanged(float value)
        {
            if (!AcceptsActions) return;
            if (!volumeDirty) volumeTime = 0.03f;
            volumeDirty = true;
        }

        private void FlushVolumes()
        {
            if (!volumeDirty || rules == null) return;
            volumeDirty = false;
            rules.SetVolumes(sfxSlider ? sfxSlider.value : rules.SfxVolume,
                musicSlider ? musicSlider.value : rules.MusicVolume);
            RefreshVolumeLabels();
            ApplyMixer();
        }

        private void RefreshVolumeLabels()
        {
            if (sfxText) sfxText.text = rules.SfxVolume.ToString("0.0", CultureInfo.InvariantCulture);
            if (musicText) musicText.text = rules.MusicVolume.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private void ApplyMixer()
        {
            if (!mixer) return;
            mixer.SetFloat(sfxParameter, BeachNativeSettingsRules.MapVolumeToDecibels(rules.SfxVolume));
            mixer.SetFloat(musicParameter, BeachNativeSettingsRules.MapVolumeToDecibels(rules.MusicVolume));
        }

        private void CommitName(string value)
        {
            if (suppressEndEdit || nativeKeyboardOpen || !AcceptsActions || rules == null) return;
            if (nameInput && nameInput.wasCanceled)
            {
                nameInput.SetTextWithoutNotify(getName() ?? string.Empty);
                return;
            }
            AcceptName(value);
        }

        private void AcceptName(string value)
        {
            if (!AcceptsActions || rules == null) return;
            if (rules.IsAllowedName(value)) rename(value);
            else
            {
                if (nameInput) nameInput.SetTextWithoutNotify(getName() ?? string.Empty);
                profanityTime = 0;
                profanityActive = true;
                SetActive(933, true);
                Sample(profanityClip, 933, 0);
            }
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void Tick(float dt)
        {
            if (volumeDirty)
            {
                volumeTime -= dt;
                if (volumeTime <= 0) FlushVolumes();
            }
            if (open)
            {
                AnimationClip clip = closing ? menuClose : menuOpen;
                float duration = clip ? clip.length : 0;
                if (closing || menuTime < duration)
                {
                    menuTime += dt;
                    Sample(clip, 80, Mathf.Min(menuTime, Mathf.Max(0, duration - 0.000001f)));
                    if (menuTime >= duration)
                    {
                        if (closing)
                        {
                            open = closing = false;
                            SetActive(80, false);
                            closed?.Invoke();
                        }
                        else SetActive(1089, true);
                    }
                }
                PollHeldKeys(Input.GetKey);
            }
            if (profanityActive)
            {
                profanityTime += dt;
                Sample(profanityClip, 933, profanityTime);
                if (!profanityClip || profanityTime >= profanityClip.length)
                {
                    profanityActive = false;
                    SetActive(933, false);
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus) EndNativeKeyboard(true);
            if (!hasFocus && nameInput) nameInput.DeactivateInputField();
        }

        private void OnDisable()
        {
            EndNativeKeyboard(true);
            if (nameInput) nameInput.DeactivateInputField();
            FlushVolumes();
        }

        public void OpenNativeKeyboard()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!AcceptsActions || fightContext || nativeKeyboardOpen || !nameInput || !QG.isVivo()) return;
            nativeKeyboardOpen = true;
            nameInput.shouldHideMobileInput = true;
            keyboardInputId = QG.OnKeyboardInput(response =>
            {
                if (nativeKeyboardOpen && AcceptsActions) nameInput.SetTextWithoutNotify(response.value ?? string.Empty);
            });
            keyboardConfirmId = QG.OnKeyboardConfirm(response =>
            {
                if (!nativeKeyboardOpen) return;
                nameInput.SetTextWithoutNotify(response.value ?? string.Empty);
                EndNativeKeyboard(true);
            });
            keyboardCompleteId = QG.OnKeyboardComplete(response =>
            {
                if (!nativeKeyboardOpen) return;
                nameInput.SetTextWithoutNotify(response.value ?? string.Empty);
                EndNativeKeyboard(true);
            });
            QG.ShowKeyboard(new KeyboardParam
            {
                defaultValue = nameInput.text, maxLength = int.MaxValue,
                multiple = false, confirmHold = false, confirmType = "done"
            });
#endif
        }

        private void EndNativeKeyboard(bool commit)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!nativeKeyboardOpen) return;
            nativeKeyboardOpen = false;
            if (commit && nameInput) AcceptName(nameInput.text);
            if (!string.IsNullOrEmpty(keyboardInputId)) QG.OffKeyboardInput(keyboardInputId, null);
            if (!string.IsNullOrEmpty(keyboardConfirmId)) QG.OffKeyboardConfirm(keyboardConfirmId, null);
            if (!string.IsNullOrEmpty(keyboardCompleteId)) QG.OffKeyboardComplete(keyboardCompleteId, null);
            keyboardInputId = keyboardConfirmId = keyboardCompleteId = null;
            QG.OffKeyboardInput();
            QG.OffKeyboardConfirm();
            QG.OffKeyboardComplete();
            QG.HideKeyboard();
            suppressEndEdit = true;
            if (nameInput) nameInput.DeactivateInputField();
            suppressEndEdit = false;
#endif
        }

        private void Hook(int sourceId, Action callback)
        {
            var button = peripheral ? peripheral.Node(sourceId)?.button : null;
            if (!button) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
        }

        private GameObject Object(int sourceId) => peripheral ? peripheral.Node(sourceId)?.transform?.gameObject : null;
        private void SetActive(int sourceId, bool active)
        {
            var target = Object(sourceId);
            if (target) target.SetActive(active);
        }
        private void Label(int sourceId, string value)
        {
            var text = peripheral ? peripheral.Node(sourceId)?.label : null;
            if (text) text.text = value;
        }
        private void Sample(AnimationClip clip, int sourceId, float time)
        {
            var target = Object(sourceId);
            if (clip && target) clip.SampleAnimation(target, time);
        }
    }
}
