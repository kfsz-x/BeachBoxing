using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    public sealed class BeachToastView : MonoBehaviour
    {
        public GameObject root;
        public Text messageLabel;
        public CanvasGroup group;

        private float remaining;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        public void Show(string message, float duration = 1.6f)
        {
            if (root == null || messageLabel == null) return;
            messageLabel.text = message ?? "";
            remaining = Mathf.Max(0.4f, duration);
            root.SetActive(true);
            if (group != null) group.alpha = 1f;
        }

        public void HideImmediately()
        {
            remaining = 0f;
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                root.SetActive(false);
                return;
            }
            if (group != null && remaining < 0.35f)
                group.alpha = Mathf.Clamp01(remaining / 0.35f);
        }
    }
}

