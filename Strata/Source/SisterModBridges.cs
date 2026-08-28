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

        public static bool VanillaGravshipExpandedLoaded { get; private set; }

        public static bool RootCellarPresent { get; private set; }

        public static bool HandDugWellPresent { get; private set; }

        public static bool FalloutScrubberPresent { get; private set; }

        public static bool AnomalyMutantPresent { get; private set; }

        static SisterModBridges()
        {
            HomesteaderLoaded = ModsConfig.IsActive("AzraelGodKing.Homesteader");
            // Water infrastructure now ships inside Homesteader (Wellspring_* defs).
            // Keep WellspringLoaded as an alias for irrigation bridges.
            WellspringLoaded = HomesteaderLoaded
                || ModsConfig.IsActive("AzraelGodKing.Wellspring");
            StormproofLoaded = ModsConfig.IsActive("AzraelGodKing.Stormproof");
            VanillaGravshipExpandedLoaded = ModsConfig.IsActive(StrataVgeCompat.PackageId)
                || ModLister.GetActiveModWithIdentifier(StrataVgeCompat.PackageId) != null;
            if (VanillaGravshipExpandedLoaded)
            {
                StrataLog.Verbose("[Strata] Soft-compat: Vanilla Gravship Expanded - Chapter 1 detected ("
                    + StrataVgeCompat.PackageId + "). Gravship underdeck follow uses VGE engine/scaffold awareness.");
            }

            RootCellarPresent = DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_RootCellar") != null;
            HandDugWellPresent = DefDatabase<ThingDef>.GetNamedSilentFail("Wellspring_HandDugWell") != null;
            FalloutScrubberPresent = DefDatabase<ThingDef>.GetNamedSilentFail("Stormproof_FalloutScrubber") != null;

            AnomalyMutantPresent = DefDatabase<PawnKindDef>.GetNamedSilentFail("Mutant") != null
                || DefDatabase<ThingDef>.GetNamedSilentFail("Mutant") != null;

            var parts = new List<string>();
            if (HomesteaderLoaded)
            {
                var extras = new List<string>();
                if (RootCellarPresent)
                {
                    extras.Add("root cellar");
                }
                if (HandDugWellPresent)
                {
                    extras.Add("wells");
                }
                parts.Add("Homesteader" + (extras.Count > 0 ? " (" + string.Join(", ", extras) + ")" : ""));
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
                StrataLog.Verbose("[Strata] Sister-mod bridges active: " + string.Join(", ", parts));
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
    // RimWorld 1.6 removed RotProgressPerInterval; rot now advances in TickInterval.
    [HarmonyPatch(typeof(CompRottable), "TickInterval")]
    public static class Patch_HomesteaderRootCellarRot
    {
        private const float UndergroundRootCellarFactor = 0.5f;

        public struct RotTickState
        {
            public float rot;
            public bool apply;
        }

        public static void Prefix(CompRottable __instance, out RotTickState __state)
        {
            __state.rot = __instance.RotProgress;
            __state.apply = ShouldSlow(__instance);
        }

        public static void Postfix(CompRottable __instance, RotTickState __state)
        {
            if (!__state.apply)
            {
                return;
            }
            float added = __instance.RotProgress - __state.rot;
            if (added > 0f)
            {
                __instance.RotProgress = __state.rot + added * UndergroundRootCellarFactor;
            }
        }

        private static bool ShouldSlow(CompRottable instance)
        {
            if (!SisterModBridges.RootCellarPresent)
            {
                return false;
            }
            Thing parent = instance.parent;
            if (parent?.Map == null || !StrataMapUtility.IsUnderground(parent.Map))
            {
                return false;
            }
            return SisterModBridges.IsOnRootCellar(parent);
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
