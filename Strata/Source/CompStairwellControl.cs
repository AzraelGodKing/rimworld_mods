using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Strata
{
    public class CompProperties_StairwellControl : CompProperties
    {
        public CompProperties_StairwellControl()
        {
            compClass = typeof(CompStairwellControl);
        }
    }

    // A sealable hatch on a stairwell. When sealed, nobody passes and the
    // temperature exchange stops - a way to wall a gas pocket or an infestation
    // onto one level instead of letting it bleed through the whole base.
    public class CompStairwellControl : ThingComp
    {
        private bool sealedShut;

        public bool Sealed => sealedShut;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref sealedShut, "strataSealed", defaultValue: false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Toggle
            {
                defaultLabel = parent is Building_ElevatorDown or Building_ElevatorUp ? "Seal elevator" : "Seal stairwell",
                defaultDesc = "Seal this passage shut. Nobody can pass, no air or gas moves between the levels — use it to contain tox gas, smoke, or an infestation on one level.",
                icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/Commands/ForbidOff", reportFailure: false),
                isActive = () => sealedShut,
                toggleAction = () => sealedShut = !sealedShut,
            };
        }

        public override string CompInspectStringExtra()
        {
            return sealedShut ? "Sealed" : null;
        }
    }
}
