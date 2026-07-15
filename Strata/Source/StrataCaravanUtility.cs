using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // While a caravan is forming on the surface, pull high-value stock from
    // linked underground levels toward the stairhead landing.
    public static class StrataCaravanUtility
    {
        private const int HaulIntervalTicks = 2500;
        private const int MaxHaulJobsPerPulse = 3;
        private const float MinMarketValue = 75f;

        private static int lastPulseTick = -99999;

        public static bool CaravanDialogOpen => Find.WindowStack.IsOpen(typeof(Dialog_FormCaravan));

        public static Map CaravanFormingMap
        {
            get
            {
                Dialog_FormCaravan dialog = Find.WindowStack.WindowOfType<Dialog_FormCaravan>();
                if (dialog == null)
                {
                    return null;
                }
                return Traverse.Create(dialog).Field("map").GetValue<Map>();
            }
        }

        public static IEnumerable<Thing> AllColonyItems(Map surface)
        {
            if (surface == null)
            {
                yield break;
            }
            foreach (Thing thing in surface.listerThings.AllThings)
            {
                if (thing.Faction == Faction.OfPlayer && thing.def.category == ThingCategory.Item)
                {
                    yield return thing;
                }
            }
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                foreach (Thing thing in link.map.listerThings.AllThings)
                {
                    if (thing.Faction == Faction.OfPlayer && thing.def.category == ThingCategory.Item)
                    {
                        yield return thing;
                    }
                }
            }
        }

        public static int CountValuableBelow(Map surface)
        {
            if (surface == null || !StrataMapUtility.IsSurfacePlayerHome(surface))
            {
                return 0;
            }
            int count = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                if (!StrataMapUtility.IsUnderground(link.map))
                {
                    continue;
                }
                foreach (Thing thing in link.map.listerThings.AllThings)
                {
                    if (thing.def.category == ThingCategory.Item
                        && thing.MarketValue * thing.stackCount >= MinMarketValue)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static void TickCaravanPull()
        {
            if (StrataMod.Settings == null || !StrataMod.Settings.caravanPullEnabled)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (now - lastPulseTick < HaulIntervalTicks)
            {
                return;
            }
            Map surface = CaravanFormingMap;
            if (surface == null || !StrataMapUtility.IsSurfacePlayerHome(surface))
            {
                return;
            }
            lastPulseTick = now;
            IssueUndergroundHaulJobs(surface);
        }

        private static void IssueUndergroundHaulJobs(Map surface)
        {
            int issued = 0;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(surface))
            {
                if (!StrataMapUtility.IsUnderground(link.map))
                {
                    continue;
                }
                MapPortal portal = link.firstStep;
                if (portal == null || !portal.Spawned)
                {
                    continue;
                }
                IntVec3 dropNear = portal.Position;
                foreach (Thing thing in link.map.listerThings.AllThings)
                {
                    if (issued >= MaxHaulJobsPerPulse)
                    {
                        return;
                    }
                    if (thing.def.category != ThingCategory.Item
                        || thing.MarketValue * thing.stackCount < MinMarketValue
                        || thing.IsForbidden(Faction.OfPlayer))
                    {
                        continue;
                    }
                    Pawn hauler = FindBestHauler(thing, link.map, portal);
                    if (hauler == null)
                    {
                        continue;
                    }
                    Job job = JobMaker.MakeJob(StrataDefOf.Strata_HaulToLevel, thing, portal);
                    job.count = thing.stackCount;
                    hauler.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    issued++;
                }
            }
        }

        private static Pawn FindBestHauler(Thing thing, Map map, MapPortal portal)
        {
            Pawn best = null;
            float bestDist = float.MaxValue;
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.Drafted || pawn.Downed || pawn.WorkTagIsDisabled(WorkTags.Hauling))
                {
                    continue;
                }
                if (!HaulAIUtility.PawnCanAutomaticallyHaul(pawn, thing, forced: false))
                {
                    continue;
                }
                if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Some)
                    || !pawn.CanReach(portal, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }
                float dist = pawn.Position.DistanceToSquared(thing.Position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = pawn;
                }
            }
            return best;
        }
    }
}
