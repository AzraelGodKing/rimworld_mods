using Verse;

namespace Strata
{
    // Wild cave flora / fauna: Biomes! scatter when that path won, otherwise
    // Strata's native deferred cavern dressing.
    public class GenStep_BiomesCavernFeatures : GenStep
    {
        public override int SeedPart => 482910337;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (BiomesCavernsUtility.ShouldScatterCavernFeatures(map))
            {
                StrataDeferredGenUtility.ScheduleBiomesCavernFeatures(map);
            }
            else if (StrataCavernUtility.ShouldGenerateNativeCavernLayout(map)
                || (MapGenerator.TryGetVar(StrataNativeCavernUtility.ForceNativeWarrenVar, out bool force) && force))
            {
                StrataDeferredGenUtility.ScheduleNativeCavernFeatures(map);
            }
        }
    }
}
