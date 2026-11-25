using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing
{
    /// <summary>The original mobile hand starts interaction on pointer down.</summary>
    public sealed class NativeStationTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Action interact;
        private int pointer = int.MinValue;
        public void Bind(Action action) => interact = action;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || pointer != int.MinValue) return;
            var button = GetComponent<Button>();
            if (button && !button.IsInteractable()) return;
            pointer = data.pointerId;
            interact?.Invoke();
        }

        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) pointer = int.MinValue; }
        public void OnPointerExit(PointerEventData data) => OnPointerUp(data);
        private void OnDisable() => pointer = int.MinValue;
    }
}
