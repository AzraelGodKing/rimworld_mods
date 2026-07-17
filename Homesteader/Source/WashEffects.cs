using RimWorld;
using Verse;

namespace Homesteader
{
    public static class WashUtility
    {
        public static void ApplyFreshlyWashed(Pawn pawn)
        {
            if (pawn?.needs?.mood == null)
            {
                return;
            }

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_FreshlyWashed");
            if (thought != null)
            {
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }
    }

    public class CompProperties_UseEffectWashWithSoap : CompProperties_UseEffect
    {
        public CompProperties_UseEffectWashWithSoap()
        {
            compClass = typeof(CompUseEffect_WashWithSoap);
        }
    }

    public class CompUseEffect_WashWithSoap : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            WashUtility.ApplyFreshlyWashed(usedBy);
        }
    }

    public class CompProperties_UseEffectWashAtTub : CompProperties_UseEffect
    {
        public float soapFuelPerWash = 1f;

        public CompProperties_UseEffectWashAtTub()
        {
            compClass = typeof(CompUseEffect_WashAtTub);
        }
    }

    public class CompUseEffect_WashAtTub : CompUseEffect
    {
        public CompProperties_UseEffectWashAtTub Props => (CompProperties_UseEffectWashAtTub)props;

        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            CompRefuelable fuel = parent.TryGetComp<CompRefuelable>();
            if (fuel == null || fuel.Fuel < Props.soapFuelPerWash)
            {
                return "Needs soap in the wash tub.";
            }

            return true;
        }

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            parent.TryGetComp<CompRefuelable>()?.ConsumeFuel(Props.soapFuelPerWash);
            WashUtility.ApplyFreshlyWashed(usedBy);
        }
    }
}
