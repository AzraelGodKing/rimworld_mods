using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LLMStoryteller
{
    /// <summary>Reads a home map into a <see cref="ColonySnapshot"/>. Main thread only.</summary>
    public static class ColonySnapshotReader
    {
        const int RecentEventDays = 5;
        const int MaxRecentEvents = 8;
        /// <summary>Roughly what one colonist eats per day.</summary>
        const float NutritionPerColonistDay = 1.6f;

        public static ColonySnapshot Read(Map map)
        {
            int now = Find.TickManager.TicksGame;
            List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
            var moods = colonists.Where(p => p.needs?.mood != null).Select(p => p.needs.mood.CurLevelPercentage).ToList();
            int lastBig = map.storyState.LastThreatBigTick;

            var s = new ColonySnapshot
            {
                Day = GenDate.DaysPassed + 1,
                Season = GenLocalDate.Season(map).LabelCap(),
                Biome = map.Biome.label,
                OutdoorTempC = map.mapTemperature.OutdoorTemp,
                Colonists = colonists.Count,
                AverageMoodPercent = moods.Count > 0 ? (int)(moods.Average() * 100f) : 0,
                Downed = colonists.Count(p => p.Downed),
                Prisoners = map.mapPawns.PrisonersOfColonySpawnedCount,
                Wealth = map.wealthWatcher.WealthTotal,
                ThreatPoints = StorytellerUtility.DefaultThreatPointsNow(map),
                FoodDays = map.resourceCounter.TotalHumanEdibleNutrition / (NutritionPerColonistDay * System.Math.Max(1, colonists.Count)),
                HostilesOnMap = GenHostility.AnyHostileActiveThreatToPlayer(map),
                DaysSinceBigThreat = lastBig < 0 ? -1f : (now - lastBig) / (float)GenDate.TicksPerDay
            };

            int since = now - RecentEventDays * GenDate.TicksPerDay;
            // The storyteller's own reasoning letters are already in its list of recent choices.
            string ownLetter = "LLMStoryteller_LetterLabel".Translate(Find.Storyteller.def.LabelCap);
            foreach (IArchivable a in Find.Archive.ArchivablesListForReading
                         .Where(a => a is Letter && a.CreatedTicksGame >= since && a.ArchivedLabel != ownLetter)
                         .OrderBy(a => a.CreatedTicksGame)
                         .Reverse().Take(MaxRecentEvents).Reverse())
            {
                s.RecentEvents.Add("Day " + (a.CreatedTicksGame / GenDate.TicksPerDay + 1) + ": " + a.ArchivedLabel);
            }
            return s;
        }
    }
}
