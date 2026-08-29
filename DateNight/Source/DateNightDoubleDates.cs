using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Two couples with overlapping Date hours share one venue. Quality is rolled
    /// once for the group; finishing spills opinion onto the other pair.
    /// </summary>
    public static class DateNightDoubleDates
    {
        private const int GuestWaitTicks = 2500;

        private static List<DoubleDateSession> sessions = new List<DoubleDateSession>();

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref sessions, "dateNightDoubleDates", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && sessions == null)
            {
                sessions = new List<DoubleDateSession>();
            }
        }

        public static bool Allowed
        {
            get { return DateNightMod.Settings == null || DateNightMod.Settings.allowDoubleDates; }
        }

        public static void Tick()
        {
            if (sessions == null || sessions.Count == 0)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                DoubleDateSession session = sessions[i];
                if (session == null)
                {
                    sessions.RemoveAt(i);
                    continue;
                }

                if (!session.noShowChecked
                    && session.guestA != 0
                    && now - session.createdTick >= GuestWaitTicks
                    && !session.GuestDating())
                {
                    session.noShowChecked = true;
                    ApplyNoShow(session);
                    session.guestA = 0;
                    session.guestB = 0;
                }

                if (session.Finished(now))
                {
                    sessions.RemoveAt(i);
                }
            }
        }

        public static void BindIfPossible(Pawn pawn, Pawn partner)
        {
            if (!Allowed || pawn?.Map == null || partner == null)
            {
                return;
            }
            if (FindSession(pawn) != null)
            {
                return;
            }

            Pawn otherA;
            Pawn otherB;
            if (!TryFindOtherCouple(pawn, partner, out otherA, out otherB))
            {
                return;
            }

            DoubleDateSession existing = FindSession(otherA) ?? FindSession(otherB);
            if (existing != null)
            {
                existing.TryJoinGuest(pawn, partner);
                return;
            }

            long mine = DateNightActivities.CoupleKey(pawn, partner);
            long theirs = DateNightActivities.CoupleKey(otherA, otherB);
            Pawn hostA = pawn;
            Pawn hostB = partner;
            Pawn guestA = otherA;
            Pawn guestB = otherB;
            if (mine > theirs)
            {
                hostA = otherA;
                hostB = otherB;
                guestA = pawn;
                guestB = partner;
            }

            sessions.Add(DoubleDateSession.Create(hostA, hostB, guestA, guestB));
        }

        public static DoubleDateSession FindSession(Pawn pawn)
        {
            if (pawn == null || sessions == null)
            {
                return null;
            }
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i] != null && sessions[i].Contains(pawn.thingIDNumber))
                {
                    return sessions[i];
                }
            }
            return null;
        }

        public static bool IsDoubleDate(Pawn pawn)
        {
            DoubleDateSession session = FindSession(pawn);
            return session != null && session.guestA != 0;
        }

        public static int StandIndex(Pawn pawn, Pawn partner)
        {
            DoubleDateSession session = FindSession(pawn);
            if (session == null)
            {
                return DateNightActivities.IsInitiator(pawn, partner) ? 0 : 1;
            }
            return session.StandIndex(pawn.thingIDNumber);
        }

        public static void NotifyFinished(Pawn pawn, Pawn partner, ThoughtDef quality)
        {
            DoubleDateSession session = FindSession(pawn);
            if (session == null || session.guestA == 0)
            {
                return;
            }
            session.MarkFinished(pawn);
            ApplySpillover(pawn, session, quality);
        }

        public static DateActivity SharedActivity(Pawn pawn, DateActivity fallback)
        {
            DoubleDateSession session = FindSession(pawn);
            if (session == null)
            {
                return fallback;
            }
            return session.activity;
        }

        public static int QualitySeed(Pawn pawn, Pawn partner)
        {
            DoubleDateSession session = FindSession(pawn);
            if (session != null)
            {
                return session.qualitySeed;
            }
            return DateNightActivities.CoupleSeed(pawn, partner);
        }

        public static bool TryGetSharedVenue(Pawn pawn, out LocalTargetInfo venue)
        {
            venue = LocalTargetInfo.Invalid;
            DoubleDateSession session = FindSession(pawn);
            if (session == null)
            {
                return false;
            }
            venue = session.VenueRoot(pawn.Map);
            return venue.IsValid;
        }

        private static bool TryFindOtherCouple(Pawn pawn, Pawn partner, out Pawn otherA, out Pawn otherB)
        {
            otherA = null;
            otherB = null;
            List<Pawn> colonists = pawn.Map.mapPawns.FreeColonistsSpawned;
            if (colonists == null)
            {
                return false;
            }

            Pawn bestA = null;
            Pawn bestB = null;
            int bestSeed = int.MaxValue;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn a = colonists[i];
                if (a == pawn || a == partner || a.Dead)
                {
                    continue;
                }
                if (!DateNightUtility.IsDateSchedule(a))
                {
                    continue;
                }

                Pawn b = LovePartnerRelationUtility.ExistingMostLikedLovePartner(a, allowDead: false);
                if (b == null || b == pawn || b == partner)
                {
                    continue;
                }
                if (a.thingIDNumber > b.thingIDNumber)
                {
                    continue;
                }
                if (!DateNightUtility.IsDateSchedule(b) || b.Map != pawn.Map)
                {
                    continue;
                }
                if (!DateNightDateUtility.CanDate(a, b, force: true))
                {
                    continue;
                }

                int seed = DateNightActivities.CoupleSeed(a, b);
                if (seed < bestSeed)
                {
                    bestSeed = seed;
                    bestA = a;
                    bestB = b;
                }
            }

            otherA = bestA;
            otherB = bestB;
            return otherA != null;
        }

        private static void ApplyNoShow(DoubleDateSession session)
        {
            Pawn a = FindPawn(session.hostA);
            Pawn b = FindPawn(session.hostB);
            GiveMood(a, DateNightDefOf.DateNight_DoubleDateNoShow);
            GiveMood(b, DateNightDefOf.DateNight_DoubleDateNoShow);
        }

        private static void ApplySpillover(Pawn pawn, DoubleDateSession session, ThoughtDef quality)
        {
            List<int> others = session.OtherCoupleIds(pawn.thingIDNumber);
            if (others == null)
            {
                return;
            }

            bool awkward = quality == DateNightDefOf.DateNight_DateAwkward
                || quality == DateNightDefOf.DateNight_DateRuined;
            for (int i = 0; i < others.Count; i++)
            {
                Pawn other = FindPawn(others[i]);
                if (other == null || other.Dead)
                {
                    continue;
                }

                int opinion = pawn.relations?.OpinionOf(other) ?? 0;
                ThoughtDef def;
                if (awkward || opinion < 0)
                {
                    def = DateNightDefOf.DateNight_DoubleDateRivals;
                }
                else
                {
                    def = DateNightDefOf.DateNight_DoubleDateFriends;
                }
                TryGain(pawn, other, def);
            }
        }

        private static void TryGain(Pawn pawn, Pawn other, ThoughtDef def)
        {
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

        private static void GiveMood(Pawn pawn, ThoughtDef def)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || def == null)
            {
                return;
            }
            pawn.needs.mood.thoughts.memories.TryGainMemory(def);
        }

        private static Pawn FindPawn(int id)
        {
            List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (pawns == null)
            {
                return null;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].thingIDNumber == id)
                {
                    return pawns[i];
                }
            }
            return null;
        }
    }

    public class DoubleDateSession : IExposable
    {
        private const int GuestWaitTicks = 2500;
        public int hostA;
        public int hostB;
        public int guestA;
        public int guestB;
        public int mapId;
        public int thingId;
        public IntVec3 cell = IntVec3.Invalid;
        public DateActivity activity = DateActivity.Hangout;
        public int qualitySeed;
        public int createdTick;
        public bool noShowChecked;
        public bool finishedHost;
        public bool finishedGuest;

        public static DoubleDateSession Create(Pawn hostA, Pawn hostB, Pawn guestA, Pawn guestB)
        {
            DateActivity activity = DateNightActivities.Resolve(hostA, hostB);
            activity = CoerceForGroup(activity);
            LocalTargetInfo venue = DateNightActivities.FindVenueRoot(activity, hostA, hostB);

            var session = new DoubleDateSession
            {
                hostA = hostA.thingIDNumber,
                hostB = hostB.thingIDNumber,
                guestA = guestA.thingIDNumber,
                guestB = guestB.thingIDNumber,
                mapId = hostA.Map.uniqueID,
                activity = activity,
                qualitySeed = Gen.HashCombineInt(
                    DateNightActivities.CoupleSeed(hostA, hostB),
                    DateNightActivities.CoupleSeed(guestA, guestB)),
                createdTick = Find.TickManager.TicksGame
            };
            session.CaptureVenue(venue);
            return session;
        }

        public bool Contains(int id)
        {
            return id == hostA || id == hostB || id == guestA || id == guestB;
        }

        public void TryJoinGuest(Pawn a, Pawn b)
        {
            if (guestA != 0)
            {
                return;
            }
            guestA = a.thingIDNumber;
            guestB = b.thingIDNumber;
        }

        public int StandIndex(int id)
        {
            if (id == hostA)
            {
                return 0;
            }
            if (id == hostB)
            {
                return 1;
            }
            if (id == guestA)
            {
                return 2;
            }
            return 3;
        }

        public LocalTargetInfo VenueRoot(Map map)
        {
            if (map == null)
            {
                return LocalTargetInfo.Invalid;
            }
            if (thingId != 0)
            {
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building.thingIDNumber == thingId)
                    {
                        return building;
                    }
                }
            }
            if (cell.IsValid && cell.InBounds(map))
            {
                return cell;
            }
            return LocalTargetInfo.Invalid;
        }

        public bool GuestDating()
        {
            return IsDating(guestA) || IsDating(guestB);
        }

        public void MarkFinished(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (id == hostA || id == hostB)
            {
                finishedHost = true;
            }
            else
            {
                finishedGuest = true;
            }
        }

        public bool Finished(int now)
        {
            if (now - createdTick > GenDate.TicksPerHour * 8)
            {
                return true;
            }
            if (guestA == 0)
            {
                return finishedHost || (!IsDating(hostA) && !IsDating(hostB) && now - createdTick > GuestWaitTicks);
            }
            return (finishedHost && finishedGuest)
                || (!IsDating(hostA) && !IsDating(hostB) && !IsDating(guestA) && !IsDating(guestB)
                    && now - createdTick > GuestWaitTicks * 2);
        }

        public List<int> OtherCoupleIds(int id)
        {
            bool host = id == hostA || id == hostB;
            if (host)
            {
                if (guestA == 0)
                {
                    return null;
                }
                return new List<int> { guestA, guestB };
            }
            return new List<int> { hostA, hostB };
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref hostA, "hostA");
            Scribe_Values.Look(ref hostB, "hostB");
            Scribe_Values.Look(ref guestA, "guestA");
            Scribe_Values.Look(ref guestB, "guestB");
            Scribe_Values.Look(ref mapId, "mapId");
            Scribe_Values.Look(ref thingId, "thingId");
            Scribe_Values.Look(ref cell, "cell");
            Scribe_Values.Look(ref activity, "activity", DateActivity.Hangout);
            Scribe_Values.Look(ref qualitySeed, "qualitySeed");
            Scribe_Values.Look(ref createdTick, "createdTick");
            Scribe_Values.Look(ref noShowChecked, "noShowChecked");
            Scribe_Values.Look(ref finishedHost, "finishedHost");
            Scribe_Values.Look(ref finishedGuest, "finishedGuest");
        }

        private void CaptureVenue(LocalTargetInfo venue)
        {
            if (venue.HasThing && venue.Thing != null)
            {
                thingId = venue.Thing.thingIDNumber;
                cell = venue.Thing.Position;
                return;
            }
            thingId = 0;
            cell = venue.IsValid ? venue.Cell : IntVec3.Invalid;
        }

        private static DateActivity CoerceForGroup(DateActivity activity)
        {
            if (activity == DateActivity.Walk || activity == DateActivity.Gift)
            {
                return DateActivity.Hangout;
            }
            return activity;
        }

        private static bool IsDating(int id)
        {
            if (id == 0)
            {
                return false;
            }
            List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            if (pawns == null)
            {
                return false;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p != null && p.thingIDNumber == id)
                {
                    return p.CurJobDef == DateNightDefOf.DateNight_GoOnDate;
                }
            }
            return false;
        }
    }
}
