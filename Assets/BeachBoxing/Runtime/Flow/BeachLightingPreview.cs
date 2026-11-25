using UnityEngine;

namespace BeachBoxing
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class BeachLightingPreview : MonoBehaviour
    {
        public BeachGameFlow flow;

        private void OnEnable() => Preview();
        private void LateUpdate() => Preview();

        private void Preview()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && flow) flow.PreviewSceneLighting();
            #endif
        }
    }
}
