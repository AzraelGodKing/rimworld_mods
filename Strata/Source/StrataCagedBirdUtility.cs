using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    public static class StrataCagedBirdUtility
    {
        public static bool UsesSustainMode =>
            StrataMod.Settings != null && StrataMod.Settings.cageSustainHunger;

        public static void MaintainPosition(Thing cage, Pawn bird)
        {
            if (cage == null || bird == null || bird.Dead || !bird.Spawned)
            {
                return;
            }
            if (bird.Map != cage.Map)
            {
                return;
            }
            if (bird.Position != cage.Position)
            {
                bird.Position = cage.Position;
            }
            bird.pather?.StopDead();
            if (bird.jobs?.curJob != null && bird.jobs.curJob.def != JobDefOf.Wait)
            {
                bird.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Wait, cage), JobCondition.InterruptForced);
            }
        }

        public static void FeedCagedBird(Thing cage, Pawn bird)
        {
            if (bird?.needs?.food == null)
            {
                return;
            }
            if (UsesSustainMode)
            {
                Need_Food food = bird.needs.food;
                food.CurLevel = food.MaxLevel;
                return;
            }
            cage.TryGetComp<CompCageFoodStorage>()?.TryFeed(bird);
        }

        public static string FeedingInspectHint(Thing cage, Pawn bird)
        {
            if (UsesSustainMode)
            {
                return $"{bird.LabelShort} — hunger sustained while caged (mod setting).";
            }
            CompCageFoodStorage storage = cage?.TryGetComp<CompCageFoodStorage>();
            if (storage == null)
            {
                return $"{bird.LabelShort} in cage.";
            }
            if (storage.HasFood)
            {
                return $"{bird.LabelShort} — eating from cage food storage.";
            }
            if (bird.needs?.food != null && bird.needs.food.CurLevelPercentage < 0.35f)
            {
                return $"{bird.LabelShort} — hungry; stock food in cage.";
            }
            return $"{bird.LabelShort} — needs food stocked in cage.";
        }

        public static bool CanAssignToColonyCage(Pawn pawn, Map map, IntVec3 cagePos, float radius, out string rejectReason)
        {
            rejectReason = null;
            if (pawn == null || pawn.Dead)
            {
                rejectReason = "Invalid bird.";
                return false;
            }
            if (StrataCagedBirdRegistry.IsContained(pawn))
            {
                rejectReason = "Already caged.";
                return false;
            }
            if (pawn.Map != map || !pawn.Position.InHorDistOf(cagePos, radius))
            {
                rejectReason = "The bird must be nearby.";
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                rejectReason = "Only colony birds can be placed in a cage.";
                return false;
            }
            return true;
        }
    }
}
