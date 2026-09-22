using System.Collections.Generic;
using RimWorld;
using Verse;

namespace LivingWorld
{
    internal static class LivingWorldRumour
    {
        public static void StampOnPublish(WorldEvent ev)
        {
            if (ev == null || ev.isCorrection)
            {
                return;
            }

            ev.channel = ChannelFor(ev);
            ev.trueKind = ev.kind;
            ev.trueFactionAName = ev.factionAName;
            ev.trueFactionBName = ev.factionBName;

            float chance = ev.channel switch
            {
                HearChannel.Radio => 0.05f,
                HearChannel.Proximity => 0.18f,
                _ => 0.55f,
            };
            if (!Rand.Chance(chance))
            {
                return;
            }

            ev.distorted = true;
            ev.correctionTick = Find.TickManager.TicksGame + Rand.RangeInclusive(15, 60) * 60000;
            Distort(ev);
        }

        public static void TickCorrections(GameComponent_LivingWorld comp)
        {
            if (comp == null)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            List<WorldEvent> due = null;
            IReadOnlyList<WorldEvent> list = comp.Chronicle;
            for (int i = 0; i < list.Count; i++)
            {
                WorldEvent ev = list[i];
                if (ev == null || !ev.distorted || ev.corrected || ev.correctionTick < 0 || now < ev.correctionTick)
                {
                    continue;
                }
                ev.corrected = true;
                ev.kind = ev.trueKind;
                ev.factionAName = ev.trueFactionAName;
                ev.factionBName = ev.trueFactionBName;
                due ??= new List<WorldEvent>();
                due.Add(new WorldEvent
                {
                    tick = now,
                    kind = WorldEventKind.Correction,
                    severity = NewsSeverity.Minor,
                    isCorrection = true,
                    factionAName = ev.trueFactionAName,
                    factionBName = ev.trueFactionBName,
                    settlementLabel = ev.settlementLabel,
                    tile = ev.tile,
                    channel = HearChannel.Radio,
                });
            }
            if (due == null)
            {
                return;
            }
            for (int i = 0; i < due.Count; i++)
            {
                comp.RecordAndPublish(due[i]);
            }
        }

        private static HearChannel ChannelFor(WorldEvent ev)
        {
            if (LivingWorldHearRules.HasCommsConsole())
            {
                return HearChannel.Radio;
            }
            if (LivingWorldHearRules.IsNearbyPublic(ev))
            {
                return HearChannel.Proximity;
            }
            return HearChannel.Rumour;
        }

        private static void Distort(WorldEvent ev)
        {
            if (ev.kind == WorldEventKind.DecisiveVictory)
            {
                ev.kind = WorldEventKind.Skirmish;
            }
            else if (ev.kind == WorldEventKind.Skirmish)
            {
                ev.kind = WorldEventKind.DecisiveVictory;
            }
            else if (ev.kind == WorldEventKind.OwnershipFlip)
            {
                ev.kind = WorldEventKind.Skirmish;
            }

            if (!string.IsNullOrEmpty(ev.factionAName) && !string.IsNullOrEmpty(ev.factionBName) && Rand.Bool)
            {
                string tmp = ev.factionAName;
                ev.factionAName = ev.factionBName;
                ev.factionBName = tmp;
            }
        }
    }
}
