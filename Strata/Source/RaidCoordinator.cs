using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // Pursuers on lower levels use LordJob_StrataAssault; the surface raid keeps
    // vanilla LordJob_AssaultColony. Without coordination they can split — surface
    // gives up or starts looting while underground groups keep assaulting. The
    // shallowest assault lord on the column is the authority; when it leaves the
    // assault toil, every other raid lord on linked maps mirrors that phase.
    public static class RaidCoordinator
    {
        private const int SyncInterval = 120;

        private static readonly List<Map> columnMaps = new List<Map>();
        private static readonly HashSet<Faction> factionBuffer = new HashSet<Faction>();

        public static void Tick(Map map)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.raidPursuitEnabled)
            {
                return;
            }
            if (map == null || !map.IsPlayerHome)
            {
                return;
            }
            int tick = Find.TickManager.TicksGame + map.uniqueID * 41;
            if (tick % SyncInterval != 0)
            {
                return;
            }
            SyncColumn(map);
        }

        // Immediate broadcast when a lead lord changes toil (Harmony on GotoToil).
        public static void NotifyLeadToilChanged(Lord lead)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.raidPursuitEnabled)
            {
                return;
            }
            if (lead?.Map == null || lead.faction == null || !IsRaidLord(lead))
            {
                return;
            }
            if (lead.CurLordToil is LordToil_AssaultColony)
            {
                return;
            }
            Map root = ColumnRoot(lead.Map);
            if (root == null)
            {
                return;
            }
            if (FindLeadLord(lead.faction, BuildColumn(root)) != lead)
            {
                return;
            }
            PropagatePhase(lead, BuildColumn(root));
        }

        private static void SyncColumn(Map root)
        {
            BuildColumn(root);
            if (columnMaps.Count == 0)
            {
                return;
            }

            factionBuffer.Clear();
            for (int m = 0; m < columnMaps.Count; m++)
            {
                List<Lord> lords = columnMaps[m].lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    Lord lord = lords[i];
                    if (IsRaidLord(lord))
                    {
                        factionBuffer.Add(lord.faction);
                    }
                }
            }

            foreach (Faction faction in factionBuffer)
            {
                Lord lead = FindLeadLord(faction, columnMaps);
                if (lead == null || lead.CurLordToil is LordToil_AssaultColony)
                {
                    continue;
                }
                PropagatePhase(lead, columnMaps);
            }
        }

        private static void PropagatePhase(Lord lead, List<Map> maps)
        {
            LordToil leadToil = lead.CurLordToil;
            if (leadToil == null)
            {
                return;
            }
            Faction faction = lead.faction;
            for (int m = 0; m < maps.Count; m++)
            {
                List<Lord> lords = maps[m].lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    Lord follower = lords[i];
                    if (follower == lead || follower.faction != faction || !IsRaidLord(follower))
                    {
                        continue;
                    }
                    if (follower.CurLordToil is not LordToil_AssaultColony)
                    {
                        continue;
                    }
                    MirrorToil(follower, leadToil);
                }
            }
        }

        private static void MirrorToil(Lord follower, LordToil leadToil)
        {
            LordToil target = FindMatchingToil(follower, leadToil);
            if (target != null && follower.CurLordToil != target)
            {
                follower.GotoToil(target);
            }
        }

        private static LordToil FindMatchingToil(Lord follower, LordToil leadToil)
        {
            Type leadType = leadToil.GetType();
            LordToil fallbackExit = null;
            List<LordToil> toils = follower.Graph.lordToils;
            for (int i = 0; i < toils.Count; i++)
            {
                LordToil toil = toils[i];
                if (toil.GetType() == leadType)
                {
                    return toil;
                }
                if (toil is LordToil_ExitMap)
                {
                    fallbackExit = toil;
                }
            }
            // Kidnap/steal subgraph toils share types across assault jobs; if the
            // lead retreated and we only have an exit toil, use that.
            if (leadToil is LordToil_ExitMap)
            {
                return fallbackExit;
            }
            return null;
        }

        private static Lord FindLeadLord(Faction faction, List<Map> maps)
        {
            Lord best = null;
            int bestDepth = int.MaxValue;
            int bestPawns = -1;
            bool bestVanilla = false;

            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                int depth = StrataDepth.Of(map);
                List<Lord> lords = map.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    Lord lord = lords[i];
                    if (lord.faction != faction || !IsRaidLord(lord))
                    {
                        continue;
                    }
                    bool vanilla = lord.LordJob is LordJob_AssaultColony;
                    int pawns = lord.ownedPawns.Count;
                    if (best == null
                        || depth < bestDepth
                        || (depth == bestDepth && vanilla && !bestVanilla)
                        || (depth == bestDepth && vanilla == bestVanilla && pawns > bestPawns))
                    {
                        best = lord;
                        bestDepth = depth;
                        bestVanilla = vanilla;
                        bestPawns = pawns;
                    }
                }
            }
            return best;
        }

        private static List<Map> BuildColumn(Map root)
        {
            columnMaps.Clear();
            if (root == null)
            {
                return columnMaps;
            }
            columnMaps.Add(root);
            foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(root))
            {
                columnMaps.Add(link.map);
            }
            return columnMaps;
        }

        private static Map ColumnRoot(Map map)
        {
            if (map == null)
            {
                return null;
            }
            if (map.IsPlayerHome)
            {
                return map;
            }
            if (!StrataMapUtility.IsUnderground(map))
            {
                return null;
            }
            // Underground maps belong to the player-home column above them.
            foreach (Map candidate in Find.Maps)
            {
                if (!candidate.IsPlayerHome)
                {
                    continue;
                }
                foreach (LevelGraph.LevelLink link in LevelGraph.ReachableLevels(candidate))
                {
                    if (link.map == map)
                    {
                        return candidate;
                    }
                }
            }
            return null;
        }

        private static bool IsRaidLord(Lord lord)
        {
            if (lord?.faction == null || !lord.faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }
            if (lord.LordJob is not LordJob_AssaultColony and not LordJob_StrataAssault)
            {
                return false;
            }
            return lord.ownedPawns.Count > 0;
        }
    }
}
