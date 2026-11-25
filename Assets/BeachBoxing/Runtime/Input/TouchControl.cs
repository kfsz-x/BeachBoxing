using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    public sealed class TouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, IPointerExitHandler
    {
        public bool Joystick { get; set; }
        public bool Held { get; private set; }
        public Vector2 Direction { get; private set; }
        public bool pulseEveryPointerDown;
        private int ownerId;
        private Vector2 origin;
        private bool pressed;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            if (pulseEveryPointerDown) pressed = true;
            if (Held) return;
            ownerId = data.pointerId;
            origin = data.position;
            Held = true;
            pressed = true;
        }

        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnDrag(PointerEventData data)
        {
            if (Joystick && Held && data.pointerId == ownerId)
                Direction = Vector2.ClampMagnitude((data.position - origin) / Mathf.Max(36, Screen.width * 0.14f), 1);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (Held && data.pointerId == ownerId) ClearPointer();
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (!Joystick) OnPointerUp(data);
        }

        public void OnEndDrag(PointerEventData data) => OnPointerUp(data);
        private void OnDisable() => Release();

        public void Release()
        {
            ClearPointer();
            pressed = false;
        }

        public bool ConsumePress()
        {
            bool value = pressed;
            pressed = false;
            return value;
        }

        private void ClearPointer()
        {
            Held = false;
            Direction = Vector2.zero;
        }
    }
}
