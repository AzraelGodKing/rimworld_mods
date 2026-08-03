using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LivingWorld
{
    public enum FactionRelationTone : byte
    {
        Peace = 0,
        Tension = 1,
        War = 2,
        Alliance = 3,
    }

    public class FactionPairState : IExposable
    {
        public int factionAId = -1;
        public int factionBId = -1;
        public FactionRelationTone tone = FactionRelationTone.Peace;
        public int intensity; // 0..3
        public int lastResolveTick;
        public int tradeBlackoutUntilTick;
        public int lastFalloutTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref factionAId, "factionAId", -1);
            Scribe_Values.Look(ref factionBId, "factionBId", -1);
            Scribe_Values.Look(ref tone, "tone", FactionRelationTone.Peace);
            Scribe_Values.Look(ref intensity, "intensity");
            Scribe_Values.Look(ref lastResolveTick, "lastResolveTick");
            Scribe_Values.Look(ref tradeBlackoutUntilTick, "tradeBlackoutUntilTick");
            Scribe_Values.Look(ref lastFalloutTick, "lastFalloutTick");
        }

        public static void NormalizeIds(ref int a, ref int b)
        {
            if (a > b)
            {
                int tmp = a;
                a = b;
                b = tmp;
            }
        }

        public static FactionPairState Create(Faction a, Faction b)
        {
            int idA = a.loadID;
            int idB = b.loadID;
            NormalizeIds(ref idA, ref idB);
            return new FactionPairState
            {
                factionAId = idA,
                factionBId = idB,
            };
        }

        public bool Matches(Faction a, Faction b)
        {
            if (a == null || b == null)
            {
                return false;
            }
            int idA = a.loadID;
            int idB = b.loadID;
            NormalizeIds(ref idA, ref idB);
            return factionAId == idA && factionBId == idB;
        }

        public Faction FactionA() => FindFaction(factionAId);

        public Faction FactionB() => FindFaction(factionBId);

        public bool TradeBlackoutActive =>
            tradeBlackoutUntilTick > 0
            && Find.TickManager.TicksGame < tradeBlackoutUntilTick;

        private static Faction FindFaction(int id)
        {
            if (id < 0)
            {
                return null;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].loadID == id)
                {
                    return all[i];
                }
            }
            return null;
        }

        public string DumpLine()
        {
            Faction a = FactionA();
            Faction b = FactionB();
            return $"{a?.Name ?? ("#" + factionAId)} / {b?.Name ?? ("#" + factionBId)} "
                + $"tone={tone} int={intensity} blackout={(TradeBlackoutActive ? "yes" : "no")}";
        }
    }

    public enum FalloutKind : byte
    {
        Refugees = 0,
        Warband = 1,
    }

    public class PendingFallout : IExposable
    {
        public int loserFactionId = -1;
        public int winnerFactionId = -1;
        public int tile = -1;
        public int enqueueTick;
        public FalloutKind kind = FalloutKind.Refugees;
        public string settlementLabel;

        public void ExposeData()
        {
            Scribe_Values.Look(ref loserFactionId, "loserFactionId", -1);
            Scribe_Values.Look(ref winnerFactionId, "winnerFactionId", -1);
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref enqueueTick, "enqueueTick");
            Scribe_Values.Look(ref kind, "kind", FalloutKind.Refugees);
            Scribe_Values.Look(ref settlementLabel, "settlementLabel");
        }

        public Faction Loser() => FindFaction(loserFactionId);

        public Faction Winner() => FindFaction(winnerFactionId);

        private static Faction FindFaction(int id)
        {
            if (id < 0)
            {
                return null;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].loadID == id)
                {
                    return all[i];
                }
            }
            return null;
        }
    }
}
