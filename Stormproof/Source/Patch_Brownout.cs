using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    public static class BrownoutUtility
    {
        public static float For(Thing thing)
        {
            if (thing?.Map == null)
            {
                return 0f;
            }
            MapComponent_Stormproof comp = thing.Map.GetComponent<MapComponent_Stormproof>();
            return comp == null ? 0f : comp.BrownoutFor(thing);
        }

        public static float For(CompPowerTrader trader)
        {
            return trader?.parent == null ? 0f : For(trader.parent);
        }

        public static string InspectLine(Thing thing)
        {
            float b = For(thing);
            if (b <= 0.02f)
            {
                return null;
            }
            return "Stormproof_Brownout_Status".Translate(b.ToStringPercent());
        }
    }

    [HarmonyPatch(typeof(CompPowerTrader), "get_PowerOutput")]
    public static class Patch_Brownout_PowerOutput
    {
        public static void Postfix(CompPowerTrader __instance, ref float __result)
        {
            if (__result >= 0f)
            {
                return;
            }
            float b = BrownoutUtility.For(__instance);
            if (b <= 0f)
            {
                return;
            }
            __result *= 1f - 0.40f * b;
        }
    }

    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    public static class Patch_Brownout_WorkSpeed
    {
        public static void Postfix(Thing thing, StatDef stat, ref float __result)
        {
            if (stat != StatDefOf.WorkTableWorkSpeedFactor || thing == null)
            {
                return;
            }
            float b = BrownoutUtility.For(thing);
            if (b <= 0f)
            {
                return;
            }
            __result *= 1f - 0.55f * b;
        }
    }

    [HarmonyPatch]
    public static class Patch_Brownout_TempControl
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Building_Heater), nameof(Building_Heater.TickRare));
            yield return AccessTools.DeclaredMethod(typeof(Building_Cooler), nameof(Building_Cooler.TickRare));
        }

        public static void Prefix(Building_TempControl __instance, out float __state)
        {
            __state = float.NaN;
            CompTempControl control = __instance?.GetComp<CompTempControl>();
            if (control == null || __instance.Map == null)
            {
                return;
            }
            __state = control.targetTemperature;
            float b = BrownoutUtility.For(__instance);
            if (b <= 0f)
            {
                return;
            }
            float room = __instance.Position.GetTemperature(__instance.Map);
            control.targetTemperature = __state + (room - __state) * 0.45f * b;
        }

        public static void Postfix(Building_TempControl __instance, float __state)
        {
            if (float.IsNaN(__state))
            {
                return;
            }
            CompTempControl control = __instance?.GetComp<CompTempControl>();
            if (control != null)
            {
                control.targetTemperature = __state;
            }
        }
    }

    [HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.RegisterCondition))]
    public static class Patch_Almanac_RegisterCondition
    {
        public static void Postfix(GameCondition cond)
        {
            if (cond?.AffectedMaps == null)
            {
                return;
            }
            foreach (Map map in cond.AffectedMaps)
            {
                map.GetComponent<MapComponent_Stormproof>()?.NotifyCondition(cond, started: true);
            }
        }
    }

    [HarmonyPatch(typeof(GameCondition), nameof(GameCondition.End))]
    public static class Patch_Almanac_EndCondition
    {
        public static void Prefix(GameCondition __instance)
        {
            if (__instance?.AffectedMaps == null)
            {
                return;
            }
            foreach (Map map in __instance.AffectedMaps)
            {
                map.GetComponent<MapComponent_Stormproof>()?.NotifyCondition(__instance, started: false);
            }
        }
    }
}
