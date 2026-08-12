using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ShiftChange
{
    /// <summary>
    /// While Shift Change is managing a pawn, suppress vanilla apparel optimization
    /// so it does not fight sleepwear / work kits mid-shift.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_OptimizeApparel), "TryGiveJob")]
    public static class Patch_OptimizeApparel_SkipManaged
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            if (comp != null && comp.IsManaged(pawn))
            {
                __result = null;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// When work AI issues a job, hint Shift Change so Cook/Doctor/Animals kits can apply
    /// before the pawn settles into the task.
    /// </summary>
    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.TryIssueJobPackage))]
    public static class Patch_JobGiver_Work_NotifyShift
    {
        public static void Postfix(Pawn pawn, ThinkResult __result)
        {
            if (pawn == null || __result.Job == null)
            {
                return;
            }

            WorkTypeDef wt = ShiftChangeUtility.WorkTypeOfJob(__result.Job);
            if (wt == null)
            {
                return;
            }

            GameComponent_ShiftChange.Get?.NotifyWorkJobIssued(pawn, wt);
        }
    }

    /// <summary>
    /// Ideology ritual start — dress participants who have a Ritual shift rule.
    /// </summary>
    [HarmonyPatch(typeof(RitualBehaviorWorker), nameof(RitualBehaviorWorker.TryExecuteOn))]
    public static class Patch_RitualBehaviorWorker_TryExecuteOn
    {
        public static void Postfix(
            TargetInfo target,
            Pawn organizer,
            Precept_Ritual ritual,
            RitualObligation obligation,
            RitualRoleAssignments assignments,
            bool playerForced)
        {
            if (assignments?.Participants == null || assignments.Participants.Count == 0)
            {
                return;
            }

            GameComponent_ShiftChange.Get?.NotifyRitualStarted(assignments.Participants);
        }
    }
}
