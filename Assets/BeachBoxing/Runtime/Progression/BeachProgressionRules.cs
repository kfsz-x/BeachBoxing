using System;
using System.Globalization;

namespace BeachBoxing
{
    public static class BeachProgressionRules
    {
        internal const int MinimumLevel = 1;
        internal const int MaximumLevel = 10;

        private static readonly int[] StrengthThresholds =
            { 0, 1000, 3000, 6000, 10000, 15000, 23000, 33000, 45000, 70000, 100000 };
        private static readonly int[] Gains = { 11, 13, 15, 20, 24, 28, 33, 38, 43, 48 };
        private static readonly float[] RootScales = { 1f, 1.15f, 1.3f, 1.45f, 1.6f, 1.7f, 1.8f, 1.9f, 2f, 2.1f };
        private static readonly float[] CameraTargetHeights = { 0f, 1f, 2f, 2.5f, 3f, 3.2f, 3.4f, 3.6f, 3.8f, 4f };
        private static readonly string[] RankTitles =
        {
            "Rookie", "Amateur", "Semi Pro", "Rising Star", "Pro Boxer", "Division Threat",
            "Title Challenger", "Title Champion", "World Champion", "Boxing Legend"
        };

        public static string FormatNumber(int strength)
        {
            RequireNonnegative(strength, nameof(strength));
            return strength <= 999
                ? strength.ToString(CultureInfo.InvariantCulture)
                : (strength / 1000f).ToString("0.00", CultureInfo.InvariantCulture) + "k";
        }

        public static float Fraction(int strength, int prestigeLevel)
        {
            RequireNonnegative(strength, nameof(strength));
            int index = LevelIndex(prestigeLevel);
            int minimum = StrengthThresholds[index];
            int maximum = StrengthThresholds[index + 1];
            float fraction = (strength - minimum) / (float)(maximum - minimum);
            return Math.Min(1f, Math.Max(0f, fraction));
        }

        public static bool IsFull(int strength, int level)
        {
            RequireNonnegative(strength, nameof(strength));
            return strength >= StrengthThresholds[LevelIndex(level) + 1];
        }

        public static bool CanLevelUp(PlayerProgress progress, int level)
        {
            int target = StrengthThresholds[LevelIndex(level) + 1];
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (progress.chest < 0 || progress.arms < 0 || progress.back < 0 || progress.legs < 0)
                throw new ArgumentOutOfRangeException(nameof(progress), "Muscle strengths must be nonnegative.");

            return level < MaximumLevel && progress.chest >= target && progress.arms >= target &&
                progress.back >= target && progress.legs >= target;
        }

        public static int NextLevel(PlayerProgress progress, int level) =>
            CanLevelUp(progress, level) ? level + 1 : level;

        public static int BaseGain(int level) => Gains[LevelIndex(level)];

        public static int CalculateGain(int level, int appliedModifierPercent)
        {
            RequireNonnegative(appliedModifierPercent, nameof(appliedModifierPercent));
            return (int)(BaseGain(level) * (1f + appliedModifierPercent / 100f));
        }

        public static float RootScale(int level) => RootScales[LevelIndex(level)];

        public static float CameraTargetY(int level) => CameraTargetHeights[LevelIndex(level)];

        public static string RankTitle(int unlockedOpponent)
        {
            RequireNonnegative(unlockedOpponent, nameof(unlockedOpponent));
            return RankTitles[Math.Min(unlockedOpponent / 5, RankTitles.Length - 1)];
        }

        private static int LevelIndex(int level)
        {
            if (level < MinimumLevel || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level), level, "Prestige level must be between 1 and 10.");
            return level - MinimumLevel;
        }

        private static void RequireNonnegative(int value, string parameterName)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be nonnegative.");
        }
    }
}
