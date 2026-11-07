using System.Collections.Generic;
using AzraelCommon;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    [DefOf]
    public static class StormproofDefOf
    {
        public static GameConditionDef SolarFlare;
        public static GameConditionDef Stormproof_IonStorm;
        public static GameConditionDef Stormproof_HeatDome;
        public static GameConditionDef Stormproof_PolarFront;
        public static GameConditionDef Stormproof_ToxicSurge;
        public static GameConditionDef Stormproof_DryLightning;

        [MayRequire("Ludeon.RimWorld.Biotech")]
        public static GameConditionDef NoxiousHaze;

        [MayRequire("Ludeon.RimWorld.Odyssey")]
        public static GameConditionDef VolcanicAsh;
        [MayRequire("Ludeon.RimWorld.Odyssey")]
        public static GameConditionDef DarkenedSkies;
        [MayRequire("Ludeon.RimWorld.Odyssey")]
        public static GameConditionDef Drought;
        [MayRequire("Ludeon.RimWorld.Odyssey")]
        public static GameConditionDef DroughtInitial;
        [MayRequire("Ludeon.RimWorld.Odyssey")]
        public static GameConditionDef DeepFreeze;

        public static ResearchProjectDef Stormproof_PerfectGrounding;
        public static ThingDef Stormproof_Fulgurite;
        public static ThingDef Stormproof_ArmoredConduit;
        public static ThingDef Stormproof_StormCapacitor;

        public static WeatherDef RainyThunderstorm;
        public static WeatherDef DryThunderstorm;

        static StormproofDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StormproofDefOf));
        }
    }

    [DefOf]
    public static class StormproofIncidentDefOf
    {
        public static IncidentDef Stormproof_IonStorm;
        public static IncidentDef Stormproof_HeatDome;
        public static IncidentDef Stormproof_PolarFront;
        public static IncidentDef Stormproof_ToxicSurge;
        public static IncidentDef Stormproof_DryLightning;
        public static IncidentDef ShortCircuit;

        internal static IncidentDef[] All => new[]
        {
            Stormproof_IonStorm, Stormproof_HeatDome, Stormproof_PolarFront,
            Stormproof_ToxicSurge, Stormproof_DryLightning,
        };

        static StormproofIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StormproofIncidentDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class StormproofInit
    {
        static StormproofInit()
        {
            int patched = SafePatchAll.Apply(new Harmony("azraelgodking.stormproof"), "[Stormproof]");
            StormproofSettings.CaptureOriginalChances();
            StormproofMod.Settings.ApplyIncidentChances();
            // The count makes a 1.7 smoke test a one-line check against the previous build.
            LongEventHandler.ExecuteWhenFinished(() => ModVersionLog.Write("[Stormproof]",
                extra: "stormproof2-hotpaths, " + patched + " patch classes applied"));
        }
    }

    // Live comps by map, so patches never scan a map or another map's comps.
    // Each map's list is kept sorted by thingIDNumber: the same order on every
    // machine and every load, so tie-breaks (which spire, which protector)
    // don't depend on hash order. Buckets are dropped when a map is removed.
    public sealed class CompRegistry<T> where T : ThingComp
    {
        private static readonly List<T> Empty = new List<T>();
        private readonly Dictionary<Map, List<T>> byMap = new Dictionary<Map, List<T>>();

        public void Add(T comp)
        {
            Map map = comp?.parent?.Map;
            if (map == null)
            {
                return;
            }
            if (!byMap.TryGetValue(map, out List<T> list))
            {
                list = new List<T>();
                byMap[map] = list;
            }
            if (list.Contains(comp))
            {
                return;
            }
            int id = comp.parent.thingIDNumber;
            int at = list.Count;
            while (at > 0 && list[at - 1].parent.thingIDNumber > id)
            {
                at--;
            }
            list.Insert(at, comp);
        }

        public void Remove(T comp, Map map)
        {
            if (map != null && byMap.TryGetValue(map, out List<T> list))
            {
                list.Remove(comp);
            }
        }

        // Read-only view; iterate by index. Never null.
        public List<T> On(Map map)
        {
            return map != null && byMap.TryGetValue(map, out List<T> list) ? list : Empty;
        }

        public void RemoveMap(Map map)
        {
            if (map != null)
            {
                byMap.Remove(map);
            }
        }

        public void Clear()
        {
            byMap.Clear();
        }
    }

    public static class StormproofRegistry
    {
        public static readonly CompRegistry<CompSolarShield> Shields = new CompRegistry<CompSolarShield>();
        public static readonly CompRegistry<CompSurgeProtector> SurgeProtectors = new CompRegistry<CompSurgeProtector>();
        public static readonly CompRegistry<CompStormSpire> Spires = new CompRegistry<CompStormSpire>();
        public static readonly CompRegistry<CompEmpDampener> Dampeners = new CompRegistry<CompEmpDampener>();
        public static readonly CompRegistry<CompStormCapacitor> Capacitors = new CompRegistry<CompStormCapacitor>();
        public static readonly CompRegistry<CompAtmosphericBarrier> AtmosphericBarriers = new CompRegistry<CompAtmosphericBarrier>();
        public static readonly CompRegistry<CompClimateStabilizer> ClimateStabilizers = new CompRegistry<CompClimateStabilizer>();
        public static readonly CompRegistry<CompSkyRestorer> SkyRestorers = new CompRegistry<CompSkyRestorer>();
        public static readonly CompRegistry<CompDroughtCondenser> DroughtCondensers = new CompRegistry<CompDroughtCondenser>();
        public static readonly CompRegistry<CompLoadShedder> LoadShedders = new CompRegistry<CompLoadShedder>();

        public static void Clear()
        {
            LoadShedders.Clear();
            Shields.Clear();
            SurgeProtectors.Clear();
            Spires.Clear();
            Dampeners.Clear();
            Capacitors.Clear();
            AtmosphericBarriers.Clear();
            ClimateStabilizers.Clear();
            SkyRestorers.Clear();
            DroughtCondensers.Clear();
        }

        // Temporary maps (quests, caravan ambushes) leave without despawning
        // every building, so drop their buckets wholesale.
        public static void RemoveMap(Map map)
        {
            LoadShedders.RemoveMap(map);
            Shields.RemoveMap(map);
            SurgeProtectors.RemoveMap(map);
            Spires.RemoveMap(map);
            Dampeners.RemoveMap(map);
            Capacitors.RemoveMap(map);
            AtmosphericBarriers.RemoveMap(map);
            ClimateStabilizers.RemoveMap(map);
            SkyRestorers.RemoveMap(map);
            DroughtCondensers.RemoveMap(map);
        }
    }
}
