using HarmonyLib;
using RimWorld;
using Verse;

namespace DeepColony.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    public static class Patch_Pawn_DeSpawn_Homecoming
    {
        public static void Prefix(Pawn __instance)
        {
            FamilyEchoUtility.NotifyChildMayHaveLeft(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    public static class Patch_Pawn_SpawnSetup_Homecoming
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad) return;
            FamilyEchoUtility.NotifyChildArrived(__instance, map);
            FamilyBeatsUtility.NotifySpawned(__instance);
        }
    }

    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Patch_AddDirectRelation_FamilyBeats
    {
        private static readonly AccessTools.FieldRef<Pawn_RelationsTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_RelationsTracker, Pawn>("pawn");

        public static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            Pawn pawn = PawnField(__instance);
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

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_FamilyBeats
    {
        public static void Postfix(Pawn __instance)
        {
            FamilyBeatsUtility.NotifyDied(__instance);
        }
    }

    [HarmonyPatch(typeof(ExecutionUtility), nameof(ExecutionUtility.DoExecutionByCut))]
    public static class Patch_Execution_FamilyBeats
    {
        // 1.6 signature: DoExecutionByCut(Pawn executioner, Pawn victim, int bloodPerWeight, bool spawnBlood)
        public static void Prefix(Pawn executioner, Pawn victim)
        {
            FamilyBeatsUtility.BeginExecution(victim);
        }

        public static void Postfix(Pawn executioner, Pawn victim)
        {
            FamilyBeatsUtility.EndExecution(victim, executioner);
        }
    }
}
