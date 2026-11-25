using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Faces the scene's already placed title canvas toward its bound camera.</summary>
    // Update after camera feedback has applied its final rotation for this frame.
    [ExecuteAlways, DefaultExecutionOrder(1100)]
    public sealed class BeachNameBillboard : MonoBehaviour
    {
        public Camera View;
        public Transform Head;
        public float HeightAboveHead = .3f;

        private void LateUpdate()
        {
            if (Head) transform.position = Head.position + Vector3.up * HeightAboveHead;
            if (View) transform.rotation = View.transform.rotation;
        }
    }
}
