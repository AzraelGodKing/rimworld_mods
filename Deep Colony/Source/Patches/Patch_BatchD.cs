using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
    public static class Patch_Thing_Ingested_FamilyMeal
    {
        public static void Postfix(Thing __instance, Pawn ingester, float __result)
        {
            if (__result <= 0f || ingester == null) return;
            FamilyMealUtility.NotifyIngested(ingester, __instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_Reunion
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            ParentReunionUtility.NotifySpawned(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Patch_AddDirectRelation_Marriage
    {
        private static readonly AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");

        public static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (def != PawnRelationDefOf.Spouse) return;
            FamilyLetterUtility.NotifyMarriage(PawnField(__instance), otherPawn);
        }
    }

    [HarmonyPatch(typeof(Lord), nameof(Lord.Cleanup))]
    public static class Patch_Lord_Cleanup_IdeologyFuneral
    {
        public static void Prefix(Lord __instance)
        {
            if (!ModsConfig.IdeologyActive) return;
            if (__instance?.LordJob == null) return;
            FuneralUtility.NotifyIdeologyRitual(__instance.LordJob);
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
