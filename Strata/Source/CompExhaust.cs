using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_Exhaust : CompProperties
    {
        // Room concentration added each atmosphere cycle while active.
        // Combustion sources (generators, campfires) divide this by room
        // size; life-support pumps (emitWhenPowered) apply the full value.
        public float emissionPerCycle = 3.5f;

        // Which atmosphere channel this source emits. Null = combustion smoke.
        public StrataGasDef gas;

        // Life-support pumps: emit while CompPowerTrader is on (not while
        // generating power like a fueled generator).
        public bool emitWhenPowered;

        public CompProperties_Exhaust()
        {
            compClass = typeof(CompExhaust);
        }
    }

    // Attached to combustion generators (and, via subclasses, other gas
    // sources like deep vents). Registers with the level's atmosphere
    // simulation and reports when it's actively emitting.
    public class CompExhaust : ThingComp
    {
        public CompProperties_Exhaust Props => (CompProperties_Exhaust)props;

        public StrataGasDef GasDef => Props.gas ?? StrataGasDefOf.Strata_Smoke;

        public virtual bool Active
        {
            get
            {
                if (Props.emitWhenPowered)
                {
                    CompPowerTrader powered = parent.GetComp<CompPowerTrader>();
                    return powered != null && powered.PowerOn;
                }
                // Generators: emit while actually producing power.
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                if (power != null)
                {
                    return power.PowerOn && power.PowerOutput > 0f;
                }
                // Campfires, torches, and other fuelled flames: emit while lit.
                CompRefuelable refuelable = parent.GetComp<CompRefuelable>();
                if (refuelable != null)
                {
                    if (!refuelable.HasFuel)
                    {
                        return false;
                    }
                    // Benches that only burn fuel while worked (fueled stove,
                    // smithy, smelter) only smoke while a pawn is at them.
                    if (refuelable.Props.consumeFuelOnlyWhenUsed)
                    {
                        return BeingWorked();
                    }
                    return true;
                }
                // Open flame with no power or fuel comp (a raw Fire): always smoking.
                return true;
            }
        }

        private bool BeingWorked()
        {
            if (!parent.def.hasInteractionCell)
            {
                return true;
            }
            Pawn pawn = parent.InteractionCell.GetFirstPawn(parent.Map);
            return pawn != null && pawn.CurJob != null
                && pawn.CurJob.GetTarget(TargetIndex.A).Thing == parent;
        }

        internal void RegisterWithAtmosphere(AtmosphereMapComponent atmosphere)
        {
            atmosphere?.Emitters.Add(this);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RegisterWithAtmosphere(parent.Map.GetComponent<AtmosphereMapComponent>());
        }

        public override string CompInspectStringExtra()
        {
            if (!Props.emitWhenPowered)
            {
                return null;
            }
            if (!Active)
            {
                return "Needs power to enrich the room with " + GasDef.label + ".";
            }
            Room room = parent.GetRoom();
            if (room == null || room.UsesOutdoorTemperature)
            {
                return "Releasing " + GasDef.label + " (open air — no enrichment needed).";
            }
            AtmosphereMapComponent atmosphere = parent.Map.GetComponent<AtmosphereMapComponent>();
            float density = atmosphere?.DensityAtCell(parent.Position, GasDef) ?? 0f;
            if (GasDef == StrataGasDefOf.Strata_Oxygen)
            {
                return density >= AtmosphereMapComponent.AmbientOxygen - 0.01f
                    ? "Room air is breathable."
                    : "Enriching room with " + GasDef.label + " (" + density.ToStringPercent() + ").";
            }
            return "Releasing " + GasDef.label + " into the room.";
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<AtmosphereMapComponent>()?.Emitters.Remove(this);
            base.PostDeSpawn(map, mode);
        }
    }
}
