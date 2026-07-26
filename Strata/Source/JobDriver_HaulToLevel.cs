using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Pick up a thing, carry it to a stairwell, then EnterPortal. Dest map +
    // return map are remembered so PortalRelayChain can multi-hop and finish
    // into a stockpile (vanilla clears jobs after OnEntered and often fails to
    // drop cargo at crowded landings).
    public class JobDriver_HaulToLevel : JobDriver
    {
        private Thing Item => job.GetTarget(TargetIndex.A).Thing;

        private MapPortal Portal => (MapPortal)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnForbidden(TargetIndex.A);
            // The player can seal the shaft mid-haul; drop the job instead of
            // walking cargo to a door that won't open.
            this.FailOn(() => StrataPortalUtility.IsSealedPortal(Portal));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            Toil enter = ToilMaker.MakeToil("EnterStairs");
            enter.initAction = delegate
            {
                if (Portal == null || !Portal.Spawned || !Portal.IsEnterable(out _))
                {
                    return;
                }

                if (HaulToLevelTargets.TryTake(pawn, out Map destMap, out Map sourceMap, out int constructibleId)
                    && destMap != null)
                {
                    Thing site = constructibleId > 0
                        ? StrataPortalUtility.FindThingByIdAcrossMaps(constructibleId)
                        : null;
                    PortalRelayChain.Mark(
                        pawn,
                        destMap,
                        RelayPurpose.Haul,
                        preferredBed: null,
                        returnMap: sourceMap,
                        preferredThing: site);
                }

                Job enterJob = JobMaker.MakeJob(JobDefOf.EnterPortal, Portal);
                pawn.jobs.jobQueue.EnqueueFirst(enterJob, JobTag.Misc);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }

    // Final dest map for HaulToLevel (may be multi-hop past the first stair).
    // Optional constructibleId: force-build / deliver-resources should finish into
    // that blueprint/frame instead of a stockpile (or dropping on failed storage).
    internal static class HaulToLevelTargets
    {
        private struct Target
        {
            public int destMapId;
            public int sourceMapId;
            public int constructibleId;
        }

        private static readonly Dictionary<int, Target> pending = new Dictionary<int, Target>();

        internal static void Remember(Pawn pawn, Map destMap, Map sourceMap, Thing constructible = null)
        {
            if (pawn == null || destMap == null || sourceMap == null)
            {
                return;
            }

            pending[pawn.thingIDNumber] = new Target
            {
                destMapId = destMap.uniqueID,
                sourceMapId = sourceMap.uniqueID,
                constructibleId = constructible != null ? constructible.thingIDNumber : 0,
            };
        }

        internal static bool TryTake(Pawn pawn, out Map destMap, out Map sourceMap, out int constructibleId)
        {
            destMap = null;
            sourceMap = null;
            constructibleId = 0;
            if (pawn == null || !pending.TryGetValue(pawn.thingIDNumber, out Target t))
            {
                return false;
            }

            pending.Remove(pawn.thingIDNumber);
            destMap = FindMap(t.destMapId);
            sourceMap = FindMap(t.sourceMapId);
            constructibleId = t.constructibleId;
            return destMap != null;
        }

        internal static void ResetSession()
        {
            pending.Clear();
        }

        private static Map FindMap(int uniqueId)
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].uniqueID == uniqueId)
                {
                    return maps[i];
                }
            }

            return null;
        }
    }
}
