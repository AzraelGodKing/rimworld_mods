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
        private const int BalanceInterval = 60;

        private const int PartnerCheckInterval = 250;

        private const float CapWatts = 2000f;

        private const int ShaftSearchRadius = 6;

        private const float LandingSearchRadius = 4.5f;

        // The end this conduit drives on the level below.
        private Building_ShaftConduit partnerBelow;

        // Set on auto-spawned ends; they are endpoints only and live and die
        // with the conduit above.
        private Building_ShaftConduit parentAbove;

        private bool IsAutoSpawned => parentAbove != null;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref partnerBelow, "strataPartnerBelow");
            Scribe_References.Look(ref parentAbove, "strataParentAbove");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad && !IsAutoSpawned)
            {
                EnsurePartnerBelow();
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Building_ShaftConduit child = partnerBelow;
            partnerBelow = null;
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
            if (IsAutoSpawned && parentAbove.Spawned)
            {
                return "It is driven by the conduit on the level above.";
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
                EnsurePartnerBelow();
            }
            if (!this.IsHashIntervalTick(BalanceInterval))
            {
                return;
            }
            CompPowerShaft node = GetComp<CompPowerShaft>();
            CompPowerShaft partner = PartnerValid() ? partnerBelow.GetComp<CompPowerShaft>() : null;
            if (node != null && partner != null)
            {
                node.DriveTie(partner, CapWatts);
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
            MapPortal portal = NearestDownPortal();
            if (portal == null)
            {
                return;
            }
            Map below = LevelGraph.OtherMapSafe(portal);
            IntVec3 landing = portal.GetDestinationLocation();
            if (below == null || !landing.IsValid)
            {
                return;
            }
            if (PartnerValid() && partnerBelow.Map == below)
            {
                return;
            }
            partnerBelow = FindAdoptable(below, landing) ?? SpawnPartner(below, landing);
        }

        // The nearest shaft on this map that leads downward (a portal that owns
        // a pocket level, i.e. not an up-landing), within reach of this conduit.
        private MapPortal NearestDownPortal()
        {
            MapPortal best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is PocketMapExit || !(thing is MapPortal portal))
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

        // A conduit near the landing that no other conduit already drives -
        // covers saves from when both ends were built by hand.
        private Building_ShaftConduit FindAdoptable(Map below, IntVec3 landing)
        {
            Building_ShaftConduit best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Building building in below.listerBuildings.AllBuildingsColonistOfDef(def))
            {
                if (!(building is Building_ShaftConduit conduit)
                    || (conduit.parentAbove != null && conduit.parentAbove != this))
                {
                    continue;
                }
                float d = landing.DistanceToSquared(building.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = conduit;
                }
            }
            if (best != null)
            {
                best.parentAbove = this;
            }
            return best;
        }

        private Building_ShaftConduit SpawnPartner(Map below, IntVec3 landing)
        {
            IntVec3 cell = FindPartnerCell(below, landing);
            if (!cell.IsValid)
            {
                return null;
            }
            cell.GetFirstMineable(below)?.Destroy(DestroyMode.Vanish);
            var child = (Building_ShaftConduit)GenSpawn.Spawn(ThingMaker.MakeThing(def), cell, below);
            child.SetFaction(Faction.OfPlayer);
            child.parentAbove = this;
            Messages.Message("Shaft conduit extended a junction to the level below.", child, MessageTypeDefOf.PositiveEvent);
            return child;
        }

        // A clear standable cell by the landing; failing that, the nearest rock
        // cell (carved out when the junction spawns).
        private static IntVec3 FindPartnerCell(Map below, IntVec3 landing)
        {
            IntVec3 carveFallback = IntVec3.Invalid;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(landing, LandingSearchRadius, useCenter: false))
            {
                if (!cell.InBounds(below) || cell.DistanceToEdge(below) < 2)
                {
                    continue;
                }
                Building edifice = cell.GetEdifice(below);
                if (edifice == null)
                {
                    if (cell.Standable(below))
                    {
                        return cell;
                    }
                }
                else if (!carveFallback.IsValid && edifice.def.building?.isNaturalRock == true)
                {
                    carveFallback = cell;
                }
            }
            return carveFallback;
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
