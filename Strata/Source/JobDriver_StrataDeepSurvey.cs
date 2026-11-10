using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Research work: run a deep survey on a seismograph the player flagged.
    public class WorkGiver_StrataDeepSurvey : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            ThingDef def = StrataThingDefOf.Strata_Seismograph;
            return def == null ? null : pawn.Map.listerThings.ThingsOfDef(def);
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            ThingDef def = StrataThingDefOf.Strata_Seismograph;
            if (def == null)
            {
                return true;
            }
            List<Thing> consoles = pawn.Map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < consoles.Count; i++)
            {
                if (consoles[i].TryGetComp<CompSeismograph>()?.SurveyRequested == true)
                {
                    return false;
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            CompSeismograph seismo = t.TryGetComp<CompSeismograph>();
            if (seismo == null || !seismo.SurveyRequested || t.Faction != pawn.Faction)
            {
                return false;
            }
            if (!seismo.CanSurveyNow(out string reason))
            {
                JobFailReason.Is(reason);
                return false;
            }
            return !t.IsForbidden(pawn)
                && !t.IsBurning()
                && pawn.CanReserve(t, ignoreOtherReservations: forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(StrataDefOf.Strata_DeepSurvey, t);
        }
    }

    public class JobDriver_StrataDeepSurvey : JobDriver
    {
        private const TargetIndex ConsoleInd = TargetIndex.A;

        private CompSeismograph Seismo => job.GetTarget(ConsoleInd).Thing?.TryGetComp<CompSeismograph>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(ConsoleInd), job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(ConsoleInd);
            this.FailOnBurningImmobile(ConsoleInd);
            this.FailOn(() => Seismo == null || !Seismo.SurveyRequested || !Seismo.Powered);
            yield return Toils_Goto.GotoThing(ConsoleInd, PathEndMode.Touch);

            Toil survey = ToilMaker.MakeToil("StrataDeepSurvey");
            survey.tickAction = () =>
            {
                Pawn actor = survey.actor;
                actor.skills?.Learn(SkillDefOf.Intellectual, 0.08f);
                float speed = actor.GetStatValue(StatDefOf.ResearchSpeed);
                if (Seismo.AddSurveyWork(speed, actor))
                {
                    ReadyForNextToil();
                }
            };
            survey.FailOnCannotTouch(ConsoleInd, PathEndMode.Touch);
            survey.WithEffect(EffecterDefOf.Research, ConsoleInd);
            survey.WithProgressBar(ConsoleInd, () => Seismo?.SurveyProgress ?? 0f);
            survey.defaultCompleteMode = ToilCompleteMode.Never;
            survey.activeSkill = () => SkillDefOf.Intellectual;
            yield return survey;
        }
    }
}
