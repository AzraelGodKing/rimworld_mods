using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony
{
    public class WorkGiver_CounselTrauma : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!DeepColonySettings.Get.enableTrauma) return true;
            if (!forced && IdeologyCounselUtility.AutoCounselBlocked) return true;
            if (pawn.WorkTagIsDisabled(WorkTags.Social)) return true;
            if (pawn.skills?.GetSkill(SkillDefOf.Social)?.TotallyDisabled == true) return true;
            return false;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient == pawn) return false;
            if (!patient.IsColonistPlayerControlled) return false;
            if (patient.Dead || !patient.Spawned) return false;
            if (patient.InMentalState || patient.Downed) return false;
            if (!TraumaUtility.HasAnyTrauma(patient)) return false;
            if (!pawn.CanReserveAndReach(patient, PathEndMode.Touch, Danger.Deadly, 1, -1, null, forced))
                return false;

            // Prefer counselors who aren't themselves crushed by trauma.
            if (!forced && TraumaUtility.HasAnyTrauma(pawn)
                && (pawn.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0) < 6)
            {
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DC_DefOf.DC_Job_CounselTrauma, t);
        }
    }
}
