using System;

namespace LivingWorld
{
    /// <summary>
    /// Game-free rules of the simulation. No RimWorld types: unit tests compile this
    /// folder on its own (Tests/LivingWorld.Tests).
    /// </summary>
    public static class LivingWorldRules
    {
        /// <summary>Lowest and highest chance either side wins a battle.</summary>
        public const float MinBattleOdds = 0.15f;
        public const float MaxBattleOdds = 0.85f;

        /// <summary>
        /// Chance that side A wins a battle against side B, from settlement counts
        /// and summed prosperity (-2..+2 per settlement). Bigger, richer factions win
        /// more often, but an upset is always possible.
        /// </summary>
        public static float BattleOddsA(int settlementsA, int settlementsB, int prosperityA, int prosperityB)
        {
            float strengthA = Math.Max(0.5f, settlementsA + 1 + prosperityA * 0.25f);
            float strengthB = Math.Max(0.5f, settlementsB + 1 + prosperityB * 0.25f);
            float odds = strengthA / (strengthA + strengthB);
            return Math.Min(MaxBattleOdds, Math.Max(MinBattleOdds, odds));
        }

        /// <summary>Wars at this intensity or higher can take settlements.</summary>
        public const int ConquestMinIntensity = 2;

        /// <summary>Chance a qualifying battle also takes a settlement.</summary>
        public const float ConquestChance = 0.35f;

        /// <summary>
        /// Whether a battle win may take one of the loser's settlements. A protected
        /// faction (Empire, quest giver) never loses its last one.
        /// </summary>
        public static bool ConquestAllowed(int pairIntensity, int loserSettlements, bool loserProtected)
        {
            if (pairIntensity < ConquestMinIntensity || loserSettlements <= 0)
            {
                return false;
            }
            return !(loserProtected && loserSettlements <= 1);
        }

        /// <summary>Starting tone and intensity for a new pair, from vanilla relations.</summary>
        public static void SeedTone(bool hostile, bool allied, out int tone, out int intensity)
        {
            // Tone values match FactionRelationTone: 0 Peace, 1 Tension, 2 War, 3 Alliance.
            if (hostile)
            {
                // Not straight to War: the concurrent-war cap still decides who fights.
                tone = 1;
                intensity = 2;
                return;
            }
            if (allied)
            {
                tone = 3;
                intensity = 1;
                return;
            }
            tone = 0;
            intensity = 0;
        }
    }
}
