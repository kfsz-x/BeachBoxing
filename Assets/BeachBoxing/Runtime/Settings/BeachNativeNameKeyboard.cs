using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    /// <summary>Preplaced selection and repeat-tap hook for the real vivo keyboard API.</summary>
    public sealed class BeachNativeNameKeyboard : MonoBehaviour, ISelectHandler, IPointerClickHandler
    {
        public BeachNativeSettingsView owner;
        public void OnSelect(BaseEventData eventData)
        {
            if (owner) owner.OpenNativeKeyboard();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Left && owner)
                owner.OpenNativeKeyboard();
        }
    }
}
