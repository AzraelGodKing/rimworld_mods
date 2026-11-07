#nullable enable
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Per-tick lookups shared by every Date Night system (AZR-542).
    /// The colonist list and id map are built once per game tick; the love
    /// partner is resolved once per pawn per tick; the full world-pawn map is
    /// only built when an id is not a colonist (prune sweeps, old bonds).
    /// </summary>
    public static class DateNightPawns
    {
        private static int colonistTick = -1;
        private static readonly List<Pawn> colonists = new List<Pawn>();
        private static readonly Dictionary<int, Pawn> colonistById = new Dictionary<int, Pawn>();

        private static int worldTick = -1;
        private static readonly Dictionary<int, Pawn> worldById = new Dictionary<int, Pawn>();

        private static int partnerTick = -1;
        private static readonly Dictionary<int, Pawn?> loveById = new Dictionary<int, Pawn?>();

        private static int Now => Find.TickManager != null ? Find.TickManager.TicksGame : -1;

        /// <summary>Free colonists on maps, in caravans and in transporters.</summary>
        public static List<Pawn> Colonists
        {
            get
            {
                RefreshColonists();
                return colonists;
            }
        }

        public static Pawn? Colonist(int id)
        {
            RefreshColonists();
            colonistById.TryGetValue(id, out Pawn? found);
            return found;
        }

        /// <summary>Any living pawn (colonist first, then the world).</summary>
        public static Pawn? Alive(int id)
        {
            Pawn? colonist = Colonist(id);
            if (colonist != null)
            {
                return colonist;
            }

            int now = Now;
            if (worldTick != now)
            {
                worldTick = now;
                worldById.Clear();
                List<Pawn> all = PawnsFinder.AllMapsWorldAndTemporary_Alive;
                if (all != null)
                {
                    for (int i = 0; i < all.Count; i++)
                    {
                        Pawn p = all[i];
                        if (p != null)
                        {
                            worldById[p.thingIDNumber] = p;
                        }
                    }
                }
            }

            worldById.TryGetValue(id, out Pawn? found);
            return found;
        }

        /// <summary>
        /// <see cref="LovePartnerRelationUtility.ExistingMostLikedLovePartner"/>, once
        /// per pawn per tick. It walks every direct relation and compares opinions,
        /// and Date Night asked it four to six times per Lovin pawn per second.
        /// </summary>
        public static Pawn? LovePartner(Pawn pawn)
        {
            int now = Now;
            if (partnerTick != now)
            {
                partnerTick = now;
                loveById.Clear();
            }
            if (loveById.TryGetValue(pawn.thingIDNumber, out Pawn? cached))
            {
                return cached;
            }
            Pawn? found = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            loveById[pawn.thingIDNumber] = found;
            return found;
        }

        /// <summary>Relations changed mid-tick (lovers made, breakup).</summary>
        public static void InvalidatePartners()
        {
            partnerTick = -1;
        }

        /// <summary>Call from the GameComponent ctor and on load.</summary>
        public static void Reset()
        {
            colonistTick = -1;
            worldTick = -1;
            partnerTick = -1;
            colonists.Clear();
            colonistById.Clear();
            worldById.Clear();
            loveById.Clear();
        }

        private static void RefreshColonists()
        {
            int now = Now;
            if (colonistTick == now)
            {
                return;
            }
            colonistTick = now;
            colonistById.Clear();
            // Own copy: PawnsFinder refills one shared list on every call.
            colonists.Clear();
            List<Pawn>? source = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (source != null)
            {
                colonists.AddRange(source);
            }
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn p = colonists[i];
                if (p != null)
                {
                    colonistById[p.thingIDNumber] = p;
                }
            }
        }
    }
}
