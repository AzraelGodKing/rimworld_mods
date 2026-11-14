using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Strata
{
    // Parties, concerts and other gatherings someone can join on their own
    // only look on the pawn's own map. When nothing here takes them, look at
    // the linked floors and walk over; on arrival vanilla's think node adds
    // them to the gathering. Rituals are left to RitualTravel.
    public static class CrossFloorGatherings
    {
        // The think node runs often; look across floors at most this often per pawn.
        private const int CheckIntervalTicks = 300;

        // After walking over for a gathering, don't go for that same one again
        // this soon: vanilla may still turn them away on arrival (allowed area,
        // the gathering winding down), and they would shuttle back and forth.
        private const int SameGatheringRetryTicks = 2500;

        private static readonly Dictionary<int, int> nextCheck = new Dictionary<int, int>();
        private static readonly Dictionary<int, KeyValuePair<int, int>> lastTrip =
            new Dictionary<int, KeyValuePair<int, int>>();

        internal static void ResetSession()
        {
            nextCheck.Clear();
            lastTrip.Clear();
        }

        public static bool Enabled =>
            StrataMod.Settings == null || StrataMod.Settings.crossLevelGatheringsEnabled;

        public static Job TryJoinOnOtherFloor(Pawn pawn, ThinkTreeDutyHook hook)
        {
            if (!Enabled || pawn?.Map == null || pawn.GetLord() != null
                || pawn.Drafted || pawn.InMentalState || !pawn.RaceProps.Humanlike
                || !PawnRelay.CanRelayBasics(pawn)
                || !LevelGraph.AnyLinkFrom(pawn.Map))
            {
                return null;
            }

            int now = Find.TickManager.TicksGame;
            if (nextCheck.TryGetValue(pawn.thingIDNumber, out int due) && now < due)
            {
                return null;
            }
            nextCheck[pawn.thingIDNumber] = now + CheckIntervalTicks;

            if (PortalRelayChain.HasIntent(pawn))
            {
                return null;
            }

            Lord best = null;
            float bestPriority = 0f;
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(pawn.Map);
            for (int l = 0; l < links.Count; l++)
            {
                Map map = links[l].map;
                if (map == null || map == pawn.Map || map.Disposed)
                {
                    continue;
                }
                List<Lord> lords = map.lordManager.lords;
                for (int i = 0; i < lords.Count; i++)
                {
                    Lord lord = lords[i];
                    if (!(lord.LordJob is LordJob_VoluntarilyJoinable joinable)
                        || lord.LordJob is LordJob_Ritual
                        || lord.CurLordToil == null
                        || lord.CurLordToil.VoluntaryJoinDutyHookFor(pawn) != hook)
                    {
                        continue;
                    }
                    // Vanilla checks the spot against the pawn's own area grid;
                    // a spot outside this map's bounds would index past it.
                    if (lord.LordJob is LordJob_Joinable_Gathering at
                        && at.Spot.IsValid && !at.Spot.InBounds(pawn.Map))
                    {
                        continue;
                    }
                    if (lastTrip.TryGetValue(pawn.thingIDNumber, out KeyValuePair<int, int> trip)
                        && trip.Key == lord.loadID && now - trip.Value < SameGatheringRetryTicks)
                    {
                        continue;
                    }
                    float priority = joinable.VoluntaryJoinPriorityFor(pawn);
                    // Links come cheapest first, so ties keep the nearer floor.
                    if (priority > bestPriority)
                    {
                        best = lord;
                        bestPriority = priority;
                    }
                }
            }
            if (best == null)
            {
                return null;
            }

            IntVec3 spot = best.LordJob is LordJob_Joinable_Gathering gathering
                ? gathering.Spot
                : IntVec3.Invalid;
            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                best.Map,
                touchCooldown: false,
                RelayPurpose.Joy,
                preferArrivalNear: spot);
            if (hop != null)
            {
                lastTrip[pawn.thingIDNumber] = new KeyValuePair<int, int>(best.loadID, now);
                StrataLog.Verbose("[Strata] Gathering: " + pawn.LabelShort + " walks to " + best.Map
                    + " to join " + best.LordJob.GetType().Name + ".");
            }
            return hop;
        }
    }

    [HarmonyPatch(typeof(ThinkNode_JoinVoluntarilyJoinableLord), nameof(ThinkNode_JoinVoluntarilyJoinableLord.TryIssueJobPackage))]
    public static class Patch_JoinGatheringOnOtherFloor
    {
        public static void Postfix(
            ThinkNode_JoinVoluntarilyJoinableLord __instance,
            Pawn pawn,
            ref ThinkResult __result)
        {
            if (__result.IsValid)
            {
                return;
            }
            try
            {
                Job hop = CrossFloorGatherings.TryJoinOnOtherFloor(pawn, __instance.dutyHook);
                if (hop != null)
                {
                    __result = new ThinkResult(hop, __instance);
                }
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Joining a gathering on another floor failed: " + e.Message, 0x5B71045);
            }
        }
    }
}
