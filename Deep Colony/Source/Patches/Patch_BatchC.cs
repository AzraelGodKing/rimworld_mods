using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    // ── C07 kids at the raid / C10 sibling disaster flavor / C05 Anomaly ─────────

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_MakeDowned_BatchC
    {
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");

        public static void Postfix(Pawn_HealthTracker __instance, DamageInfo? dinfo)
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (!dinfo.HasValue) return;

            Pawn victim = PawnField(__instance);
            if (victim == null) return;

            DamageInfo info = dinfo.Value;
            ChildRaidUtility.NotifyDowned(victim, info);
            DisasterFlavorUtility.NotifyDowned(victim, info);
            AnomalyOdysseyTraumaUtility.NotifyDowned(victim, info);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_Deathbed
    {
        public static void Prefix(Pawn __instance)
        {
            if (__instance == null || !__instance.RaceProps.Humanlike) return;
            DeathbedUtility.NotifyMentorDying(__instance);
        }
    }

    // ── C09 Homesteader meals ───────────────────────────────────────────────────

    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Thing_Ingested_GrandChef
    {
        public static void Postfix(Thing __instance, Pawn ingester, float __result)
        {
            if (__result <= 0f || ingester == null) return;
            HomesteaderMealUtility.NotifyIngested(ingester, __instance);
        }
    }

    // ── C17 adulthood ───────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
    public static class Patch_BirthdayBiological_Childhood
    {
        private static readonly AccessTools.FieldRef<Pawn_AgeTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_AgeTracker, Pawn>("pawn");

        public static void Postfix(Pawn_AgeTracker __instance)
        {
            Pawn pawn = PawnField(__instance);
            ChildhoodUtility.TryGrant(pawn);
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

    // ── C18 funerals ────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(Building_Casket), nameof(Building_Casket.TryAcceptThing))]
    public static class Patch_Casket_TryAcceptThing_Funeral
    {
        public static void Postfix(Building_Casket __instance, Thing thing, bool __result)
        {
            if (!__result || thing is not Corpse corpse) return;
            Pawn inner = corpse.InnerPawn;
            if (inner == null) return;
            FuneralUtility.NotifyBodyLaidToRest(inner, "DC_FuneralEase");
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.Destroy))]
    public static class Patch_Corpse_Destroy_Cremation
    {
        public static void Prefix(Corpse __instance, DestroyMode mode)
        {
            if (__instance?.InnerPawn == null) return;
            if (__instance.ParentHolder is Building_Casket) return;
            if (!__instance.IsBurning()) return;
            FuneralUtility.NotifyBodyLaidToRest(__instance.InnerPawn, "DC_FuneralBurn");
        }
    }

    // ── C06 Odyssey crash incidents ─────────────────────────────────────────────

    [HarmonyPatch(typeof(IncidentWorker), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_OdysseyCrash
    {
        public static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!__result) return;
            AnomalyOdysseyTraumaUtility.NotifyIncident(__instance?.def, parms);
        }
    }
}
