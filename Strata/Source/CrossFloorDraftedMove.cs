using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Strata
{
    // Drafted "go here" for colonists standing on another linked floor than the
    // one in view: walk them through the stairs, still drafted, then go to the
    // clicked cell. A group drag spreads them along the line as usual.
    // Shift-clicks on the same floor queue more stops for the arrival.
    public static class CrossFloorDraftedMove
    {
        private class Trip
        {
            public int destMapId;
            public readonly List<IntVec3> cells = new List<IntVec3>();
        }

        private static readonly Dictionary<int, Trip> trips = new Dictionary<int, Trip>();

        internal static void ResetSession()
        {
            trips.Clear();
        }

        // The floor in view, when it is not the pawn's own and a drafted order
        // there should become a stair trip.
        public static bool OrderingFromOtherFloor(Pawn pawn, out Map viewed)
        {
            viewed = Find.CurrentMap;
            if (!CrossFloorSelection.Enabled || pawn?.Map == null || viewed == null || pawn.Map == viewed)
            {
                return false;
            }
            return StrataPawnUtility.CanUseLevelPortals(pawn) && ColonyBedUtility.MapsLinked(pawn.Map, viewed);
        }

        public static bool CanGo(Pawn pawn, Map destMap, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(destMap) || !cell.Standable(destMap))
            {
                return false;
            }
            return LevelGraph.BestFirstStep(pawn.Map, destMap, pawn.Position, pawn, cell) != null;
        }

        // From the drafted move action. True when the order was handled here.
        public static bool TryOrder(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
        {
            if (!OrderingFromOtherFloor(pawn, out Map destMap))
            {
                return false;
            }

            IntVec3 dest = PickCell(destMap, clickCell, gotoLoc);
            if (!dest.IsValid || LevelGraph.BestFirstStep(pawn.Map, destMap, pawn.Position, pawn, dest) == null)
            {
                Messages.Message("CannotGoNoPath".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                return true;
            }

            bool queue = KeyBindingDefOf.QueueOrder.IsDownEvent;
            if (queue
                && trips.TryGetValue(pawn.thingIDNumber, out Trip running)
                && running.destMapId == destMap.uniqueID
                && PortalRelayChain.HasIntent(pawn, RelayPurpose.ForcedOrder))
            {
                running.cells.Add(dest);
                FleckMaker.Static(dest, destMap, FleckDefOf.FeedbackGoto);
                return true;
            }

            Job hop = PawnRelay.TryRelayToMap(
                pawn,
                destMap,
                touchCooldown: false,
                RelayPurpose.ForcedOrder,
                preferArrivalNear: dest);
            if (hop == null)
            {
                Messages.Message("CannotGoNoPath".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                return true;
            }

            var trip = new Trip { destMapId = destMap.uniqueID };
            trip.cells.Add(dest);
            trips[pawn.thingIDNumber] = trip;
            if (!pawn.jobs.TryTakeOrderedJob(hop, JobTag.DraftedOrder))
            {
                trips.Remove(pawn.thingIDNumber);
                PortalRelayChain.ClearIntent(pawn);
                return true;
            }
            FleckMaker.Static(dest, destMap, FleckDefOf.FeedbackGoto);
            StrataLog.Verbose("[Strata] Drafted move: " + pawn.LabelShort + " heads to " + destMap + " " + dest + ".");
            return true;
        }

        // Called when a ForcedOrder stair trip ends. True when it was ours.
        public static bool TryFinish(Pawn pawn)
        {
            if (pawn?.Map == null || !trips.TryGetValue(pawn.thingIDNumber, out Trip trip))
            {
                return false;
            }
            trips.Remove(pawn.thingIDNumber);
            if (pawn.Map.uniqueID != trip.destMapId)
            {
                return false;
            }
            // Undrafted on the way: the player took over; let normal AI run.
            if (!pawn.Drafted)
            {
                return true;
            }

            bool first = true;
            for (int i = 0; i < trip.cells.Count; i++)
            {
                IntVec3 cell = trip.cells[i];
                if (!cell.InBounds(pawn.Map) || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }
                Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
                if (first)
                {
                    first = false;
                    if (!pawn.jobs.TryTakeOrderedJob(job, JobTag.DraftedOrder))
                    {
                        return true;
                    }
                }
                else
                {
                    pawn.jobs.jobQueue.EnqueueLast(job, JobTag.DraftedOrder);
                }
            }
            return true;
        }

        private static IntVec3 PickCell(Map destMap, IntVec3 clickCell, IntVec3 gotoLoc)
        {
            // gotoLoc was worked out against the pawn's own floor; trust it only
            // when it is a real spot near the click on the floor in view.
            if (gotoLoc.IsValid && gotoLoc.InBounds(destMap) && gotoLoc.Standable(destMap)
                && (!clickCell.IsValid || gotoLoc.DistanceToSquared(clickCell) <= 16))
            {
                return gotoLoc;
            }
            if (!clickCell.IsValid || !clickCell.InBounds(destMap))
            {
                return IntVec3.Invalid;
            }
            return CellFinder.StandableCellNear(clickCell, destMap, 2.9f);
        }

        // Group drag: vanilla spreads the line on each pawn's own map. Redo it
        // for pawns on another floor, on the floor in view.
        internal static void FixGroupDestinations(List<Pawn> pawns, List<IntVec3> dests, IntVec3 start, IntVec3 end)
        {
            Map viewed = Find.CurrentMap;
            if (viewed == null || !CrossFloorSelection.Enabled)
            {
                return;
            }
            float span = pawns.Count <= 1 ? 1f : pawns.Count - 1;
            for (int j = 0; j < pawns.Count; j++)
            {
                Pawn pawn = pawns[j];
                if (pawn == null || !pawn.Spawned || pawn.Map == viewed)
                {
                    continue;
                }
                dests[j] = IntVec3.Invalid;
                if (!OrderingFromOtherFloor(pawn, out _))
                {
                    continue;
                }
                IntVec3 root = (start.ToVector3() + (end.ToVector3() - start.ToVector3()) * (j / span)).ToIntVec3();
                dests[j] = FreeStandableNear(viewed, root, dests);
            }
        }

        private static IntVec3 FreeStandableNear(Map map, IntVec3 root, List<IntVec3> taken)
        {
            int cells = GenRadial.NumCellsInRadius(6f);
            for (int i = 0; i < cells; i++)
            {
                IntVec3 c = root + GenRadial.RadialPattern[i];
                if (c.InBounds(map) && c.Standable(map) && !c.Fogged(map) && !taken.Contains(c))
                {
                    return c;
                }
            }
            return IntVec3.Invalid;
        }
    }

    // Single drafted pawn on another floor: vanilla's "Go here" works out the
    // spot on the pawn's own map, which can be the wrong size. Order the trip
    // to the clicked cell on the floor in view instead.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedMove), "GetSingleOption")]
    public static class Patch_DraftedMoveOption_OtherFloor
    {
        public static void Postfix(FloatMenuContext context, FloatMenuOption __result)
        {
            if (__result == null || __result.Disabled || context == null || context.IsMultiselect)
            {
                return;
            }
            Pawn pawn = context.FirstSelectedPawn;
            if (pawn == null || !CrossFloorDraftedMove.OrderingFromOtherFloor(pawn, out _))
            {
                return;
            }
            IntVec3 click = context.ClickedCell;
            __result.action = () => CrossFloorDraftedMove.TryOrder(click, pawn, IntVec3.Invalid);
        }
    }

    [HarmonyPatch(typeof(MultiPawnGotoController), nameof(MultiPawnGotoController.RecomputeDestinations))]
    public static class Patch_GroupGoto_OtherFloors
    {
        private static readonly AccessTools.FieldRef<MultiPawnGotoController, List<Pawn>> PawnsRef =
            AccessTools.FieldRefAccess<MultiPawnGotoController, List<Pawn>>("pawns");

        private static readonly AccessTools.FieldRef<MultiPawnGotoController, List<IntVec3>> DestsRef =
            AccessTools.FieldRefAccess<MultiPawnGotoController, List<IntVec3>>("dests");

        private static readonly AccessTools.FieldRef<MultiPawnGotoController, IntVec3> StartRef =
            AccessTools.FieldRefAccess<MultiPawnGotoController, IntVec3>("start");

        private static readonly AccessTools.FieldRef<MultiPawnGotoController, IntVec3> EndRef =
            AccessTools.FieldRefAccess<MultiPawnGotoController, IntVec3>("end");

        public static void Postfix(MultiPawnGotoController __instance)
        {
            try
            {
                CrossFloorDraftedMove.FixGroupDestinations(
                    PawnsRef(__instance), DestsRef(__instance), StartRef(__instance), EndRef(__instance));
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Group move across floors failed: " + e.Message, 0x5B71044);
            }
        }
    }
}
