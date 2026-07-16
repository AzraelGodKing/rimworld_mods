using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Surface threat: raiders assault the colony and batter stairwell entrances,
    // then pursue colonists underground via raid pursuit.
    public class IncidentWorker_DeepSiege : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map
                && StrataMapUtility.IsSurfacePlayerHome(map)
                && LevelGraph.AnyLinkFrom(map)
                && map.mapPawns.FreeColonistsSpawnedCount > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction faction = parms.faction ?? Find.FactionManager.RandomRaidableEnemyFaction();
            if (faction == null)
            {
                return false;
            }
            float points = parms.points > 0f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            var groupParms = new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                points = points,
                faction = faction,
                tile = map.Tile,
            };
            List<Pawn> raiders = PawnGroupMakerUtility.GeneratePawns(groupParms).ToList();
            if (raiders.Count == 0)
            {
                return false;
            }
            IntVec3 spawn = CellFinder.RandomEdgeCell(map);
            for (int attempt = 0; attempt < 12 && !spawn.Standable(map); attempt++)
            {
                spawn = CellFinder.RandomEdgeCell(map);
            }
            for (int i = 0; i < raiders.Count; i++)
            {
                GenSpawn.Spawn(raiders[i], CellFinder.RandomClosewalkCellNear(spawn, map, 8), map);
            }
            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: true, canTimeoutOrFlee: true), map, raiders);
            OrderStairwellAssault(map, raiders);
            SendStandardLetter(parms, new TargetInfo(spawn, map));
            return true;
        }

        private static void OrderStairwellAssault(Map map, List<Pawn> raiders)
        {
            List<Thing> portals = map.listerThings.ThingsInGroup(ThingRequestGroup.MapPortal);
            if (portals.Count == 0)
            {
                return;
            }
            Thing target = portals[0];
            int sent = 0;
            for (int i = 0; i < raiders.Count && sent < 3; i++)
            {
                Pawn raider = raiders[i];
                if (raider.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                    job.expiryInterval = 3500;
                    raider.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    sent++;
                }
            }
        }
    }
}
