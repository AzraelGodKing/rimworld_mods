using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_ShaftFluidJunctionLink : CompProperties
    {
        public CompProperties_ShaftFluidJunctionLink()
        {
            compClass = typeof(CompShaftFluidJunctionLink);
        }
    }

    // Cross-level partner spawning/reconciliation for shaft fluid junctions.
    // Local pipe membership comes from each mod's pipe comp on this building
    // (patched in ShaftFluid_Comps.xml) — same idea as CompPowerShaft on shaft conduits.
    public class CompShaftFluidJunctionLink : ThingComp
    {
        private const int BalanceInterval = 60;

        private const int PartnerCheckInterval = 250;

        private const int ShaftSearchRadius = 6;

        private const float LandingSearchRadius = 4.5f;

        private Building Building => (Building)parent;

        private Building partnerBelow;

        private CompShaftFluidJunctionLink parentAbove;

        private int partnerBelowId = -1;

        private int parentAboveId = -1;

        public bool IsAutoSpawned => parentAbove != null || parentAboveId >= 0;

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                partnerBelowId = partnerBelow?.thingIDNumber ?? -1;
                parentAboveId = parentAbove?.parent?.thingIDNumber ?? -1;
            }
            Scribe_Values.Look(ref partnerBelowId, "strataPartnerBelowId", -1);
            Scribe_Values.Look(ref parentAboveId, "strataParentAboveId", -1);
            partnerBelow = null;
            parentAbove = null;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RebuildPipesDeferred();
            if (respawningAfterLoad)
            {
                ReconcilePartnerLinks();
            }
            else if (!IsAutoSpawned)
            {
                EnsurePartnerBelow();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            Building child = partnerBelow;
            partnerBelow = null;
            partnerBelowId = -1;
            base.PostDeSpawn(map, mode);
            if (child != null && !child.Destroyed && child.TryGetComp<CompShaftFluidJunctionLink>()?.parentAbove == this)
            {
                child.Destroy(mode);
            }
        }

        private void RebuildPipesDeferred()
        {
            if (parent.Spawned)
            {
                PipeNetworkUtil.RebuildFor(parent);
                PipeNetworkUtil.RebuildNeighbors(parent);
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (parent.Spawned)
                    {
                        VefPipeNetworkUtil.ReconnectJunction(parent);
                        PipeNetworkUtil.RebuildNeighbors(parent);
                    }
                });
            }
        }

        public override void CompTick()
        {
            if (!parent.Spawned || IsAutoSpawned)
            {
                return;
            }
            if (parent.IsHashIntervalTick(PartnerCheckInterval))
            {
                if (!PartnerValid())
                {
                    ReconcilePartnerLinks();
                }
                EnsurePartnerBelow();
            }
            if (!parent.IsHashIntervalTick(BalanceInterval))
            {
                return;
            }
            CompShaftFluidTie node = parent.TryGetComp<CompShaftFluidTie>();
            CompShaftFluidTie partner = PartnerValid()
                ? partnerBelow.TryGetComp<CompShaftFluidTie>()
                : null;
            if (node != null && partner != null)
            {
                node.DriveTie(partner);
            }
        }

        public string LinkInspectString()
        {
            if (IsAutoSpawned)
            {
                return "Tie: driven by the junction on the level above";
            }
            if (PartnerValid())
            {
                return "Tie: linked to the junction on the level below";
            }
            if (NearestDownPortal() == null)
            {
                return "Tie: no shaft within reach - build within a few tiles of a stairwell or elevator";
            }
            return "Tie: waiting for the level below to be opened";
        }

        internal void ReconcilePartnerLinks()
        {
            if (!parent.Spawned)
            {
                return;
            }

            if (parentAboveId >= 0 && (parentAbove == null || parentAbove.parent.Destroyed))
            {
                parentAbove = FindLinkCompById(parentAboveId);
            }

            if (parentAbove != null && !parentAbove.parent.Destroyed)
            {
                parentAbove.partnerBelow = Building;
                parentAbove.partnerBelowId = parent.thingIDNumber;
                return;
            }

            if (partnerBelowId >= 0 && (partnerBelow == null || partnerBelow.Destroyed))
            {
                partnerBelow = FindBuildingById(partnerBelowId);
            }

            if (partnerBelow == null || partnerBelow.Destroyed)
            {
                partnerBelow = FindChildOnBelowMap();
            }

            if (partnerBelow != null && !partnerBelow.Destroyed)
            {
                LinkPartners(this, partnerBelow.TryGetComp<CompShaftFluidJunctionLink>());
            }
        }

        internal static void ReconcileAllAfterLoad()
        {
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                List<Thing> things = map.listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].TryGetComp<CompShaftFluidJunctionLink>() is CompShaftFluidJunctionLink link && things[i].Spawned)
                    {
                        link.ReconcilePartnerLinks();
                        PipeNetworkUtil.RebuildFor(things[i]);
                    }
                }
            }
        }

        private static void LinkPartners(CompShaftFluidJunctionLink top, CompShaftFluidJunctionLink bottom)
        {
            if (top == null || bottom == null)
            {
                return;
            }
            top.partnerBelow = (Building)bottom.parent;
            top.partnerBelowId = bottom.parent.thingIDNumber;
            bottom.parentAbove = top;
            bottom.parentAboveId = top.parent.thingIDNumber;
        }

        private Building FindChildOnBelowMap()
        {
            MapPortal portal = NearestDownPortal();
            Map below = portal != null ? LevelGraph.OtherMapSafe(portal) : null;
            if (below == null)
            {
                return null;
            }

            int myId = parent.thingIDNumber;
            Building best = null;
            float bestDist = LandingSearchRadius * LandingSearchRadius + 0.1f;
            IntVec3 aligned = StrataMapUtility.ProportionalCell(parent.Position, parent.Map, below);

            foreach (Building building in below.listerBuildings.AllBuildingsColonistOfDef(parent.def))
            {
                if (building == parent || building.TryGetComp<CompShaftFluidJunctionLink>() == null)
                {
                    continue;
                }
                CompShaftFluidJunctionLink link = building.TryGetComp<CompShaftFluidJunctionLink>();
                if (link.parentAboveId == myId || link.parentAbove == this)
                {
                    return building;
                }
                if (link.parentAbove != null && link.parentAbove != this)
                {
                    continue;
                }
                float d = aligned.DistanceToSquared(building.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = building;
                }
            }

            return best;
        }

        private static Building FindBuildingById(int thingId)
        {
            if (thingId < 0)
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Thing> things = maps[m].listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].thingIDNumber == thingId && things[i] is Building building)
                    {
                        return building;
                    }
                }
            }
            return null;
        }

        private static CompShaftFluidJunctionLink FindLinkCompById(int thingId)
        {
            Building building = FindBuildingById(thingId);
            return building?.TryGetComp<CompShaftFluidJunctionLink>();
        }

        private bool PartnerValid()
        {
            return partnerBelow != null && !partnerBelow.Destroyed && partnerBelow.Spawned;
        }

        private void EnsurePartnerBelow()
        {
            if (IsAutoSpawned)
            {
                return;
            }
            MapPortal portal = NearestDownPortal();
            if (portal == null)
            {
                return;
            }
            Map below = LevelGraph.OtherMapSafe(portal);
            if (below == null)
            {
                return;
            }

            IntVec3 aligned = StrataMapUtility.ProportionalCell(parent.Position, parent.Map, below);
            float alignRadiusSq = LandingSearchRadius * LandingSearchRadius + 0.1f;

            if (PartnerValid() && partnerBelow.Map == below)
            {
                CompShaftFluidJunctionLink childLink = partnerBelow.TryGetComp<CompShaftFluidJunctionLink>();
                if (childLink != null && childLink.parentAbove == null)
                {
                    LinkPartners(this, childLink);
                }
                if (childLink != null && childLink.parentAbove == this)
                {
                    if (aligned.DistanceToSquared(partnerBelow.Position) <= alignRadiusSq)
                    {
                        return;
                    }
                    partnerBelow.Destroy(DestroyMode.Vanish);
                    partnerBelow = null;
                    partnerBelowId = -1;
                }
                else
                {
                    partnerBelow = null;
                    partnerBelowId = -1;
                }
            }

            partnerBelow = FindAdoptable(below, aligned) ?? SpawnPartner(below, aligned);
            if (partnerBelow != null)
            {
                LinkPartners(this, partnerBelow.TryGetComp<CompShaftFluidJunctionLink>());
            }
        }

        private MapPortal NearestDownPortal()
        {
            MapPortal best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Thing thing in parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is PocketMapExit || thing is not MapPortal portal)
                {
                    continue;
                }
                if (LevelGraph.OtherMapSafe(portal) == null)
                {
                    continue;
                }
                float d = parent.Position.DistanceToSquared(portal.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = portal;
                }
            }
            return best;
        }

        private Building FindAdoptable(Map below, IntVec3 aligned)
        {
            Building best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Building building in below.listerBuildings.AllBuildingsColonistOfDef(parent.def))
            {
                CompShaftFluidJunctionLink link = building.TryGetComp<CompShaftFluidJunctionLink>();
                if (link == null || (link.parentAbove != null && link.parentAbove != this))
                {
                    continue;
                }
                float d = aligned.DistanceToSquared(building.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = building;
                }
            }
            return best;
        }

        private Building SpawnPartner(Map below, IntVec3 aligned)
        {
            IntVec3 cell = FindPartnerCell(below, aligned);
            if (!cell.IsValid)
            {
                return null;
            }
            cell.GetFirstMineable(below)?.Destroy(DestroyMode.Vanish);
            var child = (Building)GenSpawn.Spawn(ThingMaker.MakeThing(parent.def), cell, below);
            child.SetFaction(Faction.OfPlayer);
            PipeNetworkUtil.RebuildFor(child);
            PipeNetworkUtil.RebuildNeighbors(child);
            Messages.Message("Shaft fluid junction extended a junction to the level below.", child, MessageTypeDefOf.PositiveEvent);
            return child;
        }

        private static IntVec3 FindPartnerCell(Map below, IntVec3 aligned)
        {
            if (TryPartnerCell(below, aligned, carveRock: true, out IntVec3 exact))
            {
                return exact;
            }
            IntVec3 carveFallback = IntVec3.Invalid;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(aligned, LandingSearchRadius, useCenter: false))
            {
                if (!cell.InBounds(below) || cell.DistanceToEdge(below) < 2)
                {
                    continue;
                }
                if (TryPartnerCell(below, cell, carveRock: false, out IntVec3 open))
                {
                    return open;
                }
                Building edifice = cell.GetEdifice(below);
                if (!carveFallback.IsValid && edifice?.def.building?.isNaturalRock == true)
                {
                    carveFallback = cell;
                }
            }
            if (carveFallback.IsValid)
            {
                carveFallback.GetFirstMineable(below)?.Destroy(DestroyMode.Vanish);
                return carveFallback;
            }
            return IntVec3.Invalid;
        }

        private static bool TryPartnerCell(Map below, IntVec3 cell, bool carveRock, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (!cell.InBounds(below) || cell.DistanceToEdge(below) < 2)
            {
                return false;
            }
            Building edifice = cell.GetEdifice(below);
            if (edifice != null && edifice.TryGetComp<CompShaftFluidJunctionLink>()?.IsAutoSpawned == true)
            {
                return false;
            }
            if (edifice == null)
            {
                if (cell.Standable(below))
                {
                    result = cell;
                    return true;
                }
                return false;
            }
            if (carveRock && edifice.def.building?.isNaturalRock == true)
            {
                edifice.Destroy(DestroyMode.Vanish);
                if (cell.Standable(below))
                {
                    result = cell;
                    return true;
                }
            }
            return false;
        }
    }
}
