using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // M9: pilot console / launch ritual treat linked A+/B+ floors as reachable.
    // Vanilla PawnCanFillRole uses same-map CanReach → "NoPathToPilotConsole"
    // when the navigator is on underdeck/tower. Float-menu CanReach already has a
    // linked-floor pass during menu build; ritual role fill does not.
    public static class StrataGravshipPilotAcrossLevels
    {
        public static bool CanCommuteToPilotConsole(Pawn pawn, Thing console)
        {
            if (pawn?.Map == null || console == null || !console.Spawned)
            {
                return false;
            }
            if (!StrataPawnUtility.CanUseLevelPortals(pawn))
            {
                return false;
            }
            Map dest = console.MapHeld;
            if (dest == null)
            {
                return false;
            }
            if (dest == pawn.Map)
            {
                return pawn.CanReach(console, PathEndMode.InteractionCell, Danger.Deadly);
            }
            if (!ColonyBedUtility.MapsLinked(pawn.Map, dest)
                && !GravshipStackLinked(pawn.Map, dest))
            {
                return false;
            }
            return LevelGraph.BestFirstStep(pawn.Map, dest, pawn.Position, pawn) != null;
        }

        public static bool GravshipStackLinked(Map from, Map to)
        {
            if (from == null || to == null || !StrataGravshipUtility.OdysseyActive)
            {
                return false;
            }
            Map rootFrom = StrataGravshipUtility.FindGravshipStackRoot(from) ?? from;
            Map rootTo = StrataGravshipUtility.FindGravshipStackRoot(to) ?? to;
            return rootFrom != null && rootFrom == rootTo;
        }

        public static Building_GravEngine FindEngineForPawnMap(Map map)
        {
            if (map == null || !StrataGravshipUtility.OdysseyActive)
            {
                return null;
            }
            return StrataGravshipUtility.FindGravEngine(map)
                ?? StrataGravshipUtility.FindGravEngineOnMap(
                    StrataGravshipUtility.FindGravshipStackRoot(map));
        }
    }

    [HarmonyPatch(typeof(RitualBehaviorWorker_GravshipLaunch), nameof(RitualBehaviorWorker_GravshipLaunch.PawnCanFillRole))]
    public static class Patch_GravshipLaunch_PawnCanFillRole
    {
        public static void Postfix(
            Pawn pawn,
            RitualRole role,
            ref string reason,
            TargetInfo ritualTarget,
            ref bool __result)
        {
            if (__result || pawn == null || ritualTarget.Thing == null)
            {
                return;
            }
            // Pilot / manned roles need the console; spectators need the ship.
            if (role != null)
            {
                if (!StrataGravshipPilotAcrossLevels.CanCommuteToPilotConsole(pawn, ritualTarget.Thing))
                {
                    return;
                }
                reason = null;
                __result = true;
                return;
            }

            Building_GravEngine engine = ritualTarget.Thing.TryGetComp<CompPilotConsole>()?.engine
                ?? StrataGravshipPilotAcrossLevels.FindEngineForPawnMap(pawn.Map);
            if (engine == null)
            {
                return;
            }
            // Already counted onboard (linked deck) or can stair up to the host.
            if (StrataGravshipUtility.SafeIsOnboardGravship(
                    pawn.Position, engine, pawn, desperate: true, respectAllowedAreas: false)
                || StrataGravshipPilotAcrossLevels.CanCommuteToPilotConsole(pawn, ritualTarget.Thing))
            {
                reason = null;
                __result = true;
            }
        }
    }

    // Right-click Pilot Gravship while selected pawn is on a linked floor.
    [HarmonyPatch(typeof(CompPilotConsole), nameof(CompPilotConsole.CompFloatMenuOptions))]
    public static class Patch_PilotConsole_FloatMenuAcrossLevels
    {
        public static IEnumerable<FloatMenuOption> Postfix(
            IEnumerable<FloatMenuOption> __result,
            CompPilotConsole __instance,
            Pawn selPawn)
        {
            bool yieldedUsable = false;
            if (__result != null)
            {
                foreach (FloatMenuOption opt in __result)
                {
                    if (opt != null && opt.action != null)
                    {
                        yieldedUsable = true;
                    }
                    yield return opt;
                }
            }

            if (yieldedUsable || selPawn == null || __instance?.parent == null)
            {
                yield break;
            }

            AcceptanceReport canUse = __instance.CanUseNow();
            if (!canUse.Accepted)
            {
                yield break;
            }
            if (selPawn.skills == null
                || selPawn.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled)
            {
                yield break;
            }
            if (!StrataGravshipPilotAcrossLevels.CanCommuteToPilotConsole(selPawn, __instance.parent))
            {
                yield break;
            }

            Thing console = __instance.parent;
            yield return new FloatMenuOption(
                "PilotGravship".Translate(),
                delegate
                {
                    Job job = JobMaker.MakeJob(JobDefOf.PilotConsole, console);
                    if (!CrossLevelOrderedJobs.TryInterceptOrderedJob(
                            selPawn, job, JobTag.Misc, requestQueueing: false, out bool started)
                        || !started)
                    {
                        // Same-map fallback (should be rare here).
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                });
        }
    }

    // Board/leave jobgiver only searched GravEngine on pawn.Map — underdeck had none.
    [HarmonyPatch(typeof(JobGiver_BoardOrLeaveGravship), "TryGiveJob")]
    public static class Patch_BoardOrLeave_FindStackEngine
    {
        public static bool Prefix(Pawn pawn, ref Job __result)
        {
            if (!StrataGravshipUtility.OdysseyActive || pawn?.Map == null || pawn.Downed)
            {
                return true;
            }

            Building_GravEngine engine = StrataGravshipPilotAcrossLevels.FindEngineForPawnMap(pawn.Map);
            if (engine == null || engine.Map == pawn.Map)
            {
                // Vanilla path finds the same engine (or none).
                return true;
            }

            // Pawn is on a linked A+/B+ map; vanilla would no-op (no local engine).
            if (engine.pawnsToBoard != null && engine.pawnsToBoard.Contains(pawn))
            {
                // Linked decks already count as onboard — stand in place.
                if (StrataGravshipUtility.SafeIsOnboardGravship(
                        pawn.Position, engine, pawn, desperate: false, respectAllowedAreas: false))
                {
                    if (pawn.GetLord()?.LordJob is LordJob_Ritual ritual)
                    {
                        ritual.Cancel();
                    }
                    __result = JobMaker.MakeJob(JobDefOf.GotoShip, pawn.Position);
                    __result.locomotionUrgency = LocomotionUrgency.Jog;
                    return false;
                }

                // Off the pad but on a linked floor — stair to host, then board.
                Job hop = PawnRelay.TryRelayToMap(
                    pawn,
                    engine.Map,
                    touchCooldown: false,
                    RelayPurpose.ForcedOrder,
                    preferArrivalNear: engine.Position);
                if (hop != null)
                {
                    __result = hop;
                    return false;
                }
            }

            if (engine.pawnsToLeave != null && engine.pawnsToLeave.Contains(pawn))
            {
                // Already off host deck (on pocket) — done.
                if (!StrataGravshipUtility.SafeIsOnboardGravship(
                        pawn.Position, engine, pawn, desperate: true, respectAllowedAreas: false))
                {
                    pawn.GetLord()?.LordJob?.Notify_PawnLost(
                        pawn, PawnLostCondition.ForcedByPlayerAction);
                    __result = JobMaker.MakeJob(JobDefOf.LeaveShip, pawn.Position);
                    __result.locomotionUrgency = LocomotionUrgency.Jog;
                    return false;
                }
            }

            return true;
        }
    }
}
