using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // A bored colonist with nothing recreational on this level heads for a
    // linked level with ingestible joy or a recreation building.
    [HarmonyPatch(typeof(JobGiver_GetJoy), "TryGiveJob")]
    public static class Patch_JoyRelay
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result != null || !PawnRelay.CanRelay(pawn))
            {
                return;
            }
            if (StrataMod.Settings == null || !StrataMod.Settings.joyRelayEnabled)
            {
                return;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return;
            }
            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            foreach (LevelGraph.LevelLink link in links)
            {
                if (!PawnRelay.HasJoyFor(pawn, link.map))
                {
                    continue;
                }
                Job job = PawnRelay.TryClaimAndRelay(pawn, link, RelayPurpose.Joy, 2);
                if (job != null)
                {
                    __result = job;
                    return;
                }
            }
        }
    }
}
