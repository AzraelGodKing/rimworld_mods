#nullable enable
using System;
using System.Collections.Generic;
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
    /// One extra cell (Date, with Lovin on a dropdown), always last on the extra strip.
    /// Postfix after Priority.Last so other extra-column buttons paint first.
    /// Do not use a Harmony finalizer here: it wraps this OnGUI method in try/catch
    /// and Unity warns for as long as the Schedule tab is open.
    /// </summary>
    [HarmonyPatch(typeof(TimeAssignmentSelector), nameof(TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid))]
    public static class Patch_TimeAssignmentSelector_DrawGrid
    {
        private const float DropWidth = 18f;

        // Harmony Last is 0; lower runs later among postfixes.
        private const int AfterLastPostfix = Priority.Last - 1;

        private static int cachedColumn = int.MinValue;

        [HarmonyPriority(AfterLastPostfix)]
        public static void Postfix(Rect rect)
        {
            DrawCombo(rect);
        }

        private static void DrawCombo(Rect rect)
        {
            TimeAssignmentDef? date = DateNightDefOf.DateNight_Date;
            TimeAssignmentDef? lovin = DateNightDefOf.DateNight_Lovin;
            if (date == null && lovin == null)
            {
                return;
            }

            float cellW = rect.width * 0.5f;
            float cellH = rect.height * 0.5f;
            int col = Mathf.Max(0, ExtraColumnIndex() + DateNightMod.Settings.scheduleColumnOffset);
            float x = rect.x + cellW * col;
            float w = cellW;
            Rect areas = Patch_AllowedArea_DoHeader.ButtonRect;
            if (areas.width > 1f && x + w > areas.x)
            {
                float afterPrev = rect.x + cellW * Mathf.Max(0, col - 1);
                x = afterPrev + cellW;
                w = areas.x - 2f - x;
                if (w < 52f)
                {
                    w = Mathf.Min(cellW, areas.x - 2f - afterPrev);
                    x = areas.x - 2f - w;
                    if (x < afterPrev)
                    {
                        x = afterPrev;
                        w = areas.x - 2f - x;
                    }
                }
            }

            if (w < 24f)
            {
                return;
            }

            DrawComboButton(new Rect(x, rect.y, w, cellH), date, lovin);
        }

        /// <summary>
        /// Vanilla 1.6 is a 2×2 (Meditate tucks beside Sleep). Extra mods skip ahead
        /// (Rimbody Workout, Schedule Everything). Sit immediately after the rightmost
        /// of those — last on the strip, not a cell further into Manage areas.
        /// Other Harmony owners of the grid are not guessed at (they may draw no
        /// button at all); players fix those with the column-offset setting.
        /// Cached after first Schedule-tab draw; mods do not load/unload mid-session.
        /// </summary>
        private static int ExtraColumnIndex()
        {
            if (cachedColumn != int.MinValue)
            {
                return cachedColumn;
            }

            int royalty = ModsConfig.RoyaltyActive ? 1 : 0;
            int last = 3 + royalty;
            if (HasRimbodySchedulePatch())
            {
                int rimbody = 4 + royalty;
                if (IsRimbodyExosuitLoaded())
                {
                    rimbody++;
                }
                if (rimbody > last)
                {
                    last = rimbody;
                }
            }

            if (HasScheduleEverythingPatch())
            {
                int se = 5 + royalty;
                if (se > last)
                {
                    last = se;
                }
            }

            cachedColumn = last + 1;
            return cachedColumn;
        }

        private static bool HasRimbodySchedulePatch()
        {
            return ModActive("Maux36.Rimbody")
                || TypePresent("Maux36.Rimbody.TimeAssignmentSelector_DrawTimeTable_Patch");
        }

        private static bool HasScheduleEverythingPatch()
        {
            return ModActive("Mazo.Schedules")
                || TypePresent("MazoScheduleMod.TimeAssignmentSelectorPatch");
        }

        private static bool ModActive(string packageId)
        {
            return !packageId.NullOrEmpty()
                && ModLister.GetActiveModWithIdentifier(packageId, ignorePostfix: true) != null;
        }

        private static bool TypePresent(string fullName)
        {
            try
            {
                return AccessTools.TypeByName(fullName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRimbodyExosuitLoaded()
        {
            try
            {
                Type type = AccessTools.TypeByName("Maux36.Rimbody.Rimbody");
                FieldInfo? field = type == null ? null : AccessTools.Field(type, "ExosuitFrameworkLoaded");
                return field != null && field.FieldType == typeof(bool) && field.GetValue(null) is bool loaded && loaded;
            }
            catch
            {
                return false;
            }
        }

        private static void DrawComboButton(Rect rect, TimeAssignmentDef? date, TimeAssignmentDef? lovin)
        {
            TimeAssignmentDef? shown = ShownAssignment(date, lovin);
            if (shown == null)
            {
                return;
            }

            Rect inner = rect.ContractedBy(2f);
            bool hasDropdown = date != null && lovin != null && inner.width >= DropWidth + 20f;
            bool wholeOpensMenu = date != null && lovin != null && !hasDropdown;
            Rect drop = hasDropdown
                ? new Rect(inner.xMax - DropWidth, inner.y, DropWidth, inner.height)
                : Rect.zero;
            Rect main = hasDropdown
                ? new Rect(inner.x, inner.y, inner.width - DropWidth, inner.height)
                : inner;

            // Same fill vanilla gives each assignment, without a raw Unity draw call (AZR-538).
            Widgets.DrawBoxSolid(inner, shown.color);

            if (Widgets.ButtonInvisible(main))
            {
                if (wholeOpensMenu)
                {
                    OpenDropdown(date, lovin);
                }
                else
                {
                    Select(shown);
                }
            }

            if (hasDropdown && Widgets.ButtonInvisible(drop))
            {
                OpenDropdown(date, lovin);
            }

            if (Mouse.IsOver(inner))
            {
                Widgets.DrawHighlight(inner);
                TooltipHandler.TipRegion(inner, "DateNight_ScheduleComboTip".Translate());
            }

            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(main, shown.LabelCap);
            }

            if (hasDropdown)
            {
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleCenter))
                {
                    Widgets.Label(drop, "▾");
                }
            }

            TimeAssignmentDef selected = TimeAssignmentSelector.selectedAssignment;
            if (selected == date || selected == lovin)
            {
                Widgets.DrawBox(inner, 2);
            }
            else
            {
                string tag = (date ?? shown).cachedHighlightNotSelectedTag;
                if (!tag.NullOrEmpty())
                {
                    UIHighlighter.HighlightOpportunity(inner, tag);
                }
            }
        }

        private static TimeAssignmentDef? ShownAssignment(TimeAssignmentDef? date, TimeAssignmentDef? lovin)
        {
            TimeAssignmentDef selected = TimeAssignmentSelector.selectedAssignment;
            if (selected != null && (selected == date || selected == lovin))
            {
                return selected;
            }

            return date ?? lovin;
        }

        private static void OpenDropdown(TimeAssignmentDef? date, TimeAssignmentDef? lovin)
        {
            var options = new List<FloatMenuOption>();
            if (date != null)
            {
                options.Add(new FloatMenuOption(date.LabelCap, () => Select(date)));
            }
            if (lovin != null)
            {
                options.Add(new FloatMenuOption(lovin.LabelCap, () => Select(lovin)));
            }
            if (options.Count == 0)
            {
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void Select(TimeAssignmentDef ta)
        {
            TimeAssignmentSelector.selectedAssignment = ta;
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
    }

    /// <summary>
    /// Vanilla draws Manage areas in the Allowed Area header, same band as extra schedule
    /// buttons. Cache that button so Date can sit after Clean without covering it.
    /// </summary>
    [HarmonyPatch(typeof(PawnColumnWorker_AllowedArea), nameof(PawnColumnWorker_AllowedArea.DoHeader))]
    public static class Patch_AllowedArea_DoHeader
    {
        public static Rect ButtonRect;

        public static void Postfix(Rect rect)
        {
            ButtonRect = new Rect(
                rect.x,
                rect.y + (rect.height - 65f),
                Mathf.Min(rect.width, 360f),
                32f);
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), nameof(JobGiver_GetRest.GetPriority))]
    public static class Patch_GetRest_GetPriority
    {
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            if (DateNightUtility.IsDateSchedule(pawn))
            {
                Need_Rest? rest = pawn.needs?.rest;
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
        public static bool Prefix(Pawn pawn, ref Job? __result)
        {
            if (!DateNightUtility.IsLovinSchedule(pawn))
            {
                return true;
            }

            if (DateNightUtility.IsBusyWithLovin(pawn))
            {
                __result = null;
                return false;
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

            Job? bedJob = DateNightUtility.TryMakeBedJob(pawn);
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

            // Trying for a baby (Biotech): Lovin comes about twice as often.
            if (DateNightBaby.IsTrying(pawn, partner) && DateNightBaby.CanConceive(pawn, partner))
            {
                __result *= 0.5f;
            }

            // A good date leaves a spark: better lovin chance for a day after.
            if (DateNightMod.Settings.postDateLovinBoost
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
            // A couple trying for a baby wants the short cooldown; it switches off
            // by itself once she is pregnant, so pregnancy-safe still holds then.
            if (DateNightUtility.IsLovinSchedule(pawn) && DateNightBaby.ActiveFor(pawn))
            {
                __result = DateNightUtility.AlwaysDoLovinCooldownTicks;
                return;
            }
            if (DateNightMod.Settings.pregnancySafeCooldown)
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
        public static bool Prefix(Pawn pawn, ref Job? __result)
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

            Pawn? partner = DateNightRomance.FindDatePartner(pawn);
            if (partner == null || !DateNightUtility.IsDateSchedule(partner)
                || !DateNightDateUtility.CanDate(pawn, partner))
            {
                return true;
            }
            __result = DateNightDateUtility.MakeDateJob(pawn, partner);
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Patch_Pawn_GetInspectString_Romance
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            string? line = DateNightRomance.InspectLine(__instance);
            if (line == null || line.Length == 0)
            {
                return;
            }
            if (__result.NullOrEmpty())
            {
                __result = line;
                return;
            }
            __result = __result + "\n" + line;
        }
    }
}
