using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing.Tests
{
    public sealed class NativeNameKeyboardRoutingTests
    {
        [Test]
        public void RepeatedTapsReachTheKeyboardHookWithoutChangingSelection()
        {
            EventSystem system = EventSystem.current;
            GameObject ownedSystem = null;
            if (!system)
            {
                ownedSystem = new GameObject("test keyboard event system", typeof(EventSystem));
                system = ownedSystem.GetComponent<EventSystem>();
            }
            var previous = system.currentSelectedGameObject;
            var field = new GameObject("test native name field", typeof(BeachNativeNameKeyboard));
            var owner = new GameObject("test settings owner", typeof(BeachNativeSettingsView));
            try
            {
                field.GetComponent<BeachNativeNameKeyboard>().owner = owner.GetComponent<BeachNativeSettingsView>();
                system.SetSelectedGameObject(field);
                Assert.That(system.currentSelectedGameObject, Is.SameAs(field));
                var click = new PointerEventData(system) { button = PointerEventData.InputButton.Left };
                // Selecting an already selected field does not dispatch OnSelect again.
                system.SetSelectedGameObject(field);
                for (int tap = 0; tap < 2; tap++)
                {
                    Assert.That(ExecuteEvents.Execute(field, click, ExecuteEvents.pointerClickHandler), Is.True,
                        "A repeated tap must have a keyboard route even when selection is unchanged.");
                    Assert.That(system.currentSelectedGameObject, Is.SameAs(field));
                }
                // OpenNativeKeyboard's Editor/platform guard prevents native SDK calls here.
            }
            finally
            {
                system.SetSelectedGameObject(previous);
                Object.DestroyImmediate(field);
                Object.DestroyImmediate(owner);
                if (ownedSystem) Object.DestroyImmediate(ownedSystem);
            }
        }

        [Test]
        public void AnUnboundNameFieldCanReceiveSelectionAndPointerEvents()
        {
            var field = new GameObject("test unbound name field", typeof(BeachNativeNameKeyboard));
            try
            {
                var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                Assert.That(ExecuteEvents.Execute(field, new BaseEventData(EventSystem.current), ExecuteEvents.selectHandler), Is.True);
                Assert.That(ExecuteEvents.Execute(field, click, ExecuteEvents.pointerClickHandler), Is.True,
                    "The preplaced hook must remain safe before its owner is bound.");
            }
            finally { Object.DestroyImmediate(field); }
        }
    }
}
