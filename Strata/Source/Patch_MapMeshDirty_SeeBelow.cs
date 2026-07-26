using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // When the map below dirties things/buildings/fog, regenerate only the
    // Strata_BelowThings section layer on linked upper decks.
    [HarmonyPatch(typeof(MapDrawer), nameof(MapDrawer.MapMeshDirty), new Type[]
    {
        typeof(IntVec3),
        typeof(ulong),
        typeof(bool),
        typeof(bool)
    })]
    public static class Patch_MapDrawer_MapMeshDirty_SeeBelow
    {
        public static void Postfix(Map ___map, IntVec3 loc, ulong dirtyFlags)
        {
            if (!StrataBelowRenderer.Enabled || ___map == null) return;

            ulong watch = MapMeshFlagDefOf.Things | MapMeshFlagDefOf.Buildings | MapMeshFlagDefOf.FogOfWar;
            if ((dirtyFlags & watch) == 0) return;

            try
            {
                MirrorDirtyToUpperDecks(___map, loc);
            }
            catch (Exception e)
            {
                StrataBelowRenderer.DisableSession(e, "below mesh mirror");
            }
        }

        private static void MirrorDirtyToUpperDecks(Map source, IntVec3 loc)
        {
            if (Find.Maps == null) return;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map upper = maps[i];
                if (upper == null || upper.Disposed) continue;
                if (!StrataMapUtility.IsUpperLevel(upper)) continue;
                if (UpperDeckUtility.SourceMapFor(upper) != source) continue;

                IntVec3 skyCell = StrataBelowRenderer.LowerToSkyCell(source, upper, loc);
                if (!skyCell.InBounds(upper)) continue;

                upper.mapDrawer.MapMeshDirty(skyCell, StrataDefOf.Strata_BelowThings, true, false);
            }
        }
    }
}
