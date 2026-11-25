using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Enables the opaque depth buffer required by the original volume decals.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    public sealed class BeachDecalDepth : MonoBehaviour
    {
        private void OnEnable()
        {
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}
