using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_GravshipOxygenReclaimer : CompProperties
    {
        public float producePerRareTick = 0.12f;

        public CompProperties_GravshipOxygenReclaimer()
        {
            compClass = typeof(CompGravshipOxygenReclaimer);
        }
    }

    /// <summary>VGE reclaimer analog: powered O₂ production into a linked tank on the same map, else the room.</summary>
    public class CompGravshipOxygenReclaimer : ThingComp
    {
        public CompProperties_GravshipOxygenReclaimer Props => (CompProperties_GravshipOxygenReclaimer)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent?.Map == null)
            {
                return;
            }
            CompPowerTrader power = parent.GetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn)
            {
                return;
            }
            CompFlickable flick = parent.GetComp<CompFlickable>();
            if (flick != null && !flick.SwitchIsOn)
            {
                return;
            }
            if (!StrataMod.Settings.NaturalGasesActive)
            {
                return;
            }

            CompGravshipOxygenTank tank = FindNearbyTank(parent.Map, parent.Position, 12);
            if (tank != null)
            {
                tank.AcceptFromReclaimer(Props.producePerRareTick);
                return;
            }

            Room room = parent.GetRoom();
            AtmosphereMapComponent atmo = parent.Map.GetComponent<AtmosphereMapComponent>();
            StrataGasDef o2 = StrataGasDefOf.Strata_Oxygen;
            if (room != null && atmo != null && o2 != null && !room.UsesOutdoorTemperature)
            {
                atmo.AddGasToRoomPublic(room, o2, Props.producePerRareTick * 0.08f, parent.Position);
            }
        }

        private static CompGravshipOxygenTank FindNearbyTank(Map map, IntVec3 origin, int radius)
        {
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                if (b == null || !b.Spawned)
                {
                    continue;
                }
                CompGravshipOxygenTank tank = b.TryGetComp<CompGravshipOxygenTank>();
                if (tank == null)
                {
                    continue;
                }
                if (b.Position.DistanceTo(origin) <= radius)
                {
                    return tank;
                }
            }
            return null;
        }
    }
}
