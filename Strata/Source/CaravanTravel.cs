using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace Strata
{
    // Cross-level caravan formation, mirroring RitualTravel:
    //
    // 1. AllSendablePawns on a Strata surface root includes colonists,
    //    prisoners, and colony animals from every linked level.
    // 2. StartFormingCaravan keeps the lord same-map only; off-map pawns walk
    //    (or are escorted) and join on arrival. Off-map downed pawns are dropped
    //    with a one-time message — they cannot climb.
    // 3. Departure to the leave toil waits while travelers for that lord are
    //    still walking; timeouts/deaths clear the hold.
    public static class CaravanTravelUtility
    {
        private static readonly List<Map> tmpNetworkMaps = new List<Map>();
        private static readonly List<Pawn> tmpRemoved = new List<Pawn>();
        private static readonly List<Pawn> tmpDownedExcluded = new List<Pawn>();
        private static readonly HashSet<Pawn> tmpSeen = new HashSet<Pawn>();

        // Prefix → Postfix handoff for one StartFormingCaravan call.
        private static readonly List<Pawn> sessionRemoved = new List<Pawn>();
        private static Map sessionFormingMap;
        private static IntVec3 sessionMeetingPoint;
        private static bool sessionActive;

        // ForceDepartNow is one-shot; re-fire when the last traveler for a lord clears.
        private static readonly HashSet<Lord> forceDepartWhenReady = new HashSet<Lord>();

        internal static void ResetSession()
        {
            sessionRemoved.Clear();
            sessionFormingMap = null;
            sessionMeetingPoint = default;
            sessionActive = false;
            forceDepartWhenReady.Clear();
            tmpRemoved.Clear();
            tmpDownedExcluded.Clear();
            tmpSeen.Clear();
            tmpNetworkMaps.Clear();
        }

        internal static bool CrossLevelCaravansActive(Map map)
        {
            if (StrataMod.Settings != null && !StrataMod.Settings.crossLevelCaravansEnabled)
            {
                return false;
            }
            if (map == null || !StrataMapUtility.IsSurfacePlayerHome(map))
            {
                return false;
            }
            return ColonyBedUtility.MapInStrataColony(map);
        }

        internal static void AppendLinkedSendable(
            Map home,
            List<Pawn> result,
            bool allowEvenIfDowned,
            bool allowEvenIfInMentalState,
            bool allowEvenIfPrisonerNotSecure,
            bool allowCapturableDownedPawns,
            bool allowLodgers,
            int allowLoadAndEnterTransportersLordForGroupID)
        {
            if (result == null || !CrossLevelCaravansActive(home))
            {
                return;
            }
            tmpSeen.Clear();
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i] != null)
                {
                    tmpSeen.Add(result[i]);
                }
            }
            ColonyBedUtility.GetColonyNetworkMaps(home, tmpNetworkMaps);
            for (int m = 0; m < tmpNetworkMaps.Count; m++)
            {
                Map map = tmpNetworkMaps[m];
                if (map == null || map == home)
                {
                    continue;
                }
                // Linked maps are not surface roots, so this Postfix early-outs
                // and we only get vanilla same-map sendables.
                List<Pawn> linked = CaravanFormingUtility.AllSendablePawns(
                    map,
                    allowEvenIfDowned,
                    allowEvenIfInMentalState,
                    allowEvenIfPrisonerNotSecure,
                    allowCapturableDownedPawns,
                    allowLodgers,
                    allowLoadAndEnterTransportersLordForGroupID);
                for (int i = 0; i < linked.Count; i++)
                {
                    Pawn pawn = linked[i];
                    if (pawn != null && tmpSeen.Add(pawn))
                    {
                        result.Add(pawn);
                    }
                }
            }
            tmpSeen.Clear();
        }

        internal static void PrefixStartForming(
            List<Pawn> pawns,
            List<Pawn> downedPawns,
            IntVec3 meetingPoint)
        {
            sessionRemoved.Clear();
            sessionFormingMap = null;
            sessionMeetingPoint = meetingPoint;
            sessionActive = false;
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }
            Map formingMap = ResolveFormingMap(pawns);
            if (!CrossLevelCaravansActive(formingMap))
            {
                return;
            }
            sessionFormingMap = formingMap;
            sessionActive = true;

            tmpDownedExcluded.Clear();
            if (downedPawns != null)
            {
                for (int i = downedPawns.Count - 1; i >= 0; i--)
                {
                    Pawn pawn = downedPawns[i];
                    if (pawn == null)
                    {
                        downedPawns.RemoveAt(i);
                        continue;
                    }
                    if (pawn.MapHeld != formingMap)
                    {
                        tmpDownedExcluded.Add(pawn);
                        downedPawns.RemoveAt(i);
                    }
                }
            }

            tmpRemoved.Clear();
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = pawns[i];
                if (pawn == null)
                {
                    pawns.RemoveAt(i);
                    continue;
                }
                if (pawn.MapHeld == formingMap)
                {
                    continue;
                }
                if (pawn.Downed)
                {
                    tmpDownedExcluded.Add(pawn);
                    pawns.RemoveAt(i);
                    continue;
                }
                tmpRemoved.Add(pawn);
                sessionRemoved.Add(pawn);
                pawns.RemoveAt(i);
            }

            if (tmpDownedExcluded.Count > 0)
            {
                Messages.Message(
                    "Strata_MessageCaravanDownedExcluded".Translate(SummarizePawns(tmpDownedExcluded)),
                    tmpDownedExcluded,
                    MessageTypeDefOf.CautionInput);
            }
            tmpDownedExcluded.Clear();
            tmpRemoved.Clear();
        }

        internal static void PostfixStartForming(List<Pawn> pawns)
        {
            if (!sessionActive || sessionFormingMap == null || sessionRemoved.Count == 0)
            {
                sessionRemoved.Clear();
                sessionActive = false;
                return;
            }
            Lord lord = FindFormingLord(sessionFormingMap, pawns);
            StrataCaravanTravel travel = StrataCaravanTravel.Get;
            if (lord == null || travel == null)
            {
                sessionRemoved.Clear();
                sessionActive = false;
                return;
            }
            for (int i = 0; i < sessionRemoved.Count; i++)
            {
                Pawn pawn = sessionRemoved[i];
                if (pawn == null || !pawn.Spawned || pawn.MapHeld == sessionFormingMap)
                {
                    continue;
                }
                if (pawn.IsFreeColonist)
                {
                    travel.Register(pawn, sessionFormingMap, lord, sessionMeetingPoint);
                }
                else if (RitualEscortUtility.NeedsEscort(pawn))
                {
                    travel.RegisterEscorted(pawn, sessionFormingMap, lord, sessionMeetingPoint);
                }
            }
            sessionRemoved.Clear();
            sessionActive = false;
        }

        internal static bool ShouldHoldDeparture(Lord lord)
        {
            if (lord?.LordJob is not LordJob_FormAndSendCaravan)
            {
                return false;
            }
            if (StrataMod.Settings != null && !StrataMod.Settings.crossLevelCaravansEnabled)
            {
                return false;
            }
            StrataCaravanTravel travel = StrataCaravanTravel.Get;
            return travel != null && travel.HasPendingFor(lord);
        }

        internal static void NoteForceDepartHeld(Lord lord)
        {
            if (lord != null)
            {
                forceDepartWhenReady.Add(lord);
            }
        }

        internal static void NotifyTravelersCleared(Lord lord)
        {
            if (lord == null || !forceDepartWhenReady.Remove(lord))
            {
                return;
            }
            if (!PortalTravelWalker.LordAlive(lord.Map, lord))
            {
                return;
            }
            if (lord.LordJob is not LordJob_FormAndSendCaravan)
            {
                return;
            }
            lord.ReceiveMemo("ForceDepartNow");
        }

        private static Map ResolveFormingMap(List<Pawn> pawns)
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                Map map = pawns[i]?.MapHeld;
                if (map != null && StrataMapUtility.IsSurfacePlayerHome(map))
                {
                    return map;
                }
            }
            return pawns[0]?.MapHeld;
        }

        private static Lord FindFormingLord(Map map, List<Pawn> pawns)
        {
            if (map?.lordManager?.lords == null)
            {
                return null;
            }
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null)
                    {
                        continue;
                    }
                    Lord owned = pawn.GetLord();
                    if (owned?.LordJob is LordJob_FormAndSendCaravan && owned.Map == map)
                    {
                        return owned;
                    }
                }
            }
            List<Lord> lords = map.lordManager.lords;
            for (int i = lords.Count - 1; i >= 0; i--)
            {
                Lord candidate = lords[i];
                if (candidate.LordJob is LordJob_FormAndSendCaravan)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static string SummarizePawns(List<Pawn> pawns)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < pawns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(pawns[i].LabelShortCap);
            }
            return sb.ToString();
        }
    }

    [HarmonyPatch(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns))]
    public static class Patch_Caravan_AllSendablePawns
    {
        public static void Postfix(
            Map map,
            bool allowEvenIfDowned,
            bool allowEvenIfInMentalState,
            bool allowEvenIfPrisonerNotSecure,
            bool allowCapturableDownedPawns,
            bool allowLodgers,
            int allowLoadAndEnterTransportersLordForGroupID,
            List<Pawn> __result)
        {
            CaravanTravelUtility.AppendLinkedSendable(
                map,
                __result,
                allowEvenIfDowned,
                allowEvenIfInMentalState,
                allowEvenIfPrisonerNotSecure,
                allowCapturableDownedPawns,
                allowLodgers,
                allowLoadAndEnterTransportersLordForGroupID);
        }
    }

    [HarmonyPatch(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.StartFormingCaravan))]
    public static class Patch_Caravan_StartForming
    {
        public static void Prefix(List<Pawn> pawns, List<Pawn> downedPawns, IntVec3 meetingPoint)
        {
            CaravanTravelUtility.PrefixStartForming(pawns, downedPawns, meetingPoint);
        }

        public static void Postfix(List<Pawn> pawns)
        {
            CaravanTravelUtility.PostfixStartForming(pawns);
        }
    }

    // CollectAnimals re-sends AllAnimalsCollected every 100 ticks, so blocking
    // that transition is safe. ForceDepartNow is one-shot — queue a re-fire.
    [HarmonyPatch(typeof(Transition), nameof(Transition.CheckSignal))]
    public static class Patch_Caravan_DepartureHold
    {
        public static void Postfix(Transition __instance, Lord lord, TriggerSignal signal, ref bool __result)
        {
            if (!__result || lord == null || __instance?.target == null)
            {
                return;
            }
            if (__instance.target is not LordToil_PrepareCaravan_Leave)
            {
                return;
            }
            if (!CaravanTravelUtility.ShouldHoldDeparture(lord))
            {
                return;
            }
            __result = false;
            if (signal.type == TriggerSignalType.Memo && signal.memo == "ForceDepartNow")
            {
                CaravanTravelUtility.NoteForceDepartHeld(lord);
            }
        }
    }

    // Walks registered caravan pawns through stairwells toward the forming
    // map, joining the lord on arrival. Same cadence and timeout as rituals.
    public class StrataCaravanTravel : WorldComponent
    {
        private class Traveler : IExposable
        {
            public Pawn pawn;
            public Map destination;
            public Lord lord;
            public Pawn escort;
            public int started;
            public bool escorted;
            public IntVec3 preferArrivalNear;

            public void ExposeData()
            {
                Scribe_References.Look(ref pawn, "pawn");
                Scribe_References.Look(ref destination, "destination");
                Scribe_References.Look(ref lord, "lord");
                Scribe_References.Look(ref escort, "escort");
                Scribe_Values.Look(ref started, "started");
                Scribe_Values.Look(ref escorted, "escorted");
                Scribe_Values.Look(ref preferArrivalNear, "preferArrivalNear");
            }
        }

        private List<Traveler> travelers = new List<Traveler>();

        public static StrataCaravanTravel Get => Find.World?.GetComponent<StrataCaravanTravel>();

        public StrataCaravanTravel(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref travelers, "strataCaravanTravelers", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                travelers ??= new List<Traveler>();
                travelers.RemoveAll(t => t?.pawn == null);
            }
        }

        public bool HasPendingFor(Lord lord)
        {
            if (lord == null || travelers.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < travelers.Count; i++)
            {
                Traveler t = travelers[i];
                if (t != null && t.lord == lord && t.pawn != null)
                {
                    return true;
                }
            }
            return false;
        }

        public void Register(Pawn pawn, Map destination, Lord lord, IntVec3 preferArrivalNear = default)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
                escorted = false,
                preferArrivalNear = preferArrivalNear,
            };
            travelers.Add(traveler);
            PortalTravelWalker.TryAdvance(pawn, destination, preferArrivalNear);
        }

        public void RegisterEscorted(Pawn pawn, Map destination, Lord lord, IntVec3 preferArrivalNear = default)
        {
            travelers.RemoveAll(t => t.pawn == pawn);
            var traveler = new Traveler
            {
                pawn = pawn,
                destination = destination,
                lord = lord,
                started = Find.TickManager.TicksGame,
                escorted = true,
                preferArrivalNear = preferArrivalNear,
            };
            travelers.Add(traveler);
            Pawn escort = traveler.escort;
            PortalTravelWalker.TryAdvanceEscorted(
                pawn, ref escort, destination, preferArrivalNear, assignments: null);
            traveler.escort = escort;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (travelers.Count == 0 || Find.TickManager.TicksGame % PortalTravelWalker.CheckInterval != 0)
            {
                return;
            }
            for (int i = travelers.Count - 1; i >= 0; i--)
            {
                Traveler t = travelers[i];
                Lord lord = t.lord;
                if (PortalTravelWalker.ShouldAbandon(t.pawn, t.destination, t.lord, t.started))
                {
                    travelers.RemoveAt(i);
                    NotifyIfCleared(lord);
                    continue;
                }
                if (!t.pawn.Spawned)
                {
                    continue;
                }
                if (t.pawn.Map == t.destination)
                {
                    JoinLord(t);
                    travelers.RemoveAt(i);
                    NotifyIfCleared(lord);
                    continue;
                }
                if (t.escorted)
                {
                    Pawn escort = t.escort;
                    PortalTravelWalker.TryAdvanceEscorted(
                        t.pawn, ref escort, t.destination, t.preferArrivalNear, assignments: null);
                    t.escort = escort;
                }
                else
                {
                    PortalTravelWalker.TryAdvance(t.pawn, t.destination, t.preferArrivalNear);
                }
            }
        }

        private void NotifyIfCleared(Lord lord)
        {
            if (lord != null && !HasPendingFor(lord))
            {
                CaravanTravelUtility.NotifyTravelersCleared(lord);
            }
        }

        private static void JoinLord(Traveler t)
        {
            if (t.lord == null || t.pawn == null)
            {
                return;
            }
            if (t.lord.LordJob is not LordJob_FormAndSendCaravan)
            {
                return;
            }
            if (!PortalTravelWalker.LordAlive(t.destination, t.lord))
            {
                return;
            }
            if (!t.lord.ownedPawns.Contains(t.pawn) && t.lord.CanAddPawn(t.pawn))
            {
                t.lord.AddPawn(t.pawn);
            }
        }
    }
}
