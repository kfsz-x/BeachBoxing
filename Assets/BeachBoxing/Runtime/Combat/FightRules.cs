using UnityEngine;

namespace BeachBoxing
{
    public enum FightOutcome { Pending, Victory, Defeat }

    public sealed class FightRules
    {
        public const float PunchWindup = 0.10f;
        public const float PunchPeriod = 0.25f;
        public const float HitWindow = 0.05f;
        public const float PunchRange = 1.2f;
        public const float GuardDelay = 1f;
        public const float ReferenceWorldScale = 0.356f;

        public static int PlayerCapacity(int stat) => 50 + Mathf.Max(0, stat) / 20;
        public static int EnemyCapacity(int rating, int baseCapacity = 20) => baseCapacity + Mathf.Max(0, rating) / 20;
        public static int PlayerPunchCost(int arms, int baseCost) => baseCost + arms / 700;
        public static int EnemyPunchCost(int arms, int baseCost) => baseCost + arms / 400;
        public static int PlayerPunchDamage(int arms) => 3 + arms / 500;

        // Original Combat scene: the armature is rotated -90 degrees around X and scaled 201.6.
        // Player's detached sphere is positioned once at activation; the enemy sphere follows its root.
        public static Vector3 PunchCenter(Vector3 position, Quaternion rotation, bool enemyAttack, float worldScale)
        {
            var center = new Vector3(-1.1088f, enemyAttack ? 1.907823f : 3.434477f, 1.04832f);
            return position + rotation * (center * worldScale);
        }

        public static bool PunchOverlapsBody(Vector3 sphereCenter, Vector3 defenderPosition, bool enemyAttack, float worldScale)
        {
            float sphereRadius = (enemyAttack ? 2.449387f : 2.09231f) * worldScale;
            float bodyRadius = (enemyAttack ? 0.75f : 2.25f) * worldScale;
            float bodyHeight = (enemyAttack ? 2.557898f : 2.59f) * worldScale;
            float halfSegment = (enemyAttack ? 1.75688f : 1.335949f) * worldScale;
            var closest = defenderPosition + Vector3.up * bodyHeight;
            closest.y = Mathf.Clamp(sphereCenter.y, closest.y - halfSegment, closest.y + halfSegment);
            float reach = sphereRadius + bodyRadius;
            return (sphereCenter - closest).sqrMagnitude <= reach * reach;
        }

        public static Vector3 PunchCenter(Transform actor, NativeCombatGeometry geometry, float sourceScale) =>
            actor.TransformPoint(geometry.punchCenter * sourceScale);

        public static float BodyRadius(Transform actor, NativeCombatGeometry geometry, float sourceScale)
        {
            var scale = actor.lossyScale;
            return geometry.bodyRadius * sourceScale * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }

        public static float AttachedPunchRadius(Transform actor, NativeCombatGeometry geometry, float sourceScale)
        {
            var scale = actor.lossyScale;
            return geometry.punchRadius * sourceScale *
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }

        public static bool PunchOverlapsBody(Vector3 sphereCenter, float sphereRadius, Transform defender,
            NativeCombatGeometry geometry, float sourceScale)
        {
            float radius = BodyRadius(defender, geometry, sourceScale);
            float halfSegment = Mathf.Max(0,
                geometry.bodyHeight * sourceScale * Mathf.Abs(defender.lossyScale.y) * .5f - radius);
            var center = defender.TransformPoint(geometry.bodyCenter * sourceScale);
            var axis = defender.up;
            var closest = center + axis * Mathf.Clamp(Vector3.Dot(sphereCenter - center, axis), -halfSegment, halfSegment);
            float reach = sphereRadius + radius;
            return (sphereCenter - closest).sqrMagnitude <= reach * reach;
        }

        // The collider is preplaced by the explicit Editor restore. Selection only changes its geometry.
        public static void ConfigureEnemyBody(Transform actor, NativeCombatGeometry geometry, float sourceScale, bool dead)
        {
            var capsule = actor.GetComponent<CapsuleCollider>();
            if (!capsule) return;
            capsule.direction = 1;
            capsule.center = geometry.bodyCenter * sourceScale;
            capsule.height = geometry.bodyHeight * sourceScale;
            capsule.radius = (dead ? geometry.deadBodyRadius : geometry.bodyRadius) * sourceScale;
            capsule.isTrigger = false;
            capsule.enabled = true;
        }
    }

    public sealed class FighterState
    {
        public readonly int MaxHealth, MaxStamina, MaxGuard;
        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float Guard { get; private set; }
        public bool Blocking { get; private set; }
        public bool LastImpactWasBlocked { get; private set; }
        public int ImpactCount { get; private set; }
        public float PunchTime { get; private set; } = float.PositiveInfinity;
        public bool LeftPunch { get; private set; }
        public bool DamageImmune => immunityRemaining > 0;
        public bool MovementBlocked => movementHurtRemaining > 0;
        public bool InHitWindow => !landed && PunchTime >= HitWindowStart && PunchTime < HitWindowEnd;
        private float HitWindowStart => enemyPunchTiming ? 4f / 60f : FightRules.PunchWindup;
        private float HitWindowEnd => enemyPunchTiming ? (LeftPunch ? 7f : 8f) / 60f :
            FightRules.PunchWindup + FightRules.HitWindow;

        private float guardWait, staminaWait;
        private bool landed;
        private readonly float guardRecovery, staminaRecovery, healthRecovery;
        private readonly bool recoverStaminaWhileGuarding;
        private readonly bool enemyPunchTiming;
        private readonly float damageImmunity, movementHurtDuration;
        private float immunityRemaining, movementHurtRemaining;

        public FighterState(int health, int stamina, int guard, float guardRecovery = 5f,
            float staminaRecovery = 8f, float healthRecovery = 0f, bool recoverStaminaWhileGuarding = false,
            bool enemyPunchTiming = false, float damageImmunity = 0, float movementHurtDuration = 0)
        {
            Health = MaxHealth = health;
            Stamina = MaxStamina = stamina;
            Guard = MaxGuard = guard;
            this.guardRecovery = guardRecovery;
            this.staminaRecovery = staminaRecovery;
            this.healthRecovery = healthRecovery;
            this.recoverStaminaWhileGuarding = recoverStaminaWhileGuarding;
            this.enemyPunchTiming = enemyPunchTiming;
            this.damageImmunity = Mathf.Max(0, damageImmunity);
            this.movementHurtDuration = Mathf.Max(0, movementHurtDuration);
        }

        public void Tick(float seconds, bool block)
        {
            Blocking = block && Health > 0;
            PunchTime += seconds;
            immunityRemaining = Mathf.Max(0, immunityRemaining - seconds);
            movementHurtRemaining = Mathf.Max(0, movementHurtRemaining - seconds);
            guardWait -= seconds;
            staminaWait -= seconds;
            if (Health <= 0) return;
            if (guardWait <= 0) Guard = Mathf.Min(MaxGuard, Guard + guardRecovery * seconds);
            if (recoverStaminaWhileGuarding ? block : staminaWait <= 0)
                Stamina = Mathf.Min(MaxStamina, Stamina + staminaRecovery * seconds);
            if (block) Health = Mathf.Min(MaxHealth, Health + healthRecovery * seconds);
        }

        public bool Punch(int cost)
        {
            if (Blocking || Stamina <= 0 || Health <= 0 || PunchTime < FightRules.PunchPeriod) return false;
            Stamina = Mathf.Max(0, Stamina - cost);
            staminaWait = 0.55f;
            PunchTime = 0;
            LeftPunch = !LeftPunch;
            landed = false;
            return true;
        }

        public bool TryLand(FighterState opponent, float distance, int damage)
        {
            return TryLand(opponent, distance <= FightRules.PunchRange, damage);
        }

        public bool TryLand(FighterState opponent, bool overlaps, int damage)
        {
            if (!InHitWindow || !overlaps || opponent.Health <= 0) return false;
            landed = true;
            // Source OnTriggerEnter consumes the contact even if EnemyManager rejects it as immune.
            if (opponent.DamageImmune) return false;
            opponent.immunityRemaining = opponent.damageImmunity;
            opponent.movementHurtRemaining = opponent.movementHurtDuration;
            opponent.LastImpactWasBlocked = opponent.Blocking && opponent.Guard > 0;
            opponent.ImpactCount++;
            if (opponent.LastImpactWasBlocked)
            {
                opponent.Guard = Mathf.Max(0, opponent.Guard - damage);
                opponent.guardWait = FightRules.GuardDelay;
            }
            else
            {
                opponent.Health = Mathf.Max(0, opponent.Health - damage);
            }
            return true;
        }
    }
}
