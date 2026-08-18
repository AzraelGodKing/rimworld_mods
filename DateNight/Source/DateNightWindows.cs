using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DateNight
{
    /// <summary>
    /// Lovin/Date overlap windows: missed-date thoughts, made-it boost, and
    /// temporary rendezvous-bed claims that revert when the window ends.
    /// </summary>
    public static class DateNightWindows
    {
        private const int MinWaitTicks = 1500;

        private static readonly Dictionary<long, SocialWindow> Windows = new Dictionary<long, SocialWindow>();
        private static readonly Dictionary<long, BedClaim> Claims = new Dictionary<long, BedClaim>();

        public static void ExposeData()
        {
            List<BedClaim> list = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                list = new List<BedClaim>(Claims.Values);
            }

            Scribe_Collections.Look(ref list, "dateNightBedClaims", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Claims.Clear();
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        BedClaim claim = list[i];
                        if (claim != null)
                        {
                            Claims[CoupleKey(claim.pawnA, claim.pawnB)] = claim;
                        }
                    }
                }
            }
        }

        public static void NotifyScheduleTick(Pawn pawn, bool onLovin, bool onDate)
        {
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner == null)
            {
                return;
            }

            bool partnerLovin = DateNightUtility.IsLovinSchedule(partner);
            bool partnerDate = DateNightUtility.IsDateSchedule(partner);
            bool overlapLovin = onLovin && partnerLovin;
            bool overlapDate = onDate && partnerDate;

            long key = CoupleKey(pawn.thingIDNumber, partner.thingIDNumber);
            if (overlapLovin || overlapDate)
            {
                if (Windows.TryGetValue(key, out SocialWindow existing)
                    && existing.lovin != overlapLovin)
                {
                    FinalizeWindow(existing);
                    if (existing.lovin)
                    {
                        ReleaseBedClaim(key);
                    }
                    Windows.Remove(key);
                }

                if (!Windows.TryGetValue(key, out SocialWindow window))
                {
                    window = new SocialWindow
                    {
                        pawnA = pawn.thingIDNumber,
                        pawnB = partner.thingIDNumber,
                        startTick = Find.TickManager.TicksGame,
                        lovin = overlapLovin
                    };
                    Windows[key] = window;
                }

                PulseWindow(window, pawn, partner);

                if (overlapLovin && pawn.Spawned && partner.Spawned && pawn.Map == partner.Map)
                {
                    Building_Bed bed = DateNightUtility.GetRendezvousBed(pawn);
                    EnsureBedClaim(pawn, partner, bed);
                }
            }
            else if (Windows.TryGetValue(key, out SocialWindow ending)
                && !DateNightUtility.IsLovinSchedule(partner)
                && !DateNightUtility.IsDateSchedule(partner)
                && !onLovin
                && !onDate)
            {
                FinalizeWindow(ending);
                Windows.Remove(key);
                ReleaseBedClaim(key);
            }
        }

        public static void NotifyLeftSharedSchedule(Pawn pawn)
        {
            Pawn partner = LovePartnerRelationUtility.ExistingMostLikedLovePartner(pawn, allowDead: false);
            if (partner == null)
            {
                return;
            }
            if (DateNightUtility.IsLovinSchedule(partner) || DateNightUtility.IsDateSchedule(partner))
            {
                return;
            }

            long key = CoupleKey(pawn.thingIDNumber, partner.thingIDNumber);
            if (Windows.TryGetValue(key, out SocialWindow window))
            {
                FinalizeWindow(window);
                Windows.Remove(key);
            }
            ReleaseBedClaim(key);
        }

        public static void NotifyLovinSuccess(Pawn pawn, Pawn partner)
        {
            MarkSuccess(pawn, partner, lovinOnly: true);
        }

        public static void NotifyDateSuccess(Pawn pawn, Pawn partner)
        {
            MarkSuccess(pawn, partner, lovinOnly: false);
        }

        public static void EnsureBedClaim(Pawn pawn, Pawn partner, Building_Bed bed)
        {
            if (DateNightMod.Settings != null && !DateNightMod.Settings.allowWindowBedClaim)
            {
                return;
            }
            if (pawn?.ownership == null || partner?.ownership == null || bed == null)
            {
                return;
            }
            if (bed.Destroyed || !bed.Spawned || bed.Medical || bed.SleepingSlotsCount <= 1)
            {
                return;
            }

            int foreign = 0;
            bool ownedByPair = false;
            foreach (Pawn owner in bed.OwnersForReading)
            {
                if (owner == pawn || owner == partner)
                {
                    ownedByPair = true;
                }
                else
                {
                    foreign++;
                }
            }
            if (foreign > 0 && !ownedByPair)
            {
                return;
            }

            long key = CoupleKey(pawn.thingIDNumber, partner.thingIDNumber);
            if (Claims.ContainsKey(key))
            {
                return;
            }

            bool aOwns = bed.OwnersForReading.Contains(pawn);
            bool bOwns = bed.OwnersForReading.Contains(partner);
            if (aOwns && bOwns)
            {
                return;
            }

            BedClaim claim = new BedClaim
            {
                pawnA = pawn.thingIDNumber,
                pawnB = partner.thingIDNumber,
                bedId = bed.thingIDNumber,
                prevBedA = pawn.ownership.OwnedBed != null && pawn.ownership.OwnedBed != bed
                    ? pawn.ownership.OwnedBed.thingIDNumber
                    : 0,
                prevBedB = partner.ownership.OwnedBed != null && partner.ownership.OwnedBed != bed
                    ? partner.ownership.OwnedBed.thingIDNumber
                    : 0
            };

            if (!aOwns)
            {
                claim.claimedA = pawn.ownership.ClaimBedIfNonMedical(bed);
            }
            if (!bOwns)
            {
                claim.claimedB = partner.ownership.ClaimBedIfNonMedical(bed);
            }

            if (claim.claimedA || claim.claimedB)
            {
                Claims[key] = claim;
            }
        }

        private static void PulseWindow(SocialWindow window, Pawn pawn, Pawn partner)
        {
            if (window.awarded)
            {
                return;
            }

            int delta = 60;
            PulsePawn(window, pawn, partner, delta);
            PulsePawn(window, partner, pawn, delta);

            if (window.lovin)
            {
                if (pawn.CurJobDef == JobDefOf.Lovin || partner.CurJobDef == JobDefOf.Lovin)
                {
                    window.success = true;
                }
                else
                {
                    Building_Bed bed = pawn.CurrentBed();
                    if (bed != null && bed.SleepingSlotsCount > 1 && partner.CurrentBed() == bed)
                    {
                        window.success = true;
                    }
                }
            }
            else if (pawn.CurJobDef == DateNightDefOf.DateNight_GoOnDate
                || partner.CurJobDef == DateNightDefOf.DateNight_GoOnDate)
            {
                if (pawn.Spawned && partner.Spawned
                    && pawn.Map == partner.Map
                    && pawn.Position.DistanceToSquared(partner.Position) <= 64)
                {
                    window.success = true;
                }
            }
        }

        private static void PulsePawn(SocialWindow window, Pawn pawn, Pawn other, int delta)
        {
            if (pawn == null)
            {
                return;
            }

            bool unavail = IsUnavailable(pawn, other);
            bool isA = pawn.thingIDNumber == window.pawnA;
            if (unavail)
            {
                if (isA)
                {
                    window.unavailA += delta;
                }
                else
                {
                    window.unavailB += delta;
                }
            }
            else if (isA)
            {
                window.availA += delta;
            }
            else
            {
                window.availB += delta;
            }
        }

        private static void MarkSuccess(Pawn pawn, Pawn partner, bool lovinOnly)
        {
            if (pawn == null || partner == null)
            {
                return;
            }

            long key = CoupleKey(pawn.thingIDNumber, partner.thingIDNumber);
            if (!Windows.TryGetValue(key, out SocialWindow window))
            {
                window = new SocialWindow
                {
                    pawnA = pawn.thingIDNumber,
                    pawnB = partner.thingIDNumber,
                    startTick = Find.TickManager.TicksGame,
                    lovin = lovinOnly
                };
                Windows[key] = window;
            }
            window.success = true;
            if (lovinOnly && !window.awarded)
            {
                GiveThought(pawn, partner, DateNightDefOf.DateNight_MadeIt);
                GiveThought(partner, pawn, DateNightDefOf.DateNight_MadeIt);
                window.awarded = true;
            }
        }

        private static void FinalizeWindow(SocialWindow window)
        {
            if (window == null || window.awarded)
            {
                return;
            }
            window.awarded = true;

            Pawn a = FindPawn(window.pawnA);
            Pawn b = FindPawn(window.pawnB);
            if (a == null || b == null || a.Dead || b.Dead)
            {
                return;
            }

            if (window.success)
            {
                if (window.lovin)
                {
                    GiveThought(a, b, DateNightDefOf.DateNight_MadeIt);
                    GiveThought(b, a, DateNightDefOf.DateNight_MadeIt);
                }
                return;
            }

            bool aUnavail = window.unavailA > window.availA;
            bool bUnavail = window.unavailB > window.availB;
            if (window.availA >= MinWaitTicks && bUnavail && !aUnavail)
            {
                GiveThought(a, b, DateNightDefOf.DateNight_StoodUp);
            }
            if (window.availB >= MinWaitTicks && aUnavail && !bUnavail)
            {
                GiveThought(b, a, DateNightDefOf.DateNight_StoodUp);
            }
        }

        private static void ReleaseBedClaim(long key)
        {
            if (!Claims.TryGetValue(key, out BedClaim claim))
            {
                return;
            }
            Claims.Remove(key);

            Pawn a = FindPawn(claim.pawnA);
            Pawn b = FindPawn(claim.pawnB);
            Building_Bed bed = FindBed(a, b, claim.bedId);

            if (claim.claimedA && a?.ownership != null && a.ownership.OwnedBed == bed)
            {
                a.ownership.UnclaimBed();
            }
            if (claim.claimedB && b?.ownership != null && b.ownership.OwnedBed == bed)
            {
                b.ownership.UnclaimBed();
            }

            RestoreBed(a, claim.prevBedA, bed);
            RestoreBed(b, claim.prevBedB, bed);
        }

        private static void RestoreBed(Pawn pawn, int prevBedId, Building_Bed windowBed)
        {
            if (pawn?.ownership == null || prevBedId == 0 || pawn.ownership.OwnedBed != null)
            {
                return;
            }

            Building_Bed prev = FindBedOnMaps(prevBedId);
            if (prev == null || prev == windowBed || prev.Destroyed || !prev.Spawned || prev.Medical)
            {
                return;
            }
            pawn.ownership.ClaimBedIfNonMedical(prev);
        }

        private static bool IsUnavailable(Pawn pawn, Pawn waiter)
        {
            if (pawn == null || pawn.Dead)
            {
                return true;
            }
            if (pawn.Drafted || pawn.Downed)
            {
                return true;
            }
            if (!pawn.Spawned)
            {
                return true;
            }
            if (waiter != null && waiter.Spawned && pawn.Map != waiter.Map)
            {
                return true;
            }
            Building_Bed bed = pawn.CurrentBed();
            if (bed != null && bed.Medical)
            {
                return true;
            }
            return false;
        }

        private static void GiveThought(Pawn pawn, Pawn other, ThoughtDef def)
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

        private static Building_Bed FindBed(Pawn a, Pawn b, int bedId)
        {
            if (a?.ownership?.OwnedBed != null && a.ownership.OwnedBed.thingIDNumber == bedId)
            {
                return a.ownership.OwnedBed;
            }
            if (b?.ownership?.OwnedBed != null && b.ownership.OwnedBed.thingIDNumber == bedId)
            {
                return b.ownership.OwnedBed;
            }
            return FindBedOnMaps(bedId);
        }

        private static Building_Bed FindBedOnMaps(int bedId)
        {
            List<Map> maps = Find.Maps;
            for (int m = 0; m < maps.Count; m++)
            {
                foreach (Building_Bed bed in maps[m].listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
                {
                    if (bed.thingIDNumber == bedId)
                    {
                        return bed;
                    }
                }
            }
            return null;
        }

        private static long CoupleKey(int x, int y)
        {
            if (x > y)
            {
                int tmp = x;
                x = y;
                y = tmp;
            }
            return ((long)x << 32) | (uint)y;
        }

        private class SocialWindow
        {
            public int pawnA;
            public int pawnB;
            public int startTick;
            public bool lovin;
            public bool success;
            public bool awarded;
            public int availA;
            public int availB;
            public int unavailA;
            public int unavailB;
        }

        private class BedClaim : IExposable
        {
            public int pawnA;
            public int pawnB;
            public int bedId;
            public int prevBedA;
            public int prevBedB;
            public bool claimedA;
            public bool claimedB;

            public void ExposeData()
            {
                Scribe_Values.Look(ref pawnA, "pawnA");
                Scribe_Values.Look(ref pawnB, "pawnB");
                Scribe_Values.Look(ref bedId, "bedId");
                Scribe_Values.Look(ref prevBedA, "prevBedA");
                Scribe_Values.Look(ref prevBedB, "prevBedB");
                Scribe_Values.Look(ref claimedA, "claimedA");
                Scribe_Values.Look(ref claimedB, "claimedB");
            }
        }
    }
}
