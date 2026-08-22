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
        private static readonly AccessTools.FieldRef<Pawn_GuestTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_GuestTracker, Pawn>("pawn");

        public static void Postfix(Pawn_GuestTracker __instance, Faction by)
        {
            if (by == null || by.IsPlayer) return;
            FamilyLifeUtility.NotifyTaken(PawnField(__instance));
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
                FamilyLifeUtility.RefreshAllLastOfTheLine(announceTransition: true);
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
            FamilyLifeUtility.RefreshAllLastOfTheLine(announceTransition: true);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
    public static class Patch_SetFaction_FamilyLife
    {
        public static void Postfix(Pawn __instance, Faction newFaction)
        {
            if (__instance?.RaceProps == null || !__instance.RaceProps.Humanlike) return;
            if (newFaction == null || !newFaction.IsPlayer) return;
            FamilyLifeUtility.NotifyReturned(__instance);
            FamilyLifeUtility.RefreshAllLastOfTheLine(announceTransition: true);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_SpawnSetup_FamilyLife
    {
        public static void Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            FamilyLifeUtility.NotifyReturned(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Kill_FamilyLife
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.RaceProps.Humanlike) return;
            FamilyLifeUtility.RefreshAllLastOfTheLine(announceTransition: true);
        }
    }
}
