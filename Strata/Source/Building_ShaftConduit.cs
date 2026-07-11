using RimWorld;
using Verse;

namespace Strata
{
    // A standalone power conduit that ties two levels' grids through an adjacent
    // shaft - so a stairs-only base can still run power up and down. Build one
    // beside the stairwell (or elevator) on each level and connect each to its
    // local grid; the upper one drives the tie through the shaft between them.
    public class Building_ShaftConduit : Building
    {
        private const int BalanceInterval = 60;

        private const float CapWatts = 2000f;

        private const int ShaftSearchRadius = 6;

        private static ThingDef conduitDef;

        private static ThingDef ConduitDef =>
            conduitDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("Strata_ShaftConduit");

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || !this.IsHashIntervalTick(BalanceInterval))
            {
                return;
            }
            CompPowerShaft node = GetComp<CompPowerShaft>();
            if (node == null)
            {
                return;
            }

            // Only the conduit sitting by a downward shaft drives the tie.
            MapPortal portal = NearestDownPortal();
            if (portal == null)
            {
                return;
            }
            Map other = LevelGraph.OtherMapSafe(portal);
            IntVec3 landing = portal.GetDestinationLocation();
            if (other == null || !landing.IsValid)
            {
                return;
            }
            CompPowerShaft partner = PartnerNear(other, landing);
            if (partner != null)
            {
                node.DriveTie(partner, CapWatts);
            }
        }

        // The nearest shaft on this map that leads downward (a portal that owns a
        // pocket level, i.e. not an up-landing), within reach of this conduit.
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

        private static CompPowerShaft PartnerNear(Map map, IntVec3 landing)
        {
            if (ConduitDef == null)
            {
                return null;
            }
            CompPowerShaft best = null;
            float bestDist = ShaftSearchRadius * ShaftSearchRadius + 0.1f;
            foreach (Building b in map.listerBuildings.AllBuildingsColonistOfDef(ConduitDef))
            {
                float d = landing.DistanceToSquared(b.Position);
                if (d < bestDist)
                {
                    CompPowerShaft node = b.GetComp<CompPowerShaft>();
                    if (node != null)
                    {
                        bestDist = d;
                        best = node;
                    }
                }
            }
            return best;
        }
    }
}
