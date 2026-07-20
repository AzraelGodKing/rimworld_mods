using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Stormproof
{
    [DefOf]
    public static class StormproofDefOf
    {
        public static GameConditionDef SolarFlare;
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

        public static WeatherDef RainyThunderstorm;
        public static WeatherDef DryThunderstorm;

        static StormproofDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StormproofDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class StormproofInit
    {
        static StormproofInit()
        {
            new Harmony("azraelgodking.stormproof").PatchAll();
        }
    }

    // Central registry of live comps so patches don't have to scan the map.
    public static class StormproofRegistry
    {
        public static readonly HashSet<CompSolarShield> Shields = new HashSet<CompSolarShield>();
        public static readonly HashSet<CompSurgeProtector> SurgeProtectors = new HashSet<CompSurgeProtector>();
        public static readonly HashSet<CompStormSpire> Spires = new HashSet<CompStormSpire>();
        public static readonly HashSet<CompEmpDampener> Dampeners = new HashSet<CompEmpDampener>();
        public static readonly HashSet<CompStormCapacitor> Capacitors = new HashSet<CompStormCapacitor>();
        public static readonly HashSet<CompAtmosphericBarrier> AtmosphericBarriers = new HashSet<CompAtmosphericBarrier>();
        public static readonly HashSet<CompClimateStabilizer> ClimateStabilizers = new HashSet<CompClimateStabilizer>();
        public static readonly HashSet<CompSkyRestorer> SkyRestorers = new HashSet<CompSkyRestorer>();
        public static readonly HashSet<CompFireSuppressor> FireSuppressors = new HashSet<CompFireSuppressor>();
        public static readonly HashSet<CompDroughtCondenser> DroughtCondensers = new HashSet<CompDroughtCondenser>();

        public static IEnumerable<T> On<T>(HashSet<T> set, Map map) where T : ThingComp
        {
            return set.Where(c => c.parent != null && c.parent.Spawned && c.parent.Map == map);
        }
    }
}
