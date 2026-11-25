using System;
using UnityCore;
using UnityEngine;

namespace BeachBoxing
{
    public enum Muscle { Chest, Arms, Back, Legs }

    [Serializable]
    public sealed class PlayerProgress
    {
        internal const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public int chest, arms, back, legs, coins, stars, unlockedOpponent;
        public int prestigeLevel = 1;
        public string playerName = "";
        public bool generatedName;
        public bool customPlayerName;
        internal string DisplayName => customPlayerName ? playerName : BeachChineseText.Translate(playerName);
        public int completedTutorial, firstMove, keepTrainingHint;
        public BeachPeripheralState peripheral = new BeachPeripheralState();

        public int Get(Muscle muscle)
        {
            switch (muscle)
            {
                case Muscle.Chest: return chest;
                case Muscle.Arms: return arms;
                case Muscle.Back: return back;
                case Muscle.Legs: return legs;
                default: throw new ArgumentOutOfRangeException(nameof(muscle));
            }
        }

        public bool RecordVictory(int opponent, int gold, int awardedStars)
        {
            if (opponent < 0 || opponent >= NativeOpponentCatalog.PlayableCount || opponent > unlockedOpponent ||
                gold < 0 || awardedStars < 0 || coins < 0 || stars < 0) return false;
            if (coins > int.MaxValue - gold || stars > int.MaxValue - awardedStars) return false;
            coins += gold;
            stars += awardedStars;
            if (opponent == unlockedOpponent) unlockedOpponent++;
            return true;
        }

        public void Add(Muscle muscle, int amount)
        {
            if (amount <= 0) return;
            switch (muscle)
            {
                case Muscle.Chest: chest = CappedAdd(chest, amount); break;
                case Muscle.Arms: arms = CappedAdd(arms, amount); break;
                case Muscle.Back: back = CappedAdd(back, amount); break;
                case Muscle.Legs: legs = CappedAdd(legs, amount); break;
            }
        }

        private static int CappedAdd(int value, int amount) => value > int.MaxValue - amount ? int.MaxValue : value + amount;
    }

    public sealed class ProgressStore
    {
        private const string SaveKey = "beachBoxing.progress";
        private readonly LocalStore storage;
        private bool preserveExisting;

        public ProgressStore(LocalStore storage) => this.storage = storage;

        public PlayerProgress Load()
        {
            string json = storage.GetString(SaveKey);
            if (string.IsNullOrWhiteSpace(json)) return new PlayerProgress();
            try
            {
                var value = JsonUtility.FromJson<PlayerProgress>(json);
                if (value != null && (value.version == 1 || value.version == PlayerProgress.CurrentVersion) &&
                    value.chest >= 0 && value.arms >= 0 &&
                    value.back >= 0 && value.legs >= 0 && value.coins >= 0 && value.stars >= 0 &&
                    value.unlockedOpponent >= 0)
                {
                    value.prestigeLevel = Mathf.Clamp(value.prestigeLevel, 1, 10);
                    value.playerName = value.playerName ?? "";
                    if (value.version == 1)
                    {
                        // Version 1 could mark stock names as custom. Recover known generated combinations.
                        if (value.generatedName && BeachChineseText.TryTranslateGeneratedName(value.playerName, out _))
                            value.customPlayerName = false;
                        else if (value.playerName.Length > 0 && json.IndexOf("\"customPlayerName\"", StringComparison.Ordinal) < 0)
                            value.customPlayerName = true;
                        value.version = PlayerProgress.CurrentVersion;
                    }
                    value.completedTutorial = Mathf.Clamp(value.completedTutorial, 0, 1);
                    value.firstMove = Mathf.Clamp(value.firstMove, 0, 1);
                    value.keepTrainingHint = Mathf.Clamp(value.keepTrainingHint, 0, 1);
                    value.peripheral = BeachPeripheralRules.Normalize(value.peripheral);
                    return value;
                }
            }
            catch (ArgumentException) { }
            preserveExisting = true;
            return new PlayerProgress();
        }

        public void Save(PlayerProgress progress)
        {
            if (preserveExisting)
            {
                string backupKey = SaveKey + ".backup";
                if (string.IsNullOrEmpty(storage.GetString(backupKey)))
                    storage.SetString(backupKey, storage.GetString(SaveKey));
                preserveExisting = false;
            }
            storage.SetString(SaveKey, JsonUtility.ToJson(progress));
            storage.Save();
        }
    }
}
