using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(Frame), "CompleteConstruction")]
    public static class Patch_StairwellConstructionComplete
    {
        public static void Postfix(Frame __instance)
        {
            Map map = __instance.Map;
            if (map == null)
            {
                return;
            }
            foreach (IntVec3 cell in GenAdj.OccupiedRect(__instance))
            {
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Building_StairsDown stairs)
                    {
                        stairs.TryOpenLevelAfterBuilt();
                        return;
                    }
                }
            }
        }
    }
}
