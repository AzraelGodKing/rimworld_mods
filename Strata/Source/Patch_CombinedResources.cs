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
    // 2. Toggleable ("combined level resources" in play settings): the
    //    resource readout and every other ResourceCounter reader see the sum
    //    across all linked levels - vanilla's "colony owned items" view, but
    //    for the whole column. Count-based systems (designator cost labels,
    //    "make until X" bills) follow the toggle too.
    public static class StrataResources
    {
        public static bool Combined;

        // Re-entrancy guard: our GetCount postfix sums other maps' GetCount,
        // whose postfix must not recurse. Also used to read raw per-level
        // counts (LevelDemand's shortage math must never see merged totals).
        private static bool aggregating;

        private static readonly AccessTools.FieldRef<ResourceCounter, Map> mapField =
            AccessTools.FieldRefAccess<ResourceCounter, Map>("map");

        // Per-tick caches. GetCount is HOT (resource readout every frame,
        // "make until X" bills), so the cross-level sum must not re-walk the
        // level graph per call; counts only change while ticking, so one
        // rebuild per tick is exact. Keyed by map: pruned when maps die so a
        // collapsed level's Map can't be retained forever.
        private static readonly Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>> extrasCache =
            new Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>>();

        private static readonly Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>> mergedCache =
            new Dictionary<Map, Pair<int, Dictionary<ThingDef, int>>>();

        public static Map MapOf(ResourceCounter counter) => mapField(counter);

        public static bool Aggregating => aggregating;

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

        // Everything the linked levels hold, summed per def, rebuilt at most
        // once per tick per map.
        private static Dictionary<ThingDef, int> LinkedExtras(Map map)
        {
            int tick = Find.TickManager.TicksGame;
            if (extrasCache.TryGetValue(map, out Pair<int, Dictionary<ThingDef, int>> cached)
                && cached.First == tick)
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
                    foreach (KeyValuePair<ThingDef, int> kv in link.map.resourceCounter.AllCountedAmounts)
                    {
                        if (kv.Value > 0)
                        {
                            extras.TryGetValue(kv.Key, out int have);
                            extras[kv.Key] = have + kv.Value;
                        }
                    }
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

        public static Dictionary<ThingDef, int> MergedCounts(Map map, Dictionary<ThingDef, int> own)
        {
            int tick = Find.TickManager.TicksGame;
            if (mergedCache.TryGetValue(map, out Pair<int, Dictionary<ThingDef, int>> cached)
                && cached.First == tick)
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
            if (map != null)
            {
                foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(map))
                {
                    List<Thing> other = link.map.listerThings.ThingsOfDef(def);
                    if (other.Count > 0)
                    {
                        return other;
                    }
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
            if (map != null)
            {
                __result += StrataResources.LinkedExtra(map, rDef);
            }
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
            if (map != null)
            {
                __result = StrataResources.MergedCounts(map, __result);
            }
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
        private static readonly Texture2D icon =
            ContentFinder<Texture2D>.Get("UI/Buttons/ResourceReadoutCategorized", reportFailure: false) ?? BaseContent.BadTex;

        public static void Postfix(WidgetRow row, bool worldView)
        {
            if (worldView || row == null)
            {
                return;
            }
            row.ToggleableIcon(ref StrataResources.Combined, icon,
                "Strata: combined level resources - the resource readout and build costs count items on every linked level.");
        }
    }
}
