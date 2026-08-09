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
            if (map == null)
            {
                return false;
            }
            // Explicit fallback after a failed / hollow Biomes! layout — even when
            // the native-cavern setting is off (otherwise digs stay shell-only).
            if (MapGenerator.TryGetVar(StrataNativeCavernUtility.ForceNativeWarrenVar, out bool force)
                && force)
            {
                return true;
            }
            if (StrataMod.Settings?.nativeCavernLayoutEnabled == false)
            {
                return false;
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
