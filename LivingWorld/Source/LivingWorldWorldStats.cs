using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LivingWorld
{
    /// <summary>
    /// Per-tick snapshot of settlements per faction and a faction id lookup, so the
    /// diplomacy and morph pulses stop rescanning every settlement for every pair.
    /// Rebuilt on the first use in a tick, or after Living World itself moves a
    /// settlement (<see cref="Invalidate"/>).
    /// </summary>
    internal static class LivingWorldWorldStats
    {
        private static readonly Dictionary<Faction, int> settlementCounts = new Dictionary<Faction, int>();
        private static readonly Dictionary<int, Faction> factionsById = new Dictionary<int, Faction>();
        private static Game builtFor;
        private static int builtTick = -1;
        private static int factionListCount = -1;

        public static void Invalidate()
        {
            builtTick = -1;
        }

        public static int SettlementCount(Faction f)
        {
            if (f == null)
            {
                return 0;
            }
            EnsureCounts();
            return settlementCounts.TryGetValue(f, out int n) ? n : 0;
        }

        /// <summary>Settlements plus factions, used to notice when pairs need rebuilding.</summary>
        public static int Signature()
        {
            return Find.WorldObjects.Settlements.Count * 397 ^ Find.FactionManager.AllFactionsListForReading.Count;
        }

        public static Faction FactionById(int id)
        {
            if (id < 0 || Find.FactionManager == null)
            {
                return null;
            }
            List<Faction> all = Find.FactionManager.AllFactionsListForReading;
            if (Current.Game != builtFor || all.Count != factionListCount)
            {
                RebuildFactions(all);
            }
            if (factionsById.TryGetValue(id, out Faction f) && f != null)
            {
                return f;
            }
            return null;
        }

        private static void RebuildFactions(List<Faction> all)
        {
            factionsById.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null)
                {
                    factionsById[all[i].loadID] = all[i];
                }
            }
            factionListCount = all.Count;
            if (Current.Game != builtFor)
            {
                builtFor = Current.Game;
                builtTick = -1;
            }
        }

        private static void EnsureCounts()
        {
            int now = Find.TickManager.TicksGame;
            if (builtTick == now && builtFor == Current.Game)
            {
                return;
            }
            if (builtFor != Current.Game)
            {
                RebuildFactions(Find.FactionManager.AllFactionsListForReading);
            }
            builtTick = now;
            settlementCounts.Clear();
            List<Settlement> all = Find.WorldObjects.Settlements;
            for (int i = 0; i < all.Count; i++)
            {
                Faction f = all[i]?.Faction;
                if (f == null)
                {
                    continue;
                }
                settlementCounts.TryGetValue(f, out int n);
                settlementCounts[f] = n + 1;
            }
        }
    }
}
