using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_FamilyJoin
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            if (__instance == null || map == null) return;
            FamilyJoinUtility.NotifySpawned(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_SetFaction_FamilyJoinGoodwill
    {
        public static void Prefix(Pawn __instance, Faction newFaction, out bool __state)
        {
            __state = FamilyJoinUtility.ShouldSuppressGoodwillForJoin(__instance, newFaction);
            if (__state) FamilyJoinUtility.EnterSuppress();
        }

        public static Exception Finalizer(bool __state, Exception __exception)
        {
            if (__state) FamilyJoinUtility.ExitSuppress();
            return __exception;
        }
    }

    [HarmonyPatch]
    public static class Patch_TryAffectGoodwillWith_FamilyJoin
    {
        public static bool Prepare()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Faction)))
            {
                if (m.Name == "TryAffectGoodwillWith") return true;
            }
            return false;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo m in AccessTools.GetDeclaredMethods(typeof(Faction)))
            {
                if (m.Name == "TryAffectGoodwillWith")
                    yield return m;
            }
        }

        public static bool Prefix()
        {
            return !FamilyJoinUtility.SuppressGoodwillPenalty;
        }
    }
}
