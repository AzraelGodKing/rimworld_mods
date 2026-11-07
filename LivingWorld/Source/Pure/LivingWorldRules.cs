using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Order-independent key for a faction pair: (min id &lt;&lt; 32) | max id.
        /// PairKey(a, b) == PairKey(b, a).
        /// </summary>
        public static long PairKey(int a, int b)
        {
            int lo = Math.Min(a, b);
            int hi = Math.Max(a, b);
            return ((long)lo << 32) | (uint)hi;
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
    
        // ---- Tone transitions. "roll" is a uniform value in [0, 1); rate is the warRate setting.

        public enum PeaceOutcome
        {
            None,
            Alliance,
            Tension,
        }

        public static PeaceOutcome Peace(float roll, float rate, bool canAlly)
        {
            if (roll < 0.04f * rate && canAlly)
            {
                return PeaceOutcome.Alliance;
            }
            if (roll < 0.04f * rate + 0.22f * rate)
            {
                return PeaceOutcome.Tension;
            }
            return PeaceOutcome.None;
        }

        public enum TensionOutcome
        {
            None,
            CoolOff,
            Escalate,
            Skirmish,
        }

        public static TensionOutcome Tension(float roll, float rate)
        {
            if (roll < 0.28f)
            {
                return TensionOutcome.CoolOff;
            }
            if (roll < 0.28f + 0.18f * rate)
            {
                return TensionOutcome.Escalate;
            }
            if (roll < 0.28f + 0.18f * rate + 0.35f)
            {
                return TensionOutcome.Skirmish;
            }
            return TensionOutcome.None;
        }

        /// <summary>Intensity after cooling off; factions hostile in vanilla never drop below 1.</summary>
        public static int CoolOffIntensity(int intensity, bool vanillaHostile) =>
            Math.Max(vanillaHostile ? 1 : 0, intensity - 1);

        public enum WarOutcome
        {
            None,
            WhitePeace,
            Battle,
        }

        public static WarOutcome War(float roll, float rate)
        {
            if (roll < 0.20f)
            {
                return WarOutcome.WhitePeace;
            }
            if (roll < 0.20f + 0.55f * Math.Max(0.35f, rate))
            {
                return WarOutcome.Battle;
            }
            return WarOutcome.None;
        }

        public enum AllianceOutcome
        {
            None,
            Lapse,
            Betrayal,
            Dissolve,
        }

        public static AllianceOutcome Alliance(float roll, float rate, bool canAlly)
        {
            if (!canAlly)
            {
                return AllianceOutcome.Lapse;
            }
            if (roll < 0.08f * rate)
            {
                return AllianceOutcome.Betrayal;
            }
            if (roll < 0.08f * rate + 0.12f)
            {
                return AllianceOutcome.Dissolve;
            }
            return AllianceOutcome.None;
        }

        // ---- Rumours

        /// <summary>
        /// What a distorted rumour reports instead of the real kind (ids from WorldEventKind:
        /// 0 Skirmish, 1 DecisiveVictory, 7 OwnershipFlip). Other kinds keep their kind.
        /// </summary>
        public static int DistortKind(int kind)
        {
            switch (kind)
            {
                case 1:
                    return 0;
                case 0:
                    return 1;
                case 7:
                    return 0;
                default:
                    return kind;
            }
        }

        // ---- Chronicle

        /// <summary>
        /// Trims <paramref name="chronicle"/> to <paramref name="capacity"/> (oldest first out).
        /// Evicted entries that still await a correction move to <paramref name="evicted"/>,
        /// which keeps at most <paramref name="evictedCapacity"/> (oldest dropped).
        /// </summary>
        public static void Evict<T>(List<T> chronicle, int capacity, List<T> evicted, int evictedCapacity,
            Func<T, bool> awaitsCorrection)
        {
            int overflow = chronicle.Count - capacity;
            if (overflow > 0)
            {
                for (int i = 0; i < overflow; i++)
                {
                    T e = chronicle[i];
                    if (e != null && awaitsCorrection(e))
                    {
                        evicted.Add(e);
                    }
                }
                chronicle.RemoveRange(0, overflow);
            }
            int extra = evicted.Count - evictedCapacity;
            if (extra > 0)
            {
                evicted.RemoveRange(0, extra);
            }
        }

        // ---- Budgets

        /// <summary>
        /// Letter budget resets each quadrum, morph budget each year. The first call only
        /// records the period. Returns the new counters.
        /// </summary>
        public static void RollBudgets(int quadrum, int year, ref bool initialized, ref int lastQuadrum,
            ref int lastYear, ref int letters, ref int morphs)
        {
            if (!initialized)
            {
                initialized = true;
                lastQuadrum = quadrum;
                lastYear = year;
                return;
            }
            if (quadrum != lastQuadrum)
            {
                lastQuadrum = quadrum;
                letters = 0;
            }
            if (year != lastYear)
            {
                lastYear = year;
                morphs = 0;
            }
        }

        /// <summary>Orders a faction id pair so the smaller id comes first.</summary>
        public static void NormalizeIds(ref int a, ref int b)
        {
            if (a > b)
            {
                int tmp = a;
                a = b;
                b = tmp;
            }
        }
    }
}
