using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    /// <summary>D19 — wardens who are family will talk to unwavering kin prisoners.</summary>
    public class WorkGiver_FamilyLoyalty : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!DeepColonySettings.Get.enableFamilyJoin) return true;
            if (pawn.WorkTagIsDisabled(WorkTags.Social)) return true;
            if (pawn.skills?.GetSkill(SkillDefOf.Social)?.TotallyDisabled == true) return true;
            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == pawn) return false;
            if (!FamilyLoyaltyUtility.CanAttemptBreak(pawn, patient, out _)) return false;
            if (patient.InMentalState) return false;
            if (!pawn.CanReserveAndReach(patient, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
                return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DC_DefOf.DC_Job_CounselPrisoner, t);
        }
    }
}
