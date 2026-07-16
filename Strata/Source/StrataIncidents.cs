using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Strata
{
    [DefOf]
    public static class StrataIncidentDefOf
    {
        public static IncidentDef Strata_CaveIn;

        public static IncidentDef Strata_GasPocket;

        public static IncidentDef Strata_DeepVein;

        public static IncidentDef Strata_ProspectorTip;

        public static IncidentDef Strata_Tremor;

        public static IncidentDef Strata_DeepRaid;

        public static IncidentDef Strata_FloodSeep;

        public static IncidentDef Strata_GasFirestorm;

        public static IncidentDef Strata_DeepSiege;

        public static IncidentDef Strata_CaveBreakthrough;

        public static IncidentDef Strata_ProspectorDig;

        public static IncidentDef Strata_LostMiners;

        public static IncidentDef Strata_SunkenRuinDiscovered;

        public static IncidentDef Strata_CollapsedMineDiscovered;

        public static IncidentDef Strata_SealedVaultDiscovered;

        public static IncidentDef Strata_GeothermalVentDiscovered;

        static StrataIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StrataIncidentDefOf));
        }

        public static bool IsStrataUndergroundEvent(IncidentDef def)
        {
            return def == Strata_CaveIn || def == Strata_GasPocket
                || def == Strata_DeepVein || def == Strata_DeepRaid
                || def == Strata_FloodSeep || def == Strata_GasFirestorm
                || def == Strata_CaveBreakthrough || def == Strata_LostMiners;
        }
    }

    public static class StrataMapUtility
    {
        public const string UndergroundBiome = "Strata_Underground";

        public static bool IsUnderground(Map map)
        {
            if (map?.Biome == null)
            {
                return false;
            }
            if (map.Biome.defName == UndergroundBiome)
            {
                return true;
            }
            return BiomesCavernsUtility.IsActive && BiomesCavernsUtility.IsStrataCavernBiome(map.Biome);
        }

        public static bool IsSurfacePlayerHome(Map map)
        {
            return map != null && map.IsPlayerHome && !IsUnderground(map);
        }

        // Valid alone is not enough: pocket maps can carry stale tile IDs that
        // are out of WorldGrid bounds and crash TileTemperaturesComp on load.
        public static bool IsWorldGridTile(PlanetTile tile)
        {
            return tile.Valid && tile.tileId >= 0
                && Find.WorldGrid != null && tile.tileId < Find.WorldGrid.TilesCount;
        }

        // Pocket maps inherit an invalid world tile; plant growth and some
        // incidents need the surface colony tile from the portal chain.
        public static PlanetTile ResolveColonyPlanetTile(Map map)
        {
            Map current = map;
            int guard = 0;
            while (current != null && guard++ < 64)
            {
                if (IsWorldGridTile(current.Tile))
                {
                    return current.Tile;
                }
                if (current.Parent is PocketMapParent pocket)
                {
                    if (pocket.sourceMap != null)
                    {
                        current = pocket.sourceMap;
                        continue;
                    }
                    Map linkedHome = ColonyBedUtility.FindSurfacePlayerHome(current);
                    if (linkedHome != null && IsWorldGridTile(linkedHome.Tile))
                    {
                        return linkedHome.Tile;
                    }
                }
                Map home = ColonyBedUtility.FindSurfacePlayerHome(current);
                if (home != null && IsWorldGridTile(home.Tile))
                {
                    return home.Tile;
                }
                break;
            }
            if (map != null && (IsUnderground(map) || map.IsPocketMap))
            {
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    Map surface = maps[i];
                    if (IsSurfacePlayerHome(surface) && IsWorldGridTile(surface.Tile))
                    {
                        return surface.Tile;
                    }
                }
                PlanetTile fromWorld = ResolvePlayerHomePlanetTileFromWorld();
                if (IsWorldGridTile(fromWorld))
                {
                    return fromWorld;
                }
            }
            return PlanetTile.Invalid;
        }

        private static PlanetTile ResolvePlayerHomePlanetTileFromWorld()
        {
            World world = Find.World;
            if (world?.worldObjects == null)
            {
                return PlanetTile.Invalid;
            }
            List<MapParent> mapParents = world.worldObjects.MapParents;
            for (int i = 0; i < mapParents.Count; i++)
            {
                MapParent parent = mapParents[i];
                if (parent?.Faction != Faction.OfPlayer || !parent.HasMap)
                {
                    continue;
                }
                if (IsSurfacePlayerHome(parent.Map) && IsWorldGridTile(parent.Tile))
                {
                    return parent.Tile;
                }
            }
            List<Settlement> settlements = world.worldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement?.Faction == Faction.OfPlayer && IsWorldGridTile(settlement.Tile))
                {
                    return settlement.Tile;
                }
            }
            return PlanetTile.Invalid;
        }

        // Same relative position on another level's grid — used for landings,
        // shaft junctions, and camera jumps so things stack vertically.
        public static IntVec3 ProportionalCell(IntVec3 pos, Map from, Map to)
        {
            if (from == null || to == null || from.Size.x <= 0 || from.Size.z <= 0)
            {
                return to?.Center ?? IntVec3.Invalid;
            }
            int x = Mathf.Clamp(Mathf.RoundToInt((float)pos.x / from.Size.x * to.Size.x), 0, to.Size.x - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((float)pos.z / from.Size.z * to.Size.z), 0, to.Size.z - 1);
            return new IntVec3(x, 0, z);
        }

        // Proportional alignment, nudging inward when the spot hugs the map edge.
        public static IntVec3 VerticalAlign(IntVec3 pos, Map from, Map to, float searchRadius = 25f, int edgeMargin = 8)
        {
            IntVec3 target = ProportionalCell(pos, from, to);
            if (target.InBounds(to) && target.DistanceToEdge(to) >= edgeMargin)
            {
                return target;
            }
            IntVec3 origin = target.IsValid ? target : to.Center;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, searchRadius, useCenter: true))
            {
                if (cell.InBounds(to) && cell.DistanceToEdge(to) >= edgeMargin)
                {
                    return cell;
                }
            }
            return to.Center;
        }
    }

    // Underground levels are pocket maps, and pocket maps have NO incident
    // target tags in vanilla - the storyteller (and even the debug incident
    // menu) never targets them with anything. Tag Strata levels as player-home
    // incident targets so infestations, diseases, and Strata's own events can
    // fire down there; the CanFireNow patch below filters out everything that
    // can't physically reach sealed rock.
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.IncidentTargetTags))]
    public static class Patch_UndergroundIncidentTargets
    {
        public static IEnumerable<IncidentTargetTagDef> Postfix(IEnumerable<IncidentTargetTagDef> values, MapParent __instance)
        {
            bool hasPlayerHome = false;
            foreach (IncidentTargetTagDef tag in values)
            {
                hasPlayerHome |= tag == IncidentTargetTagDefOf.Map_PlayerHome;
                yield return tag;
            }
            if (!hasPlayerHome && __instance is PocketMapParent && __instance.HasMap
                && StrataMapUtility.IsUnderground(__instance.Map))
            {
                yield return IncidentTargetTagDefOf.Map_PlayerHome;
            }
        }
    }

    // Pocket maps can carry a Valid but out-of-range PlanetTile; vanilla
    // CanFireNow indexes WorldGrid unconditionally and throws during
    // storyteller ticks. Repair the parent tile when possible; otherwise run
    // the non-WorldGrid gates and CanFireNowSub only.
    internal static class IncidentCanFireUtility
    {
        private static readonly MethodInfo FiredTooRecentlyMethod =
            AccessTools.Method(typeof(IncidentWorker), "FiredTooRecently");

        private static readonly MethodInfo CanFireNowSubMethod =
            AccessTools.Method(typeof(IncidentWorker), "CanFireNowSub", new[] { typeof(IncidentParms) });

        public static bool CanFireNowBypassingWorldGrid(IncidentWorker worker, IncidentParms parms)
        {
            if (!worker.def.TargetAllowed(parms.target))
            {
                return false;
            }
            if (Find.GameEnder.gameEnding)
            {
                return false;
            }
            Storyteller storyteller = Find.Storyteller;
            if (storyteller == null)
            {
                return false;
            }
            if (worker.def.category == IncidentCategoryDefOf.ThreatBig
                && storyteller.difficulty != null
                && !storyteller.difficulty.allowBigThreats)
            {
                return false;
            }
            if (Find.TickManager.TicksGame < Find.GameEnder.newWanderersCreatedTick + 300000)
            {
                return false;
            }
            try
            {
                if (FiredTooRecentlyMethod != null
                    && (bool)FiredTooRecentlyMethod.Invoke(worker, new object[] { parms.target }))
                {
                    return false;
                }
            }
            catch
            {
            }
            if (CanFireNowSubMethod == null)
            {
                return false;
            }
            return (bool)CanFireNowSubMethod.Invoke(worker, new object[] { parms });
        }
    }

    // Enforces the "underground is sealed rock" fiction: sky and weather
    // conditions and raids/threats that can't physically reach a buried level
    // never fire down there. Infestations (which erupt from within) and
    // Strata's own underground events are always allowed. Without this filter,
    // the target tag above would let the storyteller drop solar flares and
    // mech clusters on a rock vault.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    public static class Patch_UndergroundIncidents
    {
        // Sky / climate game conditions that make no sense underground.
        private static readonly HashSet<string> BlockedConditions = new HashSet<string>
        {
            "Eclipse", "SolarFlare", "ToxicFallout", "Aurora", "Flashstorm",
            "HeatWave", "ColdSnap", "VolcanicWinter", "NoxiousHaze", "BloodRain",
            "Drought", "LavaFlow", "GrayPall", "DeathPall", "UnnaturalHeat",
            "UnnaturalDarkness",
        };

        private static bool ShouldBlockUndergroundIncident(IncidentDef def)
        {
            if (def.defName.Contains("Infestation") || StrataIncidentDefOf.IsStrataUndergroundEvent(def))
            {
                return false;
            }

            IncidentCategoryDef cat = def.category;
            if (cat == IncidentCategoryDefOf.DiseaseHuman
                || cat == IncidentCategoryDefOf.DeepDrillInfestation
                || cat == IncidentCategoryDefOf.GiveQuest)
            {
                return false;
            }

            if (cat == IncidentCategoryDefOf.ThreatBig
                || cat == IncidentCategoryDefOf.ThreatSmall
                || cat == IncidentCategoryDefOf.Misc
                || cat == IncidentCategoryDefOf.Special)
            {
                return true;
            }

            return def.gameCondition != null && BlockedConditions.Contains(def.gameCondition.defName);
        }

        public static bool Prefix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!(parms.target is Map map))
            {
                return true;
            }

            bool underground = StrataMapUtility.IsUnderground(map);
            if (PocketMapColonyTileUtility.IsStrataPocketOrUnderground(map))
            {
                PocketMapColonyTileUtility.TryAssign(map);
            }

            if (underground && ShouldBlockUndergroundIncident(__instance.def))
            {
                __result = false;
                return false;
            }

            if (!StrataMapUtility.IsWorldGridTile(map.Tile)
                && PocketMapColonyTileUtility.IsStrataPocketOrUnderground(map))
            {
                __result = IncidentCanFireUtility.CanFireNowBypassingWorldGrid(__instance, parms);
                return false;
            }

            return true;
        }

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (!__result || !(parms.target is Map map) || !StrataMapUtility.IsUnderground(map))
            {
                return;
            }
            if (ShouldBlockUndergroundIncident(__instance.def))
            {
                __result = false;
            }
        }
    }

    // Dev-mode diagnostics: deep raid is Strata's only ThreatBig incident, and
    // vanilla gates ThreatBig behind checks that fail silently (big threats
    // disabled in difficulty, post-wipe wanderer grace, refire cooldown).
    // When it refuses to fire in dev mode, say which gate did it.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow))]
    public static class Patch_DeepRaidDiagnostics
    {
        // The storyteller probes CanFireNow for every candidate incident on a
        // regular cadence; without a cooldown this line floods a dev-mode log.
        private const int LogCooldownTicks = 2500;

        private static int lastLogTick = -99999;

        public static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (__result || !Prefs.DevMode || __instance.def != StrataIncidentDefOf.Strata_DeepRaid)
            {
                return;
            }
            if (!parms.forced && Find.TickManager.TicksGame - lastLogTick < LogCooldownTicks)
            {
                return;
            }
            lastLogTick = Find.TickManager.TicksGame;
            bool firedRecently = false;
            try
            {
                firedRecently = (bool)AccessTools.Method(typeof(IncidentWorker), "FiredTooRecently")
                    .Invoke(__instance, new object[] { parms.target });
            }
            catch
            {
            }
            Log.Message("[Strata] Deep raid blocked. Vanilla gates: "
                + $"targetAllowed={__instance.def.TargetAllowed(parms.target)}, "
                + $"allowBigThreats={Find.Storyteller?.difficulty?.allowBigThreats}, "
                + $"firedTooRecently={firedRecently}, "
                + $"gameEnding={Find.GameEnder.gameEnding}, "
                + $"wanderersGraceActive={Find.TickManager.TicksGame < Find.GameEnder.newWanderersCreatedTick + 300000}, "
                + $"points={parms.points}");
        }
    }

    // With every other threat suppressed underground, lean into bugs as the
    // defining danger of the deep: infestations are meaningfully more likely on
    // a Strata level than they would be under an ordinary mountain.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.ChanceFactorNow))]
    public static class Patch_UndergroundInfestationWeight
    {
        public static void Postfix(IncidentWorker __instance, IIncidentTarget target, ref float __result)
        {
            if (__result <= 0f || !__instance.def.defName.Contains("Infestation"))
            {
                return;
            }
            if (target is Map map && StrataMapUtility.IsUnderground(map))
            {
                // Deeper levels crawl with more bugs.
                __result *= Mathf.Min(1.3f + 0.35f * StrataDepth.Of(map), 3f);
            }
        }
    }
}
