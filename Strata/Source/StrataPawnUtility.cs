using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Shared checks for who may use Strata stairwells/elevators.
    //
    // Physical traversal (EnterPortal) is mostly unrestricted — vanilla prison
    // breaks, slave escapes, guest departure, and panic flee must be able to
    // climb shafts alone. See EscapePortalUtility / Patch_ExitMapThroughPortals.
    //
    // Colony work relays: free colonists, colony slaves (they have work
    // assignments), Misc. Robots, and other player tool-user workers.
    // Prisoners never get work/food/rest relay — only escape via ExitMap.
    public static class StrataPawnUtility
    {
        private static Type miscRobotType;

        private static bool miscRobotTypeResolved;

        public static bool IsMiscRobot(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }
            if (!miscRobotTypeResolved)
            {
                miscRobotType = AccessTools.TypeByName("AIRobot.X2_AIRobot");
                miscRobotTypeResolved = true;
            }
            return miscRobotType != null && miscRobotType.IsInstanceOfType(pawn);
        }

        // Who Strata may *relay* for work/food/rest/etc. Not the same as who
        // may physically walk into a stairwell during an escape.
        public static bool CanUseLevelPortals(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            // Prisoners stay out of colony relays (escape uses EscapePortalUtility).
            if (pawn.IsPrisoner)
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }
            if (pawn.IsFreeColonist)
            {
                return true;
            }
            // Ideology slaves do colony jobs — let them commute like colonists.
            if (pawn.IsSlave && pawn.workSettings?.EverWork == true)
            {
                return true;
            }
            if (IsMiscRobot(pawn))
            {
                return !MiscRobotNeedsRecharge(pawn);
            }
            if (pawn.RaceProps.ToolUser && pawn.workSettings?.EverWork == true)
            {
                return true;
            }
            return false;
        }

        public static bool MiscRobotHasWorkOn(Pawn pawn, Map map)
        {
            if (map == null || pawn?.def?.defName == null)
            {
                return false;
            }
            string defName = pawn.def.defName;
            bool omni = defName.IndexOf("Omni", StringComparison.OrdinalIgnoreCase) >= 0;

            if ((omni || defName.IndexOf("Clean", StringComparison.OrdinalIgnoreCase) >= 0)
                && WorkRelaySignals.HasCleaning(map))
            {
                return true;
            }
            if ((omni || defName.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0)
                && WorkRelaySignals.HasHauling(map))
            {
                return true;
            }
            if ((omni || defName.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                HashSet<ThingDef> wanted = LevelDemand.DefsWantedByLinkedLevels(map);
                if (wanted.Count > 0)
                {
                    foreach (ThingDef def in wanted)
                    {
                        if (map.listerThings.ThingsOfDef(def).Count > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            if ((omni || defName.IndexOf("Mine", StringComparison.OrdinalIgnoreCase) >= 0)
                && WorkRelaySignals.HasMining(map))
            {
                return true;
            }
            if ((omni || defName.IndexOf("Farm", StringComparison.OrdinalIgnoreCase) >= 0)
                && (WorkRelaySignals.HasGrowing(map) || WorkRelaySignals.HasPlantCutting(map)))
            {
                return true;
            }
            if ((omni || defName.IndexOf("Construct", StringComparison.OrdinalIgnoreCase) >= 0
                    || defName.IndexOf("Build", StringComparison.OrdinalIgnoreCase) >= 0)
                && WorkRelaySignals.HasConstruction(map))
            {
                return true;
            }
            if ((omni || defName.IndexOf("Fire", StringComparison.OrdinalIgnoreCase) >= 0)
                && WorkRelaySignals.HasFirefighting(map))
            {
                return true;
            }
            if (omni || defName.IndexOf("Craft", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Cook", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Farm", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Mine", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (BillIngredientUtility.MapHasRunnableBill(pawn, map))
                {
                    return true;
                }
            }
            // Omni bots: any of the broad colony signals.
            if (omni)
            {
                return WorkRelaySignals.HasConstruction(map)
                    || WorkRelaySignals.HasHunting(map)
                    || WorkRelaySignals.HasResearch(map)
                    || WorkRelaySignals.HasFlickWork(map);
            }
            return false;
        }

        public static bool IsWorkSeekingJobGiver(ThinkNode_JobGiver giver)
        {
            if (WorkRelaySignals.IsRegisteredWorkSeekingGiver(giver))
            {
                return true;
            }
            string name = giver.GetType().FullName ?? giver.GetType().Name;
            return name.IndexOf("JobGiver_Work", StringComparison.Ordinal) >= 0
                || name.IndexOf("AIRobot", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Clean", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Mine", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Farm", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Construct", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Craft", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Fire", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool MiscRobotNeedsRecharge(Pawn pawn)
        {
            JobDef job = pawn.CurJobDef;
            if (job?.defName != null
                && (job.defName.IndexOf("Recharge", StringComparison.OrdinalIgnoreCase) >= 0
                    || job.defName.IndexOf("Shutdown", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
            Need rest = pawn.needs?.rest;
            return rest != null && rest.CurLevelPercentage < 0.08f;
        }
    }
}
