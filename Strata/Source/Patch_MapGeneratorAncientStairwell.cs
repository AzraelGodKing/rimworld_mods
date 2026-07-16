using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Hook the ancient-stairwell scatter step into colony surface map generators.
    [StaticConstructorOnStartup]
    public static class Patch_MapGeneratorAncientStairwell
    {
        private static readonly HashSet<string> ColonySurfaceGenerators = new HashSet<string>
        {
            "Base_Player",
            "Standard",
            "Base_Full",
        };

        static Patch_MapGeneratorAncientStairwell()
        {
            GenStepDef step = DefDatabase<GenStepDef>.GetNamedSilentFail("Strata_AncientColonyStairwell");
            if (step == null)
            {
                return;
            }
            foreach (MapGeneratorDef gen in DefDatabase<MapGeneratorDef>.AllDefsListForReading)
            {
                if (!ColonySurfaceGenerators.Contains(gen.defName) || gen.isUnderground)
                {
                    continue;
                }
                if (!gen.genSteps.Contains(step))
                {
                    gen.genSteps.Add(step);
                }
            }
        }
    }
}
