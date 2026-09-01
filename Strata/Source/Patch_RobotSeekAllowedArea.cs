using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Misc. Robots think tree runs JobGiver_SeekAllowedArea BEFORE work. Bots
    // "installed" underground with an allowed-area restriction get Goto-home
    // every think pass, so work relay to the surface never sticks. Skip that
    // pull while robot work relay wants them on another (or this) linked floor.
    // Colonists commuting home to a bed on another floor get the same skip —
    // otherwise the zone yanks them back and they sleep on the dirt.
    [HarmonyPatch(typeof(JobGiver_SeekAllowedArea), "TryGiveJob")]
    public static class Patch_RobotSeekAllowedArea_WorkRelay
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (!StrataPawnUtility.RobotShouldBypassAllowedAreaForWork(pawn)
                && !SleepRelay.ShouldBypassAllowedAreaForRest(pawn))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }
}
