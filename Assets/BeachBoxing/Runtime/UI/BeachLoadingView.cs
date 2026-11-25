using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    public sealed class BeachLoadingView : MonoBehaviour
    {
        public GameObject root;
        public Image progressFill;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        public void Show(float initialProgress = 0f)
        {
            SetProgress(initialProgress);
            if (root != null) root.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        public void SetProgress(float value)
        {
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(value);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }
    }
}

