using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
    public static class Patch_Tend_FamilyLife
    {
        public static void Postfix(Pawn doctor, Pawn patient)
        {
            FamilyLifeUtility.NotifyTended(doctor, patient);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreKidnapped))]
    public static class Patch_PreKidnapped_FamilyLife
    {
        public static void Prefix(Pawn __instance)
        {
            FamilyLifeUtility.NotifyTaken(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), nameof(Pawn_GuestTracker.CapturedBy))]
    public static class Patch_CapturedBy_FamilyLife
    {
        public static void Postfix(Pawn_GuestTracker __instance, Faction by)
        {
            if (by == null || by.IsPlayer) return;
            FamilyLifeUtility.NotifyTaken(TrackerFields.GuestPawn(__instance));
        }
    }

    [HarmonyPatch(typeof(KidnappedPawnsTracker), nameof(KidnappedPawnsTracker.RemoveKidnappedPawn))]
    public static class Patch_RemoveKidnapped_FamilyLife
    {
        public static void Postfix(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.Dead)
            {
                FamilyLifeUtility.RefreshAllLastOfTheLine(announceLast: true, announceContinue: false);
                return;
            }
            FamilyLifeUtility.NotifyReturned(pawn);
        }
    }

    [HarmonyPatch(typeof(RecruitUtility), nameof(RecruitUtility.Recruit))]
    public static class Patch_Recruit_FamilyLife
    {
        public static void Postfix(Pawn pawn, Faction faction)
        {
            if (faction == null || !faction.IsPlayer) return;
            FamilyLifeUtility.NotifyReturned(pawn);
            FamilyLifeUtility.RefreshAllLastOfTheLine(announceLast: false, announceContinue: true);
        }
    }

    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Thing_Ingested
    {
        public static void Postfix(Thing __instance, Pawn ingester, float __result)
        {
            if (__result <= 0f || ingester == null) return;
            HomesteaderMealUtility.NotifyIngested(ingester, __instance);
            FamilyMealUtility.NotifyIngested(ingester, __instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
    public static class Patch_BirthdayBiological_Childhood
    {
        public static void Postfix(Pawn_AgeTracker __instance)
        {
            ChildhoodUtility.TryGrant(TrackerFields.AgePawn(__instance));
        }
    }

    [HarmonyPatch(typeof(Hediff), nameof(Hediff.PostAdd))]
    public static class Patch_Hediff_PostAdd_Vat
    {
        public static void Postfix(Hediff __instance)
        {
            if (__instance?.pawn == null || __instance.def == null) return;
            string n = __instance.def.defName;
            if (n == "VatLearning" || n == "VatGrowing" || n == "Gestated")
                ChildhoodUtility.NoteGrowthVat(__instance.pawn);
        }
    }

    [HarmonyPatch]
    public static class Patch_PlantCollected_FirstHarvest
    {
        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Plant), "PlantCollected", new[] { typeof(Pawn) })
                ?? AccessTools.Method(typeof(Plant), "PlantCollected", new[] { typeof(Pawn), typeof(bool) });
        }

        public static void Postfix(Plant __instance, Pawn by)
        {
            FamilyLetterUtility.NotifyFirstHarvest(by, __instance);
        }
    }
}
