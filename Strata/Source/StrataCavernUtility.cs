using Verse;

namespace Strata
{
    // Native Strata cave layout for excavated colony levels when Biomes! is
    // absent or its compat toggle is off.
    public static class StrataCavernUtility
    {
        public static bool ShouldGenerateNativeCavernLayout(Map map)
        {
            if (map == null || StrataMod.Settings?.nativeCavernLayoutEnabled == false)
            {
                return false;
            }
            if (BiomesCavernsUtility.ShouldGenerateCavernLayout(map))
            {
                return false;
            }
            return StrataDepth.CountLevelsBelowSurface(map) >= 1;
        }
    }
}
