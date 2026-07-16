using Verse;

namespace Strata
{
    // Wild cave flora, fauna, and BMT scatter (stalagmites / crystals) after
    // the rock layout and hidden chambers are in place.
    public class GenStep_BiomesCavernFeatures : GenStep
    {
        public override int SeedPart => 482910337;

        public override void Generate(Map map, GenStepParams parms)
        {
            BiomesCavernsUtility.ScatterCavernFeatures(map, parms);
        }
    }
}
