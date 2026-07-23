using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // A standalone power conduit that ties two levels' grids through an adjacent
    // shaft. Build ONE beside a stairwell or elevator: it automatically extends
    // a matching junction down to the landing on the level below (the same way
    // stairs spawn their own landing) and drives the tie between the two grids.
    // Wire each level's grid to its junction and power flows both ways.
    public class Building_ShaftConduit : Building
    {
        private const int PartnerCheckInterval = 250;

        private const int ShaftSearchRadius = 6;

        private const float LandingSearchRadius = 4.5f;

        // The end this conduit drives on the level below.
        private Building_ShaftConduit partnerBelow;

        // Set on auto-spawned ends; they are endpoints only and live and die
        // with the conduit above.
        private Building_ShaftConduit parentAbove;

        // Cross-map Thing references often fail on load; IDs are reconciled in
        // ReconcilePartnerLinks after every map has finished spawning.
        private int partnerBelowId = -1;

        private int parentAboveId = -1;

        public bool IsAutoSpawned => parentAbove != null || parentAboveId >= 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref partnerBelow, "strataPartnerBelow");
            Scribe_References.Look(ref parentAbove, "strataParentAbove");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                partnerBelowId = partnerBelow?.thingIDNumber ?? -1;
                parentAboveId = parentAbove?.thingIDNumber ?? -1;
            }
            Scribe_Values.Look(ref partnerBelowId, "strataPartnerBelowId", -1);
            Scribe_Values.Look(ref parentAboveId, "strataParentAboveId", -1);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad)
            {
                ReconcilePartnerLinks();
            }
            else if (!IsAutoSpawned)
            {
                EnsurePartnerBelow();
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Building_ShaftConduit child = partnerBelow;
            partnerBelow = null;
            partnerBelowId = -1;
            base.Destroy(mode);
            // Take the auto-spawned end down with us; an adopted, player-built
            // partner stays.
            if (child != null && !child.Destroyed && child.parentAbove == this)
            {
                child.Destroy(DestroyMode.Vanish);
            }
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            if (IsAutoSpawned && parentAbove is { Spawned: true })
            {
                return "Strata_ShaftConduitDriven".Translate();
            }
            return base.DeconstructibleBy(faction);
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || IsAutoSpawned)
            {
                return;
            }
            if (this.IsHashIntervalTick(PartnerCheckInterval))
            {
                if (!PartnerValid())
                {
                    ReconcilePartnerLinks();
                }
                EnsurePartnerBelow();
            }
            // Power every tick (A4 one-net feel); partner discovery stays periodic.
            CompPowerShaft node = GetComp<CompPowerShaft>();
            CompPowerShaft partner = PartnerValid() ? partnerBelow.GetComp<CompPowerShaft>() : null;
            if (node != null && partner != null)
            {
                node.DriveTie(partner);
            }
        }

        // Rewire top/bottom ends after load. Scribe_References cannot reliably
        // link buildings on different maps.
        internal void ReconcilePartnerLinks()
        {
            if (!Spawned)
            {
                return;
            }

            if (parentAboveId >= 0 && (parentAbove == null || parentAbove.Destroyed))
            {
                parentAbove = FindConduitById(parentAboveId);
            }

            if (parentAbove != null && !parentAbove.Destroyed)
            {
                parentAbove.partnerBelow = this;
                parentAbove.partnerBelowId = thingIDNumber;
                return;
            }

            if (partnerBelowId >= 0 && (partnerBelow == null || partnerBelow.Destroyed))
            {
                partnerBelow = FindConduitById(partnerBelowId);
            }

            if (partnerBelow == null || partnerBelow.Destroyed)
            {
                partnerBelow = FindChildOnBelowMap();
            }

            if (partnerBelow != null && !partnerBelow.Destroyed)
            {
                LinkPartners(this, partnerBelow);
            }
        }

        private static void LinkPartners(Building_ShaftConduit top, Building_ShaftConduit bottom)
        {
            top.partnerBelow = bottom;
            top.partnerBelowId = bottom.thingIDNumber;
            bottom.parentAbove = top;
            bottom.parentAboveId = top.thingIDNumber;
        }

        private Building_ShaftConduit FindChildOnBelowMap()
        {
            MapPortal portal = NearestDownPortal();
            Map below = portal != null ? LevelGraph.OtherMapSafe(portal) : null;
            if (below == null)
            {
                return null;
            }

            int myId = thingIDNumber;
            Building_ShaftConduit best = null;
            float bestDist = LandingSearchRadius * LandingSearchRadius + 0.1f;
            IntVec3 aligned = StrataMapUtility.ProportionalCell(Position, Map, below);

            foreach (Building building in below.listerBuildings.AllBuildingsColonistOfDef(def))
            {
                if (building is not Building_ShaftConduit conduit || building == this)
                {
                    continue;
                }
                if (conduit.parentAboveId == myId || conduit.parentAbove == this)
                {
                    return conduit;
                }
                if (conduit.parentAbove != null && conduit.parentAbove != this)
                {
                    continue;
                }
                float d = aligned.DistanceToSquared(building.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = conduit;
                }
            }

            return best;
        }

        private static Building_ShaftConduit FindConduitById(int thingId)
        {
            if (thingId < 0)
            {
                return null;
            }
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                List<Thing> things = map.listerThings.AllThings;
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i].thingIDNumber == thingId && things[i] is Building_ShaftConduit conduit)
                    {
                        return conduit;
                    }
                }
            }
            return null;
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
                    if (things[i] is Building_ShaftConduit conduit && conduit.Spawned)
                    {
                        conduit.ReconcilePartnerLinks();
                    }
                }
            }
        }

        private bool PartnerValid()
        {
            return partnerBelow != null && !partnerBelow.Destroyed && partnerBelow.Spawned;
        }

        // Finds the shaft next to this conduit and makes sure a partner exists
        // by its landing below: adopt a free-standing player conduit that is
        // already there, or extend our own junction down. Self-healing - a
        // destroyed end gets replaced on the next periodic check.
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

            IntVec3 aligned = StrataMapUtility.ProportionalCell(Position, Map, below);
            float alignRadiusSq = LandingSearchRadius * LandingSearchRadius + 0.1f;

            if (PartnerValid() && partnerBelow.Map == below)
            {
                if (partnerBelow.parentAbove == null)
                {
                    LinkPartners(this, partnerBelow);
                }
                if (partnerBelow.parentAbove == this)
                {
                    // Keep a working junction — don't respawn just because the
                    // cell is occupied by our own partner (FindPartnerCell skips
                    // filled cells and would look "misplaced" every check).
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
                LinkPartners(this, partnerBelow);
            }
        }

        // The nearest shaft on this map that leads downward (a portal that owns
        // a pocket level, i.e. not an up-landing), within reach of this conduit.
        private MapPortal NearestDownPortal()
        {
            MapPortal best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is PocketMapExit || thing is not MapPortal portal)
                {
                    continue;
                }
                if (LevelGraph.OtherMapSafe(portal) == null)
                {
                    continue;
                }
                float d = Position.DistanceToSquared(portal.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = portal;
                }
            }
            return best;
        }

        // A conduit near the aligned cell that no other conduit already drives -
        // covers saves from when both ends were built by hand.
        private Building_ShaftConduit FindAdoptable(Map below, IntVec3 aligned)
        {
            Building_ShaftConduit best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Building building in below.listerBuildings.AllBuildingsColonistOfDef(def))
            {
                if (building is not Building_ShaftConduit conduit
                    || (conduit.parentAbove != null && conduit.parentAbove != this))
                {
                    continue;
                }
                float d = aligned.DistanceToSquared(building.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = conduit;
                }
            }
            return best;
        }

        private Building_ShaftConduit SpawnPartner(Map below, IntVec3 aligned)
        {
            IntVec3 cell = FindPartnerCell(below, aligned);
            if (!cell.IsValid)
            {
                return null;
            }
            cell.GetFirstMineable(below)?.Destroy(DestroyMode.Vanish);
            var child = (Building_ShaftConduit)GenSpawn.Spawn(ThingMaker.MakeThing(def), cell, below);
            child.SetFaction(Faction.OfPlayer);
            Messages.Message("Strata_ShaftConduitExtended".Translate(), child, MessageTypeDefOf.PositiveEvent);
            return child;
        }

        // Prefer the cell directly under the junction above; carve rock if needed.
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
            if (edifice is Building_ShaftConduit conduit && conduit.IsAutoSpawned)
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

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string state = IsAutoSpawned
                ? "Tie: driven by the conduit on the level above"
                : PartnerValid()
                    ? "Tie: linked to the junction on the level below"
                    : NearestDownPortal() == null
                        ? "Tie: no shaft within reach - build within a few tiles of a stairwell or elevator"
                        : "Tie: waiting for the level below to be opened";
            return text.NullOrEmpty() ? state : text + "\n" + state;
        }
    }
}
