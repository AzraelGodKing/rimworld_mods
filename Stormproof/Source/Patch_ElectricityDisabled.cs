using System.Collections.Generic;
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
    //
    // RimWorld 1.6 changed ElectricityDisabled from a property getter to a
    // method taking the map, so we patch the method form.
    [HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.ElectricityDisabled))]
    public static class Patch_ElectricityDisabled
    {
        private static readonly List<GameCondition> tmpConditions = new List<GameCondition>();

        public static void Postfix(GameConditionManager __instance, Map map, ref bool __result)
        {
            if (!__result || map == null)
            {
                return;
            }
            // Some non-flare condition disables electricity? Then stay disabled.
            tmpConditions.Clear();
            __instance.GetAllGameConditionsAffectingMap(map, tmpConditions);
            foreach (GameCondition cond in tmpConditions)
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
