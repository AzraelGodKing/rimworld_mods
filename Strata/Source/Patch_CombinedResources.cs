using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Makes the level graph read as one colony inventory, without any phantom
    // stockpile. Two layers:
    //
    // 1. Always on: the build menu's stuff dropdown accepts materials that
    //    exist on ANY linked level, so a wood wall is placeable on a level
    //    with no wood - construction demand pull fetches it after placement.
    //
    // 2. Toggleable ("combined level resources" in play settings, default ON):
    //    the resource readout and every other ResourceCounter reader see the
    //    sum across all linked levels. Counts are taken from live stockpile /
    //    shelf HeldThings so hibernated or rarely-ticked floors stay accurate.
    public static class StrataResources
    {
        // Default on — matches AASB “one colony column” inventory. Persisted
        // via StrataSettings.combinedLevelResources; sync on load / toggle.
        public static bool Combined = true;

        // Re-entrancy guard: our GetCount postfix sums other maps' stockpiles,
        // whose ResourceCounter readers must not recurse. Also used to read raw
        // per-level counts (LevelDemand's shortage math must never see merged totals).
        private static bool aggregating;

        private static readonly AccessTools.FieldRef<ResourceCounter, Map> mapField =
            AccessTools.FieldRefAccess<ResourceCounter, Map>("map");

        // Per-tick caches were too hot once we walked live stockpiles: GetCount
        // runs every UI frame. Rebuild extras at most every CacheInterval ticks.
        private const int CacheInterval = 60;

        private static readonly Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>> extrasCache =
            new Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>>();

        private static readonly Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>> mergedCache =
            new Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>>();

        public static Map MapOf(ResourceCounter counter) => mapField(counter);

        public static bool Aggregating => aggregating;

        public static void SyncFromSettings()
        {
            if (StrataMod.Settings != null)
            {
                Combined = StrataMod.Settings.combinedLevelResources;
            }
        }

        public static void SyncToSettings()
        {
            if (StrataMod.Settings != null)
            {
                StrataMod.Settings.combinedLevelResources = Combined;
            }
        }

        public static int RawGetCount(Map map, ThingDef def)
        {
            bool prev = aggregating;
            aggregating = true;
            try
            {
                return map.resourceCounter.GetCount(def);
            }
            finally
            {
                aggregating = prev;
            }
        }

        public static int LinkedExtra(Map map, ThingDef def)
        {
            return LinkedExtras(map).TryGetValue(def, out int extra) ? extra : 0;
        }

        private static Dictionary<ThingDef, int> LinkedExtras(Map map)
        {
            int tick = Find.TickManager.TicksGame;
            if (extrasCache.TryGetValue(map, out Pair<int, Dictionary<ThingDef, int>> cached)
                && tick - cached.First < CacheInterval)
            {
                return cached.Second;
            }
            var extras = new Dictionary<ThingDef, int>();
            bool prev = aggregating;
            aggregating = true;
            try
            {
                foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
                {
                    Map other = link.map;
                    if (other == null || other.Disposed) continue;
                    // Prefer counter snapshot (cheap). HeldThings only when the
                    // other map's counter looks empty but it has storage groups.
                    AddCountedResources(other, extras);
                }
            }
            finally
            {
                aggregating = prev;
            }
            extrasCache[map] = new Pair<int, Dictionary<ThingDef, int>>(tick, extras);
            PruneDeadMaps(extrasCache);
            return extras;
        }

        private static void AddCountedResources(Map other, Dictionary<ThingDef, int> into)
        {
            Dictionary<ThingDef, int> amounts = other.resourceCounter?.AllCountedAmounts;
            if (amounts != null && amounts.Count > 0)
            {
                int positive = 0;
                foreach (KeyValuePair<ThingDef, int> kv in amounts)
                {
                    if (kv.Value <= 0) continue;
                    positive++;
                    into.TryGetValue(kv.Key, out int have);
                    into[kv.Key] = have + kv.Value;
                }
                if (positive > 0) return;
            }

            // Fallback for hibernated / never-updated counters.
            List<SlotGroup> groups = other.haulDestinationManager?.AllGroupsListForReading;
            if (groups == null) return;
            for (int i = 0; i < groups.Count; i++)
            {
                SlotGroup group = groups[i];
                if (group == null) continue;
                foreach (Thing held in group.HeldThings)
                {
                    Thing inner = held.GetInnerIfMinified();
                    if (inner == null || !inner.def.CountAsResource) continue;
                    into.TryGetValue(inner.def, out int have);
                    into[inner.def] = have + inner.stackCount;
                }
            }
        }

        public static Dictionary<ThingDef, int> MergedCounts(Map map, Dictionary<ThingDef, int> own)
        {
            int tick = Find.TickManager.TicksGame;
            if (mergedCache.TryGetValue(map, out Pair<int, Dictionary<ThingDef, int>> cached)
                && tick - cached.First < CacheInterval)
            {
                return cached.Second;
            }
            var merged = new Dictionary<ThingDef, int>(own);
            foreach (KeyValuePair<ThingDef, int> kv in LinkedExtras(map))
            {
                merged.TryGetValue(kv.Key, out int have);
                merged[kv.Key] = have + kv.Value;
            }
            mergedCache[map] = new Pair<int, Dictionary<ThingDef, int>>(tick, merged);
            PruneDeadMaps(mergedCache);
            return merged;
        }

        internal static void ClearCaches()
        {
            extrasCache.Clear();
            mergedCache.Clear();
        }

        private static void PruneDeadMaps(Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>> cache)
        {
            if (cache.Count <= Find.Maps.Count)
            {
                return;
            }
            List<Map> dead = null;
            foreach (Map key in cache.Keys)
            {
                if (!Find.Maps.Contains(key))
                {
                    (dead ??= new List<Map>()).Add(key);
                }
            }
            if (dead != null)
            {
                for (int i = 0; i < dead.Count; i++)
                {
                    cache.Remove(dead[i]);
                }
            }
        }

        // The stuff dropdown's presence check, widened to the whole level
        // graph. Returns a non-empty list when the stuff exists anywhere
        // reachable; the caller only reads Count.
        public static List<Thing> ThingsOfDefAcrossLevels(ListerThings lister, ThingDef def)
        {
            List<Thing> local = lister.ThingsOfDef(def);
            if (local.Count > 0)
            {
                return local;
            }
            Map map = Find.CurrentMap;
            if (map == null || !LevelGraph.AnyLinkFrom(map))
            {
                return local;
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
            {
                List<Thing> other = link.map.listerThings.ThingsOfDef(def);
                if (other.Count > 0)
                {
                    return other;
                }
            }
            return local;
        }
    }

    [HarmonyPatch(typeof(ResourceCounter), nameof(ResourceCounter.GetCount))]
    public static class Patch_ResourceCounter_GetCount
    {
        public static void Postfix(ResourceCounter __instance, ThingDef rDef, ref int __result)
        {
            if (!StrataResources.Combined || StrataResources.Aggregating)
            {
                return;
            }
            Map map = StrataResources.MapOf(__instance);
            if (map == null || !LevelGraph.AnyLinkFrom(map))
            {
                return;
            }
            __result += StrataResources.LinkedExtra(map, rDef);
        }
    }

    [HarmonyPatch(typeof(ResourceCounter), nameof(ResourceCounter.AllCountedAmounts), MethodType.Getter)]
    public static class Patch_ResourceCounter_AllCountedAmounts
    {
        public static void Postfix(ResourceCounter __instance, ref Dictionary<ThingDef, int> __result)
        {
            if (!StrataResources.Combined || StrataResources.Aggregating)
            {
                return;
            }
            Map map = StrataResources.MapOf(__instance);
            if (map == null || !LevelGraph.AnyLinkFrom(map))
            {
                return;
            }
            __result = StrataResources.MergedCounts(map, __result);
        }
    }

    // Swap the stuff dropdown's "is this material on the map" check for the
    // cross-level version, so materials on other levels unlock placement.
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.ProcessInput))]
    public static class Patch_DesignatorBuild_StuffMenu
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.Method(typeof(ListerThings), nameof(ListerThings.ThingsOfDef));
            MethodInfo replacement = AccessTools.Method(typeof(StrataResources), nameof(StrataResources.ThingsOfDefAcrossLevels));
            bool patched = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!patched && instruction.Calls(original))
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Call, replacement)
                        .WithLabels(instruction.labels).WithBlocks(instruction.blocks);
                    continue;
                }
                yield return instruction;
            }
            if (!patched)
            {
                Log.Warning("[Strata] Could not widen the build menu's stuff dropdown to other levels (ThingsOfDef call not found).");
            }
        }
    }

    // Play-settings toggle for the combined readout, next to the smoke toggle.
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    public static class Patch_CombinedResourcesToggle
    {
        private static Texture2D icon;
        private static bool lastCombined = true;

        // Lazy: StaticConstructor ContentFinder can cache BadTex before textures load.
        private static Texture2D Icon =>
            icon ??= TexButton.CategorizedResourceReadout
                ?? ContentFinder<Texture2D>.Get("UI/Buttons/ResourceReadoutCategorized", reportFailure: false)
                ?? BaseContent.WhiteTex;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref StrataResources.Combined, Icon,
                "Strata_PlaySettings_CombinedResourcesTip".Translate());
            if (StrataResources.Combined != lastCombined)
            {
                lastCombined = StrataResources.Combined;
                StrataResources.SyncToSettings();
                StrataResources.ClearCaches();
            }
        }
    }
}
