using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Puts the fault guard around each subsystem's entry points without
    // touching their bodies. A guarded call that throws is reported against
    // its subsystem and swallowed, so the vanilla tick or draw that called it
    // carries on. Once a subsystem trips, its entry points are skipped.
    //
    // Level generation is report-only: gen steps keep their normal vanilla
    // error handling, and a trip blocks opening further levels instead
    // (StrataPocketMapOpen.TryBeginOpen).
    [HarmonyPatch]
    public static class Patch_FaultGuardEntries
    {
        private struct Entry
        {
            public StrataFaultGuard.System system;
            public bool reportOnly;
        }

        private static readonly Dictionary<MethodBase, Entry> entries = new Dictionary<MethodBase, Entry>();

        private static void Add(StrataFaultGuard.System system, Type type, string method, bool reportOnly = false)
        {
            if (type == null)
            {
                return;
            }
            MethodInfo m = AccessTools.DeclaredMethod(type, method);
            if (m != null && !m.IsAbstract && !entries.ContainsKey(m))
            {
                entries[m] = new Entry { system = system, reportOnly = reportOnly };
            }
        }

        private static void Build()
        {
            entries.Clear();
            // See-below
            Add(StrataFaultGuard.System.SeeBelow, typeof(SectionLayer_GroundBelow), nameof(SectionLayer.Regenerate));
            Add(StrataFaultGuard.System.SeeBelow, typeof(SectionLayer_StrataBelowThings), nameof(SectionLayer.Regenerate));
            Add(StrataFaultGuard.System.SeeBelow, typeof(StrataBelowRenderer), nameof(StrataBelowRenderer.DrawBelowStatic));
            Add(StrataFaultGuard.System.SeeBelow, typeof(StrataBelowRenderer), nameof(StrataBelowRenderer.DrawBelowDynamic));
            // Cross-level combat
            Add(StrataFaultGuard.System.CrossLevelCombat, typeof(MapComponent_CrossLevelCombat), nameof(MapComponent.MapComponentTick));
            Add(StrataFaultGuard.System.CrossLevelCombat, typeof(MapComponent_CrossLevelThreatWatch), nameof(MapComponent.MapComponentTick));
            Add(StrataFaultGuard.System.CrossLevelCombat, typeof(StrataCrossLevelTurret), nameof(StrataCrossLevelTurret.TickPair));
            // Flood
            Add(StrataFaultGuard.System.Flood, typeof(FloodMapComponent), nameof(FloodMapComponent.FloodCell));
            // Gravship stacks
            Add(StrataFaultGuard.System.GravshipStacks, typeof(WorldComponent_StrataGravshipStacks), nameof(RimWorld.Planet.WorldComponent.WorldComponentTick));
            Add(StrataFaultGuard.System.GravshipStacks, typeof(MapComponent_StrataGravshipUpperDeckSync), nameof(MapComponent.MapComponentTick));
            // Power shafts
            Add(StrataFaultGuard.System.PowerShafts, typeof(StairwellPowerUtility), nameof(StairwellPowerUtility.MaintainVerticalTie));
            // Pick Up And Haul inventory drop-off across levels
            Add(StrataFaultGuard.System.PuahCompat, typeof(StrataPuahSoftCompat), nameof(StrataPuahSoftCompat.TryDeliverInventory));

            Assembly strata = typeof(Patch_FaultGuardEntries).Assembly;
            foreach (Type type in AccessTools.GetTypesFromAssembly(strata))
            {
                if (type.IsAbstract)
                {
                    continue;
                }
                // Strata incidents: a throwing worker just does not fire.
                if (typeof(IncidentWorker).IsAssignableFrom(type))
                {
                    Add(StrataFaultGuard.System.Incidents, type, "TryExecuteWorker");
                    Add(StrataFaultGuard.System.Incidents, type, "CanFireNowSub");
                }
                else if (typeof(GenStep).IsAssignableFrom(type))
                {
                    Add(StrataFaultGuard.System.LevelGeneration, type, nameof(GenStep.Generate), reportOnly: true);
                }
            }
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Build();
            return entries.Keys;
        }

        public static bool Prefix(MethodBase __originalMethod)
        {
            if (!entries.TryGetValue(__originalMethod, out Entry entry) || entry.reportOnly)
            {
                return true;
            }
            // Skipped: void returns nothing, bool returns false (incident does not fire).
            return !StrataFaultGuard.IsTripped(entry.system);
        }

        public static Exception Finalizer(Exception __exception, MethodBase __originalMethod)
        {
            if (__exception == null || !entries.TryGetValue(__originalMethod, out Entry entry))
            {
                return __exception;
            }
            StrataFaultGuard.Report(entry.system,
                "running " + __originalMethod.DeclaringType?.Name + "." + __originalMethod.Name,
                __exception);
            return entry.reportOnly ? __exception : null;
        }
    }
}

namespace Strata
{
    // Soft-compat bridges live in their own Harmony ids (CE, Guards For Me,
    // Open The Windows, ReGrowth, Vehicle Framework) or in a few named patch
    // classes (sister-mod bridges). Guard their patch methods the same way:
    // an exception is reported and swallowed, and a tripped bridge stops
    // doing anything (a skipped bool prefix lets the original run).
    public static class StrataBridgeGuard
    {
        private static readonly Dictionary<string, StrataFaultGuard.System> ByHarmonyId =
            new Dictionary<string, StrataFaultGuard.System>
            {
                { "azraelgodking.strata.ce", StrataFaultGuard.System.CombatExtendedCompat },
                { "azraelgodking.strata.gfm", StrataFaultGuard.System.GuardsForMeCompat },
                { "azraelgodking.strata.otw", StrataFaultGuard.System.OpenTheWindowsCompat },
                { "azraelgodking.strata.regrowth.splashes", StrataFaultGuard.System.ReGrowthCompat },
                { "azrael.strata.vehicleframework.roofdeck", StrataFaultGuard.System.VehicleFrameworkCompat },
            };

        private static readonly Dictionary<Type, StrataFaultGuard.System> ByPatchClass =
            new Dictionary<Type, StrataFaultGuard.System>
            {
                { typeof(Patch_HomesteaderRootCellarRot), StrataFaultGuard.System.SisterModBridges },
                { typeof(Patch_WellspringUndergroundIrrigation), StrataFaultGuard.System.SisterModBridges },
            };

        private static readonly Dictionary<MethodBase, StrataFaultGuard.System> guarded =
            new Dictionary<MethodBase, StrataFaultGuard.System>();

        private static bool applied;

        public static void Apply()
        {
            if (applied)
            {
                return;
            }
            applied = true;
            var harmony = new Harmony("azraelgodking.strata.faultguard");
            HarmonyMethod prefixVoid = new HarmonyMethod(typeof(StrataBridgeGuard), nameof(PrefixVoid));
            HarmonyMethod prefixBool = new HarmonyMethod(typeof(StrataBridgeGuard), nameof(PrefixBool));
            HarmonyMethod finalVoid = new HarmonyMethod(typeof(StrataBridgeGuard), nameof(FinalizerVoid));
            HarmonyMethod finalBool = new HarmonyMethod(typeof(StrataBridgeGuard), nameof(FinalizerBool));
            Assembly strata = typeof(StrataBridgeGuard).Assembly;

            foreach (MethodBase original in Harmony.GetAllPatchedMethods())
            {
                Patches info = Harmony.GetPatchInfo(original);
                if (info == null)
                {
                    continue;
                }
                Collect(info.Prefixes, strata);
                Collect(info.Postfixes, strata);
            }

            int count = 0;
            foreach (KeyValuePair<MethodBase, StrataFaultGuard.System> pair in guarded)
            {
                if (!(pair.Key is MethodInfo m))
                {
                    continue;
                }
                try
                {
                    if (m.ReturnType == typeof(void))
                    {
                        harmony.Patch(m, prefix: prefixVoid, finalizer: finalVoid);
                        count++;
                    }
                    else if (m.ReturnType == typeof(bool) && !m.Name.Contains("Postfix"))
                    {
                        harmony.Patch(m, prefix: prefixBool, finalizer: finalBool);
                        count++;
                    }
                }
                catch (Exception e)
                {
                    StrataLog.Verbose("[Strata] Fault guard could not wrap " + m.DeclaringType?.Name + "." + m.Name + ": " + e.Message);
                }
            }
            StrataLog.Verbose("[Strata] Fault guard wraps " + count + " soft-compat patch method(s).");
        }

        private static void Collect(IEnumerable<Patch> patches, Assembly strata)
        {
            foreach (Patch patch in patches)
            {
                MethodInfo m = patch.PatchMethod;
                if (m == null || m.DeclaringType?.Assembly != strata || guarded.ContainsKey(m))
                {
                    continue;
                }
                if (ByHarmonyId.TryGetValue(patch.owner, out StrataFaultGuard.System system))
                {
                    guarded[m] = system;
                    continue;
                }
                Type owner = m.DeclaringType;
                while (owner != null)
                {
                    if (ByPatchClass.TryGetValue(owner, out system))
                    {
                        guarded[m] = system;
                        break;
                    }
                    owner = owner.DeclaringType;
                }
            }
        }

        public static bool PrefixVoid(MethodBase __originalMethod)
        {
            return !guarded.TryGetValue(__originalMethod, out StrataFaultGuard.System s) || !StrataFaultGuard.IsTripped(s);
        }

        public static bool PrefixBool(MethodBase __originalMethod, ref bool __result)
        {
            if (guarded.TryGetValue(__originalMethod, out StrataFaultGuard.System s) && StrataFaultGuard.IsTripped(s))
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static Exception FinalizerVoid(Exception __exception, MethodBase __originalMethod)
        {
            return Handle(__exception, __originalMethod);
        }

        public static Exception FinalizerBool(Exception __exception, MethodBase __originalMethod, ref bool __result)
        {
            Exception rethrow = Handle(__exception, __originalMethod);
            if (__exception != null && rethrow == null)
            {
                __result = true;
            }
            return rethrow;
        }

        private static Exception Handle(Exception e, MethodBase method)
        {
            if (e == null || !guarded.TryGetValue(method, out StrataFaultGuard.System s))
            {
                return e;
            }
            StrataFaultGuard.Report(s, "running " + method.DeclaringType?.Name + "." + method.Name, e);
            return null;
        }
    }
}
