using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Calendar-day anniversary of the current love bond (lover / fiancé / spouse).
    /// Letter on the day; a finished date that day is a peak mood, and letting
    /// the day pass without one stings at midnight.
    /// </summary>
    public static class DateNightAnniversaries
    {
        // couple key -> last quadrum-year we already sent the letter for
        private static Dictionary<long, int> lastCelebratedYear = new Dictionary<long, int>();
        // couple key -> year we already applied dated-or-missed outcome
        private static Dictionary<long, int> lastOutcomeYear = new Dictionary<long, int>();
        private static int lastScanDay = -1;

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref lastCelebratedYear, "dateNightAnniversaryYears",
                LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lastOutcomeYear, "dateNightAnniversaryOutcomeYears",
                LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (lastCelebratedYear == null)
                {
                    lastCelebratedYear = new Dictionary<long, int>();
                }
                if (lastOutcomeYear == null)
                {
                    lastOutcomeYear = new Dictionary<long, int>();
                }
            }
        }

        public static void Tick()
        {
            if (DateNightMod.Settings != null && !DateNightMod.Settings.enableAnniversaries)
            {
                return;
            }

            int day = GenDate.DaysPassed;
            if (day == lastScanDay)
            {
                return;
            }
            lastScanDay = day;

            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (colonists == null)
            {
                return;
            }

            var seen = new HashSet<long>();
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
                if (partner == null || pawn.thingIDNumber > partner.thingIDNumber)
                {
                    continue;
                }
                if (!LovePartnerRelationUtility.LovePartnerRelationExists(pawn, partner))
                {
                    continue;
                }

                long key = DateNightActivities.CoupleKey(pawn, partner);
                if (!seen.Add(key))
                {
                    continue;
                }

                if (WasAnniversaryYesterday(pawn, partner))
                {
                    ResolveMissedDate(pawn, partner, key);
                }

                if (!TryGetYearsTogether(pawn, partner, out int years) || years < 1)
                {
                    continue;
                }
                if (!IsAnniversaryToday(pawn, partner))
                {
                    continue;
                }

                int yearNow = CurrentYear(pawn);
                if (lastCelebratedYear.TryGetValue(key, out int already) && already >= yearNow)
                {
                    continue;
                }

                lastCelebratedYear[key] = yearNow;
                Celebrate(pawn, partner, years);
            }
        }

        public static bool IsAnniversaryToday(Pawn pawn, Pawn partner)
        {
            if (DateNightMod.Settings != null && !DateNightMod.Settings.enableAnniversaries)
            {
                return false;
            }
            DirectPawnRelation rel = GetLoveRelation(pawn, partner);
            if (rel == null)
            {
                return false;
            }

            long startAbs = Find.TickManager.gameStartAbsTick + rel.startTicks;
            float longitude = Longitude(pawn);
            int startDay = GenDate.DayOfYear(startAbs, longitude);
            int nowDay = GenDate.DayOfYear(Find.TickManager.TicksAbs, longitude);
            return startDay == nowDay
                && Find.TickManager.TicksGame - rel.startTicks >= GenDate.TicksPerYear;
        }

        public static bool WasAnniversaryYesterday(Pawn pawn, Pawn partner)
        {
            if (DateNightMod.Settings != null && !DateNightMod.Settings.enableAnniversaries)
            {
                return false;
            }
            DirectPawnRelation rel = GetLoveRelation(pawn, partner);
            if (rel == null)
            {
                return false;
            }

            long startAbs = Find.TickManager.gameStartAbsTick + rel.startTicks;
            float longitude = Longitude(pawn);
            int startDay = GenDate.DayOfYear(startAbs, longitude);
            int yesterdayAbs = Find.TickManager.TicksAbs - GenDate.TicksPerDay;
            int yesterdayDay = GenDate.DayOfYear(yesterdayAbs, longitude);
            int ticksYesterday = Find.TickManager.TicksGame - GenDate.TicksPerDay;
            return startDay == yesterdayDay
                && ticksYesterday - rel.startTicks >= GenDate.TicksPerYear;
        }

        public static void NotifyDatedOnAnniversary(Pawn pawn, Pawn partner)
        {
            if (pawn == null || partner == null)
            {
                return;
            }
            lastOutcomeYear[DateNightActivities.CoupleKey(pawn, partner)] = CurrentYear(pawn);
            RemoveAbout(pawn, DateNightDefOf.DateNight_Anniversary, partner);
            RemoveAbout(partner, DateNightDefOf.DateNight_Anniversary, pawn);
            RemoveAbout(pawn, DateNightDefOf.DateNight_AnniversaryMissed, partner);
            RemoveAbout(partner, DateNightDefOf.DateNight_AnniversaryMissed, pawn);
        }

        public static void DebugForceMissed(Pawn pawn, Pawn partner)
        {
            if (pawn == null || partner == null)
            {
                return;
            }
            GiveMissedThought(pawn, partner);
            GiveMissedThought(partner, pawn);
        }

        public static bool TryGetYearsTogether(Pawn pawn, Pawn partner, out int years)
        {
            years = 0;
            DirectPawnRelation rel = GetLoveRelation(pawn, partner);
            if (rel == null)
            {
                return false;
            }
            years = (Find.TickManager.TicksGame - rel.startTicks) / GenDate.TicksPerYear;
            return years >= 0;
        }

        public static DirectPawnRelation GetLoveRelation(Pawn pawn, Pawn partner)
        {
            if (pawn?.relations == null || partner == null)
            {
                return null;
            }

            List<DirectPawnRelation> relations = pawn.relations.DirectRelations;
            DirectPawnRelation best = null;
            for (int i = 0; i < relations.Count; i++)
            {
                DirectPawnRelation rel = relations[i];
                if (rel.otherPawn != partner)
                {
                    continue;
                }
                if (!LovePartnerRelationUtility.IsLovePartnerRelation(rel.def))
                {
                    continue;
                }
                if (best == null || rel.startTicks < best.startTicks)
                {
                    best = rel;
                }
            }
            return best;
        }

        public static void DebugForceLetter(Pawn pawn, Pawn partner)
        {
            if (pawn == null || partner == null)
            {
                return;
            }
            TryGetYearsTogether(pawn, partner, out int years);
            if (years < 1)
            {
                years = 1;
            }
            Celebrate(pawn, partner, years);
        }

        public static void DebugSetBondAgeYears(Pawn pawn, Pawn partner, int years)
        {
            DirectPawnRelation rel = GetLoveRelation(pawn, partner);
            if (rel == null)
            {
                return;
            }
            int ticks = years * GenDate.TicksPerYear;
            rel.startTicks = Find.TickManager.TicksGame - ticks;
            DirectPawnRelation other = GetLoveRelation(partner, pawn);
            if (other != null)
            {
                other.startTicks = rel.startTicks;
            }
        }

        private static void Celebrate(Pawn pawn, Pawn partner, int years)
        {
            GiveAnniversaryThought(pawn, partner);
            GiveAnniversaryThought(partner, pawn);

            string yearsText = years == 1
                ? "DateNight_OneYear".Translate()
                : "DateNight_NYears".Translate(years);
            TaggedString title = "DateNight_Letter_AnniversaryLabel".Translate();
            TaggedString body = "DateNight_Letter_AnniversaryText".Translate(
                pawn.Named("PAWN"),
                partner.Named("PARTNER"),
                yearsText.Named("YEARS"));
            Find.LetterStack.ReceiveLetter(
                title,
                body,
                LetterDefOf.PositiveEvent,
                new LookTargets(pawn, partner));
        }

        private static void GiveAnniversaryThought(Pawn pawn, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null
                || DateNightDefOf.DateNight_Anniversary == null
                || other == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }
            pawn.needs.mood.thoughts.memories.TryGainMemory(DateNightDefOf.DateNight_Anniversary, other);
        }

        private static void ResolveMissedDate(Pawn pawn, Pawn partner, long key)
        {
            int year = GenDate.Year(
                Find.TickManager.TicksAbs - GenDate.TicksPerDay, Longitude(pawn));
            if (lastOutcomeYear.TryGetValue(key, out int already) && already >= year)
            {
                RemoveAbout(pawn, DateNightDefOf.DateNight_Anniversary, partner);
                RemoveAbout(partner, DateNightDefOf.DateNight_Anniversary, pawn);
                return;
            }

            lastOutcomeYear[key] = year;
            if (HasThought(pawn, DateNightDefOf.DateNight_AnniversaryDate, partner)
                || HasThought(partner, DateNightDefOf.DateNight_AnniversaryDate, pawn))
            {
                RemoveAbout(pawn, DateNightDefOf.DateNight_Anniversary, partner);
                RemoveAbout(partner, DateNightDefOf.DateNight_Anniversary, pawn);
                return;
            }

            GiveMissedThought(pawn, partner);
            GiveMissedThought(partner, pawn);
        }

        private static void GiveMissedThought(Pawn pawn, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null
                || DateNightDefOf.DateNight_AnniversaryMissed == null
                || other == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }
            RemoveAbout(pawn, DateNightDefOf.DateNight_Anniversary, other);
            pawn.needs.mood.thoughts.memories.TryGainMemory(
                DateNightDefOf.DateNight_AnniversaryMissed, other);
        }

        private static bool HasThought(Pawn pawn, ThoughtDef def, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || def == null || other == null)
            {
                return false;
            }
            List<Thought_Memory> memories = pawn.needs.mood.thoughts.memories.Memories;
            for (int i = 0; i < memories.Count; i++)
            {
                Thought_Memory mem = memories[i];
                if (mem.def == def && mem.otherPawn == other)
                {
                    return true;
                }
            }
            return false;
        }

        private static void RemoveAbout(Pawn pawn, ThoughtDef def, Pawn other)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || def == null || other == null)
            {
                return;
            }
            MemoryThoughtHandler memories = pawn.needs.mood.thoughts.memories;
            List<Thought_Memory> list = memories.Memories;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Thought_Memory mem = list[i];
                if (mem.def == def && mem.otherPawn == other)
                {
                    memories.RemoveMemory(mem);
                }
            }
        }

        private static int CurrentYear(Pawn pawn)
        {
            return GenDate.Year(Find.TickManager.TicksAbs, Longitude(pawn));
        }

        private static float Longitude(Pawn pawn)
        {
            if (pawn?.Map != null)
            {
                return Find.WorldGrid.LongLatOf(pawn.Map.Tile).x;
            }
            if (pawn != null && pawn.Tile.Valid)
            {
                return Find.WorldGrid.LongLatOf(pawn.Tile).x;
            }
            return 0f;
        }
    }
}
