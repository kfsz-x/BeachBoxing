using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachMobileStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        private const float DeadZone = 0.12f;

        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform baseRing;
        [SerializeField] private RectTransform knob;

        public Vector2 Direction { get; private set; }
        private Func<bool> gate;
        private int? pointerId;
        private Vector2 originScreen;
        private float radiusScreen;

        public void Bind(Func<bool> canAccept)
        {
            gate = canAccept;
            ResetStick();
        }

        private void OnEnable() => BeachTouchModule.PointerCanceled += OnPointerCanceled;
        private void OnDisable()
        {
            BeachTouchModule.PointerCanceled -= OnPointerCanceled;
            ResetStick();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
            if (gate != null && !gate()) return;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            pointerId = eventData.pointerId;
            eventData.useDragThreshold = false;
            var ring = baseRing != null ? baseRing : (RectTransform)transform;
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            radiusScreen = Mathf.Max(16f, ring.rect.width * 0.42f * scale);
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            originScreen = RectTransformUtility.WorldToScreenPoint(cam, ring.position);
            UpdateDrag(eventData.position, scale);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (pointerId != eventData.pointerId) return;
            if (gate != null && !gate()) { ResetStick(); return; }
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            UpdateDrag(eventData.position, scale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pointerId == eventData.pointerId) ResetStick();
        }

        private void OnPointerCanceled(int canceledId)
        {
            if (pointerId == canceledId) ResetStick();
        }

        public void ResetStick()
        {
            pointerId = null;
            Direction = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateDrag(Vector2 screenPos, float scale)
        {
            Vector2 delta = screenPos - originScreen;
            Vector2 norm = delta / Mathf.Max(1f, radiusScreen);
            float mag = norm.magnitude;
            Direction = mag <= DeadZone
                ? Vector2.zero
                : norm / mag * Mathf.Clamp01(Mathf.InverseLerp(DeadZone, 1f, mag));
            if (knob != null)
            {
                float maxLocal = (baseRing != null ? baseRing.rect.width : ((RectTransform)transform).rect.width) * 0.34f;
                knob.anchoredPosition = Vector2.ClampMagnitude(delta / scale, maxLocal);
            }
        }
    }
}

