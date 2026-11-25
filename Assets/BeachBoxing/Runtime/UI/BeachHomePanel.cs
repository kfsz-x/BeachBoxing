using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing
{
    public sealed class BeachHomePanel : MonoBehaviour
    {
        public GameObject root;
        public Button startButton;
        public RectTransform startButtonVisual;
        public Button settingsButton;
        public Button subscribeButton;
        public Button addDesktopButton;
        public Button shareButton;
        public Button entryRewardButton;
        public GameObject entryRewardAlert;

        private Vector3 startBaseScale = Vector3.one;

        private void Awake()
        {
            BeachCommonUI.Ensure();
            if (startButtonVisual != null) startBaseScale = startButtonVisual.localScale;
            BindButton(startButton, OnStartClicked);
            BindButton(settingsButton, OnSettingsClicked);
            BindButton(subscribeButton, () => BeachAdService.RequestSdkEntry("订阅"));
            BindButton(addDesktopButton, () => BeachAdService.RequestSdkEntry("添加桌面"));
            BindButton(shareButton, () => BeachAdService.RequestSdkEntry("分享"));
            BindButton(entryRewardButton, () => BeachAdService.RequestSdkEntry("入口有奖"));
        }

        private void OnEnable()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (startButtonVisual != null)
            {
                float wave = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.03f;
                startButtonVisual.localScale = startBaseScale * wave;
            }
        }

        private void OnStartClicked()
        {
            var common = BeachCommonUI.Ensure();
            if (common != null) common.EnterGame();
        }

        private void OnSettingsClicked()
        {
            var common = BeachCommonUI.Ensure();
            if (common != null && common.Settings != null) common.Settings.Open();
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
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
                BeachCommonUI.PlayButtonClick();
                action?.Invoke();
            });
        }
    }
}

