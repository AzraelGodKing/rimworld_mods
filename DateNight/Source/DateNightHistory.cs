#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>One couple's finished-date record (for the romance panel and the API).</summary>
    public class CoupleHistory : IExposable
    {
        public int pawnA;
        public int pawnB;
        public int dates;
        public int lastDateTick = -1;
        public ThoughtDef? lastQuality;
        /// <summary>Wonderful dates in a row; feeds the proposal weight (AZR-574).</summary>
        public int wonderfulStreak;
        /// <summary>Biotech "trying for a baby" Lovin mode (AZR-574).</summary>
        public bool tryingForBaby;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnA, "pawnA");
            Scribe_Values.Look(ref pawnB, "pawnB");
            Scribe_Values.Look(ref dates, "dates");
            Scribe_Values.Look(ref lastDateTick, "lastDateTick", -1);
            Scribe_Defs.Look(ref lastQuality, "lastQuality");
            Scribe_Values.Look(ref wonderfulStreak, "wonderfulStreak", 0);
            Scribe_Values.Look(ref tryingForBaby, "tryingForBaby", false);
        }
    }

    /// <summary>
    /// Finished dates per couple, saved as <c>dateNightCoupleHistory</c> (new in 2.0.0;
    /// 1.x saves start empty). Kept regardless of the favourite-spot setting.
    /// </summary>
    public static class DateNightHistory
    {
        private static Dictionary<long, CoupleHistory> byCouple = new Dictionary<long, CoupleHistory>();

        public static void Reset()
        {
            byCouple = new Dictionary<long, CoupleHistory>();
        }

        public static void ExposeData()
        {
            List<CoupleHistory>? list = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                list = new List<CoupleHistory>(byCouple.Values);
            }
            Scribe_Collections.Look(ref list, "dateNightCoupleHistory", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                byCouple = new Dictionary<long, CoupleHistory>();
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        CoupleHistory h = list[i];
                        if (h != null)
                        {
                            byCouple[DateNightActivities.CoupleKey(h.pawnA, h.pawnB)] = h;
                        }
                    }
                }
            }
        }

        public static CoupleHistory GetOrCreate(Pawn a, Pawn b)
        {
            long key = DateNightActivities.CoupleKey(a, b);
            if (!byCouple.TryGetValue(key, out CoupleHistory? h))
            {
                h = new CoupleHistory { pawnA = a.thingIDNumber, pawnB = b.thingIDNumber };
                byCouple[key] = h;
            }
            return h;
        }

        /// <summary>Most recent finished date of any couple this pawn is in, or -1.</summary>
        public static CoupleHistory? LatestFor(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            CoupleHistory? best = null;
            foreach (CoupleHistory h in byCouple.Values)
            {
                if ((h.pawnA == id || h.pawnB == id) && (best == null || h.lastDateTick > best.lastDateTick))
                {
                    best = h;
                }
            }
            return best;
        }

        /// <summary>
        /// Ends "trying for a baby" once a partner is pregnant or the couple can no
        /// longer conceive (age, sterility, breakup). Called from the 2500-tick sweep.
        /// </summary>
        public static void TickBabyPlans()
        {
            foreach (CoupleHistory h in byCouple.Values)
            {
                if (!h.tryingForBaby)
                {
                    continue;
                }
                Pawn? a = DateNightDateUtility.FindPawnById(h.pawnA);
                Pawn? b = DateNightDateUtility.FindPawnById(h.pawnB);
                if (a == null || b == null || !DateNightBaby.CanConceive(a, b))
                {
                    h.tryingForBaby = false;
                }
            }
        }

        public static CoupleHistory? Get(Pawn a, Pawn b)
        {
            byCouple.TryGetValue(DateNightActivities.CoupleKey(a, b), out CoupleHistory? h);
            return h;
        }

        /// <summary>Both partners finish the same date; count it once.</summary>
        public static void NotifyFinished(Pawn pawn, Pawn partner, ThoughtDef? quality)
        {
            int now = Find.TickManager.TicksGame;
            CoupleHistory h = GetOrCreate(pawn, partner);
            if (h.lastDateTick >= 0 && now - h.lastDateTick < GenDate.TicksPerHour)
            {
                return;
            }
            h.dates++;
            h.lastDateTick = now;
            h.lastQuality = quality;
            if (quality != null && quality == DateNightDefOf.DateNight_DateWonderful)
            {
                h.wonderfulStreak++;
            }
            else
            {
                h.wonderfulStreak = 0;
            }
        }

        public static void PruneDeadPawns()
        {
            if (byCouple.Count == 0)
            {
                return;
            }
            List<long>? remove = null;
            foreach (KeyValuePair<long, CoupleHistory> kv in byCouple)
            {
                if (!DateNightDateUtility.PawnStillAlive(kv.Value.pawnA)
                    || !DateNightDateUtility.PawnStillAlive(kv.Value.pawnB))
                {
                    (remove ??= new List<long>()).Add(kv.Key);
                }
            }
            if (remove == null)
            {
                return;
            }
            for (int i = 0; i < remove.Count; i++)
            {
                byCouple.Remove(remove[i]);
            }
        }
    }
}
