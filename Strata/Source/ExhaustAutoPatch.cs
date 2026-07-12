using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // At startup, tag obvious combustion buildings that other mods forgot to patch.
    public static class ExhaustAutoPatch
    {
        private static readonly HashSet<string> ExcludedDefNames = new HashSet<string>
        {
            "Strata_ExhaustFan",
            "Strata_UpdraftFilter",
            "Strata_SmokeLouver",
            "Strata_SmokeDuct",
            "SolarGenerator",
            "WindTurbine",
            "GeothermalGenerator",
            "WoodFiredGenerator",
            "ChemfuelPoweredGenerator",
            "Campfire",
            "TorchLamp",
            "Fire",
            "Homesteader_WoodGenerator",
            "Homesteader_PortableGenerator",
        };

        public static void Apply()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.category != ThingCategory.Building || def.IsBlueprint || def.IsFrame)
                {
                    continue;
                }
                if (ExcludedDefNames.Contains(def.defName) || HasExhaustComp(def))
                {
                    continue;
                }
                float emission = EmissionFor(def);
                if (emission <= 0f)
                {
                    continue;
                }
                def.comps ??= new List<CompProperties>();
                def.comps.Add(new CompProperties_Exhaust { emissionPerCycle = emission });
            }
        }

        private static float EmissionFor(ThingDef def)
        {
            if (def.GetCompProperties<CompProperties_Refuelable>() != null)
            {
                // Fueled work benches (stove, smithy, smelter...) smoke gently,
                // and only while a pawn works them (see CompExhaust.Active). A
                // workshop without ventilation should be uncomfortable, not a
                // death trap.
                if (def.IsWorkTable)
                {
                    return 1.5f;
                }
                if (def.defName.Contains("Torch") || def.defName.Contains("Candle"))
                {
                    return 0.1f;
                }
                if (def.defName.Contains("Campfire") || def.defName.Contains("Fire"))
                {
                    return 2.5f;
                }
                // Anything else refuelable only smokes with evidence of actual
                // combustion - a passive cooler burns nothing. Braziers and
                // other always-lit flames sit at campfire level, not generator
                // level, so an ideoligion room doesn't smoke itself out.
                if (LooksLikeFlame(def))
                {
                    return 2f;
                }
                return 0f;
            }
            CompProperties_Power power = def.GetCompProperties<CompProperties_Power>();
            if (power != null && power.PowerConsumption < 0f)
            {
                if (def.defName.Contains("Chemfuel") || def.defName.Contains("Portable"))
                {
                    return 4.5f;
                }
                return 3.5f;
            }
            return 0f;
        }

        private static bool LooksLikeFlame(ThingDef def)
        {
            if (def.GetCompProperties<CompProperties_FireOverlay>() != null)
            {
                return true;
            }
            CompProperties_HeatPusher heat = def.GetCompProperties<CompProperties_HeatPusher>();
            return heat != null && heat.heatPerSecond > 0f;
        }

        private static bool HasExhaustComp(ThingDef def)
        {
            if (def.comps == null)
            {
                return false;
            }
            for (int i = 0; i < def.comps.Count; i++)
            {
                if (def.comps[i].compClass == typeof(CompExhaust))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
