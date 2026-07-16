using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    [HarmonyPatch(typeof(MainTabWindow_Work), "Pawns", MethodType.Getter)]
    public static class Patch_WorkTabPawns
    {
        public static bool Prefix(ref IEnumerable<Pawn> __result)
        {
            if (!StrataColonyPawnsUtility.ShowLinkedColonists
                || !StrataColonyPawnsUtility.CanOfferLinkedColonistToggle(Find.CurrentMap))
            {
                return true;
            }
            __result = StrataColonyPawnsUtility.WorkTabPawns();
            return false;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.DoWindowContents))]
    public static class Patch_WorkTabLinkedColonistsToggle
    {
        public static void Postfix(MainTabWindow_Work __instance, Rect rect)
        {
            if (Event.current.type == EventType.Layout)
            {
                return;
            }
            StrataColonyPawnsUtility.DrawLinkedColonistsToggle(
                new Rect(rect.xMax - 150f, rect.y + 5f, 140f, 24f),
                __instance);
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Schedule), "Pawns", MethodType.Getter)]
    public static class Patch_ScheduleTabPawns
    {
        public static bool Prefix(ref IEnumerable<Pawn> __result)
        {
            if (!StrataColonyPawnsUtility.ShowLinkedColonists
                || !StrataColonyPawnsUtility.CanOfferLinkedColonistToggle(Find.CurrentMap))
            {
                return true;
            }
            __result = StrataColonyPawnsUtility.ScheduleTabPawns();
            return false;
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_Schedule), nameof(MainTabWindow_Schedule.DoWindowContents))]
    public static class Patch_ScheduleTabLinkedColonistsToggle
    {
        public static void Postfix(MainTabWindow_Schedule __instance, Rect fillRect)
        {
            if (Event.current.type == EventType.Layout)
            {
                return;
            }
            StrataColonyPawnsUtility.DrawLinkedColonistsToggle(
                new Rect(fillRect.xMax - 150f, fillRect.y + 5f, 140f, 24f),
                __instance);
        }
    }
}
