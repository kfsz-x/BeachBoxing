namespace BeachBoxing
{
    // Exact serialized EnemyManager public tuning; source IDs are retained on each profile.
    [System.Serializable]
    public sealed class NativeOpponentAi
    {
        public float speed;
        public float blockSpeed;
        public float health;
        public float maxHealth;
        public float healthRecoverRate;
        public float stamina;
        public float maxStamina;
        public float staminaRecoverRate;
        public float block;
        public float maxBlock;
        public float blockRecoverRate;
        public bool randomPunches;
        public float punchInterval;
        public float punchIntervalVariation;
        public bool constantPunches;
        public float constantPunchCooldown;
        public bool distancePunch;
        public float distanceToPunch;
        public float distancePunchCooldown;
        public bool horizontalMovement;
        public float timeDoingHorizontalMovement;
        public float horizontalMovementCooldown;
        public bool randomMovement;
        public float timeMoving;
        public float timeStationary;
        public bool moveInAndOut;
        public float timeMovingIn;
        public float timeMovingOut;
        public float timeNotMoving;
        public bool staminaBasedMovement;
        public int staminaToBackOut;
        public int staminaToMoveIn;
        public bool healthBasedMovement;
        public int healthToBackOut;
        public int healthToMoveIn;
        public bool aggressive;
        public float timeAggressing;
        public float timeNotAggressing;
        public float averageTimeBlocking;
        public float timeBlockingVariance;
        public bool startsBlockingAtCertainStamina;
        public int staminaToStartBlocking;
        public bool startsBlockingAfterCooldown;
        public float blockingCooldown;
        public bool blocking;
        public float invincibleCooldown;
    }

    [System.Serializable]
    public sealed class OpponentProfile
    {
        public string name, modelName;
        public int sourceIndex = -1, sourcePrefabId, sourceEnemyManagerId, sourcePortraitId;
        public int rating, coins, stars, chest, arms, back, legs, prestige;
        public float approachSpeed, retreatSpeed, punchInterval;
        public int baseCapacity = 20;
        public bool staminaBasedMovement;
        public NativeOpponentAi ai;
        public NativeCombatGeometry geometry = NativeCombatGeometry.Enemy();

        public OpponentProfile(string name, int rating, int coins, int stars, float approachSpeed,
            float retreatSpeed, float punchInterval, int baseCapacity = 20, bool staminaBasedMovement = false)
        {
            this.name = name; this.rating = chest = arms = back = legs = rating;
            this.coins = coins; this.stars = stars;
            this.approachSpeed = approachSpeed; this.retreatSpeed = retreatSpeed;
            this.punchInterval = punchInterval; this.baseCapacity = baseCapacity;
            this.staminaBasedMovement = staminaBasedMovement;
            ai = new NativeOpponentAi { speed = approachSpeed, blockSpeed = retreatSpeed,
                maxHealth = baseCapacity, maxStamina = baseCapacity, maxBlock = 20,
                healthRecoverRate = rating == 0 ? 1f : 1.6f, staminaRecoverRate = 10,
                blockRecoverRate = rating == 0 ? 1.5f : 3f, distancePunch = true,
                distanceToPunch = 7, distancePunchCooldown = punchInterval,
                horizontalMovement = true, timeDoingHorizontalMovement = 2, horizontalMovementCooldown = 5,
                randomMovement = !staminaBasedMovement, timeMoving = 1.5f, timeStationary = 2,
                staminaBasedMovement = staminaBasedMovement, staminaToBackOut = 5, staminaToMoveIn = 6,
                averageTimeBlocking = 5, timeBlockingVariance = 2,
                startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, invincibleCooldown = .2f };
        }

        public static OpponentProfile[] RookieRoster() => new[]
        {
            new OpponentProfile("Ramez", 0, 50, 3, 9f, 5f, 0.5f, 20, false)
            {
                sourceIndex = 0, modelName = "0", sourcePrefabId = 2641, sourceEnemyManagerId = 37808, sourcePortraitId = 402,
                geometry = NativeCombatGeometry.Enemy(37174, 37219, 37264),
                chest = 0, arms = 0, back = 0, legs = 0, prestige = 1,
                ai = new NativeOpponentAi { speed = 9f, blockSpeed = 5f, health = 20f, maxHealth = 20f, healthRecoverRate = 1f, stamina = 20f, maxStamina = 20f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 1.5f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.5f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = true, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = false, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kyle", 200, 100, 6, 9f, 5f, 0.400000006f, 30, true)
            {
                sourceIndex = 1, modelName = "1", sourcePrefabId = 2621, sourceEnemyManagerId = 37788, sourcePortraitId = 397,
                geometry = NativeCombatGeometry.Enemy(37154, 37199, 37244),
                chest = 200, arms = 200, back = 200, legs = 200, prestige = 1,
                ai = new NativeOpponentAi { speed = 9f, blockSpeed = 5f, health = 30f, maxHealth = 30f, healthRecoverRate = 1.60000002f, stamina = 30f, maxStamina = 30f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kenji", 500, 150, 9, 12f, 8f, 0.400000006f, 40, true)
            {
                sourceIndex = 2, modelName = "2", sourcePrefabId = 2617, sourceEnemyManagerId = 37784, sourcePortraitId = 516,
                geometry = NativeCombatGeometry.Enemy(37150, 37195, 37240),
                chest = 500, arms = 200, back = 800, legs = 500, prestige = 1,
                ai = new NativeOpponentAi { speed = 12f, blockSpeed = 8f, health = 40f, maxHealth = 40f, healthRecoverRate = 1.60000002f, stamina = 40f, maxStamina = 40f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("The Mountain", 3000, 200, 12, 9f, 6f, 0.400000006f, 50, true)
            {
                sourceIndex = 3, modelName = "3", sourcePrefabId = 2622, sourceEnemyManagerId = 37789, sourcePortraitId = 495,
                geometry = NativeCombatGeometry.Enemy(37155, 37200, 37245),
                chest = 3000, arms = 800, back = 800, legs = 800, prestige = 4,
                ai = new NativeOpponentAi { speed = 9f, blockSpeed = 6f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Malik", 1600, 250, 15, 17f, 10f, 0.400000006f, 50, true)
            {
                sourceIndex = 4, modelName = "4", sourcePrefabId = 2616, sourceEnemyManagerId = 37783, sourcePortraitId = 459,
                geometry = NativeCombatGeometry.Enemy(37149, 37194, 37239),
                chest = 1600, arms = 1400, back = 1600, legs = 1600, prestige = 1,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 10f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Yuri", 3000, 300, 18, 12f, 8f, 0.5f, 50, true)
            {
                sourceIndex = 5, modelName = "5", sourcePrefabId = 2632, sourceEnemyManagerId = 37799, sourcePortraitId = 494,
                geometry = NativeCombatGeometry.Enemy(37165, 37210, 37255),
                chest = 3000, arms = 3000, back = 1000, legs = 2000, prestige = 2,
                ai = new NativeOpponentAi { speed = 12f, blockSpeed = 8f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = true, constantPunchCooldown = 1f, distancePunch = false, distanceToPunch = 7f, distancePunchCooldown = 0.5f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Ace", 2600, 350, 21, 15f, 10f, 0.400000006f, 50, true)
            {
                sourceIndex = 6, modelName = "6", sourcePrefabId = 2613, sourceEnemyManagerId = 37780, sourcePortraitId = 431,
                geometry = NativeCombatGeometry.Enemy(37146, 37191, 37236),
                chest = 2600, arms = 2600, back = 2600, legs = 2600, prestige = 2,
                ai = new NativeOpponentAi { speed = 15f, blockSpeed = 10f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Minho", 2000, 400, 24, 15f, 10f, 0.5f, 50, true)
            {
                sourceIndex = 7, modelName = "7", sourcePrefabId = 2650, sourceEnemyManagerId = 37817, sourcePortraitId = 412,
                geometry = NativeCombatGeometry.Enemy(37183, 37228, 37273),
                chest = 2000, arms = 6000, back = 2000, legs = 2000, prestige = 2,
                ai = new NativeOpponentAi { speed = 15f, blockSpeed = 10f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = true, constantPunchCooldown = 0.800000012f, distancePunch = false, distanceToPunch = 7f, distancePunchCooldown = 0.5f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 5, staminaToMoveIn = 6, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Rico", 4000, 450, 27, 15f, 10f, 0.400000006f, 50, true)
            {
                sourceIndex = 8, modelName = "8", sourcePrefabId = 2629, sourceEnemyManagerId = 37796, sourcePortraitId = 396,
                geometry = NativeCombatGeometry.Enemy(37162, 37207, 37252),
                chest = 4000, arms = 4000, back = 4000, legs = 4000, prestige = 2,
                ai = new NativeOpponentAi { speed = 15f, blockSpeed = 10f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kylian", 4500, 500, 30, 19f, 14f, 0.400000006f, 50, true)
            {
                sourceIndex = 9, modelName = "9", sourcePrefabId = 2633, sourceEnemyManagerId = 37800, sourcePortraitId = 423,
                geometry = NativeCombatGeometry.Enemy(37166, 37211, 37256),
                chest = 4500, arms = 4500, back = 4500, legs = 4500, prestige = 3,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 14f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Tyrese", 5000, 550, 33, 17f, 10f, 0.400000006f, 50, true)
            {
                sourceIndex = 10, modelName = "10", sourcePrefabId = 2642, sourceEnemyManagerId = 37809, sourcePortraitId = 447,
                geometry = NativeCombatGeometry.Enemy(37175, 37220, 37265),
                chest = 5000, arms = 5000, back = 5000, legs = 5000, prestige = 3,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 10f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Punkz", 5000, 600, 36, 17f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 11, modelName = "11", sourcePrefabId = 2643, sourceEnemyManagerId = 37810, sourcePortraitId = 433,
                geometry = NativeCombatGeometry.Enemy(37176, 37221, 37266),
                chest = 5000, arms = 6500, back = 5000, legs = 7000, prestige = 3,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Don", 6000, 650, 39, 17f, 13f, 0.400000006f, 50, false)
            {
                sourceIndex = 12, modelName = "12", sourcePrefabId = 2646, sourceEnemyManagerId = 37813, sourcePortraitId = 500,
                geometry = NativeCombatGeometry.Enemy(37179, 37224, 37269),
                chest = 6000, arms = 6000, back = 7500, legs = 7500, prestige = 4,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = true, punchInterval = 0.200000003f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = false, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = true, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = false, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 3f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 20, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Takumi", 6000, 700, 42, 23f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 13, modelName = "13", sourcePrefabId = 2648, sourceEnemyManagerId = 37815, sourcePortraitId = 440,
                geometry = NativeCombatGeometry.Enemy(37181, 37226, 37271),
                chest = 6000, arms = 6000, back = 6000, legs = 6000, prestige = 3,
                ai = new NativeOpponentAi { speed = 23f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = true, timeAggressing = 10f, timeNotAggressing = 0f, averageTimeBlocking = 3f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Nikko", 7000, 750, 45, 17f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 14, modelName = "14", sourcePrefabId = 2644, sourceEnemyManagerId = 37811, sourcePortraitId = 410,
                geometry = NativeCombatGeometry.Enemy(37177, 37222, 37267),
                chest = 7000, arms = 7000, back = 7000, legs = 7000, prestige = 4,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Darren", 7500, 800, 48, 17f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 15, modelName = "15", sourcePrefabId = 2607, sourceEnemyManagerId = 37774, sourcePortraitId = 477,
                geometry = NativeCombatGeometry.Enemy(37140, 37185, 37230),
                chest = 7500, arms = 7500, back = 7500, legs = 7500, prestige = 4,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Amir", 8000, 850, 51, 18f, 14f, 0.400000006f, 50, true)
            {
                sourceIndex = 16, modelName = "16", sourcePrefabId = 2628, sourceEnemyManagerId = 37795, sourcePortraitId = 465,
                geometry = NativeCombatGeometry.Enemy(37161, 37206, 37251),
                chest = 8000, arms = 8000, back = 8000, legs = 8000, prestige = 4,
                ai = new NativeOpponentAi { speed = 18f, blockSpeed = 14f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Alejandro", 11000, 900, 54, 17f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 17, modelName = "17", sourcePrefabId = 2651, sourceEnemyManagerId = 37818, sourcePortraitId = 429,
                geometry = NativeCombatGeometry.Enemy(37184, 37229, 37274),
                chest = 11000, arms = 9000, back = 4000, legs = 10000, prestige = 8,
                ai = new NativeOpponentAi { speed = 17f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 40, staminaToMoveIn = 41, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Ramez (Rematch)", 10000, 950, 57, 19f, 14f, 0.400000006f, 50, true)
            {
                sourceIndex = 18, modelName = "18", sourcePrefabId = 2615, sourceEnemyManagerId = 37782, sourcePortraitId = 499,
                geometry = NativeCombatGeometry.Enemy(37148, 37193, 37238),
                chest = 10000, arms = 10000, back = 9000, legs = 9000, prestige = 4,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 14f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("HORSE", 9000, 1000, 60, 23f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 19, modelName = "19", sourcePrefabId = 2637, sourceEnemyManagerId = 37804, sourcePortraitId = 399,
                geometry = NativeCombatGeometry.Enemy(37170, 37215, 37260),
                chest = 9000, arms = 14000, back = 3000, legs = 5000, prestige = 4,
                ai = new NativeOpponentAi { speed = 23f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Brad", 9800, 1050, 63, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 20, modelName = "20", sourcePrefabId = 2638, sourceEnemyManagerId = 37805, sourcePortraitId = 496,
                geometry = NativeCombatGeometry.Enemy(37171, 37216, 37261),
                chest = 9800, arms = 9800, back = 9800, legs = 9800, prestige = 4,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Dean", 9000, 1100, 66, 23f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 21, modelName = "21", sourcePrefabId = 2609, sourceEnemyManagerId = 37776, sourcePortraitId = 446,
                geometry = NativeCombatGeometry.Enemy(37142, 37187, 37232),
                chest = 9000, arms = 11000, back = 9000, legs = 9000, prestige = 3,
                ai = new NativeOpponentAi { speed = 23f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Faheem", 11200, 1150, 69, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 22, modelName = "22", sourcePrefabId = 2611, sourceEnemyManagerId = 37778, sourcePortraitId = 481,
                geometry = NativeCombatGeometry.Enemy(37144, 37189, 37234),
                chest = 11200, arms = 11200, back = 11200, legs = 11200, prestige = 4,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kyung  ", 9000, 1200, 72, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 23, modelName = "23", sourcePrefabId = 2623, sourceEnemyManagerId = 37790, sourcePortraitId = 452,
                geometry = NativeCombatGeometry.Enemy(37156, 37201, 37246),
                chest = 9000, arms = 13500, back = 10000, legs = 13000, prestige = 5,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Robo", 13000, 1250, 75, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 24, modelName = "24", sourcePrefabId = 2635, sourceEnemyManagerId = 37802, sourcePortraitId = 473,
                geometry = NativeCombatGeometry.Enemy(37168, 37213, 37258),
                chest = 13000, arms = 13000, back = 13000, legs = 13000, prestige = 5,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kwame", 13000, 1300, 78, 22f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 25, modelName = "25", sourcePrefabId = 2620, sourceEnemyManagerId = 37787, sourcePortraitId = 406,
                geometry = NativeCombatGeometry.Enemy(37153, 37198, 37243),
                chest = 13000, arms = 16000, back = 13000, legs = 13000, prestige = 5,
                ai = new NativeOpponentAi { speed = 22f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Koji", 15400, 1350, 81, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 26, modelName = "26", sourcePrefabId = 2630, sourceEnemyManagerId = 37797, sourcePortraitId = 418,
                geometry = NativeCombatGeometry.Enemy(37163, 37208, 37253),
                chest = 15400, arms = 15400, back = 15400, legs = 15400, prestige = 5,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kai", 20000, 1400, 84, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 27, modelName = "27", sourcePrefabId = 2639, sourceEnemyManagerId = 37806, sourcePortraitId = 487,
                geometry = NativeCombatGeometry.Enemy(37172, 37217, 37262),
                chest = 20000, arms = 11000, back = 13000, legs = 13000, prestige = 8,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 80, staminaToMoveIn = 81, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("HORSE (Rematch)", 17800, 1450, 87, 19f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 28, modelName = "28", sourcePrefabId = 2645, sourceEnemyManagerId = 37812, sourcePortraitId = 464,
                geometry = NativeCombatGeometry.Enemy(37178, 37223, 37268),
                chest = 17800, arms = 17800, back = 17800, legs = 17800, prestige = 6,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Fang", 17400, 1500, 90, 23f, 13f, 0.400000006f, 50, true)
            {
                sourceIndex = 29, modelName = "29", sourcePrefabId = 2619, sourceEnemyManagerId = 37786, sourcePortraitId = 428,
                geometry = NativeCombatGeometry.Enemy(37152, 37197, 37242),
                chest = 17400, arms = 17400, back = 17400, legs = 17400, prestige = 5,
                ai = new NativeOpponentAi { speed = 23f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.400000006f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Rook", 20500, 1550, 93, 19f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 30, modelName = "30", sourcePrefabId = 2649, sourceEnemyManagerId = 37816, sourcePortraitId = 416,
                geometry = NativeCombatGeometry.Enemy(37182, 37227, 37272),
                chest = 20500, arms = 20500, back = 20500, legs = 20500, prestige = 6,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Vex", 20000, 1600, 96, 19f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 31, modelName = "31", sourcePrefabId = 2624, sourceEnemyManagerId = 37791, sourcePortraitId = 398,
                geometry = NativeCombatGeometry.Enemy(37157, 37202, 37247),
                chest = 20000, arms = 25000, back = 20000, legs = 25000, prestige = 6,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Zeno", 23500, 1650, 99, 19f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 32, modelName = "32", sourcePrefabId = 2625, sourceEnemyManagerId = 37792, sourcePortraitId = 504,
                geometry = NativeCombatGeometry.Enemy(37158, 37203, 37248),
                chest = 23500, arms = 23500, back = 23500, legs = 23500, prestige = 6,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Tino", 20000, 1700, 102, 22f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 33, modelName = "33", sourcePrefabId = 2627, sourceEnemyManagerId = 37794, sourcePortraitId = 490,
                geometry = NativeCombatGeometry.Enemy(37160, 37205, 37250),
                chest = 20000, arms = 30000, back = 20000, legs = 20000, prestige = 7,
                ai = new NativeOpponentAi { speed = 22f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Lynx", 27000, 1750, 105, 19f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 34, modelName = "34", sourcePrefabId = 2631, sourceEnemyManagerId = 37798, sourcePortraitId = 457,
                geometry = NativeCombatGeometry.Enemy(37164, 37209, 37254),
                chest = 27000, arms = 27000, back = 27000, legs = 27000, prestige = 7,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Zade", 40000, 1800, 108, 19f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 35, modelName = "35", sourcePrefabId = 2610, sourceEnemyManagerId = 37777, sourcePortraitId = 451,
                geometry = NativeCombatGeometry.Enemy(37143, 37188, 37233),
                chest = 40000, arms = 26000, back = 26000, legs = 26000, prestige = 10,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Ghost", 32000, 1850, 111, 19f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 36, modelName = "36", sourcePrefabId = 2612, sourceEnemyManagerId = 37779, sourcePortraitId = 460,
                geometry = NativeCombatGeometry.Enemy(37145, 37190, 37235),
                chest = 32000, arms = 32000, back = 32000, legs = 32000, prestige = 7,
                ai = new NativeOpponentAi { speed = 19f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Striker", 33000, 1900, 114, 23f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 37, modelName = "37", sourcePrefabId = 2608, sourceEnemyManagerId = 37775, sourcePortraitId = 513,
                geometry = NativeCombatGeometry.Enemy(37141, 37186, 37231),
                chest = 33000, arms = 33000, back = 33000, legs = 33000, prestige = 6,
                ai = new NativeOpponentAi { speed = 23f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 150, staminaToMoveIn = 151, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Tank", 41000, 1950, 117, 20f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 38, modelName = "38", sourcePrefabId = 2640, sourceEnemyManagerId = 37807, sourcePortraitId = 493,
                geometry = NativeCombatGeometry.Enemy(37173, 37218, 37263),
                chest = 41000, arms = 41000, back = 41000, legs = 41000, prestige = 8,
                ai = new NativeOpponentAi { speed = 20f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Buckshot", 40000, 2000, 120, 20f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 39, modelName = "39", sourcePrefabId = 2634, sourceEnemyManagerId = 37801, sourcePortraitId = 467,
                geometry = NativeCombatGeometry.Enemy(37167, 37212, 37257),
                chest = 40000, arms = 50000, back = 47000, legs = 47000, prestige = 9,
                ai = new NativeOpponentAi { speed = 20f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Cain", 54000, 2050, 123, 20f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 40, modelName = "40", sourcePrefabId = 2618, sourceEnemyManagerId = 37785, sourcePortraitId = 480,
                geometry = NativeCombatGeometry.Enemy(37151, 37196, 37241),
                chest = 54000, arms = 54000, back = 54000, legs = 54000, prestige = 8,
                ai = new NativeOpponentAi { speed = 20f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Snipe", 50000, 2100, 126, 22f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 41, modelName = "41", sourcePrefabId = 2626, sourceEnemyManagerId = 37793, sourcePortraitId = 475,
                geometry = NativeCombatGeometry.Enemy(37159, 37204, 37249),
                chest = 50000, arms = 70000, back = 50000, legs = 50000, prestige = 8,
                ai = new NativeOpponentAi { speed = 22f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Malik (Rematch)", 70000, 2150, 129, 25f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 42, modelName = "42", sourcePrefabId = 2647, sourceEnemyManagerId = 37814, sourcePortraitId = 510,
                geometry = NativeCombatGeometry.Enemy(37180, 37225, 37270),
                chest = 70000, arms = 70000, back = 60000, legs = 60000, prestige = 7,
                ai = new NativeOpponentAi { speed = 25f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Kylian (Rematch)", 80000, 2200, 132, 20f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 43, modelName = "43", sourcePrefabId = 2614, sourceEnemyManagerId = 37781, sourcePortraitId = 471,
                geometry = NativeCombatGeometry.Enemy(37147, 37192, 37237),
                chest = 80000, arms = 80000, back = 80000, legs = 80000, prestige = 10,
                ai = new NativeOpponentAi { speed = 20f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
            new OpponentProfile("Ramez (Rematch)", 100000, 2250, 135, 20f, 13f, 0.330000013f, 50, true)
            {
                sourceIndex = 44, modelName = "44", sourcePrefabId = 2636, sourceEnemyManagerId = 37803, sourcePortraitId = 421,
                geometry = NativeCombatGeometry.Enemy(37169, 37214, 37259),
                chest = 100000, arms = 100000, back = 100000, legs = 100000, prestige = 10,
                ai = new NativeOpponentAi { speed = 20f, blockSpeed = 13f, health = 50f, maxHealth = 50f, healthRecoverRate = 1.60000002f, stamina = 50f, maxStamina = 50f, staminaRecoverRate = 10f, block = 20f, maxBlock = 20f, blockRecoverRate = 3f, randomPunches = false, punchInterval = 0f, punchIntervalVariation = 0f, constantPunches = false, constantPunchCooldown = 0f, distancePunch = true, distanceToPunch = 7f, distancePunchCooldown = 0.330000013f, horizontalMovement = true, timeDoingHorizontalMovement = 2f, horizontalMovementCooldown = 5f, randomMovement = false, timeMoving = 1.5f, timeStationary = 2f, moveInAndOut = false, timeMovingIn = 0f, timeMovingOut = 0f, timeNotMoving = 0f, staminaBasedMovement = true, staminaToBackOut = 200, staminaToMoveIn = 201, healthBasedMovement = false, healthToBackOut = 0, healthToMoveIn = 0, aggressive = false, timeAggressing = 0f, timeNotAggressing = 0f, averageTimeBlocking = 5f, timeBlockingVariance = 2f, startsBlockingAtCertainStamina = true, staminaToStartBlocking = 5, startsBlockingAfterCooldown = false, blockingCooldown = 0f, blocking = false, invincibleCooldown = 0.200000003f }
            },
        };
    }
}
