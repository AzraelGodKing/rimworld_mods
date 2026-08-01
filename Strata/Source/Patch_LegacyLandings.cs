using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // Saves from before Strata spawned its own landings have the vanilla rope
    // "cave exit" at the bottom of every level - no power shaft comp, no Strata
    // behavior at all. Swap each one for its entrance's real exitDef on load.
    // Also realigns any landing that still sits at map center instead of under
    // its shaft (same fix as shaft conduit vertical stacking).
    [HarmonyPatch(typeof(Game), nameof(Game.FinalizeInit))]
    public static class Patch_ReplaceLegacyLandings
    {
        public static void Postfix()
        {
            // Session statics first: tick-stamped, pawn-ID-keyed state from a
            // previously loaded save is garbage in this one.
            // Session statics first: tick-stamped, pawn-ID-keyed state from a
            // previously loaded save is garbage in this one.
            PawnRelay.ResetSession();
            RelayClaims.ResetSession();
            MapComponent_RaidPursuit.ResetSession();
            StrataRobotDiagnostics.ResetSession();
            DraftedPortalPathing.ResetSession();
            PortalRelayChain.ResetSession();
            SleepRelay.ResetSession();
            HaulToLevelTargets.ResetSession();
            CrossLevelOrderedJobs.ResetSession();
            StrataCrossLevelCombat.ResetSession();
            StrataConstructAcrossLevels.ResetSession();
            CaravanTravelUtility.ResetSession();
            StrataPortalUtility.ResetHaulDeliverSession();
            StrataFaultGuard.ResetSession();
            StrataPocketMapOpen.ResetSession();
            StrataResources.ClearCaches();

            UpgradeVanillaCaveExits();
            RealignAllStairLandings();
            StrataPortalUtility.RepairMissingLandings();
            StrataPortalUtility.StampLinkedPortalPairIds();
            int relinked = StrataPortalUtility.RelinkPortalsByPairId();
            if (relinked > 0)
            {
                Log.Message("[Strata] Relinked " + relinked + " portal pair(s) by pair ID.");
            }
            RepairAllPocketMapTiles();
            Building_ShaftConduit.ReconcileAllAfterLoad();
            Building_OreHoist.ReconcileAllAfterLoad();
            CompShaftFluidJunctionLink.ReconcileAllAfterLoad();
            RebindGravshipOrphansAfterLoad();
        }

        // A bad gravship land can leave furnished levels detached and shafts
        // unwired; saves carry that damage forward. Re-adopt and re-wire once
        // per load — quiet no-op on healthy saves.
        // MUST run on the main thread: FinalizeInit executes on the LongEvent
        // worker thread, and this path reads engine.ValidSubstructure, which
        // regenerates the GravshipMask section mesh — creating a UnityEngine.Mesh
        // off-thread hard-crashes.
        private static void RebindGravshipOrphansAfterLoad()
        {
            LongEventHandler.ExecuteWhenFinished(RebindGravshipOrphansOnMainThread);
        }

        private static void RebindGravshipOrphansOnMainThread()
        {
            var stacks = WorldComponent_StrataGravshipStacks.Get();
            if (stacks == null)
            {
                return;
            }
            // Snapshot hosts first: RebindOrphans can destroy duplicate pocket
            // maps, which mutates Find.Maps mid-iteration.
            var hosts = new List<Map>();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !map.IsPlayerHome)
                {
                    continue;
                }
                if (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
                {
                    continue;
                }
                if (StrataGravshipUtility.FindGravEngineOnMap(map) == null)
                {
                    continue;
                }
                if (StrataGravshipOrphanLevels.HasRepairWork(map))
                {
                    hosts.Add(map);
                }
            }
            for (int i = 0; i < hosts.Count; i++)
            {
                Log.Message("[Strata] Load repair: rebinding gravship levels on map "
                    + hosts[i].uniqueID + ".");
                stacks.RebindOrphans(hosts[i]);
            }

            // Even on healthy saves: snap landings under their shafts and clear
            // orphaned duplicate landings + ghost deck left by old versions.
            maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null)
                {
                    continue;
                }
                // Upper decks (colony towers and ship decks): shrink deck whose
                // support below vanished while the SetRoof hook was bypassed.
                if (StrataMapUtility.IsUpperLevel(map))
                {
                    UpperDeckUtility.SweepUnsupportedDeck(map);
                    continue;
                }
                if (StrataMapUtility.IsUnderground(map)
                    || StrataGravshipUtility.FindGravEngineOnMap(map) == null)
                {
                    continue;
                }
                StrataGravshipPortalTravel.SnapAllLandingsUnderShafts(map);
                StrataGravshipPortalTravel.CleanupPocketLeftovers(map);
            }
        }

        private static void UpgradeVanillaCaveExits()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                var portals = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal));
                for (int j = 0; j < portals.Count; j++)
                {
                    if (!(portals[j] is PocketMapExit exit) || exit is Building_StairsUp || !exit.Spawned)
                    {
                        continue;
                    }
                    if (!(exit.entrance is Building_StairsDown entrance))
                    {
                        continue;
                    }
                    ThingDef properDef = entrance.def.portal?.exitDef;
                    if (properDef == null || exit.def == properDef)
                    {
                        continue;
                    }
                    PocketMapUtility.currentlyGeneratingPortal = entrance;
                    try
                    {
                        exit.DeSpawn();
                        IntVec3 spot = entrance.FindLandingCell(map);
                        if (!spot.IsValid)
                        {
                            spot = StrataMapUtility.VerticalAlign(entrance.Position, entrance.Map, map);
                        }
                        StrataPortalUtility.SpawnLanding(properDef, spot, map);
                    }
                    finally
                    {
                        PocketMapUtility.currentlyGeneratingPortal = null;
                    }
                    Log.Message($"[Strata] Upgraded legacy landing under {entrance.LabelCap} to {properDef.defName}.");
                }
            }
        }

        private static void RealignAllStairLandings()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                var entrances = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal));
                for (int j = 0; j < entrances.Count; j++)
                {
                    if (entrances[j] is Building_StairsDown entrance && entrance.Spawned && entrance.PocketMapExists)
                    {
                        entrance.TryRealignLandingIfNeeded();
                    }
                }
            }
        }

        private static void RepairAllPocketMapTiles()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                PocketMapColonyTileUtility.TryAssign(maps[i]);
            }
        }
    }
}
