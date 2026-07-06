using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    // When a powered solar shield is protecting the map, the solar flare no
    // longer disables electricity. We only veto the flare: if some other
    // condition also disables electricity (e.g. from another mod or DLC),
    // the shield does not help against it.
    [HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.ElectricityDisabled), MethodType.Getter)]
    public static class Patch_ElectricityDisabled
    {
        public static void Postfix(GameConditionManager __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            Map map = __instance.ownerMap;
            if (map == null)
            {
                return;
            }
            // Some non-flare condition disables electricity? Then stay disabled.
            foreach (GameCondition cond in __instance.ActiveConditions)
            {
                if (cond.ElectricityDisabled && cond.def != StormproofDefOf.SolarFlare)
                {
                    return;
                }
            }
            if (StormproofRegistry.On(StormproofRegistry.Shields, map).Any(s => s.Protecting))
            {
                __result = false;
            }
        }
    }
}
