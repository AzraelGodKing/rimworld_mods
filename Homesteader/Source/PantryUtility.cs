using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    internal sealed class PantryReport
    {
        public int preserveKinds;
        public float nutrition;
        public float daysOfFood;
        public int colonistCount;
        public Thing nearestRot;
        public float nearestRotDays = -1f;
        public readonly List<KindRow> kinds = new List<KindRow>();

        public struct KindRow
        {
            public ThingDef def;
            public int count;
            public float rotProgress;
        }
    }

    internal enum PantrySortMode
    {
        Count,
        Name,
        RotSoonest
    }

    internal static class PantryUtility
    {
        private static readonly string[] StorageDefNames =
        {
            "Homesteader_StorageCrate",
            "Homesteader_StorageBarrel",
            "Homesteader_LargeStorageCrate",
            "Homesteader_IngredientBarrel",
            "Homesteader_RootCellar",
            "Homesteader_PreservesShelf",
            "Homesteader_Icehouse",
            "Homesteader_Springhouse"
        };

        private const float NutritionPerColonistPerDay = 1.6f;
        private static PantryReport cached;
        private static int cachedTick = -99999;

        internal static PantryReport Snapshot()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cached != null && now - cachedTick < 60)
            {
                return cached;
            }

            cached = Scan();
            cachedTick = now;
            return cached;
        }

        internal static void Invalidate()
        {
            cachedTick = -99999;
        }

        private static PantryReport Scan()
        {
            PantryReport report = new PantryReport();
            Dictionary<ThingDef, int> kindCounts = new Dictionary<ThingDef, int>();
            Dictionary<ThingDef, float> kindRot = new Dictionary<ThingDef, float>();
            HashSet<string> seenPreserveDefs = new HashSet<string>();
            int colonists = 0;

            if (Verse.Current.ProgramState != ProgramState.Playing || Find.Maps == null)
            {
                return report;
            }

            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null)
                {
                    continue;
                }

                colonists += map.mapPawns.FreeColonistsSpawnedCount;

                for (int s = 0; s < StorageDefNames.Length; s++)
                {
                    ThingDef storageDef = DefDatabase<ThingDef>.GetNamedSilentFail(StorageDefNames[s]);
                    if (storageDef == null)
                    {
                        continue;
                    }

                    List<Thing> buildings = map.listerThings.ThingsOfDef(storageDef);
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        Building_Storage storage = buildings[i] as Building_Storage;
                        if (storage?.GetSlotGroup() == null)
                        {
                            continue;
                        }

                        foreach (Thing thing in storage.GetSlotGroup().HeldThings)
                        {
                            Tally(thing, report, kindCounts, kindRot, seenPreserveDefs);
                        }
                    }
                }
            }

            report.colonistCount = colonists;
            report.preserveKinds = seenPreserveDefs.Count;
            if (colonists > 0)
            {
                report.daysOfFood = report.nutrition / (colonists * NutritionPerColonistPerDay);
            }

            foreach (KeyValuePair<ThingDef, int> kv in kindCounts)
            {
                kindRot.TryGetValue(kv.Key, out float rot);
                report.kinds.Add(new PantryReport.KindRow { def = kv.Key, count = kv.Value, rotProgress = rot });
            }

            SortKinds(report, PantrySortMode.Count);
            return report;
        }

        internal static void SortKinds(PantryReport report, PantrySortMode mode)
        {
            if (report == null)
            {
                return;
            }

            switch (mode)
            {
                case PantrySortMode.Name:
                    report.kinds.Sort((a, b) => string.Compare(a.def?.label, b.def?.label, System.StringComparison.CurrentCultureIgnoreCase));
                    break;
                case PantrySortMode.RotSoonest:
                    report.kinds.Sort((a, b) =>
                    {
                        int c = b.rotProgress.CompareTo(a.rotProgress);
                        return c != 0 ? c : b.count.CompareTo(a.count);
                    });
                    break;
                default:
                    report.kinds.Sort((a, b) => b.count.CompareTo(a.count));
                    break;
            }
        }

        private static void Tally(
            Thing thing,
            PantryReport report,
            Dictionary<ThingDef, int> kindCounts,
            Dictionary<ThingDef, float> kindRot,
            HashSet<string> seenPreserveDefs)
        {
            if (thing?.def == null)
            {
                return;
            }

            bool ingestible = thing.def.IsIngestible;
            bool preserve = PreserveCatalog.IsPreserveKind(thing.def);
            if (!ingestible && !preserve)
            {
                return;
            }

            int n = thing.stackCount;
            if (!kindCounts.TryGetValue(thing.def, out int have))
            {
                kindCounts[thing.def] = n;
            }
            else
            {
                kindCounts[thing.def] = have + n;
            }

            if (preserve)
            {
                seenPreserveDefs.Add(thing.def.defName);
            }

            float rotProgress = SpoilageTriage.FreshRotProgress(thing);
            if (!kindRot.TryGetValue(thing.def, out float worst) || rotProgress > worst)
            {
                kindRot[thing.def] = rotProgress;
            }

            if (ingestible)
            {
                report.nutrition += thing.GetStatValue(StatDefOf.Nutrition) * n;
            }

            CompRottable rot = thing.TryGetComp<CompRottable>();
            if (rot == null || rot.Stage != RotStage.Fresh)
            {
                return;
            }

            int ticks = rot.TicksUntilRotAtCurrentTemp;
            if (ticks < 0)
            {
                return;
            }

            float days = ticks / 60000f;
            if (report.nearestRot == null || days < report.nearestRotDays)
            {
                report.nearestRot = thing;
                report.nearestRotDays = days;
            }
        }
    }
}
