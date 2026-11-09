using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    public static class BrownoutUtility
    {
        // Share of a consumer's draw cut at full brownout (grid forecast models the same).
        internal const float DrawCut = 0.40f;
        private const float WorkSpeedCut = 0.55f;

        public static float For(Thing thing)
        {
            // Settings first: with brownout off, no lookups at all (AZR-713).
            if (!StormproofMod.Settings.enableBrownout || thing?.Map == null)
            {
                return 0f;
            }
            MapComponent_Stormproof comp = MapComponent_Stormproof.For(thing.Map);
            return comp == null ? 0f : comp.BrownoutFor(thing);
        }

        public static float For(PowerNet net)
        {
            if (!StormproofMod.Settings.enableBrownout || net == null)
            {
                return 0f;
            }
            // Not net.Map: that getter throws on a net no PowerNetManager owns.
            // Some mods build such nets for their own buildings, and vanilla's
            // power inspect line asks every one of them for its gain rate.
            Map map = net.powerNetManager?.map;
            if (map == null)
            {
                return 0f;
            }
            MapComponent_Stormproof comp = MapComponent_Stormproof.For(map);
            return comp == null ? 0f : comp.BrownoutFor(net);
        }

        internal static float WorkSpeedFactor(float brownout) => 1f - WorkSpeedCut * brownout;

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

    // Brownout trims what a weak grid's consumers draw. It is applied to the
    // grid's energy gain, where vanilla's power tick reads it, instead of to
    // CompPowerTrader.PowerOutput: that getter is tiny enough for the .NET 10
    // JIT to inline into its callers, which would skip a patch on it silently.
    // This also costs one lookup per grid per tick instead of one per consumer.
    [HarmonyPatch(typeof(PowerNet), nameof(PowerNet.CurrentEnergyGainRate))]
    public static class Patch_Brownout_GainRate
    {
        public static void Postfix(PowerNet __instance, ref float __result)
        {
            if (DebugSettings.unlimitedPower)
            {
                return;
            }
            float b = BrownoutUtility.For(__instance);
            if (b <= 0f)
            {
                return;
            }
            List<CompPowerTrader> comps = __instance.powerComps;
            if (comps == null)
            {
                return;
            }
            float draw = 0f;
            for (int i = 0; i < comps.Count; i++)
            {
                CompPowerTrader trader = comps[i];
                if (trader != null && trader.PowerOn && trader.PowerOutput < 0f)
                {
                    draw -= trader.PowerOutput;
                }
            }
            __result += draw * BrownoutUtility.DrawCut * b * CompPower.WattsToWattDaysPerTick;
        }
    }

    // Workbench slowdown as a stat part on WorkTableWorkSpeedFactor (added by
    // Patches/Stormproof_Brownout.xml), so it shows in the stat tooltip and no
    // global GetStatValue patch is needed.
    public class StatPart_Brownout : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing)
            {
                float b = BrownoutUtility.For(req.Thing);
                if (b > 0f)
                {
                    val *= BrownoutUtility.WorkSpeedFactor(b);
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!req.HasThing)
            {
                return null;
            }
            float b = BrownoutUtility.For(req.Thing);
            return b > 0f
                ? "Stormproof_Brownout_StatPart".Translate(b.ToStringPercent(),
                    BrownoutUtility.WorkSpeedFactor(b).ToStringPercent())
                : null;
        }
    }

    [HarmonyPatch]
    public static class Patch_Brownout_TempControl
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            // A game update that moves TickRare to the base class must not hand
            // Harmony a null target and take the whole patch class down.
            MethodBase heater = AccessTools.DeclaredMethod(typeof(Building_Heater), nameof(Building_Heater.TickRare));
            if (heater != null)
            {
                yield return heater;
            }
            MethodBase cooler = AccessTools.DeclaredMethod(typeof(Building_Cooler), nameof(Building_Cooler.TickRare));
            if (cooler != null)
            {
                yield return cooler;
            }
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

        // Finalizer, not Postfix: an exception inside TickRare must not leave the
        // thermostat target shifted for good.
        public static void Finalizer(Building_TempControl __instance, float __state)
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
                MapComponent_Stormproof.For(map)?.NotifyCondition(cond, started: true);
                StormproofCompatApi.NotifyCondition(cond, map, started: true);
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
                MapComponent_Stormproof.For(map)?.NotifyCondition(__instance, started: false);
                StormproofCompatApi.NotifyCondition(__instance, map, started: false);
            }
        }
    }
}
