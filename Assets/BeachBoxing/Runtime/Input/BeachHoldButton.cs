using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held => pointerId.HasValue;
        private Func<bool> gate;
        private int? pointerId;
        private bool pressedPulse;
        private bool releasedPulse;

        public void Bind(Func<bool> canAccept)
        {
            gate = canAccept;
            ResetButton();
        }

        public bool ConsumePress()
        {
            bool value = pressedPulse;
            pressedPulse = false;
            return value;
        }

        public bool ConsumeRelease()
        {
            bool value = releasedPulse;
            releasedPulse = false;
            return value;
        }

        private void OnEnable() => BeachTouchModule.PointerCanceled += OnPointerCanceled;
        private void OnDisable()
        {
            BeachTouchModule.PointerCanceled -= OnPointerCanceled;
            ResetButton();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId.HasValue || eventData.button != PointerEventData.InputButton.Left) return;
            if (gate != null && !gate()) return;
            pointerId = eventData.pointerId;
            pressedPulse = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pointerId == eventData.pointerId)
            {
                pointerId = null;
                releasedPulse = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (pointerId == eventData.pointerId)
            {
                pointerId = null;
                releasedPulse = true;
            }
        }

        private void OnPointerCanceled(int canceledId)
        {
            if (pointerId == canceledId)
            {
                pointerId = null;
                releasedPulse = true;
            }
        }

        public void ResetButton()
        {
            pointerId = null;
            pressedPulse = false;
            releasedPulse = false;
        }
    }
}

