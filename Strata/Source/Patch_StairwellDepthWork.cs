using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // RimWorld 1.6 StatExtension.GetStatValue overloads vary; resolve at patch time.
    [HarmonyPatch]
    public static class Patch_StairwellDepthWork
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in typeof(StatExtension).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != nameof(StatExtension.GetStatValue))
                {
                    continue;
                }
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 2
                    && parameters[0].ParameterType == typeof(Thing)
                    && parameters[1].ParameterType == typeof(StatDef))
                {
                    yield return method;
                }
            }
        }

        public static void Postfix(Thing thing, StatDef stat, ref float __result)
        {
            if (stat != StatDefOf.WorkToBuild || thing?.Map == null)
            {
                return;
            }
            string name = thing.def?.defName;
            if (name != "Strata_StairsDown" && name != "Strata_DigDownShaft")
            {
                return;
            }
            if (StrataDepth.Of(thing.Map) >= 1)
            {
                __result += LevelExcavationUtility.DeepWorkOffset;
            }
        }
    }
}
