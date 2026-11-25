using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    public readonly struct InputFrame
    {
        public readonly Vector2 Move;
        public readonly bool AttackPressed, AttackReleased, AttackHeld, BlockHeld, InteractPressed, BoostHeld, BoostPressed, BoostTouch;
        public readonly bool JumpPressed, JumpReleased;

        public InputFrame(Vector2 move, bool attack, bool previous, bool block, bool interact, bool boost, bool pulse = false,
            bool boostPressed = false, bool boostTouch = false, bool jumpPressed = false, bool jumpReleased = false)
        {
            Move = move;
            AttackPressed = pulse || (attack && !previous);
            AttackReleased = !attack && previous;
            AttackHeld = attack;
            BlockHeld = block;
            InteractPressed = interact;
            BoostHeld = boost;
            BoostPressed = boostPressed;
            BoostTouch = boostTouch;
            JumpPressed = jumpPressed;
            JumpReleased = jumpReleased;
        }
    }

    public sealed class BeachInput
    {
        private bool attackPreviously;
        private bool waitForRelease;
        private bool touchDevice;
        public TouchControl Move, Attack, Block, Boost;
        public System.Func<int, KeyCode> KeyBinding;
        public System.Func<Vector2> NativeMove;
        public System.Func<bool> NativePunch, NativeBlock;
        public bool Phone;

        public KeyCode Key(int index)
        {
            if (KeyBinding != null) return KeyBinding(index);
            switch (index)
            {
                case 0: return KeyCode.W;
                case 1: return KeyCode.A;
                case 2: return KeyCode.S;
                case 3: return KeyCode.D;
                case 4: return KeyCode.Space;
                case 5: return KeyCode.Escape;
                case 6: return KeyCode.M;
                case 7: return KeyCode.I;
                case 8: return KeyCode.E;
                default: throw new System.ArgumentOutOfRangeException(nameof(index));
            }
        }

        public bool UseTouchLayout(bool mobilePlatform, bool touchSupported, bool portrait, bool touching)
        {
            // Keep device capability across releases and gameplay input resets.
            touchDevice |= mobilePlatform || touchSupported || touching;
            return touchDevice || portrait;
        }

        public InputFrame ReadFrame()
        {
            var move = new Vector2(
                Axis(Key(1), KeyCode.LeftArrow, Key(3), KeyCode.RightArrow),
                Axis(Key(2), KeyCode.DownArrow, Key(0), KeyCode.UpArrow));
            bool mouseAttack = !Phone && Input.GetMouseButton(0) && Input.touchCount == 0 &&
                (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject());
            bool keyBlock = Input.GetKey(KeyCode.Space) || (!Phone && Input.touchCount == 0 && Input.GetMouseButton(1));
            bool keyBoost = Input.GetKey(KeyCode.E);
            bool keyInteract = Input.GetKeyDown(Key(8));
            bool pulseBoost = Input.GetKeyDown(KeyCode.E);
            bool jumpPressed = Input.GetKeyDown(Key(4)), jumpReleased = Input.GetKeyUp(Key(4));
            if (waitForRelease)
            {
                if (!mouseAttack && !keyBlock && !keyBoost && !Input.GetKey(Key(4)) &&
                    !Input.GetKey(Key(8)) && move == Vector2.zero) waitForRelease = false;
                move = Vector2.zero;
                mouseAttack = keyBlock = keyBoost = keyInteract = false;
                pulseBoost = jumpPressed = jumpReleased = false;
            }
            if (move == Vector2.zero) move = NativeMove != null ? NativeMove() : Move ? Move.Direction : Vector2.zero;
            bool attack = (NativePunch?.Invoke() == true) || (Attack && Attack.Held) || mouseAttack;
            bool pulse = (Attack && Attack.ConsumePress()) || (mouseAttack && Input.GetMouseButtonDown(0));
            bool block = (NativeBlock?.Invoke() == true) || (Block && Block.Held) || keyBlock;
            bool boost = (Boost && Boost.Held) || keyBoost;
            bool boostPointer = Boost && Boost.ConsumePress();
            var frame = new InputFrame(Vector2.ClampMagnitude(move, 1), attack, attackPreviously, block,
                keyInteract, boost, pulse, boostPointer || pulseBoost, touchDevice || Input.touchSupported,
                jumpPressed, jumpReleased);
            attackPreviously = attack;
            return frame;
        }

        public void Reset()
        {
            if (Move) Move.Release();
            if (Attack) Attack.Release();
            if (Block) Block.Release();
            if (Boost) Boost.Release();
            attackPreviously = false;
            waitForRelease = true;
        }

        private static int Axis(KeyCode negative, KeyCode negativeAlt, KeyCode positive, KeyCode positiveAlt) =>
            (Input.GetKey(positive) || Input.GetKey(positiveAlt) ? 1 : 0) -
            (Input.GetKey(negative) || Input.GetKey(negativeAlt) ? 1 : 0);
    }
}
