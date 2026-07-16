using RimWorld;
using Verse;

namespace Strata
{
    // Surface → B1 needs digging-down research; B2 and deeper need deep excavation.
    // Extra stairwells that join an already-open level below skip the tier check.
    public static class LevelExcavationUtility
    {
        public const string FirstLevelResearchDefName = "Strata_DiggingDown";
        public const string DeepLevelResearchDefName = "Strata_Excavation";

        public static bool IsDiggingDownResearchFinished()
        {
            ResearchProjectDef research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(FirstLevelResearchDefName);
            return research == null || research.IsFinished;
        }

        public static bool IsDeepExcavationResearchFinished()
        {
            ResearchProjectDef research = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(DeepLevelResearchDefName);
            return research == null || research.IsFinished;
        }

        public static bool MapHasOpenLevelBelow(Map map, Thing ignore = null)
        {
            if (map == null)
            {
                return false;
            }
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (thing == ignore || thing is not Building_StairsDown down
                    || thing is Building_StairsBuildUp
                    || thing is Building_AncientColonyStairsDown
                    || !down.Spawned || !down.PocketMapExists)
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        public static bool CanOpenNewLevelBelow(Map map, out string reason, Thing ignore = null)
        {
            reason = null;
            if (map == null)
            {
                reason = "Invalid map.";
                return false;
            }
            if (StrataMapUtility.IsUpperLevel(map))
            {
                reason = "Dig down from the surface or an underground level — not from an upper floor.";
                return false;
            }
            if (MapHasOpenLevelBelow(map, ignore))
            {
                return true;
            }
            if (StrataDepth.Of(map) == 0)
            {
                if (!IsDiggingDownResearchFinished())
                {
                    reason = "Requires digging down research to open the first underground level.";
                    return false;
                }
                return true;
            }
            if (!IsDeepExcavationResearchFinished())
            {
                reason = "Requires deep excavation research to dig below the first underground level.";
                return false;
            }
            return true;
        }

        public const float SurfaceWorkToBuild = 12000f;
        public const float DeepWorkOffset = 8000f;

        public static float WorkToBuildForMap(Map map)
        {
            float work = SurfaceWorkToBuild;
            if (map != null && StrataDepth.Of(map) >= 1)
            {
                work += DeepWorkOffset;
            }
            return work;
        }
    }
}
