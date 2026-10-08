using Verse;

namespace Stormproof
{
    /// <summary>
    /// Stable soft-compat surface for sibling mods. Fail-open: every member is
    /// safe when Stormproof is loaded alone; callers should still probe the
    /// assembly rather than hard-depend.
    /// <list type="bullet">
    /// <item>Strata — ion-immune underground grid / surge awareness</item>
    /// <item>Homesteader — drought inspect on wells / cisterns</item>
    /// <item>Nemesis — high-aggression ion-storm baiting</item>
    /// </list>
    /// </summary>
    public static class StormproofCompatApi
    {
        public static bool IsActive => true;

        /// <summary>True while a Stormproof ion storm is active on the map.</summary>
        public static bool IsIonStormActive(Map map)
        {
            if (map == null)
            {
                return false;
            }
            return HazardProtection.ConditionActive(map, StormproofDefOf.Stormproof_IonStorm);
        }

        /// <summary>
        /// Ion storm or dry-lightning surge — Strata can treat the surface grid
        /// as hostile / flood unpumped levels.
        /// </summary>
        public static bool IsGridSurgeActive(Map map)
        {
            if (map == null)
            {
                return false;
            }
            return HazardProtection.AnyConditionActive(
                map,
                StormproofDefOf.Stormproof_IonStorm,
                StormproofDefOf.Stormproof_DryLightning);
        }

        /// <summary>Odyssey drought (or initial drought) on this map.</summary>
        public static bool IsDroughtActive(Map map)
        {
            if (map == null)
            {
                return false;
            }
            return HazardProtection.AnyConditionActive(
                map,
                StormproofDefOf.Drought,
                StormproofDefOf.DroughtInitial);
        }

        /// <summary>
        /// Nemesis high-aggression baiting: ion storm is currently active.
        /// Same as <see cref="IsIonStormActive"/>; named for the ROADMAP hook.
        /// </summary>
        public static bool IsIonStormActiveForBaiting(Map map) => IsIonStormActive(map);
    }
}
