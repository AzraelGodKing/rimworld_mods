using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Carry an infant through stairs to their assigned crib on another linked
    // floor. Vanilla ChildcareUtility.SafePlaceForBaby / FindBedFor only see
    // the baby's current map, so assigned upstairs cribs are treated as
    // unreachable and the baby is left "already safe" on the landing.
    public class WorkGiver_BringBabyToCribAcrossLevels : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            if (!ModsConfig.BiotechActive || pawn?.Map == null || pawn.Faction == null)
            {
                yield break;
            }

            List<Pawn> pawns = pawn.Map.mapPawns.FreeHumanlikesSpawnedOfFaction(pawn.Faction);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn baby = pawns[i];
                if (baby.DevelopmentalStage.Baby())
                {
                    yield return baby;
                }
            }
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !ModsConfig.BiotechActive || !LevelGraph.AnyLinkFrom(pawn.Map);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return TryFindJob(pawn, t as Pawn, forced, out _, out _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!TryFindJob(pawn, t as Pawn, forced, out MapPortal portal, out Building_Bed crib))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(StrataDefOf.Strata_BringBabyToLevel, t, portal, crib);
            job.count = 1;
            return job;
        }

        private static bool TryFindJob(
            Pawn hauler,
            Pawn baby,
            bool forced,
            out MapPortal portal,
            out Building_Bed crib)
        {
            portal = null;
            crib = null;
            if (hauler == null || baby == null || !ModsConfig.BiotechActive)
            {
                return false;
            }

            if (!ChildcareUtility.CanSuckle(baby, out _)
                || !ChildcareUtility.CanHaulBabyNow(hauler, baby, forced, out _))
            {
                return false;
            }

            if (baby.CurrentBed() != null)
            {
                return false;
            }

            crib = baby.ownership?.OwnedBed;
            if (crib == null || !crib.Spawned || crib.Map == null || crib.Map == baby.Map)
            {
                return false;
            }

            if (!LevelGraph.AnyLinkFrom(baby.Map))
            {
                return false;
            }

            bool cribReachable = false;
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(baby.Map))
            {
                if (link.map == crib.Map)
                {
                    cribReachable = true;
                    break;
                }
            }

            if (!cribReachable)
            {
                return false;
            }

            MapPortal step = LevelGraph.BestFirstStep(baby.Map, crib.Map, hauler.Position, hauler);
            if (step == null || !step.Spawned || !step.IsEnterable(out _)
                || !hauler.CanReach(step, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }

            portal = step;
            return true;
        }
    }
}
