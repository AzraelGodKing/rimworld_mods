using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Strata
{
    // Sleep across linked floors:
    // 1) Owned bed always wins — go there on any floor (ground / A+ / B+).
    // 2) Only if homeless → scan linked floors for a fully free bed and claim it.
    // Never steal a half-filled double upstairs when a bed is already assigned.
    public static class SleepRelay
    {
        private static readonly HashSet<int> bedNotFound = new HashSet<int>();

        [StrataSessionReset]
        internal static void ResetSession()
        {
            bedNotFound.Clear();
        }

        public static Job TryMakeJob(Pawn pawn, Job vanillaResult)
        {
            if (pawn == null || !pawn.Spawned
                || (StrataMod.Settings != null && !StrataMod.Settings.restRelayEnabled))
            {
                return null;
            }
            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return null;
            }

            if (ShouldYieldToCommute(pawn))
            {
                return null;
            }

            if (!WantsSleep(pawn, vanillaResult))
            {
                ClearBedNotFound(pawn);
                return null;
            }

            // Vanilla found a bed on this map — keep it only if it is THEIR bed
            // (or they are homeless). Never keep a random double-bed share.
            if (IsAcceptableVanillaBedJob(pawn, vanillaResult))
            {
                ClearBedNotFound(pawn);
                return null;
            }

            Building_Bed bed = ResolveBed(pawn);
            if (bed == null)
            {
                NoteBedNotFound(pawn);
                return null;
            }

            Job job;
            if (bed.Map == pawn.Map)
            {
                job = JobOnSameMap(pawn, bed);
            }
            else if (!CanStartPortalTrip(pawn))
            {
                job = null;
            }
            else
            {
                // Direct stair to A1 (etc.) when reachable.
                job = PawnRelay.TryRelayToMap(
                    pawn,
                    bed.Map,
                    touchCooldown: false,
                    RelayPurpose.Rest,
                    bed);
                // Stair sealed in a closed room — go G1 → B1 → G1 → A1.
                job ??= DraftedPortalPathing.TryMakeCrossMapRestDetourJob(pawn, bed);
            }

            if (job != null)
            {
                ClearBedNotFound(pawn);
            }
            else
            {
                NoteBedNotFound(pawn);
            }

            return job;
        }

        // Vanilla GetRest / ForceSleepNow often returns LayDown with no bed
        // when the assigned bed is on another map. If we cannot start a
        // commute, drop that ground nap — do not let them sleep in the dirt.
        public static bool ShouldBlockGroundSleep(Pawn pawn, Job vanillaResult)
        {
            if (pawn == null || vanillaResult == null)
            {
                return false;
            }
            if (vanillaResult.def != JobDefOf.LayDown && !vanillaResult.forceSleep)
            {
                return false;
            }
            if (vanillaResult.targetA.Thing is Building_Bed vanillaBed
                && vanillaBed.Spawned
                && vanillaBed.Map == pawn.Map)
            {
                Building_Bed owned = GetOwnedSleepBed(pawn);
                return owned != null && owned != vanillaBed && owned.Map != pawn.Map;
            }

            Building_Bed home = GetOwnedSleepBed(pawn);
            return home != null
                && home.Map != pawn.Map
                && ColonyBedUtility.MapsLinked(pawn.Map, home.Map);
        }

        public static void CollectBedNotFoundCulprits(List<GlobalTargetInfo> into)
        {
            if (into == null || bedNotFound.Count == 0)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                IReadOnlyList<Pawn> colonists = maps[i].mapPawns.FreeColonistsSpawned;
                for (int j = 0; j < colonists.Count; j++)
                {
                    Pawn pawn = colonists[j];
                    if (!bedNotFound.Contains(pawn.thingIDNumber))
                    {
                        continue;
                    }

                    if (!StillNeedsBedAlert(pawn))
                    {
                        bedNotFound.Remove(pawn.thingIDNumber);
                        continue;
                    }

                    into.Add(pawn);
                }
            }
        }

        private static bool StillNeedsBedAlert(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead || pawn.Downed || !pawn.IsFreeColonist)
            {
                return false;
            }

            if (ShouldYieldToCommute(pawn))
            {
                return false;
            }

            Building_Bed owned = GetOwnedSleepBed(pawn);
            if (owned != null)
            {
                return true;
            }

            return FindFreeBedToClaim(pawn) == null;
        }

        private static void NoteBedNotFound(Pawn pawn)
        {
            if (pawn != null && pawn.IsFreeColonist)
            {
                bedNotFound.Add(pawn.thingIDNumber);
            }
        }

        private static void ClearBedNotFound(Pawn pawn)
        {
            if (pawn != null)
            {
                bedNotFound.Remove(pawn.thingIDNumber);
            }
        }

        // Only yield for an in-progress REST commute (or walking into a portal).
        // A leftover work/food intent must not block going home to bed.
        public static bool ShouldYieldToCommute(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (PortalRelayChain.HasIntent(pawn, RelayPurpose.Rest)
                || DraftedPortalPathing.HasActiveRoute(pawn))
            {
                return true;
            }

            if (pawn.CurJobDef == JobDefOf.EnterPortal)
            {
                return true;
            }

            return pawn.CurJobDef == JobDefOf.Goto
                && pawn.CurJob?.targetA.Thing is MapPortal;
        }

        private static Building_Bed ResolveBed(Pawn pawn)
        {
            Building_Bed owned = GetOwnedSleepBed(pawn);
            if (owned != null)
            {
                return owned;
            }

            // Babies never auto-claim — childcare / assign UI own cribs. Spare
            // beds on every floor used to reassign infants every sleep tick.
            if (pawn.RaceProps.Humanlike && pawn.DevelopmentalStage.Baby())
            {
                return null;
            }

            // Homeless only — never claim over an existing assignment.
            Building_Bed free = FindFreeBedToClaim(pawn);
            if (free == null)
            {
                return null;
            }

            pawn.ownership?.ClaimBedIfNonMedical(free);
            return GetOwnedSleepBed(pawn) ?? free;
        }

        // Trust ownership. Do not reject an assigned bed because Ideology
        // CanUseBedEver / ForColonists quirks failed — that was sending people
        // upstairs into half-empty doubles and overwriting their assignment.
        private static Building_Bed GetOwnedSleepBed(Pawn pawn)
        {
            Building_Bed owned = pawn.ownership?.OwnedBed;
            if (owned == null || !owned.Spawned)
            {
                return null;
            }

            if (owned.ForPrisoners || owned.IsBurning())
            {
                return null;
            }

            if (owned.Faction != null && owned.Faction != Faction.OfPlayer)
            {
                return null;
            }

            return owned;
        }

        private static Building_Bed FindFreeBedToClaim(Pawn pawn)
        {
            Building_Bed best = FindFreeBedOnMap(pawn, pawn.Map);
            if (best != null)
            {
                return best;
            }

            if (!LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return null;
            }

            var links = new List<LevelGraph.LevelLink>(LevelGraph.ReachableLevels(pawn.Map));
            links.Sort((a, b) => a.depth.CompareTo(b.depth));

            Building_Bed nearest = null;
            int nearestDepth = int.MaxValue;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < links.Count; i++)
            {
                LevelGraph.LevelLink link = links[i];
                Building_Bed bed = FindFreeBedOnMap(pawn, link.map);
                if (bed == null)
                {
                    continue;
                }

                float dist = link.firstStep != null
                    ? link.firstStep.Position.DistanceToSquared(pawn.Position)
                    : float.MaxValue;
                if (link.depth < nearestDepth
                    || (link.depth == nearestDepth && dist < nearestDist))
                {
                    nearestDepth = link.depth;
                    nearestDist = dist;
                    nearest = bed;
                }
            }

            return nearest;
        }

        // Prefer a fully free bed; else the empty half of a love-partner's double
        // (so couples can share across floors without random upstairs stealing).
        private static Building_Bed FindFreeBedOnMap(Pawn pawn, Map map)
        {
            if (map == null)
            {
                return null;
            }

            Building_Bed bestEmpty = null;
            Building_Bed bestPartner = null;
            float bestEmptyDist = float.MaxValue;
            float bestPartnerDist = float.MaxValue;

            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Bed))
            {
                if (thing is not Building_Bed bed || !IsBaseClaimableBed(pawn, bed))
                {
                    continue;
                }

                float dist = map == pawn.Map
                    ? pawn.Position.DistanceToSquared(bed.Position)
                    : 0f;

                if (bed.OwnersForReading.Count == 0 && bed.AnyUnownedSleepingSlot)
                {
                    if (bestEmpty == null || dist < bestEmptyDist)
                    {
                        bestEmptyDist = dist;
                        bestEmpty = bed;
                    }
                }
                else if (IsLovePartnerHalfSlot(pawn, bed)
                    && (bestPartner == null || dist < bestPartnerDist))
                {
                    bestPartnerDist = dist;
                    bestPartner = bed;
                }
            }

            return bestEmpty ?? bestPartner;
        }

        private static bool IsBaseClaimableBed(Pawn pawn, Building_Bed bed)
        {
            if (bed == null
                || !bed.Spawned
                || bed.Faction != Faction.OfPlayer
                || !bed.ForColonists
                || !bed.def.building.bed_humanlike
                || bed.Medical
                || bed.ForPrisoners
                || bed.IsForbidden(pawn)
                || bed.IsBurning()
                || !RestUtility.CanUseBedEver(pawn, bed.def))
            {
                return false;
            }

            // Adults never auto-claim cribs; babies never reach claim (ResolveBed).
            if (bed.ForHumanBabies)
            {
                return false;
            }

            return true;
        }

        private static bool IsLovePartnerHalfSlot(Pawn pawn, Building_Bed bed)
        {
            if (!bed.AnyUnownedSleepingSlot || bed.OwnersForReading.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < bed.OwnersForReading.Count; i++)
            {
                Pawn owner = bed.OwnersForReading[i];
                if (owner != null && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, owner))
                {
                    return true;
                }
            }

            return false;
        }

        private static Job JobOnSameMap(Pawn pawn, Building_Bed bed)
        {
            if (pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
            {
                return JobMaker.MakeJob(JobDefOf.LayDown, bed);
            }

            if (StrataPawnUtility.CanUseLevelPortals(pawn) && !pawn.Drafted)
            {
                return DraftedPortalPathing.TryMakeRestDetourJob(pawn, bed);
            }

            return null;
        }

        // Sleep must not be gated by work-relay HasIntent / cooldown.
        private static bool CanStartPortalTrip(Pawn pawn)
        {
            return StrataPawnUtility.CanUseLevelPortals(pawn)
                && !pawn.Drafted
                && !pawn.InMentalState
                && !pawn.IsBurning()
                && LevelGraph.AnyLinkFrom(pawn.Map);
        }

        private static bool WantsSleep(Pawn pawn, Job vanillaResult)
        {
            if (vanillaResult != null
                && (vanillaResult.def == JobDefOf.LayDown || vanillaResult.forceSleep))
            {
                return true;
            }

            return PawnRelay.ShouldCommuteForRest(pawn, vanillaResult);
        }

        private static bool IsAcceptableVanillaBedJob(Pawn pawn, Job job)
        {
            if (job?.def != JobDefOf.LayDown || job.targetA.Thing is not Building_Bed bed)
            {
                return false;
            }

            if (!bed.Spawned || bed.Map != pawn.Map)
            {
                return false;
            }

            Building_Bed owned = GetOwnedSleepBed(pawn);
            if (owned != null)
            {
                return bed == owned;
            }

            // Homeless: vanilla may seat them in a free bed on this map.
            return PawnRelay.IsUsableColonistBedJob(pawn, bed)
                && bed.OwnersForReading.Count == 0;
        }
    }
}
