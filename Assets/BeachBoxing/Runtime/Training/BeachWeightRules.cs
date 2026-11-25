using UnityEngine;

namespace BeachBoxing
{
    /// <summary>Original transient kilogram controls; weight does not alter the source training gain or speed.</summary>
    public sealed class BeachWeightRules
    {
        public int CurrentWeight { get; private set; } = 5;
        public int RecommendedWeight { get; private set; } = 5;
        public bool TipNeeded => CurrentWeight < RecommendedWeight;

        public static int RecommendedFor(int strength)
        {
            float scaled = strength * 0.0196667f;
            scaled = (scaled + 5f) / 5f;
            return Mathf.Clamp(Mathf.RoundToInt(scaled) * 5, 5, 300);
        }

        public void Enter(int strength) => UseRecommended(strength);

        public void Change(int amount, int strength)
        {
            CurrentWeight = Mathf.Max(5, unchecked(CurrentWeight + amount));
            RefreshRecommendation(strength);
        }

        public void UseRecommended(int strength)
        {
            RefreshRecommendation(strength);
            CurrentWeight = RecommendedWeight;
        }

        public void RefreshRecommendation(int strength) => RecommendedWeight = RecommendedFor(strength);
    }
}
