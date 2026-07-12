using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    public class CompProperties_Exhaust : CompProperties
    {
        // Smoke density added to the room each cycle while the burner runs,
        // before dilution by room size.
        public float emissionPerCycle = 3.5f;

        public CompProperties_Exhaust()
        {
            compClass = typeof(CompExhaust);
        }
    }

    // Attached to combustion generators. Registers with the level's smoke
    // simulation and reports when it's actively burning (producing power).
    public class CompExhaust : ThingComp
    {
        public CompProperties_Exhaust Props => (CompProperties_Exhaust)props;

        public bool Active
        {
            get
            {
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

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<SmokeMapComponent>()?.Emitters.Add(this);
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            map.GetComponent<SmokeMapComponent>()?.Emitters.Remove(this);
            base.PostDeSpawn(map, mode);
        }
    }
}
