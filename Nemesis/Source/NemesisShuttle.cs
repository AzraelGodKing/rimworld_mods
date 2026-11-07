#nullable enable
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Nemesis
{
    /// <summary>
    /// Odyssey shuttle drop and extract (AZR-706). A captain from a faction that can fly lands
    /// by shuttle, the shuttle waits at the landing spot, and the hunt lord's retreat runs back
    /// to it. Fail-open: any missing def or error falls back to walking in.
    /// </summary>
    public static class NemesisShuttle
    {
        private const float ShuttleChance = 0.4f;

        private static TransportShipDef? ShipDef => TransportShipDefOf.Ship_Shuttle;

        public static bool Available =>
            (NemesisMod.Settings?.enableShuttleDrops ?? true)
            && ModsConfig.OdysseyActive
            && ShipDef?.shipThing != null;

        public static bool ShouldUse(NemesisData data, Faction faction) =>
            Available
            && faction.def.techLevel >= TechLevel.Industrial
            && Rand.Chance(ShuttleChance);

        public static bool TryDrop(NemesisData data, Pawn captain, Faction faction, Map map,
            NemesisTactic tactic, bool kidnap)
        {
            TransportShipDef? def = ShipDef;
            if (!Available || def == null) return false;
            IntVec3 cell = DropCellFinder.GetBestShuttleLandingSpot(map, faction);
            if (!cell.IsValid) return false;

            try
            {
                NemesisPawnUtil.DetachFromLord(captain);
                NemesisPawnUtil.RecoverForReturn(captain);
                if (captain.Spawned)
                    captain.DeSpawn(DestroyMode.Vanish);
                NemesisPawnUtil.EnsureNotWorldPawn(captain);

                TransportShip ship = TransportShipMaker.MakeTransportShip(def, new List<Thing> { captain }, null);
                ship.shipThing.SetFaction(faction);
                LordMaker.MakeNewLord(faction,
                    new LordJob_NemesisHunt(faction, data.huntId, kidnap, tactic, cell, ship),
                    map);
                ship.ArriveAt(cell, map.Parent);
                ship.AddJobs(ShipJobDefOf.Unload, ShipJobDefOf.WaitForever);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[Nemesis] Shuttle drop failed, walking in instead: " + e.Message);
                // Never leave the captain in limbo: back to the world, the walk-in picks them up.
                if (!captain.Destroyed && !captain.Spawned)
                {
                    captain.holdingOwner?.Remove(captain);
                    NemesisPawnUtil.ParkAsWorldNemesis(captain);
                }
                return false;
            }
        }
    }
}
