using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing
{
    [DefaultExecutionOrder(32000)]
    public sealed class BeachChineseUi : MonoBehaviour
    {
        public Text[] labels;

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (labels == null) return;
            foreach (var label in labels)
            {
                if (!label || label is OriginalSdfText) continue;
                var input = label.GetComponentInParent<InputField>();
                if (input && input.textComponent == label) continue;
                string translated = BeachChineseText.Translate(label.text);
                if (translated != label.text) label.text = translated;
            }
        }
    }
}
