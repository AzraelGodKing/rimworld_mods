using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Smoke piling up on a level with nobody on it - a generator left running
    // in a sealed room downstairs kills the next colonist who walks in.
    public class Alert_SmokeOnVacantLevel : Alert
    {
        private const float WorryThreshold = 0.35f;

        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public Alert_SmokeOnVacantLevel()
        {
            defaultLabel = "Smoke building underground";
            defaultExplanation = "Combustion smoke is accumulating on a level with no colonists on it. "
                + "Something is burning down there with no working ventilation - the next person "
                + "to walk in will be breathing it.";
        }

        public override AlertReport GetReport()
        {
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!StrataMapUtility.IsUnderground(map) || map.mapPawns.FreeColonistsSpawnedCount > 0)
                {
                    continue;
                }
                SmokeMapComponent smoke = map.GetComponent<SmokeMapComponent>();
                if (smoke != null && smoke.TryGetWorstCloud(out IntVec3 cell, out float density)
                    && density >= WorryThreshold)
                {
                    targets.Add(new GlobalTargetInfo(cell, map));
                }
            }
            return targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
        }
    }

    // Colonists on a level whose every way up is sealed. Deliberate bunkering
    // is fine; forgetting them down there is not.
    public class Alert_ColonistsBelowSealedShaft : Alert
    {
        private readonly List<GlobalTargetInfo> targets = new List<GlobalTargetInfo>();

        public Alert_ColonistsBelowSealedShaft()
        {
            defaultLabel = "Colonists sealed below";
            defaultExplanation = "Colonists are on an underground level whose every exit shaft is "
                + "sealed. They cannot come up until a stairwell or elevator is unsealed.";
            defaultPriority = AlertPriority.High;
        }

        public override AlertReport GetReport()
        {
            targets.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (!StrataMapUtility.IsUnderground(map) || map.mapPawns.FreeColonistsSpawnedCount == 0)
                {
                    continue;
                }
                if (!AllExitsSealed(map))
                {
                    continue;
                }
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int j = 0; j < colonists.Count; j++)
                {
                    targets.Add(colonists[j]);
                }
            }
            return targets.Count > 0 ? AlertReport.CulpritsAre(targets) : false;
        }

        private static bool AllExitsSealed(Map map)
        {
            bool anyExit = false;
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal))
            {
                if (!(thing is PocketMapExit exit) || thing is Building_StairsDown || !exit.Spawned)
                {
                    continue;
                }
                anyExit = true;
                if (!StrataPortalUtility.IsSealedPortal(exit.entrance ?? (Thing)exit))
                {
                    return false; // at least one way up is open
                }
            }
            return anyExit;
        }
    }
}
