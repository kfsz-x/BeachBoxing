using UnityEngine;

namespace BeachBoxing
{
    public sealed class FightController
    {
        public readonly FighterState Player, Enemy;
        public float Countdown => externalOpening == null ? countdown :
            BeachPresentationTimeline.OpeningDuration - externalOpening.OpeningTime;
        public FightOutcome Outcome { get; private set; }
        public float PlayerFlash { get; private set; }
        public float EnemyFlash { get; private set; }
        public Vector2 PlayerAnimationAxes { get; private set; }
        public Vector2 EnemyAnimationAxes { get; private set; }
        public bool EmptyPunchRequested { get; private set; }
        private readonly Transform player, enemy;
        private readonly BeachPresentationTimeline externalOpening;
        private float countdown = BeachPresentationTimeline.OpeningDuration;
        private readonly OpponentBrain brain;
        private readonly System.Random random;
        private readonly PlayerProgress progress;
        private readonly Rect bounds;
        private readonly int enemyDamage, enemyArms;
        private readonly float worldScale, enemySourceScale;
        private readonly NativeCombatGeometry playerGeometry, enemyGeometry;
        private Vector3 playerPunchCenter;
        private bool playerPunchPositionCaptured;
        private readonly bool nativePhysics;
        private readonly Rigidbody playerBody, enemyBody;
        private readonly float enemyMoveSpeed, enemyBlockSpeed;
        private Quaternion playerFacingTarget;

        public FightController(Transform player, Transform enemy, PlayerProgress progress, OpponentProfile opponent, Rect bounds, int seed,
            float worldScale = FightRules.ReferenceWorldScale, bool nativePhysics = false, BeachPresentationTimeline externalOpening = null)
        {
            this.player = player;
            this.enemy = enemy;
            this.progress = progress;
            this.bounds = bounds;
            this.worldScale = worldScale;
            this.nativePhysics = nativePhysics;
            this.externalOpening = externalOpening;
            playerBody = player.GetComponent<Rigidbody>();
            enemyBody = enemy.GetComponent<Rigidbody>();
            playerGeometry = NativeCombatGeometry.Player();
            enemyGeometry = opponent.geometry;
            enemySourceScale = worldScale * BeachProgressionRules.RootScale(Mathf.Max(1, opponent.prestige));
            FightRules.ConfigureEnemyBody(enemy, enemyGeometry, enemySourceScale, false);
            random = new System.Random(seed);
            brain = new OpponentBrain(opponent, seed, worldScale);
            Player = new FighterState(FightRules.PlayerCapacity(progress.chest), FightRules.PlayerCapacity(progress.legs),
                20 + progress.back / 60, 3f + progress.back / 200, 13f + progress.back / 300,
                1.6f + progress.back / 800, recoverStaminaWhileGuarding: true, movementHurtDuration: .3f);
            var ai = opponent.ai;
            Enemy = new FighterState((int)ai.maxHealth + opponent.chest / 20,
                (int)ai.maxStamina + opponent.legs / 20, 20 + opponent.back / 60,
                ai.blockRecoverRate + opponent.back / 450,
                ai.staminaRecoverRate + opponent.back / 300,
                ai.healthRecoverRate + opponent.back / 800,
                recoverStaminaWhileGuarding: true, enemyPunchTiming: true,
                damageImmunity: ai.invincibleCooldown, movementHurtDuration: ai.invincibleCooldown);
            enemyMoveSpeed = ai.speed * worldScale;
            enemyBlockSpeed = ai.blockSpeed * worldScale;
            enemyArms = opponent.arms;
            enemyDamage = 3 + enemyArms / 250;
            FaceOpponents();
            playerFacingTarget = player.rotation;
        }

        public void Tick(float seconds, InputFrame frame)
        {
            EmptyPunchRequested = false;
            if (nativePhysics)
                player.rotation = Quaternion.Lerp(player.rotation, playerFacingTarget, seconds * 50);
            if (Outcome != FightOutcome.Pending) return;
            if (Countdown > 0)
            {
                MathDirection(out _, out float countdownSeparation);
                brain.Step(seconds, countdownSeparation, Enemy, Player);
                if (externalOpening == null) countdown = Mathf.Max(0, countdown - seconds);
                return;
            }

            MathDirection(out var toward, out float separation);
            var decision = brain.Step(seconds, separation, Enemy, Player);
            PlayerAnimationAxes = -frame.Move;
            EnemyAnimationAxes = new Vector2(brain.LateralSpeed, decision.speed);
            Player.Tick(seconds, frame.BlockHeld);
            Enemy.Tick(seconds, decision.block);
            var right = Vector3.Cross(Vector3.up, toward);
            var direction = nativePhysics ? playerFacingTarget * new Vector3(frame.Move.x, 0, frame.Move.y) :
                right * frame.Move.x + toward * frame.Move.y;
            var enemyDirection = nativePhysics ? enemy.forward * decision.speed + enemy.right * brain.LateralSpeed :
                -toward * decision.speed - right * brain.LateralSpeed;
            if (nativePhysics && playerBody && enemyBody)
            {
                if (!Player.MovementBlocked) SetNativeVelocity(playerBody, direction, (Player.Blocking ? 9f : 17f) * worldScale, seconds);
                if (!Enemy.MovementBlocked) SetNativeVelocity(enemyBody, enemyDirection, Enemy.Blocking ? enemyBlockSpeed : enemyMoveSpeed, seconds);
            }
            else
            {
                if (!Player.MovementBlocked) BeachMotor.Translate(player, direction * ((Player.Blocking ? 9f : 17f) * worldScale * seconds), bounds);
                if (!Enemy.MovementBlocked) BeachMotor.Translate(enemy, enemyDirection * seconds, bounds, enemy: true);
            }
            MathDirection(out toward, out separation);
            float minimumSeparation = FightRules.BodyRadius(player, playerGeometry, worldScale)
                + FightRules.BodyRadius(enemy, enemyGeometry, enemySourceScale);
            if (!nativePhysics && separation < minimumSeparation)
            {
                float offset = (minimumSeparation - separation) * 0.5f;
                BeachMotor.Translate(player, -toward * offset, bounds);
                BeachMotor.Translate(enemy, toward * offset, bounds, enemy: true);
            }
            if (!nativePhysics) FaceOpponents();

            EmptyPunchRequested = (frame.AttackHeld || frame.AttackPressed) && !Player.Blocking &&
                Player.Health > 0 && Player.Stamina <= 0 && Player.PunchTime >= FightRules.PunchPeriod;
            if ((frame.AttackHeld || frame.AttackPressed) &&
                Player.Punch(FightRules.PlayerPunchCost(progress.arms, random.Next(1, 3)))) playerPunchPositionCaptured = false;
            if (decision.attack) Enemy.Punch(FightRules.EnemyPunchCost(enemyArms, random.Next(3, 5)));
            PlayerFlash = Mathf.Max(0, PlayerFlash - seconds);
            EnemyFlash = Mathf.Max(0, EnemyFlash - seconds);
            if (Player.InHitWindow && !playerPunchPositionCaptured)
            {
                playerPunchCenter = FightRules.PunchCenter(player, playerGeometry, worldScale);
                playerPunchPositionCaptured = true;
            }
            if (Player.TryLand(Enemy, FightRules.PunchOverlapsBody(playerPunchCenter, playerGeometry.punchRadius * worldScale,
                enemy, enemyGeometry, enemySourceScale),
                FightRules.PlayerPunchDamage(progress.arms)))
            {
                EnemyFlash = 0.16f;
                ApplyKnockback(enemy, nativePhysics ? -enemy.forward : toward, 1500f);
            }
            Vector3 enemyPunchCenter = FightRules.PunchCenter(enemy, enemyGeometry, enemySourceScale);
            if (Enemy.Health > 0 && Enemy.TryLand(Player,
                FightRules.PunchOverlapsBody(enemyPunchCenter,
                    FightRules.AttachedPunchRadius(enemy, enemyGeometry, enemySourceScale), player, playerGeometry, worldScale), enemyDamage))
            {
                PlayerFlash = 0.16f;
                ApplyKnockback(player, nativePhysics ? -player.forward : -toward, 400f);
            }

            if (Enemy.Health <= 0)
            {
                Outcome = FightOutcome.Victory;
                FightRules.ConfigureEnemyBody(enemy, enemyGeometry, enemySourceScale, true);
                if (nativePhysics && enemyBody) enemyBody.isKinematic = true;
            }
            else if (Player.Health <= 0)
            {
                Outcome = FightOutcome.Defeat;
                if (nativePhysics && playerBody) playerBody.isKinematic = true;
            }
        }

        private void ApplyKnockback(Transform target, Vector3 direction, float force)
        {
            var body = target.GetComponent<Rigidbody>();
            if (body) body.AddForce(direction * (force * worldScale), ForceMode.Force);
        }

        public void UpdateNativeFacing(float seconds)
        {
            if (!nativePhysics || Outcome != FightOutcome.Pending) return;
            MathDirection(out var toward, out _);
            playerFacingTarget = Quaternion.LookRotation(toward);
            enemy.rotation = Quaternion.Lerp(enemy.rotation, Quaternion.LookRotation(-toward), seconds * 50);
        }

        private static void SetNativeVelocity(Rigidbody body, Vector3 direction, float speed, float seconds)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > 1e-10f) direction.Normalize();
            var target = new Vector3(direction.x * speed, body.velocity.y, direction.z * speed);
            body.velocity = Vector3.Lerp(body.velocity, target, seconds * 20);
        }

        private void FaceOpponents()
        {
            MathDirection(out var toward, out _);
            player.rotation = Quaternion.LookRotation(toward);
            enemy.rotation = Quaternion.LookRotation(-toward);
        }

        private void MathDirection(out Vector3 toward, out float separation)
        {
            var offset = enemy.position - player.position;
            offset.y = 0;
            separation = offset.magnitude;
            toward = separation > 0.001f ? offset / separation : player.forward;
        }
    }
}
