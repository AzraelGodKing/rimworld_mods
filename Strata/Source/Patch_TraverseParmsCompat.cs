using System;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace Strata
{
    // RimWorld 1.6 added avoidPersistentDanger to TraverseParms.For(Pawn, …).
    // Mods compiled against the old 6-arg signature throw MissingMethodException
    // inside JobGiver_Work / AIRobot work seekers and leave pawns (incl. mechs
    // and Misc. Robots) idle after undraft. Swallow that specific failure so
    // Strata relays and vanilla jobs keep running; the outdated mod still needs
    // an update.
    [HarmonyPatch(typeof(ThinkNode_JobGiver), nameof(ThinkNode_JobGiver.TryIssueJobPackage))]
    public static class Patch_ThinkNodeJobGiver_TraverseParmsCompat
    {
        private static int lastWarnTick = -999999;
        private static string lastWarnKey;

        public static Exception Finalizer(
            Exception __exception,
            ThinkNode_JobGiver __instance,
            Pawn pawn,
            ref ThinkResult __result)
        {
            if (__exception is not MissingMethodException mme)
            {
                return __exception;
            }

            string message = mme.Message ?? string.Empty;
            if (message.IndexOf("TraverseParms", StringComparison.Ordinal) < 0
                && message.IndexOf("TraverseParms.For", StringComparison.Ordinal) < 0)
            {
                return __exception;
            }

            string giver = __instance?.GetType().FullName ?? "unknown";
            string key = giver + "|" + (pawn?.thingIDNumber ?? 0);
            int now = Find.TickManager?.TicksGame ?? 0;
            if (key != lastWarnKey || now - lastWarnTick > 600)
            {
                lastWarnKey = key;
                lastWarnTick = now;
                Log.Warning("[Strata] Outdated mod call in " + giver
                    + " for " + (pawn?.LabelShortCap ?? "pawn")
                    + ": " + message
                    + " — usually a 1.5-era assembly calling old TraverseParms.For"
                    + " (missing avoidPersistentDanger). Update/remove that mod;"
                    + " Strata continues without this think result.");
            }

            __result = ThinkResult.NoJob;
            return null;
        }
    }
}
