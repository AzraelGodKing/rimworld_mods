using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_ExhaustVent : CompProperties
    {
        // Fraction of intake-room smoke cleared per cycle while active.
        public float ventPower = 0.25f;

        // When true, requires CompPowerTrader to be on (exhaust fan). Louvers leave this false.
        public bool requiresPower;

        public CompProperties_ExhaustVent()
        {
            compClass = typeof(CompExhaustVent);
        }
    }

    // One-way wall vent: pulls smoke from the room behind the building and pushes
    // it out the facing side (another room, outdoors, or a duct run).
    public class CompExhaustVent : ThingComp
    {
        public CompProperties_ExhaustVent Props => (CompProperties_ExhaustVent)props;

        public bool Active
        {
            get
            {
                if (!Props.requiresPower)
                {
                    return true;
                }
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public Room IntakeRoom => SmokeVentUtility.IntakeRoom(parent);

        public Room ExhaustRoom => SmokeVentUtility.ExhaustRoom(parent);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<AtmosphereMapComponent>()?.Vents.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<AtmosphereMapComponent>()?.Vents.Remove(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            Room intake = IntakeRoom;
            if (intake == null)
            {
                return "Strata_ExhaustVentIntake".Translate();
            }
            if (SmokeVentUtility.ExhaustOpensIntoDuct(parent, out HashSet<IntVec3> network)
                && !SmokeVentUtility.DuctNetworkReachesOutdoor(parent.Map, network))
            {
                return "Strata_ExhaustVentNoOutdoors".Translate();
            }
            return null;
        }
    }
}
