using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // A powered freight hoist. Placed next to a stairwell, it moves loose items
    // between this level and the linked one on its own - no colonist required.
    // Toggle sets whether it pulls goods up to this level or sends them down.
    public class Building_CargoLift : Building
    {
        private const int PulseInterval = 120;

        private const int MaxStacksPerPulse = 2;

        private const float PickupRadius = 5.9f;

        private bool importing = true; // true: pull from the linked level to here

        private CompPowerTrader power;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            power = GetComp<CompPowerTrader>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref importing, "strataLiftImporting", defaultValue: true);
        }

        protected override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(PulseInterval) && Spawned && (power == null || power.PowerOn))
            {
                Pulse();
            }
        }

        private void Pulse()
        {
            MapPortal portal = NearestPortal();
            if (portal == null)
            {
                return;
            }
            Map other = LevelGraph.OtherMapSafe(portal);
            if (other == null)
            {
                return;
            }
            IntVec3 farLanding = portal.GetDestinationLocation();
            if (!farLanding.IsValid)
            {
                return;
            }

            Map sourceMap = importing ? other : Map;
            IntVec3 sourceCell = importing ? farLanding : Position;
            Map destMap = importing ? Map : other;
            IntVec3 destCell = importing ? Position : farLanding;

            int moved = 0;
            foreach (Thing item in ItemsNear(sourceMap, sourceCell))
            {
                if (moved >= MaxStacksPerPulse)
                {
                    break;
                }
                item.DeSpawn();
                if (!GenPlace.TryPlaceThing(item, destCell, destMap, ThingPlaceMode.Near))
                {
                    // No room at the destination - put it back where it was.
                    GenPlace.TryPlaceThing(item, sourceCell, sourceMap, ThingPlaceMode.Near);
                    break;
                }
                moved++;
            }
        }

        private List<Thing> ItemsNear(Map map, IntVec3 center)
        {
            var found = new List<Thing>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, PickupRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing t = things[i];
                    if (t.def.EverHaulable && !t.IsForbidden(Faction.OfPlayer))
                    {
                        found.Add(t);
                    }
                }
            }
            return found;
        }

        private MapPortal NearestPortal()
        {
            MapPortal best = null;
            float bestDist = float.MaxValue;
            foreach (Thing thing in Map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing is MapPortal portal && LevelGraph.OtherMapSafe(portal) != null)
                {
                    float d = Position.DistanceToSquared(portal.Position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = portal;
                    }
                }
            }
            return best;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            yield return new Command_Toggle
            {
                defaultLabel = importing ? "Hoisting: up" : "Hoisting: down",
                defaultDesc = "Toggle whether the lift pulls goods up to this level from the linked level, or sends them down to it.",
                isActive = () => importing,
                toggleAction = () => importing = !importing,
            };
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            string dir = importing ? "Hoisting goods up to this level" : "Sending goods down to the linked level";
            if (NearestPortal() == null)
            {
                dir = "No stairwell nearby - build beside a stairwell.";
            }
            return text.NullOrEmpty() ? dir : text + "\n" + dir;
        }
    }
}
