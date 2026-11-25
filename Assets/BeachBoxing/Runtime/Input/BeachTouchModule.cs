using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    public sealed class BeachTouchModule : StandaloneInputModule
    {
        public static event Action<int> PointerCanceled;
        private readonly List<PointerEventData> canceledPointers = new List<PointerEventData>();

        public override void ActivateModule()
        {
            base.ActivateModule();
            if (eventSystem != null)
                eventSystem.sendNavigationEvents = false;
        }

        public override void Process()
        {
            if (eventSystem != null && eventSystem.sendNavigationEvents)
                eventSystem.sendNavigationEvents = false;
            for (int index = 0; index < input.touchCount; index++)
            {
                Touch touch = input.GetTouch(index);
                if (touch.phase == TouchPhase.Canceled)
                    PointerCanceled?.Invoke(touch.fingerId);
            }
            CancelInactivePointers(beforeProcess: true);
            base.Process();
            CancelInactivePointers(beforeProcess: false);
        }

        public override void DeactivateModule()
        {
            CancelAllPointers();
            base.DeactivateModule();
        }

        protected override void OnDisable()
        {
            CancelAllPointers();
            base.OnDisable();
        }

        private void CancelInactivePointers(bool beforeProcess)
        {
            foreach (var pointer in m_PointerData.Values)
            {
                if (pointer.pointerId == kMouseLeftId || pointer.pointerId == kMouseRightId || pointer.pointerId == kMouseMiddleId)
                    continue;

                if (!TryGetTouch(pointer.pointerId, out Touch touch) || touch.phase == TouchPhase.Canceled ||
                    (beforeProcess && touch.phase == TouchPhase.Began) || (!beforeProcess && touch.phase == TouchPhase.Ended))
                    canceledPointers.Add(pointer);
            }
            FlushCanceledPointers();
        }

        private bool TryGetTouch(int pointerId, out Touch touch)
        {
            for (int index = 0; index < input.touchCount; index++)
            {
                touch = input.GetTouch(index);
                if (touch.fingerId == pointerId)
                    return true;
            }
            touch = default;
            return false;
        }

        private void CancelAllPointers()
        {
            canceledPointers.AddRange(m_PointerData.Values);
            FlushCanceledPointers();
        }

        private void FlushCanceledPointers()
        {
            while (canceledPointers.Count > 0)
            {
                int last = canceledPointers.Count - 1;
                PointerEventData pointer = canceledPointers[last];
                canceledPointers.RemoveAt(last);
                PointerCanceled?.Invoke(pointer.pointerId);
                pointer.eligibleForClick = false;
                pointer.pointerCurrentRaycast = default;
                ProcessTouchPress(pointer, pressed: false, released: true);
                RemovePointerData(pointer);
            }
        }
    }
}

