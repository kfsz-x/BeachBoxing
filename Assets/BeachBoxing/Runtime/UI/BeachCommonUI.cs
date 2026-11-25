using System.Collections;
using UnityCore;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing
{
    public sealed class BeachCommonUI : MonoBehaviour
    {
        public const string PrefabResourcePath = "BeachBoxing/CommonUI";
        public const string InitScenePath = "Assets/GameFrame/Init.unity";
        public const string GameScenePath = "Assets/BeachBoxing/Game.unity";

        public static BeachCommonUI Instance { get; private set; }
        public static bool IsTransitioning { get; private set; }

        public BeachLoadingView Loading;
        public BeachSettingsPanel Settings;
        public BeachToastView Toast;

        [Header("UI Audio")]
        public AudioSource uiSfxSource;
        public AudioClip buttonClickClip;
        public AudioClip popupOpenClip;
        public AudioClip rewardClip;
        public AudioClip errorClip;
        [Range(0f, 1f)] public float buttonClickVolume = 1f;
        [Range(0f, 1f)] public float popupOpenVolume = 0.85f;
        [Range(0f, 1f)] public float rewardVolume = 0.85f;
        [Range(0f, 1f)] public float errorVolume = 0.85f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            IsTransitioning = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CoreRoot.GetOrCreate();
            BeachPlatformInput.EnsureEventSystem(transform);
            EnsureUiSfxSource();
            if (Settings != null) Settings.SyncMixerFromSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void PlayButtonClick()
        {
            var audio = CoreRoot.Current != null ? CoreRoot.Current.Audio : null;
            if (audio != null && !audio.SfxEnabled) return;
            var common = Ensure();
            if (common == null) return;
            common.PlayButtonClickInternal(audio != null ? audio.SfxVolume : 1f);
        }

        public static void PlayPopupOpen(bool fallbackToClick = false)
        {
            var audio = CoreRoot.Current != null ? CoreRoot.Current.Audio : null;
            if (audio != null && !audio.SfxEnabled) return;
            var common = Ensure();
            if (common == null) return;
            float sfxVolume = audio != null ? audio.SfxVolume : 1f;
            if (common.popupOpenClip != null)
                common.PlayClipInternal(common.popupOpenClip, common.popupOpenVolume * sfxVolume);
            else if (fallbackToClick)
                common.PlayButtonClickInternal(sfxVolume);
        }

        public static void PlayReward()
        {
            var audio = CoreRoot.Current != null ? CoreRoot.Current.Audio : null;
            if (audio != null && !audio.SfxEnabled) return;
            var common = Ensure();
            if (common == null) return;
            var clip = common.rewardClip != null ? common.rewardClip : OriginalArt.Get<AudioClip>("Audio/Training");
            common.PlayClipInternal(clip, common.rewardVolume * (audio != null ? audio.SfxVolume : 1f));
        }

        public static void PlayError()
        {
            var audio = CoreRoot.Current != null ? CoreRoot.Current.Audio : null;
            if (audio != null && !audio.SfxEnabled) return;
            var common = Ensure();
            if (common == null || common.errorClip == null) return;
            common.PlayClipInternal(common.errorClip, common.errorVolume * (audio != null ? audio.SfxVolume : 1f));
        }

        private void PlayButtonClickInternal(float sfxVolume)
        {
            if (buttonClickClip == null)
                buttonClickClip = OriginalArt.Get<AudioClip>("Audio/Click");
            PlayClipInternal(buttonClickClip, buttonClickVolume * sfxVolume);
        }

        private void PlayClipInternal(AudioClip clip, float rawVolume)
        {
            EnsureUiSfxSource();
            if (uiSfxSource == null || clip == null) return;
            float volume = Mathf.Clamp01(rawVolume);
            if (volume <= 0f) return;
            uiSfxSource.PlayOneShot(clip, volume);
        }

        private void EnsureUiSfxSource()
        {
            if (uiSfxSource == null)
            {
                uiSfxSource = GetComponent<AudioSource>();
                if (uiSfxSource == null)
                    uiSfxSource = gameObject.AddComponent<AudioSource>();
            }
            uiSfxSource.playOnAwake = false;
            uiSfxSource.loop = false;
            uiSfxSource.spatialBlend = 0f;
            uiSfxSource.ignoreListenerPause = true;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        public static BeachCommonUI Ensure()
        {
            if (Instance != null) return Instance;
            var existing = FindObjectOfType<BeachCommonUI>(true);
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }
            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError("Missing shared BeachCommonUI prefab at Resources/" + PrefabResourcePath);
                return null;
            }
            var go = Instantiate(prefab);
            go.name = nameof(BeachCommonUI);
            return Instance != null ? Instance : go.GetComponent<BeachCommonUI>();
        }

        public void EnterGame()
        {
            if (IsTransitioning) return;
            StartCoroutine(LoadSceneRoutine(GameScenePath, showLoading: true));
        }

        public void ReturnToInit()
        {
            if (IsTransitioning) return;
            StartCoroutine(LoadSceneRoutine(InitScenePath, showLoading: false));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BeachPlatformInput.EnsureEventSystem(transform);
            if (Settings != null)
            {
                Settings.HideImmediately();
                Settings.SyncMixerFromSettings();
            }
            if (Toast != null) Toast.HideImmediately();
        }

        private IEnumerator LoadSceneRoutine(string scenePath, bool showLoading)
        {
            IsTransitioning = true;
            Time.timeScale = 1f;
            if (Settings != null) Settings.HideImmediately();
            if (Toast != null) Toast.HideImmediately();

            if (showLoading && Loading != null)
            {
                Loading.Show(0f);
                yield return null;
            }

            var op = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (op == null)
            {
                if (Loading != null) Loading.Hide();
                IsTransitioning = false;
                yield break;
            }

            op.allowSceneActivation = false;
            float visualProgress = 0f;
            float elapsed = 0f;
            const float minDisplayTime = 0.25f;

            while (op.progress < 0.9f || (showLoading && elapsed < minDisplayTime))
            {
                elapsed += Time.unscaledDeltaTime;
                float target = Mathf.Clamp01(op.progress / 0.9f);
                if (showLoading)
                {
                    float timeCap = Mathf.Clamp01(elapsed / minDisplayTime);
                    visualProgress = Mathf.MoveTowards(visualProgress, Mathf.Min(target, timeCap), Time.unscaledDeltaTime * 3.5f);
                    if (Loading != null) Loading.SetProgress(visualProgress);
                }
                yield return null;
            }

            if (showLoading && Loading != null)
            {
                Loading.SetProgress(1f);
                yield return null;
            }

            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;

            yield return null;
            if (Loading != null) Loading.Hide();
            IsTransitioning = false;
        }
    }
}

