using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // The substructure overlay only asks grav engines on the current map.
    // Upper decks have no engine, so every gravpanel there draws red
    // ("Disconnected from grav engine") even when it sits on the ship.
    [HarmonyPatch(typeof(SubstructureGrid), "CellBoolDrawerGetExtraColorInt")]
    public static class Patch_SubstructureOverlayLinkedDeck
    {
        private static readonly AccessTools.FieldRef<SubstructureGrid, Map> MapRef =
            AccessTools.FieldRefAccess<SubstructureGrid, Map>("map");

        public static bool Prepare()
        {
            return AccessTools.Method(typeof(SubstructureGrid), "CellBoolDrawerGetExtraColorInt") != null
                && AccessTools.Field(typeof(SubstructureGrid), "map") != null;
        }

        public static void Postfix(SubstructureGrid __instance, int index, ref Color __result)
        {
            if (!StrataGravshipUtility.OdysseyActive || __instance == null)
            {
                return;
            }
            if (__result == GravshipUtility.ConnectedSubstructureColor)
            {
                return;
            }

            Map map = MapRef(__instance);
            if (map == null || !StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return;
            }

            TerrainDef foundation = map.terrainGrid.FoundationAt(index);
            if (foundation == null || !foundation.IsSubstructure)
            {
                return;
            }

            Building_GravEngine engine = StrataGravshipUtility.FindGravEngine(map);
            if (engine == null || engine.Map == map)
            {
                return;
            }

            IntVec3 cell = map.cellIndices.IndexToCell(index);
            if (CountsAsShipDeck(map, cell, engine))
            {
                __result = GravshipUtility.ConnectedSubstructureColor;
            }
        }

        // The deck itself, or a gravpanel placed against it. The place worker
        // already allows that one-cell ring; the overlay should match.
        private static bool CountsAsShipDeck(Map map, IntVec3 cell, Building_GravEngine engine)
        {
            if (StrataGravshipUtility.IsLinkedDeckCell(map, cell, engine))
            {
                return true;
            }
            for (int i = 0; i < 4; i++)
            {
                IntVec3 next = cell + GenAdj.CardinalDirections[i];
                if (next.InBounds(map) && StrataGravshipUtility.IsLinkedDeckCell(map, next, engine))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
