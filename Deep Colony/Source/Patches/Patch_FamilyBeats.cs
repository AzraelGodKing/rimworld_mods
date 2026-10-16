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
