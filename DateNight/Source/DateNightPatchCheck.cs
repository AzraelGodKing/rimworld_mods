#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DateNight
{
    /// <summary>
    /// One startup line that says whether every Date Night patch landed (AZR-538).
    /// A renamed or removed target on a new game build shows up here by name
    /// instead of as a silent missing feature.
    /// </summary>
    internal static class DateNightPatchCheck
    {
        internal static void Verify(string harmonyId)
        {
            var targets = new List<(string label, MethodBase? method)>
            {
                ("TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid",
                    AccessTools.Method(typeof(TimeAssignmentSelector), nameof(TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid))),
                ("PawnColumnWorker_AllowedArea.DoHeader",
                    AccessTools.Method(typeof(PawnColumnWorker_AllowedArea), nameof(PawnColumnWorker_AllowedArea.DoHeader))),
                ("JobGiver_GetRest.GetPriority",
                    AccessTools.Method(typeof(JobGiver_GetRest), nameof(JobGiver_GetRest.GetPriority))),
                ("JobGiver_GetRest.TryGiveJob", AccessTools.Method(typeof(JobGiver_GetRest), "TryGiveJob")),
                ("JobGiver_Work.GetPriority",
                    AccessTools.Method(typeof(JobGiver_Work), nameof(JobGiver_Work.GetPriority))),
                ("LovePartnerRelationUtility.GetLovinMtbHours",
                    AccessTools.Method(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetLovinMtbHours))),
                ("JobDriver_Lovin.GenerateRandomMinTicksToNextLovin",
                    AccessTools.Method(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin")),
                ("ThinkNode_Priority_GetJoy.GetPriority",
                    AccessTools.Method(typeof(ThinkNode_Priority_GetJoy), nameof(ThinkNode_Priority_GetJoy.GetPriority))),
                ("JobGiver_GetJoy.TryGiveJob", AccessTools.Method(typeof(JobGiver_GetJoy), "TryGiveJob")),
                ("Pawn.GetInspectString", AccessTools.Method(typeof(Pawn), nameof(Pawn.GetInspectString))),
                ("Pawn.GetGizmos", AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos))),
                ("InteractionWorker_MarriageProposal.RandomSelectionWeight",
                    AccessTools.Method(typeof(InteractionWorker_MarriageProposal), nameof(InteractionWorker_MarriageProposal.RandomSelectionWeight))),
            };
            if (DateNightRjwSoftCompat.Active)
            {
                targets.Add(("JobDriver.Cleanup (RimJobWorld)",
                    AccessTools.Method(typeof(JobDriver), nameof(JobDriver.Cleanup))));
            }

            var missing = new List<string>();
            for (int i = 0; i < targets.Count; i++)
            {
                if (!PatchedBy(targets[i].method, harmonyId))
                {
                    missing.Add(targets[i].label);
                }
            }

            if (missing.Count == 0)
            {
                Log.Message("[DateNight] All " + targets.Count + " patches applied.");
                return;
            }

            Log.Warning("[DateNight] " + missing.Count + " of " + targets.Count
                + " patches did not apply; those features are off: " + string.Join(", ", missing) + ".");
        }

        private static bool PatchedBy(MethodBase? method, string harmonyId)
        {
            if (method == null)
            {
                return false;
            }
            try
            {
                Patches? info = Harmony.GetPatchInfo(method);
                return info != null && info.Owners.Contains(harmonyId);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
