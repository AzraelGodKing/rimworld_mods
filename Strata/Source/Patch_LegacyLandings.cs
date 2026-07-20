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
            PawnRelay.ResetSession();
            RelayClaims.ResetSession();
            MapComponent_RaidPursuit.ResetSession();
            StrataRobotDiagnostics.ResetSession();
            DraftedPortalPathing.ResetSession();
            PortalRelayChain.ResetSession();
            SleepRelay.ResetSession();
            HaulToLevelTargets.ResetSession();
            CrossLevelOrderedJobs.ResetSession();
            CaravanTravelUtility.ResetSession();
            StrataPortalUtility.ResetHaulDeliverSession();
            StrataResources.ClearCaches();

            UpgradeVanillaCaveExits();
            RealignAllStairLandings();
            StrataPortalUtility.RepairMissingLandings();
            RepairAllPocketMapTiles();
            Building_ShaftConduit.ReconcileAllAfterLoad();
            Building_OreHoist.ReconcileAllAfterLoad();
            CompShaftFluidJunctionLink.ReconcileAllAfterLoad();
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
