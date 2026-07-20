using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nemesis
{
    public static class NemesisMood
    {
        public static void NotifyActionStruck(NemesisData data)
        {
            if (data == null || data.targetMode != NemesisTargetMode.Pawn) return;
            Pawn target = GameComponent_Nemesis.Instance?.FindTargetPawn();
            if (target?.needs?.mood?.thoughts?.memories == null) return;
            ThoughtDef spike = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_HuntedSpike");
            if (spike != null)
                target.needs.mood.thoughts.memories.TryGainMemory(spike);
        }

        public static void NotifyHuntEnded(NemesisData data, NemesisEndReason reason)
        {
            if (data == null) return;
            if (reason != NemesisEndReason.Killed && reason != NemesisEndReason.Captured
                && reason != NemesisEndReason.Cleared)
            {
                // TargetDied / HandedOver: no celebration.
                return;
            }

            ThoughtDef targetRelief = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_HuntOverTarget");
            ThoughtDef colonyRelief = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_HuntOverColony");

            if (data.targetMode == NemesisTargetMode.Pawn)
            {
                Pawn target = GameComponent_Nemesis.Instance?.FindTargetPawn();
                if (target?.needs?.mood?.thoughts?.memories != null && targetRelief != null)
                    target.needs.mood.thoughts.memories.TryGainMemory(targetRelief);
            }

            if (colonyRelief == null) return;
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                Map map = maps[m];
                if (map?.mapPawns?.FreeColonistsSpawned == null) continue;
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn p = colonists[i];
                    if (p?.needs?.mood?.thoughts?.memories == null) continue;
                    if (data.targetMode == NemesisTargetMode.Pawn && p.thingIDNumber == data.targetPawnId)
                        continue; // target already got the stronger thought
                    p.needs.mood.thoughts.memories.TryGainMemory(colonyRelief);
                }
            }
        }

        /// <summary>Short-range cellmate aura while nemesis is imprisoned (stronger on fixation target).</summary>
        public static void NotifyCellmateAura(NemesisData data, Pawn nemesis)
        {
            if (data == null || nemesis == null || !nemesis.Spawned || nemesis.Map == null) return;
            ThoughtDef aura = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_CellmateAura");
            ThoughtDef auraTarget = DefDatabase<ThoughtDef>.GetNamedSilentFail("Nemesis_CellmateAuraTarget");
            if (aura == null && auraTarget == null) return;

            List<Pawn> colonists = nemesis.Map.mapPawns?.FreeColonistsSpawned;
            if (colonists == null) return;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p?.needs?.mood?.thoughts?.memories == null) continue;
                if (p.Position.DistanceTo(nemesis.Position) > 12f) continue;
                bool isTarget = data.targetMode == NemesisTargetMode.Pawn
                    && p.thingIDNumber == data.targetPawnId;
                ThoughtDef use = isTarget && auraTarget != null ? auraTarget : aura;
                if (use != null)
                    p.needs.mood.thoughts.memories.TryGainMemory(use);
            }
        }
    }
}
