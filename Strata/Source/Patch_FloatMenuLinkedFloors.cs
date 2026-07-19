using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Vanilla float menus require the selected pawn to be on Find.CurrentMap, so
    // selecting a colonist on B1 and viewing the surface (or vice versa) shows
    // nothing when right-clicking babies, filth, etc. Allow linked floors, treat
    // those targets as reachable while the menu builds, then commute + run the
    // ordered job via CrossLevelOrderedJobs / RelayPurpose.ForcedOrder.
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ShouldGenerateFloatMenuForPawn))]
    public static class Patch_FloatMenu_AllowLinkedFloors
    {
        public static void Postfix(Pawn pawn, ref AcceptanceReport __result)
        {
            if (__result.Accepted || pawn?.Map == null || Find.CurrentMap == null)
            {
                return;
            }
            if (pawn.Map == Find.CurrentMap
                || !ColonyBedUtility.MapsLinked(pawn.Map, Find.CurrentMap)
                || !StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return;
            }

            if (pawn.Downed)
            {
                __result = "IsIncapped".Translate(pawn.LabelCap, pawn);
                return;
            }
            if (ModsConfig.BiotechActive && pawn.Deathresting)
            {
                __result = "IsDeathresting".Translate(pawn.Named("PAWN"));
                return;
            }
            Lord lord = pawn.GetLord();
            if (lord != null)
            {
                AcceptanceReport lordReport = lord.AllowsFloatMenu(pawn);
                if (!lordReport.Accepted)
                {
                    __result = lordReport;
                    return;
                }
            }
            __result = true;
        }
    }

    // While building the menu for an off-floor pawn, pretend linked-map targets
    // are reachable so prioritize / childcare options enable (real pathing is
    // the stair commute started when the option is chosen).
    [HarmonyPatch(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach), new[]
    {
        typeof(Pawn),
        typeof(LocalTargetInfo),
        typeof(PathEndMode),
        typeof(Danger),
        typeof(bool),
        typeof(bool),
        typeof(TraverseMode),
    })]
    public static class Patch_FloatMenu_CanReachLinkedTargets
    {
        public static void Postfix(Pawn pawn, LocalTargetInfo dest, ref bool __result)
        {
            if (__result || pawn?.Map == null || FloatMenuMakerMap.makingFor != pawn)
            {
                return;
            }

            Map destMap = dest.HasThing ? dest.Thing.MapHeld : Find.CurrentMap;
            if (destMap == null || destMap == pawn.Map)
            {
                return;
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, destMap))
            {
                return;
            }
            if (LevelGraph.BestFirstStep(pawn.Map, destMap, pawn.Position, pawn) == null)
            {
                return;
            }
            __result = true;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJobPrioritizedWork))]
    public static class Patch_OrderedPrioritizedWork_AcrossLevels
    {
        private static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> pawnField =
            AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

        public static bool Prefix(
            Pawn_JobTracker __instance,
            Job job,
            WorkGiver giver,
            IntVec3 cell,
            ref bool __result)
        {
            Pawn pawn = pawnField(__instance);
            if (CrossLevelOrderedJobs.TryInterceptPrioritizedWork(pawn, job, giver, cell, out __result))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
    public static class Patch_OrderedJob_AcrossLevels
    {
        private static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> pawnField =
            AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

        public static bool Prefix(
            Pawn_JobTracker __instance,
            Job job,
            JobTag? tag,
            bool requestQueueing,
            ref bool __result)
        {
            Pawn pawn = pawnField(__instance);
            // Avoid recursion when we ourselves order the EnterPortal hop.
            if (job?.def == JobDefOf.EnterPortal
                && PortalRelayChain.HasIntent(pawn, RelayPurpose.ForcedOrder))
            {
                return true;
            }

            if (CrossLevelOrderedJobs.TryInterceptOrderedJob(pawn, job, tag, requestQueueing, out __result))
            {
                return false;
            }
            return true;
        }
    }
}
