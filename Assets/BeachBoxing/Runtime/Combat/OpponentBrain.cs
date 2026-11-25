using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace BeachBoxing
{
    public sealed class OpponentBrain
    {
        private readonly NativeOpponentAi ai;
        private readonly Random random;
        private readonly float worldScale;
        private float decisionTime = 5f, staminaCheckTime = 1f;
        private float movementTime = 5f, horizontalTime = 5f, clock;
        private int vertical, horizontal;
        private bool moving, movingHorizontally, blocking;
        private readonly List<float> unblockTimes = new List<float>();
        public float LateralSpeed { get; private set; }

        public OpponentBrain(OpponentProfile profile, int seed, float worldScale = FightRules.ReferenceWorldScale)
        {
            ai = profile.ai;
            random = new Random(seed);
            this.worldScale = worldScale;
        }

        public (float speed, bool block, bool attack) Step(float elapsed, float separation, FighterState self, FighterState target)
        {
            if (self.Health <= 0) { LateralSpeed = 0; return (0, false, false); }
            clock += elapsed;
            decisionTime -= elapsed;
            staminaCheckTime -= elapsed;
            movementTime -= elapsed;
            horizontalTime -= elapsed;
            // Native Invoke allows several pending Unblocks while stamina remains low.
            for (int i = unblockTimes.Count - 1; i >= 0; i--)
                if (unblockTimes[i] <= clock) { blocking = false; unblockTimes.RemoveAt(i); }
            if (staminaCheckTime <= 0)
            {
                staminaCheckTime = 1f;
                if (ai.startsBlockingAtCertainStamina && self.Stamina <= ai.staminaToStartBlocking)
                {
                    blocking = true;
                    unblockTimes.Add(clock + ai.averageTimeBlocking +
                        ((float)random.NextDouble() - .5f) * ai.timeBlockingVariance);
                }
            }
            bool attack = false;
            if (decisionTime <= .00001f)
            {
                // Start selects one attack mode in this precise order.
                if (ai.randomPunches)
                {
                    attack = true;
                    decisionTime = ai.punchInterval + ((float)random.NextDouble() - .5f) * ai.punchIntervalVariation;
                }
                else if (ai.constantPunches) { attack = true; decisionTime = ai.constantPunchCooldown; }
                else if (ai.distancePunch)
                {
                    attack = separation < ai.distanceToPunch * worldScale;
                    decisionTime = ai.distancePunchCooldown;
                }
                else decisionTime = float.PositiveInfinity;
            }
            if (ai.horizontalMovement && horizontalTime <= 0)
            {
                movingHorizontally = !movingHorizontally;
                horizontal = movingHorizontally ? random.Next(-1, 1) : 0;
                horizontalTime = (movingHorizontally ? ai.timeDoingHorizontalMovement : ai.horizontalMovementCooldown)
                    + random.Next(-1, 1);
            }
            if (movementTime <= 0)
            {
                // Takumi also has aggressive=true; staminaBasedMovement wins the native chain.
                if (ai.randomMovement)
                {
                    moving = !moving;
                    horizontal = moving ? random.Next(-1, 1) : 0;
                    vertical = moving ? random.Next(-1, 1) : 0;
                    movementTime = (moving ? ai.timeMoving : ai.timeStationary) + random.Next(-1, 1);
                }
                else if (ai.staminaBasedMovement)
                {
                    if (self.Stamina < ai.staminaToBackOut) vertical = -1;
                    else if (self.Stamina > ai.staminaToMoveIn) vertical = 1;
                    movementTime = 1f;
                }
                else if (ai.aggressive)
                {
                    moving = !moving; vertical = moving ? 1 : 0;
                    movementTime = moving ? ai.timeAggressing : ai.timeNotAggressing;
                }
                else movementTime = float.PositiveInfinity;
            }
            float speed = (blocking ? ai.blockSpeed : ai.speed) * worldScale;
            float magnitude = Mathf.Max(1f, Mathf.Sqrt(vertical * vertical + horizontal * horizontal));
            LateralSpeed = horizontal * speed / magnitude;
            return (vertical * speed / magnitude, blocking, attack && !blocking);
        }
    }
}
