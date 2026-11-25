using System;

namespace BeachBoxing
{
    [Serializable]
    public sealed class BeachPeripheralState
    {
        public int[] trainerLevels = new int[BeachPeripheralRules.TrainerCount];
        public int equippedTrainer = -1;
        public int[] appliedModifiers = new int[BeachPeripheralRules.MuscleCount];
        public int dailyIndex;
        public string lastDailyDate = "";
        public int claimedDays;
        public bool trainerAlert;
    }

    public static class BeachPeripheralRules
    {
        public const int TrainerCount = 6;
        public const int DailyRewardCount = 7;
        internal const int MuscleCount = 4;

        private const int RewardMask = (1 << DailyRewardCount) - 1;
        private static readonly string[] TrainerNames = { "Cody", "Dion", "Nova", "Zara", "Serena", "Yui" };
        private static readonly string[] TrainerArchetypes = { "力量型", "均衡型", "防御型", "敏捷型", "全能型", "传奇型" };
        private static readonly int[] PurchaseCosts = { -1, 50, 200, -1, 500, 900 };

        // Columns match the Muscle enum: Chest, Arms, Back, Legs.
        private static readonly int[,] BaseModifiers =
        {
            { 5, 5, 5, 5 },
            { 10, 10, 10, 10 },
            { 30, 0, 30, 0 },
            { 10, 40, 10, 40 },
            { 50, 30, 20, 50 },
            { 50, 50, 50, 50 }
        };

        private static readonly (int Trainer, int Coins, int Stars)[] DailyRewards =
        {
            (0, 0, 0),
            (-1, 3000, 0),
            (-1, 0, 60),
            (-1, 10000, 0),
            (-1, 0, 100),
            (-1, 1000000, 0),
            (3, 0, 0)
        };

        public static BeachPeripheralState Normalize(BeachPeripheralState state)
        {
            if (state == null) return new BeachPeripheralState();

            NormalizeArray(ref state.trainerLevels, TrainerCount);
            if (TrainerLevel(state, state.equippedTrainer) == 0)
            {
                state.equippedTrainer = -1;
                RefreshEquippedModifiers(state);
            }
            else if (state.appliedModifiers == null || state.appliedModifiers.Length != MuscleCount)
            {
                RefreshEquippedModifiers(state);
            }
            else
            {
                NormalizeArray(ref state.appliedModifiers, MuscleCount);
            }

            state.dailyIndex = Math.Max(0, state.dailyIndex);
            state.lastDailyDate = state.lastDailyDate ?? "";
            state.claimedDays &= RewardMask;
            return state;
        }

        public static string GetTrainerName(int trainerId) =>
            IsTrainerId(trainerId) ? TrainerNames[trainerId] : "";

        public static string GetTrainerArchetype(int trainerId) =>
            IsTrainerId(trainerId) ? TrainerArchetypes[trainerId] : "";

        public static int GetPurchaseCost(int trainerId) =>
            IsTrainerId(trainerId) ? PurchaseCosts[trainerId] : -1;

        public static bool TryPurchaseTrainer(PlayerProgress progress, BeachPeripheralState state, int trainerId)
        {
            int cost = GetPurchaseCost(trainerId);
            if (cost < 0 || !HasValidBalances(progress) || progress.stars < cost ||
                state?.trainerLevels?.Length != TrainerCount || state.trainerLevels[trainerId] != 0)
                return false;

            progress.stars -= cost;
            state.trainerLevels[trainerId] = 1;
            return true;
        }

        public static int GetUpgradeCost(BeachPeripheralState state, int trainerId)
        {
            int level = TrainerLevel(state, trainerId);
            return level > 0 && level < int.MaxValue / 8 ? 8 * (level + 1) : -1;
        }

        public static bool TryUpgradeTrainer(PlayerProgress progress, BeachPeripheralState state, int trainerId)
        {
            int cost = GetUpgradeCost(state, trainerId);
            if (cost < 0 || !HasValidBalances(progress) || progress.coins < cost ||
                state.equippedTrainer != trainerId)
                return false;

            progress.coins -= cost;
            // The applied bonuses stay at the last equip or startup level.
            state.trainerLevels[trainerId]++;
            return true;
        }

        public static bool TryEquipTrainer(BeachPeripheralState state, int trainerId)
        {
            if (TrainerLevel(state, trainerId) == 0) return false;

            state.equippedTrainer = trainerId;
            RefreshEquippedModifiers(state);
            return true;
        }

        public static void RefreshEquippedModifiers(BeachPeripheralState state)
        {
            if (state == null) return;
            if (state.appliedModifiers == null || state.appliedModifiers.Length != MuscleCount)
                state.appliedModifiers = new int[MuscleCount];

            int trainerId = state.equippedTrainer;
            int level = TrainerLevel(state, trainerId);
            for (int muscle = 0; muscle < MuscleCount; muscle++)
                state.appliedModifiers[muscle] = level > 0 ? Modifier(trainerId, muscle, level) : 0;
        }

        public static int GetAppliedModifier(BeachPeripheralState state, Muscle muscle)
        {
            int index = (int)muscle;
            if ((uint)index >= MuscleCount || state?.appliedModifiers?.Length != MuscleCount ||
                TrainerLevel(state, state.equippedTrainer) == 0)
                return 0;

            return Math.Max(0, state.appliedModifiers[index]);
        }

        public static int GetDisplayedModifier(BeachPeripheralState state, int trainerId, Muscle muscle)
        {
            int index = (int)muscle;
            if (!IsTrainerId(trainerId) || (uint)index >= MuscleCount) return 0;

            int level = TrainerLevel(state, trainerId);
            return Modifier(trainerId, index, level > 1 ? level : 0);
        }

        public static bool AdvanceDailyDate(BeachPeripheralState state, string localDate)
        {
            if (state == null || string.IsNullOrWhiteSpace(localDate) ||
                string.Equals(state.lastDailyDate, localDate, StringComparison.Ordinal))
                return false;

            state.lastDailyDate = localDate;
            if (state.dailyIndex < int.MaxValue) state.dailyIndex = Math.Max(0, state.dailyIndex) + 1;
            return true;
        }

        public static bool CanClaim(BeachPeripheralState state, int day) =>
            state != null && (uint)day < DailyRewardCount && day < state.dailyIndex &&
            (state.claimedDays & (1 << day)) == 0;

        public static bool HasDailyReward(BeachPeripheralState state)
        {
            for (int day = 0; day < DailyRewardCount; day++)
                if (CanClaim(state, day)) return true;

            return false;
        }

        public static int GetNextClaimableDay(BeachPeripheralState state)
        {
            for (int day = 0; day < DailyRewardCount; day++)
                if (CanClaim(state, day)) return day;
            return -1;
        }

        public static bool TryClaimDaily(PlayerProgress progress, BeachPeripheralState state, int day, int multiplier = 1)
        {
            if (!CanClaim(state, day) || !HasValidBalances(progress)) return false;

            int mult = Math.Max(1, multiplier);
            var reward = DailyRewards[day];
            long addedCoins = (long)reward.Coins * mult;
            long addedStars = (long)reward.Stars * mult;
            if ((reward.Trainer >= 0 && state.trainerLevels?.Length != TrainerCount) ||
                addedCoins > int.MaxValue - progress.coins || addedStars > int.MaxValue - progress.stars)
                return false;

            if (reward.Trainer >= 0)
            {
                state.trainerLevels[reward.Trainer] = Math.Max(1, state.trainerLevels[reward.Trainer]);
                state.trainerAlert = true;
            }
            progress.coins += (int)addedCoins;
            progress.stars += (int)addedStars;
            state.claimedDays |= 1 << day;
            return true;
        }

        private static int TrainerLevel(BeachPeripheralState state, int id) =>
            IsTrainerId(id) && state?.trainerLevels?.Length == TrainerCount ? Math.Max(0, state.trainerLevels[id]) : 0;

        private static bool IsTrainerId(int id) => (uint)id < TrainerCount;

        private static bool HasValidBalances(PlayerProgress progress) =>
            progress != null && progress.coins >= 0 && progress.stars >= 0;

        private static int Modifier(int trainerId, int muscle, int level)
        {
            int baseModifier = BaseModifiers[trainerId, muscle];
            return baseModifier + Math.Min(level, int.MaxValue - baseModifier);
        }

        private static void NormalizeArray(ref int[] values, int length)
        {
            if (values == null || values.Length != length) Array.Resize(ref values, length);
            for (int i = 0; i < values.Length; i++) values[i] = Math.Max(0, values[i]);
        }
    }
}
