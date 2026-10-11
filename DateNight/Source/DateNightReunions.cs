using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Remembers when love partners got separated (caravan, capture, another map)
    /// and gives both a one-shot "glad you're back" thought once they share a map
    /// or caravan again after a real absence.
    /// </summary>
    public static class DateNightReunions
    {
        private const int MinSeparationTicks = GenDate.TicksPerDay * 2;

        // couple key -> tick the couple was first seen apart
        private static Dictionary<long, int> separatedSince = new Dictionary<long, int>();

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref separatedSince, "dateNightSeparatedSince",
                LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && separatedSince == null)
            {
                separatedSince = new Dictionary<long, int>();
            }
        }

        /// <summary>Call from the GameComponent ctor; a new game never runs ExposeData.</summary>
        public static void Reset()
        {
            separatedSince = new Dictionary<long, int>();
        }

        public static void Tick()
        {
            List<Pawn> colonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (colonists == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            var seen = new HashSet<long>();
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
                if (partner == null)
                {
                    continue;
                }

                long key = DateNightActivities.CoupleKey(pawn, partner);
                if (!seen.Add(key))
                {
                    continue;
                }

                if (!Together(pawn, partner))
                {
                    if (!separatedSince.ContainsKey(key))
                    {
                        separatedSince[key] = now;
                    }
                    continue;
                }

                if (!separatedSince.TryGetValue(key, out int since))
                {
                    continue;
                }
                separatedSince.Remove(key);
                if (now - since >= MinSeparationTicks)
                {
                    GiveThought(pawn, partner);
                    GiveThought(partner, pawn);
                }
            }
        }

        public static void PruneDeadPawns()
        {
            if (separatedSince == null || separatedSince.Count == 0)
            {
                return;
            }

            List<long> remove = null;
            foreach (long key in separatedSince.Keys)
            {
                Pawn a = DateNightDateUtility.FindPawnById((int)(key >> 32));
                Pawn b = DateNightDateUtility.FindPawnById((int)(key & 0xffffffffL));
                if (a == null || b == null || !LovePartnerRelationUtility.LovePartnerRelationExists(a, b))
                {
                    if (remove == null)
                    {
                        remove = new List<long>();
                    }
                    remove.Add(key);
                }
            }

            if (remove == null)
            {
                return;
            }
            for (int i = 0; i < remove.Count; i++)
            {
                separatedSince.Remove(remove[i]);
            }
        }

        private static bool Together(Pawn a, Pawn b)
        {
            if (a.Dead || b.Dead)
            {
                return false;
            }
            if (a.Spawned && b.Spawned && a.Map == b.Map)
            {
                return true;
            }
            Caravan caravan = a.GetCaravan();
            return caravan != null && caravan == b.GetCaravan();
        }

        private static void GiveThought(Pawn pawn, Pawn other)
        {
            ThoughtDef def = DateNightDefOf.DateNight_Reunited;
            if (pawn?.needs?.mood?.thoughts?.memories == null || def == null || other == null)
            {
                return;
            }
            if (pawn.ageTracker == null || !pawn.ageTracker.Adult || !pawn.DevelopmentalStage.Adult())
            {
                return;
            }
            pawn.needs.mood.thoughts.memories.TryGainMemory(def, other);
        }
    }
}
