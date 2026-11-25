using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachNativeFloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Canvas canvas;
        public RectTransform background, handle, landscape;
        public Vector2 Direction { get; private set; }

        private Func<bool> acceptsInput;
        private int? pointer;
        private RectTransform HitArea => (RectTransform)transform;
        private Camera EventCamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        public void Bind(Func<bool> gate)
        {
            acceptsInput = gate;
            ResetInput();
        }

        public void ApplyLandscapePose()
        {
            if (!landscape) return;
            HitArea.position = landscape.position;
            HitArea.localScale = landscape.localScale;
        }

        private void Start()
        {
            ApplyLandscapePose();
            ResetInput();
        }

        private void OnDisable() => ResetInput();

        public void OnPointerDown(PointerEventData data)
        {
            if (pointer.HasValue || (acceptsInput != null && !acceptsInput())) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(HitArea, data.position, EventCamera, out var local))
                return;

            // The source moves the background relative to its authored anchor inside the 10000² hit area.
            pointer = data.pointerId;
            background.anchoredPosition = local + Vector2.Scale(HitArea.sizeDelta, HitArea.pivot - background.anchorMax);
            background.gameObject.SetActive(true);
            OnDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (pointer != data.pointerId) return;
            if (acceptsInput != null && !acceptsInput())
            {
                ResetInput();
                return;
            }

            var center = RectTransformUtility.WorldToScreenPoint(EventCamera, background.position);
            var radius = background.sizeDelta * .5f;
            var screenRadius = radius * canvas.scaleFactor;
            var offset = data.position - center;
            Direction = Vector2.ClampMagnitude(new Vector2(offset.x / screenRadius.x, offset.y / screenRadius.y), 1);
            handle.anchoredPosition = Vector2.Scale(Direction, radius);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (pointer == data.pointerId) ResetInput();
        }

        public void ResetInput()
        {
            pointer = null;
            Direction = Vector2.zero;
            if (handle) handle.anchoredPosition = Vector2.zero;
            if (background) background.gameObject.SetActive(false);
        }
    }
}
