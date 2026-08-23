using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    /// <summary>
    /// Keep Strata colony dig/tower shafts off temporary quest maps (Ancient
    /// Urban Ruins, etc.). Digging there orphans pocket levels when the site
    /// is abandoned. Gravship PlaceWorkers are separate and unchanged.
    /// </summary>
    public static class StrataQuestMapGuard
    {
        public static bool IsUnsafeForColonyShafts(Map map, out string reasonKey)
        {
            reasonKey = null;
            if (map == null)
            {
                reasonKey = "Strata_Place_QuestMapNoShaft";
                return true;
            }

            // Strata's own quest-site pockets (sunken ruin, collapsed mine, …).
            if (IsStrataQuestSiteLevel(map))
            {
                reasonKey = "Strata_Place_QuestMapNoShaft";
                return true;
            }

            // Underground / upper levels dug from the home colony are safe for
            // deeper shafts even though pocket maps have IsPlayerHome == false.
            if (StrataMapUtility.IsUnderground(map) || StrataMapUtility.IsUpperLevel(map))
            {
                return false;
            }

            // World sites / non-home maps (AUR floors, caravan camps, etc.).
            if (!map.IsPlayerHome || map.Parent is Site)
            {
                reasonKey = "Strata_Place_QuestMapNoShaft";
                return true;
            }

            return false;
        }

        public static bool IsStrataQuestSiteLevel(Map map)
        {
            string gen = map?.generatorDef?.defName;
            if (gen == null)
            {
                return false;
            }
            return gen.Contains("SunkenRuin")
                || gen.Contains("CollapsedMine")
                || gen.Contains("SealedVault")
                || gen.Contains("GeothermalVent");
        }
    }
}
