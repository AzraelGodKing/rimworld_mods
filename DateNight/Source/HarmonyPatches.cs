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
        public static void Postfix(Rect rect)
        {
            TimeAssignmentDef lovin = DateNightDefOf.DateNight_Lovin;
            if (lovin == null)
            {
                return;
            }

            // Match vanilla cell math from the 191×65 rect MainTabWindow_Schedule passes
            // (Anything/Work/Joy/Sleep[/Meditate] are drawn in one row of half-width cells).
            float cellW = rect.width * 0.5f;
            float cellH = rect.height * 0.5f;
            int index = ModsConfig.RoyaltyActive ? 5 : 4;
            Rect cell = new Rect(rect.x + cellW * index, rect.y, cellW, cellH);
            DrawSelectorButton(cell, lovin);
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

            __result = DateNightUtility.TryMakeBedJob(pawn);
            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_Work), nameof(JobGiver_Work.GetPriority))]
    public static class Patch_Work_GetPriority
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (!DateNightUtility.IsLovinSchedule(pawn))
            {
                return true;
            }

            // Same as Sleep: work is low-priority background.
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
            if (!DateNightUtility.ShouldBoostLovinChance(pawn, partner))
            {
                return;
            }

            __result = DateNightUtility.AlwaysDoLovinMtbHours;
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
}
