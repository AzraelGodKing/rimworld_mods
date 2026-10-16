using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Patch_AddDirectRelation
    {
        public static bool Prefix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (!TouchAverseUtility.IsLoveRelation(def)) return true;
            Pawn pawn = TrackerFields.RelationsPawn(__instance);
            if (TouchAverseUtility.CanFormLoveRelation(pawn, otherPawn))
            {
                TouchAverseUtility.NotifyLoveRelationFormed(pawn, otherPawn);
                return true;
            }
            return false;
        }

        public static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            Pawn pawn = TrackerFields.RelationsPawn(__instance);
            if (def == PawnRelationDefOf.Spouse)
                FamilyLetterUtility.NotifyMarriage(pawn, otherPawn);

            if (pawn == null || otherPawn == null || def == null) return;
            if (def == PawnRelationDefOf.Spouse)
            {
                FamilyBeatsUtility.NotifyMarriage(pawn, otherPawn);
                FamilyLifeUtility.NotifyStepFamily(pawn, otherPawn);
                return;
            }
            if (def == PawnRelationDefOf.ExLover
                || (PawnRelationDefOf.ExSpouse != null && def == PawnRelationDefOf.ExSpouse))
            {
                FamilyBeatsUtility.NotifyBreakup(pawn, otherPawn);
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_RomanceAttempt_TouchAverse
    {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(
                typeof(InteractionWorker_RomanceAttempt),
                nameof(InteractionWorker_RomanceAttempt.RandomSelectionWeight));
        }

        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            if (__result <= 0f) return;
            if (!TouchAverseUtility.CanAttemptRomance(initiator, recipient))
            {
                __result = 0f;
                return;
            }
            if (TouchAverseUtility.HasStarved(initiator)
                || TouchAverseUtility.HasStarved(recipient))
                __result *= 1.6f;
        }
    }

    [HarmonyPatch]
    public static class Patch_RomanceSuccessChance_TouchAverse
    {
        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(InteractionWorker_RomanceAttempt), "SuccessChance")
                ?? AccessTools.DeclaredMethod(typeof(InteractionWorker_RomanceAttempt), "SuccessChance");
        }

        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            if (__result <= 0f) return;
            if (!TouchAverseUtility.CanAttemptRomance(initiator, recipient))
                __result = 0f;
        }
    }

    [HarmonyPatch]
    public static class Patch_MarriageProposal_TouchAverse
    {
        public static bool Prepare() => TargetMethod() != null;

        public static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(
                typeof(InteractionWorker_MarriageProposal),
                nameof(InteractionWorker_MarriageProposal.RandomSelectionWeight));
        }

        public static void Postfix(Pawn initiator, Pawn recipient, ref float __result)
        {
            if (__result <= 0f) return;
            if (!TouchAverseUtility.CanAttemptRomance(initiator, recipient))
                __result = 0f;
        }
    }

    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.RomanceEligiblePair))]
    public static class Patch_RomanceEligiblePair_TouchAverse
    {
        public static void Postfix(Pawn initiator, Pawn target, ref AcceptanceReport __result)
        {
            if (!__result.Accepted) return;
            if (TouchAverseUtility.CanFormLoveRelation(initiator, target)) return;
            Pawn blocked = TouchAverseUtility.NeedsRomanceGate(initiator)
                && !TouchAverseUtility.MeetsRomanceTier(initiator, target)
                ? initiator
                : target;
            Pawn other = blocked == initiator ? target : initiator;
            __result = "DC_RomanceBlockedTouchTier".Translate(
                blocked.LabelShort.Named("PAWN"),
                other.LabelShort.Named("OTHER"),
                TouchAverseUtility.TierLabel(
                    TouchAverseUtility.TierOf(
                        TouchAverseUtility.GetComfort(blocked, other))).Named("TIER"),
                TouchAverseUtility.TierLabel(
                    TouchAverseUtility.RequiredRomanceTier(blocked)).Named("NEED"));
        }
    }

    /// <summary>
    /// Last so Homesteader polyarmory cannot force a bed share the averse pawn
    /// still refuses.
    /// </summary>
    [HarmonyPatch(typeof(BedUtility), nameof(BedUtility.WillingToShareBed))]
    [HarmonyPriority(Priority.Last)]
    public static class Patch_WillingToShareBed_TouchAverse
    {
        public static void Postfix(Pawn pawn1, Pawn pawn2, ref bool __result)
        {
            if (!__result) return;
            if (TouchAverseUtility.RefusesToShareBed(pawn1, pawn2))
                __result = false;
        }
    }
}
