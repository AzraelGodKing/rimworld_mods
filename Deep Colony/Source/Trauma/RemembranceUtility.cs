using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeepColony
{
    /// <summary>B18 — annual mood day remembering fallen colonists.</summary>
    public static class RemembranceUtility
    {
        private const int TicksPerYear = 3600000; // 60 days
        private const int CheckInterval = 2500;

        public static void NotifyColonistDied(Pawn victim)
        {
            if (victim == null) return;
            var gc = GameComp_DeepColony.Instance;
            if (gc == null) return;

            if (gc.remembranceEntries == null)
                gc.remembranceEntries = new List<RemembranceEntry>();

            gc.remembranceEntries.Add(new RemembranceEntry
            {
                deathTick = Find.TickManager.TicksGame,
                name = victim.Name?.ToStringShort ?? victim.LabelShort
            });

            // Cap history.
            while (gc.remembranceEntries.Count > 40)
                gc.remembranceEntries.RemoveAt(0);
        }

        public static void GameTick()
        {
            if (!DeepColonySettings.Get.enableTrauma) return;
            if (Find.TickManager.TicksGame % CheckInterval != 0) return;

            var gc = GameComp_DeepColony.Instance;
            if (gc?.remembranceEntries == null || gc.remembranceEntries.Count == 0) return;

            int now = Find.TickManager.TicksGame;
            int dayOfYear = (now % TicksPerYear) / 60000;
            if (gc.lastRemembranceDayOfYear == dayOfYear) return;

            RemembranceEntry match = null;
            for (int i = 0; i < gc.remembranceEntries.Count; i++)
            {
                RemembranceEntry e = gc.remembranceEntries[i];
                // Skip first year after death.
                if (now - e.deathTick < TicksPerYear) continue;
                int deathDay = (e.deathTick % TicksPerYear) / 60000;
                if (deathDay == dayOfYear)
                {
                    match = e;
                    break;
                }
            }

            if (match == null) return;
            gc.lastRemembranceDayOfYear = dayOfYear;
            FireRemembrance(match);
        }

        private static void FireRemembrance(RemembranceEntry entry)
        {
            if (DC_DefOf.DC_Thought_DayOfRemembrance == null) return;

            int applied = 0;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.FreeColonistsSpawned)
                {
                    if (p.needs?.mood?.thoughts == null) continue;
                    var thought = (Thought_Memory)ThoughtMaker.MakeThought(DC_DefOf.DC_Thought_DayOfRemembrance);
                    p.needs.mood.thoughts.memories.TryGainMemory(thought);
                    applied++;
                }
            }

            if (applied > 0)
            {
                Messages.Message(
                    "DC_DayOfRemembrance".Translate(entry.name.Named("NAME")),
                    MessageTypeDefOf.NeutralEvent,
                    false);
            }
        }
    }

    public class RemembranceEntry : IExposable
    {
        public int deathTick;
        public string name;

        public void ExposeData()
        {
            Scribe_Values.Look(ref deathTick, "deathTick", 0);
            Scribe_Values.Look(ref name, "name");
        }
    }
}
