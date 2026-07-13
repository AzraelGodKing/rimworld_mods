using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    // Shared checks for who may use Strata stairwells/elevators. Colonists,
    // Misc. Robots, and other player workers are included; raiders and guests are not.
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

        public static bool CanUseLevelPortals(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed)
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer || pawn.IsPrisoner || pawn.IsSlave)
            {
                return false;
            }
            if (pawn.IsFreeColonist)
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
            if (defName.IndexOf("Clean", StringComparison.OrdinalIgnoreCase) >= 0
                && map.listerFilthInHomeArea.FilthInHomeArea.Count > 0)
            {
                return true;
            }
            if (defName.IndexOf("Craft", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Cook", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Farm", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Mine", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Omni", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (BillIngredientUtility.MapHasRunnableBill(pawn, map))
                {
                    return true;
                }
            }
            if (defName.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0)
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
            if (map.listerHaulables.ThingsPotentiallyNeedingHauling().Count > 0
                && (defName.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0
                    || defName.IndexOf("Omni", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
            if (defName.IndexOf("Omni", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return map.listerFilthInHomeArea.FilthInHomeArea.Count > 0;
            }
            return defName.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0
                && map.listerHaulables.ThingsPotentiallyNeedingHauling().Count > 0;
        }

        public static bool IsWorkSeekingJobGiver(ThinkNode_JobGiver giver)
        {
            string name = giver.GetType().FullName ?? giver.GetType().Name;
            return name.IndexOf("JobGiver_Work", StringComparison.Ordinal) >= 0
                || name.IndexOf("AIRobot", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Haul", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Clean", StringComparison.OrdinalIgnoreCase) >= 0;
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
