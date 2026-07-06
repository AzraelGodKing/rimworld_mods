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

        public static IEnumerable<T> On<T>(HashSet<T> set, Map map) where T : ThingComp
        {
            return set.Where(c => c.parent != null && c.parent.Spawned && c.parent.Map == map);
        }
    }
}
