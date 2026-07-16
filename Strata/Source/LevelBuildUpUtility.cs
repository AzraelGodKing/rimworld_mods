using RimWorld;
using Verse;

namespace Strata
{
    // Surface → A1 (and later A2+) needs building-up research. Extra tower
    // stairwells that join an already-open upper level skip the tier check.
    public static class LevelBuildUpUtility
    {
        public const string ResearchDefName = "Strata_BuildingUp";

        public static bool IsBuildingUpResearchFinished()
        {
            ResearchProjectDef research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ResearchDefName);
            return research == null || research.IsFinished;
        }

        public static bool MapHasOpenLevelAbove(Map map, Thing ignore = null)
        {
            if (map == null)
            {
                return false;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing == ignore || thing is not Building_StairsBuildUp up
                    || thing is IStrataGravshipPortal
                    || !up.Spawned || !up.PocketMapExists)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        public static bool CanOpenNewLevelAbove(Map map, out string reason, Thing ignore = null)
        {
            reason = null;
            if (map == null)
            {
                reason = "Invalid map.";
                return false;
            }
            if (StrataMapUtility.IsUnderground(map))
            {
                reason = "Build the tower stairwell on the surface (or an upper floor), not underground.";
                return false;
            }
            if (MapHasOpenLevelAbove(map, ignore))
            {
                return true;
            }
            if (!IsBuildingUpResearchFinished())
            {
                reason = "Requires building up research to open the first upper level.";
                return false;
            }
            return true;
        }
    }
}
