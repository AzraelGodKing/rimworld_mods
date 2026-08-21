using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace DateNight
{
    /// <summary>
    /// Vanilla hardcodes Anything/Work/Joy/Sleep(/Meditate) in TimeAssignmentSelector —
    /// custom TimeAssignmentDefs never appear unless we draw them ourselves.
    /// </summary>
    [HarmonyPatch(typeof(TimeAssignmentSelector), nameof(TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid))]
    public static class Patch_TimeAssignmentSelector_DrawGrid
    {
        // Vanilla 2×2 uses columns 0–1. Extra schedule buttons (Meditate is in-grid)
        // continue the top row at index 4+, matching Rimbody / Exosuit.
        private const int FirstExtraColumn = 4;

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Rect rect)
        {
            TimeAssignmentDef lovin = DateNightDefOf.DateNight_Lovin;
            TimeAssignmentDef date = DateNightDefOf.DateNight_Date;
            if (lovin == null && date == null)
            {
                return;
            }

            float cellW = rect.width * 0.5f;
            float cellH = rect.height * 0.5f;
            int index = LovinColumnIndex();
            if (lovin != null)
            {
                Rect cell = new Rect(rect.x + cellW * index, rect.y, cellW, cellH);
                DrawSelectorButton(cell, lovin);
                index++;
            }
            if (date != null)
            {
                Rect cell = new Rect(rect.x + cellW * index, rect.y, cellW, cellH);
                DrawSelectorButton(cell, date);
            }
        }

        private static int? cachedColumn;

        private static int LovinColumnIndex()
        {
            if (cachedColumn != null)
            {
                return cachedColumn.Value;
            }

            int index = FirstExtraColumn;
            if (ModsConfig.RoyaltyActive)
            {
                index++;
            }
            if (HasExosuitScheduleButton())
            {
                index++;
            }
            if (HasRimbodyScheduleButton())
            {
                index++;
            }
            if (HasScheduleEverythingButton())
            {
                index++;
            }

            cachedColumn = index;
            return index;
        }

        private static bool HasExosuitScheduleButton()
        {
            return ModActive("AOBA.ExosuitFramework")
                || ModActive("AOBA.MechsuitFramework")
                || DefDatabase<TimeAssignmentDef>.GetNamedSilentFail("Piloting") != null
                || DefDatabase<TimeAssignmentDef>.GetNamedSilentFail("Exosuit_Piloting") != null;
        }

        /// <summary>
        /// Rimbody draws Workout in the same extra column Date Night used to occupy.
        /// Clicks then open Workout / Joy (useRecToSelect) instead of selecting Lovin.
        /// When useRecToSelect is on, Workout shares the Joy cell — no extra column.
        /// </summary>
        private static bool HasRimbodyScheduleButton()
        {
            if (!ModActive("Maux36.Rimbody")
                && DefDatabase<TimeAssignmentDef>.GetNamedSilentFail("Rimbody_Workout") == null)
            {
                return false;
            }

            try
            {
                Type settings = AccessTools.TypeByName("Maux36.Rimbody.RimbodySettings");
                FieldInfo rec = settings != null
                    ? AccessTools.Field(settings, "useRecToSelect")
                    : null;
                if (rec != null && rec.IsStatic && rec.FieldType == typeof(bool) && (bool)rec.GetValue(null))
                {
                    return false;
                }
            }
            catch (Exception)
            {
                // Fail-open: assume the extra Workout button exists.
            }

            return true;
        }

        private static bool HasScheduleEverythingButton()
        {
            return ModNameContains("Schedule Everything");
        }

        private static bool ModActive(string packageId)
        {
            return !packageId.NullOrEmpty()
                && ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null;
        }

        private static bool ModNameContains(string fragment)
        {
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod?.Name != null
                    && mod.Name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private static void DrawSelectorButton(Rect rect, TimeAssignmentDef ta)
        {
            rect = rect.ContractedBy(2f);
            GUI.DrawTexture(rect, ta.ColorTexture);
            if (Widgets.ButtonInvisible(rect))
            {
                TimeAssignmentSelector.selectedAssignment = ta;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(rect, ta.LabelCap);
            }
            if (TimeAssignmentSelector.selectedAssignment == ta)
            {
                Widgets.DrawBox(rect, 2);
            }
            else
            {
                UIHighlighter.HighlightOpportunity(rect, ta.cachedHighlightNotSelectedTag);
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), nameof(JobGiver_GetRest.GetPriority))]
    public static class Patch_GetRest_GetPriority
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (DateNightUtility.IsDateSchedule(pawn))
            {
                Need_Rest rest = pawn.needs?.rest;
                if (rest != null && rest.CurCategory >= RestCategory.Exhausted)
                {
                    return true;
                }

                __result = 0f;
                return false;
            }

            if (!DateNightUtility.IsLovinSchedule(pawn))
            {
                return true;
            }

            Lord lord = pawn.GetLord();
            if (lord?.CurLordToil != null && !lord.CurLordToil.AllowSatisfyLongNeeds)
            {
                __result = 0f;
                return false;
            }

            // Food / chem first — drop bed priority so GetFood (9.5) etc. win.
            if (DateNightUtility.ShouldSatisfyNeedsBeforeBed(pawn))
            {
                __result = 0f;
                return false;
            }

            // Sleep-like urgency even when rest need is null/full (no-sleep genes).
            __result = 8f;
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_GetRest_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (!DateNightUtility.IsLovinSchedule(pawn))
            {
                return true;
            }

            if (DateNightUtility.ShouldSatisfyNeedsBeforeBed(pawn))
            {
                __result = null;
                return false;
            }

            if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
            {
                return true;
            }

            Job bedJob = DateNightUtility.TryMakeBedJob(pawn);
            if (bedJob != null)
            {
                __result = bedJob;
                return false;
            }

            // No reachable bed: fall back to vanilla rest (ground / medical).
            return true;
        }
    }

    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.GetPriority))]
    public static class Patch_Work_GetPriority
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (!DateNightUtility.IsLovinOrDateSchedule(pawn))
            {
                return true;
            }

            // Same as Sleep/Joy: work is low-priority background.
            if (pawn.workSettings == null || !pawn.workSettings.EverWork)
            {
                __result = 0f;
                return false;
            }

            __result = 3f;
            return false;
        }
    }

    [HarmonyPatch(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetLovinMtbHours))]
    public static class Patch_GetLovinMtbHours
    {
        public static void Postfix(Pawn pawn, Pawn partner, ref float __result)
        {
            if (__result <= 0f)
            {
                return;
            }
            if (DateNightHooks.BiotechBlocksForcedLovin(pawn)
                || DateNightHooks.BiotechBlocksForcedLovin(partner))
            {
                return;
            }

            if (DateNightUtility.ShouldBoostLovinChance(pawn, partner))
            {
                __result = DateNightUtility.AlwaysDoLovinMtbHours;
                return;
            }

            // A good date leaves a spark: better lovin chance for a day after.
            if ((DateNightMod.Settings == null || DateNightMod.Settings.postDateLovinBoost)
                && (DateNightDateUtility.HadRecentGoodDate(pawn)
                    || DateNightDateUtility.HadRecentGoodDate(partner)))
            {
                __result *= 0.25f;
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_LovinCooldown
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(JobDriver_Lovin), "GenerateRandomMinTicksToNextLovin");
        }

        public static void Postfix(Pawn pawn, ref int __result)
        {
            if (DateNightMod.Settings == null || DateNightMod.Settings.pregnancySafeCooldown)
            {
                return;
            }
            if (!DateNightMod.Settings.eagerCooldown)
            {
                return;
            }
            if (!DateNightUtility.IsLovinSchedule(pawn))
            {
                return;
            }

            __result = DateNightUtility.AlwaysDoLovinCooldownTicks;
        }
    }

    /// <summary>
    /// Vanilla GetPriority only handles Anything / Joy / Sleep / Meditate — any other
    /// TimeAssignmentDef with allowJoy (including Date) throws NotImplementedException.
    /// Must Prefix-skip the original; a Postfix never runs after that throw.
    /// </summary>
    [HarmonyPatch(typeof(ThinkNode_Priority_GetJoy), nameof(ThinkNode_Priority_GetJoy.GetPriority))]
    public static class Patch_GetJoy_GetPriority
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (!DateNightUtility.IsDateSchedule(pawn))
            {
                return true;
            }
            if (DateNightUtility.ShouldSatisfyNeedsBeforeBed(pawn))
            {
                __result = 0f;
                return false;
            }

            __result = 8f;
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetJoy), "TryGiveJob")]
    public static class Patch_GetJoy_TryGiveJob
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (!DateNightUtility.IsDateSchedule(pawn))
            {
                return true;
            }
            if (DateNightUtility.ShouldSatisfyNeedsBeforeBed(pawn))
            {
                __result = null;
                return false;
            }

            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner == null || !DateNightUtility.IsDateSchedule(partner)
                || !DateNightDateUtility.CanDate(pawn, partner))
            {
                return true;
            }
            if (DateNightDefOf.DateNight_GoOnDate == null)
            {
                return true;
            }

            LocalTargetInfo spot = DateNightDateUtility.FindDateSpot(pawn, partner);
            Job job = JobMaker.MakeJob(DateNightDefOf.DateNight_GoOnDate);
            job.SetTarget(TargetIndex.A, partner);
            job.SetTarget(TargetIndex.B, spot);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.ignoreForbidden = true;
            __result = job;
            return false;
        }
    }
}
