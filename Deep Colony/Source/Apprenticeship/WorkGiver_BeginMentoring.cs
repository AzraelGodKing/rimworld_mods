using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>
    /// Work giver that lets high-skill pawns actively mentor their designated apprentice.
    /// Only offered when mentor and apprentice are on the same map and nearby.
    /// </summary>
    public class WorkGiver_BeginMentoring : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!DeepColonySettings.Get.enableMentoring) return true;
            // Only skilled pawns with a designated apprentice do this work.
            var comp = pawn.TryGetComp<Comp_DeepColony>();
            return comp == null || pawn.relations.GetDirectRelationsCount(DC_DefOf.DC_MentorOf) == 0;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn apprentice)) return false;
            if (apprentice == pawn) return false;

            var apprenticeComp = apprentice.TryGetComp<Comp_DeepColony>();
            if (apprenticeComp?.mentor != pawn) return false;
            if (!MentorshipUtility.MentorLeadsInAnySkill(pawn, apprentice)) return false;
            if (!apprentice.IsColonistPlayerControlled) return false;
            if (apprentice.Dead || !apprentice.Spawned) return false;
            if (!pawn.CanReach(apprentice, PathEndMode.Touch, Danger.Deadly)) return false;
            if (!pawn.CanReserve(apprentice)) return false;

            // Don't mentor if apprentice is in combat or downed
            if (apprentice.InMentalState || apprentice.Downed) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DC_DefOf.DC_Job_Mentor, t);
        }
    }
}
