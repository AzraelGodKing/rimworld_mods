using Verse;

namespace Strata
{
    // Native Strata cave layout for excavated colony levels when Biomes! Caverns
    // is not loaded. If Biomes! is present, it always wins layout generation;
    // native only runs as a failure fallback (ForceNativeWarren).
    public static class StrataCavernUtility
    {
        public static bool ShouldGenerateNativeCavernLayout(Map map)
        {
            if (map == null || StrataMod.Settings?.nativeCavernLayoutEnabled == false)
            {
                return false;
            }
            // Explicit fallback after a failed Biomes! layout attempt.
            if (MapGenerator.TryGetVar(StrataNativeCavernUtility.ForceNativeWarrenVar, out bool force)
                && force)
            {
                return true;
            }
            // Biomes! Caverns stack loaded → never prefer Strata caves.
            if (BiomesCavernsUtility.IsActive)
            {
                return false;
            }
            return StrataDepth.CountLevelsBelowSurface(map) >= 1;
        }
    }
}
