using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    /// <summary>Routes preplaced landscape controls through the active gameplay context.</summary>
    [DisallowMultipleComponent]
    public sealed class BeachNativeMobileControls : MonoBehaviour
    {
        public BeachNativeFloatingJoystick beachJoystick, combatJoystick;
        public GameObject combatRoot, blockKeyboardPrompt, punchKeyboardPrompt, combatMoveTutorial;

        private Func<bool> acceptBeachInput, acceptCombatInput;
        private Action queueJump, interact, interactUp;
        private BeachPhase currentPhase;
        private bool phone, beachEligible, combatEligible, combatScreen, combatTutorialDismissed;
        private int? punchPointer, blockPointer;

        public Vector2 MoveDirection
        {
            get
            {
                if (!phone) return Vector2.zero;
                if (AcceptCombat() && combatJoystick) return combatJoystick.Direction;
                return AcceptBeach() && beachJoystick ? beachJoystick.Direction : Vector2.zero;
            }
        }

        public bool PunchHeld => punchPointer.HasValue && AcceptCombat();
        public bool BlockHeld => blockPointer.HasValue && AcceptCombat();

        public void Bind(Func<bool> acceptBeachInput, Func<bool> acceptCombatInput, Action queueJump,
            Action interact, Action interactUp = null)
        {
            this.acceptBeachInput = acceptBeachInput;
            this.acceptCombatInput = acceptCombatInput;
            this.queueJump = queueJump;
            this.interact = interact;
            this.interactUp = interactUp;
            if (beachJoystick) beachJoystick.Bind(() => phone && AcceptBeach());
            if (combatJoystick) combatJoystick.Bind(() => phone && AcceptCombat());
            ResetInput();
        }

        public void Render(bool phone, BeachPhase phase, bool fightScreenVisible, bool beachEligible, bool combatEligible)
        {
            if (this.phone != phone || currentPhase != phase) ResetInput();
            this.phone = phone;
            currentPhase = phase;
            this.beachEligible = phase == BeachPhase.Training && beachEligible;
            combatScreen = phase == BeachPhase.Fight && fightScreenVisible;
            this.combatEligible = combatScreen && combatEligible;
            if (phase != BeachPhase.Fight) combatTutorialDismissed = false;

            if (!phone || !AcceptBeach())
            {
                if (beachJoystick) beachJoystick.ResetInput();
            }
            if (!AcceptCombat())
            {
                punchPointer = blockPointer = null;
                if (combatJoystick) combatJoystick.ResetInput();
            }
            SetActive(combatRoot, combatScreen);
            if (combatJoystick) SetActive(combatJoystick.gameObject, phone);
            SetActive(blockKeyboardPrompt, !phone);
            SetActive(punchKeyboardPrompt, !phone);
            SetActive(combatMoveTutorial, phone && !combatTutorialDismissed);
        }

        public void ObserveCombatMove(Vector2 effectiveMove)
        {
            if (!AcceptCombat() || effectiveMove.sqrMagnitude == 0) return;
            combatTutorialDismissed = true;
            SetActive(combatMoveTutorial, false);
        }

        public void ResetInput()
        {
            punchPointer = blockPointer = null;
            if (beachJoystick) beachJoystick.ResetInput();
            if (combatJoystick) combatJoystick.ResetInput();
        }

        public void Jump()
        {
            if (AcceptBeach()) queueJump?.Invoke();
        }

        public void Interact()
        {
            if (AcceptBeach()) interact?.Invoke();
        }

        public void InteractUp()
        {
            if (AcceptBeach()) interactUp?.Invoke();
        }

        public void PressPunch(BaseEventData data) => Press(data, ref punchPointer);
        public void PressBlock(BaseEventData data) => Press(data, ref blockPointer);
        public void ReleasePunch(BaseEventData data) => Release(data, ref punchPointer);
        public void ReleaseBlock(BaseEventData data) => Release(data, ref blockPointer);

        private void OnDisable() => ResetInput();
        private bool AcceptBeach() => beachEligible && acceptBeachInput?.Invoke() == true;
        private bool AcceptCombat() => combatEligible && acceptCombatInput?.Invoke() == true;

        private void Press(BaseEventData data, ref int? owner)
        {
            if (!owner.HasValue && AcceptCombat() && data is PointerEventData pointer)
                owner = pointer.pointerId;
        }

        private static void Release(BaseEventData data, ref int? owner)
        {
            if (data is PointerEventData pointer && owner == pointer.pointerId) owner = null;
        }

        private static void SetActive(GameObject node, bool active)
        {
            if (node && node.activeSelf != active) node.SetActive(active);
        }
    }
}
