using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Patients with no medical bed on this level walk to a linked level that
    // has one; doctors with nobody to tend here commute to patients elsewhere.
    [HarmonyPatch(typeof(JobGiver_PatientGoToBed), "TryGiveJob")]
    public static class Patch_MedicalRelay_Patient
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !NeedsMedicalBed(pawn))
            {
                return;
            }
            if (StrataMod.Settings == null || !StrataMod.Settings.medicalRelayEnabled)
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            LevelRoleUtility.SortLinksByRole(links, LevelRole.Hospital);
            foreach (LevelGraph.LevelLink link in links)
            {
                Building_Bed bed = PawnRelay.FindMedicalBedFor(pawn, link.map);
                if (bed == null)
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Medical, 2, bed);
                if (job != null)
                {
                    __result = job;
                    return;
                }
            }
        }

        private static bool NeedsMedicalBed(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.IsColonist
                && HealthAIUtility.ShouldSeekMedicalRest(pawn)
                && (pawn.CurrentBed()?.Medical != true);
        }
    }

    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage))]
    public static class Patch_MedicalRelay_Doctor
    {
        public static void Postfix(JobGiver_Work __instance, Pawn pawn, ref ThinkResult __result)
        {
            if (__result.Job != null || __instance.emergency)
            {
                return;
            }
            if (StrataMod.Settings == null || !StrataMod.Settings.medicalRelayEnabled)
            {
                return;
            }
            if (pawn?.workSettings?.WorkIsActive(WorkTypeDefOf.Doctor) != true)
            {
                return;
            }
            if (StrataPawnUtility.IsParamedicMech(pawn))
            {
                return;
            }
            if (!PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            LevelRoleUtility.SortLinksByRole(links, LevelRole.Hospital);
            foreach (LevelGraph.LevelLink link in links)
            {
                if (!PawnRelay.HasPatientsNeedingTend(link.map))
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Medical, 2);
                if (job != null)
                {
                    __result = new ThinkResult(job, __instance, JobTag.MiscWork);
                    return;
                }
            }
        }
    }
}
