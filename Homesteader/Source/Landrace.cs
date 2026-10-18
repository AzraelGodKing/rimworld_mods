using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Homesteader
{
    public class CompProperties_Landrace : CompProperties
    {
        public CompProperties_Landrace()
        {
            compClass = typeof(CompLandrace);
        }
    }

    /// <summary>
    /// Hidden crop-line quality on saved seed and on the plant grown from it.
    /// </summary>
    public class CompLandrace : ThingComp
    {
        public const float Cap = 0.25f;

        public float yieldBonus;
        public float frostBonus;
        public float droughtBonus;
        public int generations;

        public float Total => yieldBonus + frostBonus + droughtBonus;

        public void CopyFrom(CompLandrace other)
        {
            if (other == null)
            {
                return;
            }

            yieldBonus = other.yieldBonus;
            frostBonus = other.frostBonus;
            droughtBonus = other.droughtBonus;
            generations = other.generations;
        }

        public void ImproveOnHarvest(Map map, IntVec3 pos)
        {
            if (map == null)
            {
                return;
            }

            if (map.fertilityGrid.FertilityAt(pos) >= 1f)
            {
                yieldBonus = Mathf.Min(Cap, yieldBonus + 0.012f);
            }

            if (GenTemperature.GetTemperatureForCell(pos, map) < 5f)
            {
                frostBonus = Mathf.Min(Cap, frostBonus + 0.012f);
            }

            if (map.weatherManager.RainRate < 0.02f)
            {
                droughtBonus = Mathf.Min(Cap, droughtBonus + 0.012f);
            }

            generations++;
        }

        public override bool AllowStackWith(Thing other)
        {
            CompLandrace o = other?.TryGetComp<CompLandrace>();
            if (o == null)
            {
                return false;
            }

            return Bucket(yieldBonus) == Bucket(o.yieldBonus)
                && Bucket(frostBonus) == Bucket(o.frostBonus)
                && Bucket(droughtBonus) == Bucket(o.droughtBonus);
        }

        public override void PostSplitOff(Thing piece)
        {
            CompLandrace c = piece.TryGetComp<CompLandrace>();
            c?.CopyFrom(this);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref yieldBonus, "hsYieldBonus", 0f);
            Scribe_Values.Look(ref frostBonus, "hsFrostBonus", 0f);
            Scribe_Values.Look(ref droughtBonus, "hsDroughtBonus", 0f);
            Scribe_Values.Look(ref generations, "hsGenerations", 0);
        }

        public override string CompInspectStringExtra()
        {
            if (Total <= 0.001f)
            {
                return null;
            }

            string line = "Homesteader_Landrace".Translate(
                (yieldBonus * 100f).ToString("F0"),
                (frostBonus * 100f).ToString("F0"),
                (droughtBonus * 100f).ToString("F0"),
                generations);
            if ((frostBonus > 0f && !PatchHealth.LandraceFrostHooked)
                || (droughtBonus > 0f && !PatchHealth.LandraceDroughtHooked))
            {
                line += "\n" + "Homesteader_LandraceGrowthInactive".Translate();
            }

            return line;
        }

        private static int Bucket(float v) => Mathf.RoundToInt(v * 100f);
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.SpawnSetup))]
    public static class Patch_LandraceOnSow
    {
        public static void Postfix(Plant __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad || __instance == null || map == null)
            {
                return;
            }

            CompLandrace plantComp = __instance.TryGetComp<CompLandrace>();
            if (plantComp == null)
            {
                return;
            }

            if (!ColonistSowingAt(map, __instance.Position))
            {
                return;
            }

            LandraceUtility.TryConsumeSeed(__instance.def, map, plantComp);
        }

        private static bool ColonistSowingAt(Map map, IntVec3 cell)
        {
            List<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            for (int i = 0; i < pawns.Count; i++)
            {
                Job job = pawns[i].CurJob;
                if (job != null && job.def == JobDefOf.Sow && job.targetA.Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Snapshot before PlantCollected destroys or resets the plant (AZR-526).
    /// Harvest that despawns the plant leaves Map null / Growth reset in the postfix.
    /// </summary>
    public struct LandraceHarvestState
    {
        public bool valid;
        public Map map;
        public IntVec3 pos;
        public ThingDef plantDef;
        public float growth;
        public float yieldBonus;
        public float frostBonus;
        public float droughtBonus;
        public int generations;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantCollected))]
    public static class Patch_LandraceOnHarvest
    {
        public static void Prefix(Plant __instance, out LandraceHarvestState __state)
        {
            __state = default;
            if (__instance?.Map == null)
            {
                return;
            }

            CompLandrace plantComp = __instance.TryGetComp<CompLandrace>();
            if (plantComp == null)
            {
                return;
            }

            __state.valid = true;
            __state.map = __instance.Map;
            __state.pos = __instance.Position;
            __state.plantDef = __instance.def;
            __state.growth = __instance.Growth;
            __state.yieldBonus = plantComp.yieldBonus;
            __state.frostBonus = plantComp.frostBonus;
            __state.droughtBonus = plantComp.droughtBonus;
            __state.generations = plantComp.generations;
        }

        public static void Postfix(Plant __instance, Pawn by, LandraceHarvestState __state)
        {
            if (!__state.valid || __state.growth < 0.85f || __state.map == null || __state.plantDef == null)
            {
                return;
            }

            CompLandrace snap = new CompLandrace
            {
                yieldBonus = __state.yieldBonus,
                frostBonus = __state.frostBonus,
                droughtBonus = __state.droughtBonus,
                generations = __state.generations,
            };
            snap.ImproveOnHarvest(__state.map, __state.pos);
            LandraceUtility.DropSavedSeed(__state.plantDef, __state.map, __state.pos, snap);
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.YieldNow))]
    public static class Patch_LandraceYield
    {
        public static void Postfix(Plant __instance, ref int __result)
        {
            CompLandrace c = __instance?.TryGetComp<CompLandrace>();
            if (c == null || c.yieldBonus <= 0f || __result <= 0)
            {
                return;
            }

            __result = Mathf.Max(1, Mathf.RoundToInt(__result * (1f + c.yieldBonus)));
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRateFactor_Temperature), MethodType.Getter)]
    public static class Patch_LandraceFrost
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            CompLandrace c = __instance?.TryGetComp<CompLandrace>();
            if (c == null || c.frostBonus <= 0f || __result >= 0.99f)
            {
                return;
            }

            __result = Mathf.Min(1f, __result + (1f - __result) * c.frostBonus);
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GrowthRateFactor_Drought), MethodType.Getter)]
    public static class Patch_LandraceDrought
    {
        public static void Postfix(Plant __instance, ref float __result)
        {
            CompLandrace c = __instance?.TryGetComp<CompLandrace>();
            if (c == null || c.droughtBonus <= 0f || __result >= 0.99f)
            {
                return;
            }

            __result = Mathf.Min(1f, __result + (1f - __result) * c.droughtBonus);
        }
    }

    internal static class LandraceUtility
    {
        internal static ThingDef SeedDefFor(ThingDef plantDef)
        {
            if (plantDef?.defName == null)
            {
                return null;
            }

            switch (plantDef.defName)
            {
                case "Homesteader_Plant_Barley":
                    return DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_SeedBarley");
                case "Homesteader_Plant_Pumpkin":
                    return DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_SeedPumpkin");
                case "Homesteader_Plant_Herbs":
                    return DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_SeedHerbs");
                case "Homesteader_Plant_SugarBeet":
                    return DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_SeedSugarBeet");
                case "Homesteader_Plant_Flax":
                    return DefDatabase<ThingDef>.GetNamedSilentFail("Homesteader_SeedFlax");
                default:
                    return null;
            }
        }

        internal static void TryConsumeSeed(ThingDef plantDef, Map map, CompLandrace plantComp)
        {
            ThingDef seedDef = SeedDefFor(plantDef);
            if (seedDef == null || map == null || plantComp == null)
            {
                return;
            }

            List<Thing> list = map.listerThings.ThingsOfDef(seedDef);
            Thing best = null;
            float bestScore = -1f;
            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];
                if (t == null || t.Destroyed || t.stackCount < 1)
                {
                    continue;
                }

                if (t.IsForbidden(Faction.OfPlayer))
                {
                    continue;
                }

                CompLandrace c = t.TryGetComp<CompLandrace>();
                float score = c != null ? c.Total : 0f;
                if (best == null || score > bestScore)
                {
                    best = t;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                return;
            }

            Thing taken = best.stackCount == 1 ? best : best.SplitOff(1);
            CompLandrace from = taken.TryGetComp<CompLandrace>();
            plantComp.CopyFrom(from);
            if (!taken.Destroyed)
            {
                taken.Destroy(DestroyMode.Vanish);
            }
        }

        internal static void DropSavedSeed(ThingDef plantDef, Map map, IntVec3 pos, CompLandrace plantComp)
        {
            if (plantDef == null || map == null || plantComp == null)
            {
                return;
            }

            ThingDef seedDef = SeedDefFor(plantDef);
            if (seedDef == null)
            {
                return;
            }

            Thing seed = ThingMaker.MakeThing(seedDef);
            seed.stackCount = 1;
            seed.TryGetComp<CompLandrace>()?.CopyFrom(plantComp);
            GenPlace.TryPlaceThing(seed, pos, map, ThingPlaceMode.Near);
        }
    }
}
