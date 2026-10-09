using UnityEngine;

namespace SealFind
{
    /// <summary>Alle Punkte- und Sterne-Regeln an einem Ort – leicht zu balancen und zu testen.</summary>
    public static class ScoreCalculator
    {
        public const float ComboWindow = 3.5f;
        public const int MaxCombo = 5;
        public const float HintPenaltySeconds = 5f;
        public const int MissScorePenalty = 25;
        public const int ExtraSealPoints = 50;

        /// <summary>Strafsekunden für Fehlklicks: wer wild herumtippt, zahlt mehr.</summary>
        public static float MissPenalty(int missStreak) => Mathf.Min(2f + (Mathf.Max(1, missStreak) - 1), 5f);

        public static int NextCombo(int combo, float sinceLastFind) =>
            sinceLastFind <= ComboWindow ? Mathf.Min(combo + 1, MaxCombo) : 1;

        public static int SealPoints(SealSpecies species, bool countsForGoal, bool bonus, int combo)
        {
            int basePoints = countsForGoal || bonus ? RarityInfo.Points(species.Rarity) : ExtraSealPoints;
            return basePoints * Mathf.Clamp(combo, 1, MaxCombo);
        }

        public static int TimeBonus(LevelDefinition level, float effectiveTime)
        {
            if (level.IsTimed) return Mathf.RoundToInt(Mathf.Max(0f, level.TimeLimit - effectiveTime) * 20f);
            return Mathf.RoundToInt(Mathf.Max(0f, level.ParTime * 1.5f - effectiveTime) * 15f);
        }

        public static int Stars(LevelDefinition level, float effectiveTime, bool success)
        {
            if (!success) return 0;
            if (level.IsTimed)
            {
                float left = (level.TimeLimit - effectiveTime) / level.TimeLimit;
                return left >= 0.4f ? 3 : left >= 0.2f ? 2 : 1;
            }
            if (effectiveTime <= level.ParTime) return 3;
            if (effectiveTime <= level.ParTime * 1.75f) return 2;
            return 1;
        }
    }
}
