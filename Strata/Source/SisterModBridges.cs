using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Soft bridges to sister mods in this repo — every hook no-ops when the
    // target mod or def is absent.
    [StaticConstructorOnStartup]
    public static class SisterModBridges
    {
        public static bool HomesteaderLoaded { get; private set; }

        public static bool WellspringLoaded { get; private set; }

        public static bool StormproofLoaded { get; private set; }

        public static bool RootCellarPresent { get; private set; }

        public static bool HandDugWellPresent { get; private set; }

        public static bool FalloutScrubberPresent { get; private set; }

        public static bool AnomalyMutantPresent { get; private set; }

        static SisterModBridges()
        {
            HomesteaderLoaded = ModsConfig.IsActive("AzraelGodKing.Homesteader");
            WellspringLoaded = ModsConfig.IsActive("AzraelGodKing.Wellspring");
            StormproofLoaded = ModsConfig.IsActive("AzraelGodKing.Stormproof");

            RootCellarPresent = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_RootCellar") != null;
            HandDugWellPresent = DefDatabase<ThingDef>.GetNamedSilentFail("Wellspring_HandDugWell") != null;
            FalloutScrubberPresent = DefDatabase<ThingDef>.GetNamedSilentFail("Stormproof_FalloutScrubber") != null;

            AnomalyMutantPresent = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mutant") != null
                || DefDatabase<ThingDef>.GetNamedSilentFail("Mutant") != null;

            var parts = new List<string>();
            if (HomesteaderLoaded)
            {
                parts.Add("Homesteader" + (RootCellarPresent ? " (root cellar)" : ""));
            }
            if (WellspringLoaded)
            {
                parts.Add("Wellspring" + (HandDugWellPresent ? " (hand-dug well)" : ""));
            }
            if (StormproofLoaded)
            {
                parts.Add("Stormproof" + (FalloutScrubberPresent ? " (fallout scrubber)" : ""));
            }
            if (AnomalyMutantPresent)
            {
                parts.Add("Anomaly (Mutant def)");
            }
            if (parts.Count > 0)
            {
                Log.Message("[Strata] Sister-mod bridges active: " + string.Join(", ", parts));
            }
        }

        internal static bool IsOnRootCellar(Thing thing)
        {
            if (!RootCellarPresent || thing?.Map == null || !thing.Spawned)
            {
                return false;
            }
            Building building = thing.Position.GetFirstBuilding(thing.Map);
            return building?.def?.defName != null && building.def.defName.Contains("RootCellar");
        }
    }

    // Root cellars stay cool; underground Strata levels add another chill layer.
    [HarmonyPatch(typeof(CompRottable), "RotProgressPerInterval", MethodType.Getter)]
    public static class Patch_HomesteaderRootCellarRot
    {
        private const float UndergroundRootCellarFactor = 0.5f;

        public static void Postfix(CompRottable __instance, ref float __result)
        {
            if (__result <= 0f || !SisterModBridges.RootCellarPresent)
            {
                return;
            }
            Thing parent = __instance.parent;
            if (parent?.Map == null || !StrataMapUtility.IsUnderground(parent.Map))
            {
                return;
            }
            if (!SisterModBridges.IsOnRootCellar(parent))
            {
                return;
            }
            __result *= UndergroundRootCellarFactor;
        }
    }

    // Stormproof fallout scrubbers treat enclosed underground rooms as indoor.
    [HarmonyPatch(typeof(Room), "PsychologicallyOutdoors", MethodType.Getter)]
    public static class Patch_StormproofUndergroundRooms
    {
        public static void Postfix(Room __instance, ref bool __result)
        {
            if (!__result || !SisterModBridges.StormproofLoaded || __instance?.Map == null)
            {
                return;
            }
            if (!StrataMapUtility.IsUnderground(__instance.Map))
            {
                return;
            }
            if (!__instance.UsesOutdoorTemperature && __instance.CellCount > 1)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Plant), "GrowthRateFactor_Fertility", MethodType.Getter)]
    public static class Patch_WellspringUndergroundIrrigation
    {
        private const string IrrigatedSoilDefName = "Wellspring_IrrigatedSoil";

        private const float UndergroundFertilityBonus = 0.05f;

        public static void Postfix(Plant __instance, ref float __result)
        {
            if (!SisterModBridges.WellspringLoaded || __result <= 0f)
            {
                return;
            }
            Map map = __instance.Map;
            if (map == null || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            TerrainDef terrain = map.terrainGrid.TerrainAt(__instance.Position);
            if (terrain?.defName != IrrigatedSoilDefName)
            {
                return;
            }
            float baseFertility = terrain.fertility;
            if (baseFertility <= 0f)
            {
                return;
            }
            __result *= (baseFertility + UndergroundFertilityBonus) / baseFertility;
        }
    }
}
