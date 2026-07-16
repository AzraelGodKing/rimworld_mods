using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // RimWorld 1.6: GetReport returns plain true/false (no culprit list) and only
    // checks IsPlayerHome maps — underground B1 beds never count.
    [HarmonyPatch(typeof(Alert_NeedColonistBeds), "NeedColonistBeds")]
    public static class Patch_Alert_NeedColonistBeds
    {
        public static bool Prefix(Map map, ref bool __result)
        {
            if (map == null || !map.IsPlayerHome || !LevelGraph.AnyLinkFrom(map))
            {
                return true;
            }

            __result = ColonyBedUtility.NeedsColonistBedsInNetwork(map);
            return false;
        }
    }
}
