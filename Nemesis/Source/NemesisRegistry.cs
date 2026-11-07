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
            GameComponent_Nemesis comp = GameComponent_Nemesis.Instance;
            if (comp == null) return;
            IReadOnlyList<NemesisData> hunts = comp.Hunts;
            for (int i = 0; i < hunts.Count; i++)
                hunts[i].ClearCaches();
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
