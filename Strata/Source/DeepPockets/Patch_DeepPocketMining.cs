using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(Mineable), nameof(Mineable.DestroyMined))]
    public static class Patch_DeepPocketMining
    {
        public static void Prefix(Mineable __instance, out Map __state)
        {
            __state = __instance.Map;
        }

        public static void Postfix(Mineable __instance, Map __state)
        {
            Map map = __state ?? __instance.Map;
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            MapComponent_DeepPockets comp = map.GetComponent<MapComponent_DeepPockets>();
            DeepPocketRecord pocket = comp?.PocketAt(__instance.Position);
            if (pocket != null)
            {
                DeepPocketUtility.Breach(map, pocket, sendLetter: true);
            }
        }
    }
}
