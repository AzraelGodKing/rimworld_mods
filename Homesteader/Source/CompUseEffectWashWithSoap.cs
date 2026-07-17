using RimWorld;
using Verse;

namespace Homesteader
{
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
            if (usedBy?.needs?.mood == null)
            {
                return;
            }

            ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Homesteader_FreshlyWashed");
            if (thought != null)
            {
                usedBy.needs.mood.thoughts.memories.TryGainMemory(thought);
            }
        }
    }
}
