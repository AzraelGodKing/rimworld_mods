using RimWorld;
using Verse;

namespace Strata
{
    public class CompProperties_GasScrubber : CompProperties
    {
        // Room concentration removed each atmosphere cycle while active.
        public float scrubPerCycle = 0.008f;

        public StrataGasDef gas;

        public bool requiresPower = true;

        // Optional O₂ top-up while scrubbing (black-damp scrubbers restore air).
        public float restoreOxygenPerCycle;

        public CompProperties_GasScrubber()
        {
            compClass = typeof(CompGasScrubber);
        }
    }

    // Powered life-support scrubber — pulls a chosen gas out of its room
    // (carbon dioxide by default) and traps it in filters.
    public class CompGasScrubber : ThingComp
    {
        public CompProperties_GasScrubber Props => (CompProperties_GasScrubber)props;

        public StrataGasDef GasDef => Props.gas ?? StrataGasDefOf.Strata_CarbonDioxide;

        public bool Active
        {
            get
            {
                CompRefuelable refuel = parent.GetComp<CompRefuelable>();
                if (refuel != null && !refuel.HasFuel)
                {
                    return false;
                }
                if (!Props.requiresPower)
                {
                    return true;
                }
                CompPowerTrader power = parent.GetComp<CompPowerTrader>();
                return power != null && power.PowerOn;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<AtmosphereMapComponent>()?.Scrubbers.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<AtmosphereMapComponent>()?.Scrubbers.Remove(this);
            base.PostDeSpawn(map, mode);
        }

        public override string CompInspectStringExtra()
        {
            if (!Active)
            {
                return "Strata_GasScrubberNeedsPower".Translate(GasDef.label);
            }
            Room room = parent.GetRoom();
            if (room == null || room.UsesOutdoorTemperature)
            {
                return null;
            }
            AtmosphereMapComponent atmosphere = parent.Map.GetComponent<AtmosphereMapComponent>();
            float density = atmosphere?.DensityInRoom(room, GasDef) ?? 0f;
            return density > 0.01f
                ? "Strata_GasScrubberScrubbing".Translate(GasDef.label)
                : "Strata_GasScrubberNone".Translate(GasDef.label);
        }
    }
}
