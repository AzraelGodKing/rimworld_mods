#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Mod-local flags and checks shared by the hunt tick. Pawn caches live on each
    /// <see cref="NemesisData"/>.
    /// </summary>
    public static class NemesisRegistry
    {
        /// <summary>Set by Kill / faction / guest patches; the next tick re-checks end conditions.</summary>
        public static bool ResolutionDirty;

        public static void Clear()
        {
            ResolutionDirty = false;
            GameComponent_Nemesis? comp = GameComponent_Nemesis.Instance;
            if (comp == null) return;
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
                hunts[i].ClearCaches();
        }

        private const int DeferHostiles = 12;

        /// <summary>
        /// Skip harassment while a large hostile force is already on the player map, counting
        /// every Strata level linked to it, so deep raids in the cellars count too (AZR-556).
        /// </summary>
        public static bool ShouldDeferActions(Map map)
        {
            if (map?.mapPawns == null) return false;
            int hostiles = CountHostiles(map, 0);
            if (hostiles >= DeferHostiles) return true;
            List<Map> levels = SoftCompat.StrataReachableMaps(map);
            for (int i = 0; i < levels.Count && hostiles < DeferHostiles; i++)
            {
                if (levels[i] != map && levels[i]?.mapPawns != null)
                    hostiles = CountHostiles(levels[i], hostiles);
            }
            return hostiles >= DeferHostiles;
        }

        private static int CountHostiles(Map map, int hostiles)
        {
            IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < spawned.Count && hostiles < DeferHostiles; i++)
            {
                Pawn p = spawned[i];
                if (p != null && !p.Dead && p.HostileTo(Faction.OfPlayer))
                    hostiles++;
            }
            return hostiles;
        }
    }
}
