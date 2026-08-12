using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ShiftChange
{
    public static class ShiftChangeUtility
    {
        public static readonly string[] DefaultWorkTypeDefNames =
        {
            "Cooking",
            "Doctor",
            "Handling",
        };

        public static bool IsSleepSchedule(Pawn pawn)
        {
            if (pawn?.timetable == null || pawn.Dead || !pawn.Spawned)
            {
                return false;
            }

            TimeAssignmentDef ta = pawn.timetable.CurrentAssignment;
            return ta == TimeAssignmentDefOf.Sleep;
        }

        public static bool IsInIdeologyRitual(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return false;
            }

            Lord lord = pawn.GetLord();
            return lord?.LordJob is LordJob_Ritual;
        }

        public static WorkTypeDef WorkTypeOfJob(Job job)
        {
            if (job == null)
            {
                return null;
            }

            if (job.workGiverDef?.workType != null)
            {
                return job.workGiverDef.workType;
            }

            return null;
        }

        public static Zone_Stockpile FindWardrobe(Pawn pawn, ShiftChangeRule rule)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Map map = pawn.Map;
            List<Zone> zones = map.zoneManager?.AllZones;
            if (zones == null)
            {
                return null;
            }

            if (rule != null && rule.wardrobeZoneId >= 0)
            {
                for (int i = 0; i < zones.Count; i++)
                {
                    if (zones[i] is Zone_Stockpile stock && stock.ID == rule.wardrobeZoneId)
                    {
                        return stock;
                    }
                }
            }

            string label = ShiftChangeMod.Settings?.defaultWardrobeLabel;
            if (!string.IsNullOrEmpty(label))
            {
                for (int i = 0; i < zones.Count; i++)
                {
                    if (zones[i] is Zone_Stockpile stock
                        && stock.label != null
                        && stock.label.IndexOf(label, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return stock;
                    }
                }
            }

            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i] is Zone_Stockpile stock && StockpileHasApparel(stock))
                {
                    return stock;
                }
            }

            return null;
        }

        public static bool StockpileHasApparel(Zone_Stockpile zone)
        {
            if (zone?.cells == null)
            {
                return false;
            }

            Map map = zone.Map;
            for (int i = 0; i < zone.cells.Count; i++)
            {
                List<Thing> things = map.thingGrid.ThingsListAtFast(zone.cells[i]);
                for (int t = 0; t < things.Count; t++)
                {
                    if (things[t] is Apparel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<Apparel> CollectApparelInZone(Zone_Stockpile zone)
        {
            List<Apparel> list = new List<Apparel>();
            if (zone?.cells == null)
            {
                return list;
            }

            Map map = zone.Map;
            for (int i = 0; i < zone.cells.Count; i++)
            {
                List<Thing> things = map.thingGrid.ThingsListAtFast(zone.cells[i]);
                for (int t = 0; t < things.Count; t++)
                {
                    if (things[t] is Apparel apparel && !apparel.Destroyed && apparel.Spawned)
                    {
                        list.Add(apparel);
                    }
                }
            }

            return list;
        }

        public static List<int> SnapshotWornApparelIds(Pawn pawn)
        {
            List<int> ids = new List<int>();
            if (pawn?.apparel?.WornApparel == null)
            {
                return ids;
            }

            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] != null)
                {
                    ids.Add(worn[i].thingIDNumber);
                }
            }

            return ids;
        }

        public static Apparel FindApparelById(Map map, int thingId)
        {
            if (map == null || thingId <= 0)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    List<Apparel> worn = pawns[i]?.apparel?.WornApparel;
                    if (worn == null)
                    {
                        continue;
                    }

                    for (int a = 0; a < worn.Count; a++)
                    {
                        if (worn[a] != null && worn[a].thingIDNumber == thingId)
                        {
                            return worn[a];
                        }
                    }
                }
            }

            List<Thing> all = map.listerThings?.ThingsInGroup(ThingRequestGroup.Apparel);
            if (all == null)
            {
                return null;
            }

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is Apparel apparel && apparel.thingIDNumber == thingId && !apparel.Destroyed)
                {
                    return apparel;
                }
            }

            // Also check inventories.
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn p = pawns[i];
                    ThingOwner inner = p?.inventory?.innerContainer;
                    if (inner == null)
                    {
                        continue;
                    }

                    for (int t = 0; t < inner.Count; t++)
                    {
                        if (inner[t] is Apparel apparel && apparel.thingIDNumber == thingId)
                        {
                            return apparel;
                        }
                    }
                }
            }

            return null;
        }

        public static bool PolicyAllows(ApparelPolicy policy, Apparel apparel)
        {
            if (policy?.filter == null || apparel == null)
            {
                return false;
            }

            return policy.filter.Allows(apparel);
        }

        public static IntVec3 FindStandCellNear(Zone_Stockpile zone, Pawn pawn)
        {
            if (zone == null || pawn == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 best = IntVec3.Invalid;
            float bestDist = float.MaxValue;
            for (int i = 0; i < zone.cells.Count; i++)
            {
                IntVec3 c = zone.cells[i];
                if (!c.Standable(zone.Map))
                {
                    continue;
                }

                if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float d = (c - pawn.Position).LengthHorizontalSquared;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }

            return best;
        }

        public static bool TryStartApplyJob(Pawn pawn, ShiftChangeRule rule)
        {
            if (pawn?.jobs == null || rule == null)
            {
                return false;
            }

            if (ShiftChangeDefOf.ShiftChange_Apply == null)
            {
                return false;
            }

            Zone_Stockpile zone = FindWardrobe(pawn, rule);
            if (zone == null)
            {
                return false;
            }

            IntVec3 cell = FindStandCellNear(zone, pawn);
            if (!cell.IsValid)
            {
                return false;
            }

            Job job = JobMaker.MakeJob(ShiftChangeDefOf.ShiftChange_Apply, cell);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.playerForced = true;
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public static bool TryStartRestoreJob(Pawn pawn, ShiftChangeRule rule)
        {
            if (pawn?.jobs == null)
            {
                return false;
            }

            if (ShiftChangeDefOf.ShiftChange_Restore == null)
            {
                return false;
            }

            Zone_Stockpile zone = FindWardrobe(pawn, rule);
            IntVec3 cell = zone != null ? FindStandCellNear(zone, pawn) : pawn.Position;
            if (!cell.IsValid)
            {
                cell = pawn.Position;
            }

            Job job = JobMaker.MakeJob(ShiftChangeDefOf.ShiftChange_Restore, cell);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            job.playerForced = true;
            return pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        public static void RemoveApparelFromPawn(Pawn pawn, Apparel apparel, Zone_Stockpile zone)
        {
            if (pawn?.apparel == null || apparel == null)
            {
                return;
            }

            if (!pawn.apparel.WornApparel.Contains(apparel))
            {
                return;
            }

            pawn.apparel.Remove(apparel);

            bool preferInv = ShiftChangeMod.Settings == null
                || ShiftChangeMod.Settings.preferInventoryForRemoved;
            if (preferInv && pawn.inventory != null && pawn.inventory.innerContainer.TryAdd(apparel, canMergeWithExistingStacks: false))
            {
                return;
            }

            IntVec3 dropCell = zone != null && zone.cells.Count > 0
                ? zone.cells[0]
                : pawn.Position;
            if (!apparel.Spawned)
            {
                GenPlace.TryPlaceThing(apparel, dropCell, pawn.Map, ThingPlaceMode.Near);
            }
        }

        public static bool TryWearFromZone(Pawn pawn, Apparel apparel, Zone_Stockpile zone, bool replace)
        {
            if (pawn?.apparel == null || apparel == null || apparel.Destroyed)
            {
                return false;
            }

            if (apparel.Wearer != null && apparel.Wearer != pawn)
            {
                return false;
            }

            GameComponent_ShiftChange comp = GameComponent_ShiftChange.Get;
            if (comp != null && comp.IsClaimedByOther(apparel.thingIDNumber, pawn.thingIDNumber))
            {
                return false;
            }

            if (apparel.Spawned
                && !apparel.Position.InHorDistOf(pawn.Position, 2.9f)
                && !pawn.CanReserveAndReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return false;
            }

            if (comp != null && !comp.TryClaimApparel(apparel.thingIDNumber, pawn.thingIDNumber))
            {
                return false;
            }

            if (replace)
            {
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = worn.Count - 1; i >= 0; i--)
                {
                    Apparel w = worn[i];
                    if (w != null && !ApparelUtility.CanWearTogether(w.def, apparel.def, pawn.RaceProps.body))
                    {
                        RemoveApparelFromPawn(pawn, w, zone);
                    }
                }
            }
            else
            {
                // Add mode: skip if a conflicting layer is already worn.
                List<Apparel> worn = pawn.apparel.WornApparel;
                for (int i = 0; i < worn.Count; i++)
                {
                    Apparel w = worn[i];
                    if (w != null && !ApparelUtility.CanWearTogether(w.def, apparel.def, pawn.RaceProps.body))
                    {
                        return false;
                    }
                }
            }

            // Pull out of inventory or zone.
            if (apparel.ParentHolder is Pawn_InventoryTracker inv && inv.pawn != null)
            {
                inv.innerContainer.Remove(apparel);
            }
            else if (apparel.Spawned)
            {
                apparel.DeSpawn();
            }

            pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            return true;
        }

        public static IEnumerable<Pawn> RitualParticipants(LordJob_Ritual ritual)
        {
            if (ritual?.lord?.ownedPawns == null)
            {
                yield break;
            }

            List<Pawn> owned = ritual.lord.ownedPawns;
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i] != null)
                {
                    yield return owned[i];
                }
            }
        }
    }
}
