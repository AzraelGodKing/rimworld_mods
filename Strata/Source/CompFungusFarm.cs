using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_FungusFarm : CompProperties
    {
        public int produceIntervalTicks = 2500;

        public int minStack = 2;

        public int maxStack = 5;

        public float co2PerCycle = 0.001f;

        public float o2PerCycle = 0.0015f;

        public float sporesPerCycle = 0.0008f;

        public CompProperties_FungusFarm()
        {
            compClass = typeof(CompFungusFarm);
        }
    }

    // Dark underground grow bed — produces cave fungus and trades a little CO₂ for O₂.
    public class CompFungusFarm : ThingComp
    {
        public CompProperties_FungusFarm Props => (CompProperties_FungusFarm)props;

        public override void CompTick()
        {
            base.CompTick();
            // Slow spore seep while the bed is actively growing.
            if (parent.IsHashIntervalTick(250) && CanGrow()
                && StrataGasDefOf.Strata_Spores != null && Props.sporesPerCycle > 0f)
            {
                Room sporeRoom = parent.GetRoom();
                if (sporeRoom != null && !sporeRoom.UsesOutdoorTemperature)
                {
                    parent.Map.GetComponent<AtmosphereMapComponent>()
                        ?.AddGasToRoomPublic(sporeRoom, StrataGasDefOf.Strata_Spores, Props.sporesPerCycle,
                            parent.Position);
                }
            }
            if (!parent.IsHashIntervalTick(Props.produceIntervalTicks))
            {
                return;
            }
            if (!CanGrow())
            {
                return;
            }
            Room room = parent.GetRoom();
            if (room != null && !room.UsesOutdoorTemperature)
            {
                AtmosphereMapComponent atmosphere = parent.Map.GetComponent<AtmosphereMapComponent>();
                if (atmosphere != null && StrataGasDefOf.Strata_CarbonDioxide != null)
                {
                    atmosphere.ConsumeGasFromRoomPublic(room, StrataGasDefOf.Strata_CarbonDioxide, Props.co2PerCycle);
                    atmosphere.AddGasToRoomPublic(room, StrataGasDefOf.Strata_Oxygen, Props.o2PerCycle, parent.Position);
                }
            }
            ThingDef fungus = DefDatabase<ThingDef>.GetNamedSilentFail("Strata_Fungus");
            if (fungus == null)
            {
                return;
            }
            int count = Rand.RangeInclusive(Props.minStack, Props.maxStack);
            Thing made = ThingMaker.MakeThing(fungus);
            made.stackCount = count;
            GenPlace.TryPlaceThing(made, parent.Position, parent.Map, ThingPlaceMode.Near);
        }

        private bool CanGrow()
        {
            Map map = parent.Map;
            if (StrataMapUtility.IsUnderground(map))
            {
                return true;
            }
            Room room = parent.GetRoom();
            if (room == null || room.UsesOutdoorTemperature)
            {
                return false;
            }
            foreach (IntVec3 cell in parent.OccupiedRect())
            {
                if (map.glowGrid.GroundGlowAt(cell) > 0.05f)
                {
                    return false;
                }
            }
            return true;
        }

        public override string CompInspectStringExtra()
        {
            return CanGrow()
                ? "Growing cave fungus."
                : "Needs darkness or an underground level.";
        }
    }
}
