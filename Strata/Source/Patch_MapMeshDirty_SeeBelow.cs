using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // When the map below dirties things/buildings/fog/terrain, regenerate the
    // matching see-below layers on linked upper decks.
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
            if (___map == null) return;

            ulong watchThings = MapMeshFlagDefOf.Things | MapMeshFlagDefOf.Buildings | MapMeshFlagDefOf.FogOfWar;
            ulong watchTerrain = MapMeshFlagDefOf.Terrain;
            bool dirtyThings = StrataBelowRenderer.Enabled && (dirtyFlags & watchThings) != 0;
            bool dirtyTerrain = (dirtyFlags & watchTerrain) != 0;
            if (!dirtyThings && !dirtyTerrain) return;

            try
            {
                MirrorDirtyToUpperDecks(___map, loc, dirtyThings, dirtyTerrain);
            }
            catch (Exception e)
            {
                StrataBelowRenderer.DisableSession(e, "below mesh mirror");
            }
        }

        private static void MirrorDirtyToUpperDecks(Map source, IntVec3 loc, bool dirtyThings, bool dirtyTerrain)
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

                if (dirtyThings)
                {
                    upper.mapDrawer.MapMeshDirty(skyCell, StrataDefOf.Strata_BelowThings, true, false);
                }
                if (dirtyTerrain)
                {
                    // Roof-deck underlay samples surface terrain.
                    upper.mapDrawer.MapMeshDirty(skyCell, MapMeshFlagDefOf.Terrain, true, false);
                }
            }
        }
    }
}
