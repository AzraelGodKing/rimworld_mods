using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Cleanup))]
    public static class Patch_JobDriver_Cleanup_PrisonVisit
    {
        public static void Postfix(JobDriver __instance, JobCondition condition)
        {
            if (condition != JobCondition.Succeeded) return;
            if (__instance is not JobDriver_ChatWithPrisoner) return;
            Pawn prisoner = __instance.job?.targetA.Pawn;
            FamilyEchoUtility.NotifyPrisonVisit(__instance.pawn, prisoner);
        }
    }

    [HarmonyPatch(typeof(GenGuest), nameof(GenGuest.PrisonerRelease))]
    public static class Patch_PrisonerRelease_FamilyEcho
    {
        public static void Prefix(Pawn p)
        {
            FamilyEchoUtility.NotifyReleased(p);
        }
    }
}
