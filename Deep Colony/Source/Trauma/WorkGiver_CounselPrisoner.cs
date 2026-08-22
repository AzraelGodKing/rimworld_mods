using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    public class WorkGiver_CounselPrisoner : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn.WorkTagIsDisabled(WorkTags.Social)) return true;
            if (pawn.skills?.GetSkill(SkillDefOf.Social)?.TotallyDisabled == true) return true;
            if (DeepColonySettings.Get.enablePrisonerCounsel && DeepColonySettings.Get.enableTrauma)
                return false;
            if (DeepColonySettings.Get.enableFamilyJoin)
                return false;
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == pawn) return false;
            if (!patient.IsPrisonerOfColony) return false;
            if (patient.Dead || !patient.Spawned) return false;
            if (patient.InMentalState || patient.Downed) return false;
            if (patient.guest == null) return false;
            if (!pawn.CanReserveAndReach(patient, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
                return false;

            if (FamilyLoyaltyUtility.IsUnwaveringPrisoner(patient))
                return FamilyLoyaltyUtility.CanAttemptBreak(pawn, patient, out _);

            if (!DeepColonySettings.Get.enablePrisonerCounsel) return false;
            if (!DeepColonySettings.Get.enableTrauma) return false;
            if ((pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0) < 4 && !forced) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DC_DefOf.DC_Job_CounselPrisoner, t);
        }
    }
}
