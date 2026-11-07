#nullable enable
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
        public Thing? nearestRot;
        public float nearestRotDays = -1f;
        public readonly List<KindRow> kinds = new List<KindRow>();

        /// <summary>Distinct preserve kinds found, by defName.</summary>
        public readonly List<ThingDef> preserveDefs = new List<ThingDef>();

        public struct KindRow
        {
            public ThingDef? def;
            public int count;
            public float rotProgress;

            /// <summary>Row text, built once per scan instead of every frame (AZR-561).</summary>
            public string label;
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
        private static PantryReport? cached;
        private static int cachedTick = -99999;

        internal static PantryReport Snapshot()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cached != null && now >= cachedTick && now - cachedTick < 60)
            {
                return cached;
            }

            cached = Scan();
            cachedTick = now;
            return cached;
        }

        internal static void Invalidate()
        {
            cached = null;
            cachedTick = -99999;
            byMap.Clear();
        }

        // Game-logic snapshots per map (larder mood, festival). Only the game tick
        // refreshes them (GameComponent_HarvestFestival, every 250 ticks), so UI
        // reads such as the Needs tab never change what Multiplayer clients compute.
        private static readonly Dictionary<int, PantryReport> byMap = new Dictionary<int, PantryReport>();
        private static readonly PantryReport Empty = new PantryReport();

        /// <summary>One map's pantry as of the last refresh (AZR-576, AZR-581).</summary>
        internal static PantryReport ForMap(Map map)
        {
            return byMap.TryGetValue(map.uniqueID, out PantryReport? report) ? report : Empty;
        }

        /// <summary>Rescans one map. Game tick only.</summary>
        internal static PantryReport RefreshMap(Map map)
        {
            PantryReport report = Scan(map);
            byMap[map.uniqueID] = report;
            return report;
        }

        private static PantryReport Scan(Map? only = null)
        {
            PantryReport report = new PantryReport();
            Dictionary<ThingDef, int> kindCounts = new Dictionary<ThingDef, int>();
            Dictionary<ThingDef, float> kindRot = new Dictionary<ThingDef, float>();
            HashSet<ThingDef> seenPreserveDefs = new HashSet<ThingDef>();
            Dictionary<ThingDef, float> kindLandrace = new Dictionary<ThingDef, float>();
            int colonists = 0;

            if (Verse.Current.ProgramState != ProgramState.Playing || Find.Maps == null)
            {
                return report;
            }

            foreach (Map map in Find.Maps)
            {
                if (map?.mapPawns == null || (only != null && map != only))
                {
                    continue;
                }

                colonists += map.mapPawns.FreeColonistsSpawnedCount;

                for (int s = 0; s < StorageDefNames.Length; s++)
                {
                    ThingDef? storageDef = DefDatabase<ThingDef>.GetNamedSilentFail(StorageDefNames[s]);
                    if (storageDef == null)
                    {
                        continue;
                    }

                    List<Thing> buildings = map.listerThings.ThingsOfDef(storageDef);
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        Building_Storage? storage = buildings[i] as Building_Storage;
                        if (storage?.GetSlotGroup() == null)
                        {
                            continue;
                        }

                        foreach (Thing thing in storage.GetSlotGroup().HeldThings)
                        {
                            Tally(thing, report, kindCounts, kindRot, seenPreserveDefs, kindLandrace);
                        }
                    }
                }
            }

            report.colonistCount = colonists;
            report.preserveKinds = seenPreserveDefs.Count;
            report.preserveDefs.AddRange(seenPreserveDefs);
            report.preserveDefs.Sort((a, b) => string.CompareOrdinal(a.defName, b.defName));
            if (colonists > 0)
            {
                report.daysOfFood = report.nutrition / (colonists * NutritionPerColonistPerDay);
            }

            foreach (KeyValuePair<ThingDef, int> kv in kindCounts)
            {
                kindRot.TryGetValue(kv.Key, out float rot);
                kindLandrace.TryGetValue(kv.Key, out float landrace);
                string label = kv.Key.LabelCap + "  ×" + kv.Value;
                if (landrace > 0.001f)
                {
                    // The hidden seed-line bonus, made visible in the stock list (AZR-698).
                    label += "  " + "Homesteader_PantryLandrace".Translate((landrace * 100f).ToString("F0"));
                }

                report.kinds.Add(new PantryReport.KindRow
                {
                    def = kv.Key,
                    count = kv.Value,
                    rotProgress = rot,
                    label = label
                });
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
            HashSet<ThingDef> seenPreserveDefs,
            Dictionary<ThingDef, float> kindLandrace)
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
                seenPreserveDefs.Add(thing.def);
            }

            if (HomesteaderHarmony.HasLandrace(thing.def))
            {
                float total = thing.TryGetComp<CompLandrace>()?.Total ?? 0f;
                if (!kindLandrace.TryGetValue(thing.def, out float best) || total > best)
                {
                    kindLandrace[thing.def] = total;
                }
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

            CompRottable? rot = thing.TryGetComp<CompRottable>();
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
