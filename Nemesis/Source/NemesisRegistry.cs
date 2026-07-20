using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    /// <summary>
    /// Mod-local performance: cached refs so tick work does not scan maps / world pawns every interval.
    /// </summary>
    public static class NemesisRegistry
    {
        public static Pawn CachedNemesis;
        public static Pawn CachedTarget;
        public static int CachedNemesisId = -1;
        public static int CachedTargetId = -1;
        public static bool ResolutionDirty;
        public static int LastHeavyCheckTick = -999999;

        public static void InvalidateNemesis()
        {
            CachedNemesis = null;
            CachedNemesisId = -1;
        }

        public static void InvalidateTarget()
        {
            CachedTarget = null;
            CachedTargetId = -1;
        }

        public static void Clear()
        {
            InvalidateNemesis();
            InvalidateTarget();
            ResolutionDirty = false;
            LastHeavyCheckTick = -999999;
        }

        public static bool MapIsViewed(Map map)
        {
            return map != null && Find.CurrentMap == map;
        }

        /// <summary>
        /// Skip expensive harassment while a large hostile force is already on the player map.
        /// </summary>
        public static bool ShouldDeferActions(Map map)
        {
            if (map?.mapPawns == null) return false;
            IReadOnlyList<Pawn> spawned = map.mapPawns.AllPawnsSpawned;
            int hostiles = 0;
            for (int i = 0; i < spawned.Count; i++)
            {
                Pawn p = spawned[i];
                if (p == null || p.Dead || !p.HostileTo(Faction.OfPlayer)) continue;
                hostiles++;
                if (hostiles >= 12) return true;
            }
            return false;
        }
    }
}
