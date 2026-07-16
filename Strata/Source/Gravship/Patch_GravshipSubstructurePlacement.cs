using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(PlaceWorker_InSubstructureFootprint), nameof(PlaceWorker_InSubstructureFootprint.AllowsPlacing))]
    public static class Patch_Gravship_InSubstructureFootprint
    {
        public static void Postfix(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            ref AcceptanceReport __result)
        {
            if (__result.Accepted || !StrataGravshipUtility.OdysseyActive || map == null)
            {
                return;
            }
            if (checkingDef is not ThingDef thingDef
                || thingDef.defName != StrataGravshipSubstructureSync.SubstructureDefName)
            {
                return;
            }
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngine(map);
            if (engine == null || !StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return;
            }
            foreach (IntVec3 cell in GenAdj.OccupiedRect(loc, rot, thingDef.size))
            {
                if (!StrataGravshipSubstructureSync.CanPlaceSubstructureOn(map, cell, engine))
                {
                    return;
                }
            }
            __result = true;
        }
    }

    [HarmonyPatch(typeof(PlaceWorker_BuildingsValidOverSubstructure), nameof(PlaceWorker_BuildingsValidOverSubstructure.AllowsPlacing))]
    public static class Patch_Gravship_BuildingsValidOverSubstructure
    {
        public static void Postfix(
            BuildableDef checkingDef,
            IntVec3 loc,
            Rot4 rot,
            Map map,
            ref AcceptanceReport __result)
        {
            if (__result.Accepted || !StrataGravshipUtility.OdysseyActive || map == null
                || !StrataGravshipUtility.IsGravshipLinkedLevel(map))
            {
                return;
            }
            Building_GravEngine engine = StrataGravshipUtility.FindGravEngine(map);
            if (engine == null)
            {
                return;
            }
            ThingDef thingDef = checkingDef as ThingDef;
            IntVec2 size = thingDef?.size ?? IntVec2.One;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(loc, rot, size))
            {
                if (!StrataGravshipUtility.IsLinkedDeckCell(map, cell, engine))
                {
                    return;
                }
            }
            __result = true;
        }
    }
}
