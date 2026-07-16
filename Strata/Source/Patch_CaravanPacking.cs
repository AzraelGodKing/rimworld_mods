using HarmonyLib;
using RimWorld;
using Verse;

namespace Strata
{
    // When the caravan dialog opens on a surface home map, nudge an immediate
    // underground haul pulse so goods start moving toward the stairhead.
    [HarmonyPatch(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostOpen))]
    public static class Patch_CaravanPacking_PostOpen
    {
        public static void Postfix()
        {
            if (StrataMod.Settings?.caravanPullEnabled == true)
            {
                StrataCaravanUtility.TickCaravanPull();
            }
        }
    }
}
