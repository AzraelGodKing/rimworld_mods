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

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnA, "pawnA");
            Scribe_Values.Look(ref pawnB, "pawnB");
            Scribe_Values.Look(ref dates, "dates");
            Scribe_Values.Look(ref lastDateTick, "lastDateTick", -1);
            Scribe_Defs.Look(ref lastQuality, "lastQuality");
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

        public static CoupleHistory? Get(Pawn a, Pawn b)
        {
            byCouple.TryGetValue(DateNightActivities.CoupleKey(a, b), out CoupleHistory? h);
            return h;
        }

        /// <summary>Both partners finish the same date; count it once.</summary>
        public static void NotifyFinished(Pawn pawn, Pawn partner, ThoughtDef? quality)
        {
            long key = DateNightActivities.CoupleKey(pawn, partner);
            int now = Find.TickManager.TicksGame;
            if (!byCouple.TryGetValue(key, out CoupleHistory? h))
            {
                h = new CoupleHistory { pawnA = pawn.thingIDNumber, pawnB = partner.thingIDNumber };
                byCouple[key] = h;
            }
            if (h.lastDateTick >= 0 && now - h.lastDateTick < GenDate.TicksPerHour)
            {
                return;
            }
            h.dates++;
            h.lastDateTick = now;
            h.lastQuality = quality;
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
