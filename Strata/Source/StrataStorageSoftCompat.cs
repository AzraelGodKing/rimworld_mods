using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Strata
{
    /// <summary>
    /// Storage accept / delivery helpers that respect multi-stack shelves
    /// (Adaptive Storage Framework, Neat Storage, Deep Storage, etc.) via
    /// vanilla StoreUtility. Fail-open: exceptions → treat as no room.
    /// </summary>
    public static class StrataStorageSoftCompat
    {
        public const string AdaptiveStorageId = "adaptive.storage.framework";

        private const int FailCooldownTicks = 5000;

        private static bool? asfActive;

        // (map uniqueID, ThingDef shortHash) → tick until which we skip re-export
        private static readonly Dictionary<long, int> failUntilTick = new Dictionary<long, int>();

        public static bool AdaptiveStorageActive
        {
            get
            {
                if (asfActive == null)
                {
                    asfActive = ModLister.GetActiveModWithIdentifier(AdaptiveStorageId, ignorePostfix: true) != null;
                }
                return asfActive.Value;
            }
        }

        public static void ResetCaches()
        {
            asfActive = null;
            failUntilTick.Clear();
        }

        public static bool CellIsGoodStore(IntVec3 cell, Map map, Thing t, Pawn carrier = null)
        {
            if (map == null || t == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            try
            {
                Faction faction = carrier?.Faction ?? Faction.OfPlayer;
                return StoreUtility.IsGoodStoreCell(cell, map, t, carrier, faction);
            }
            catch
            {
                return false;
            }
        }

        public static void NoteFailedDest(Map map, ThingDef def)
        {
            if (map == null || def == null || Find.TickManager == null)
            {
                return;
            }

            failUntilTick[Key(map, def)] = Find.TickManager.TicksGame + FailCooldownTicks;
        }

        public static bool IsDestCoolingDown(Map map, ThingDef def)
        {
            if (map == null || def == null || Find.TickManager == null)
            {
                return false;
            }

            long key = Key(map, def);
            if (!failUntilTick.TryGetValue(key, out int until))
            {
                return false;
            }

            if (Find.TickManager.TicksGame >= until)
            {
                failUntilTick.Remove(key);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Storage job that prefers HaulToContainer for buildings (ASF shelves)
        /// and cell haul for stockpile zones. Optional preferStoreCell is the
        /// cell chosen before a stair trip — same finish as vanilla HaulToCell.
        /// </summary>
        public static Job TryMakeStorageJob(Pawn pawn, Thing cargo, IntVec3 preferStoreCell = default)
        {
            if (pawn?.Map == null || cargo == null)
            {
                return null;
            }

            if (TryMakePreferredStoreJob(pawn, cargo, preferStoreCell, out Job preferred))
            {
                return preferred;
            }

            if (!TryFindStorage(pawn, cargo, StoragePriority.Unstored, out IntVec3 cell, out IHaulDestination dest)
                && !TryFindStorage(pawn, cargo, StoreUtility.CurrentStoragePriorityOf(cargo), out cell, out dest))
            {
                return null;
            }

            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return null;
            }

            if (dest is Building building && building.Spawned && building.Map == pawn.Map)
            {
                Job containerJob = JobMaker.MakeJob(JobDefOf.HaulToContainer, cargo, building);
                containerJob.count = cargo.stackCount;
                containerJob.haulMode = HaulMode.ToContainer;
                return containerJob;
            }

            if (cell.IsValid)
            {
                return HaulAIUtility.HaulToCellStorageJob(pawn, cargo, cell, fitInStoreCell: true);
            }

            return null;
        }

        private static bool TryMakePreferredStoreJob(
            Pawn pawn,
            Thing cargo,
            IntVec3 preferStoreCell,
            out Job job)
        {
            job = null;
            Map map = pawn.Map;
            if (!preferStoreCell.IsValid || !preferStoreCell.InBounds(map))
            {
                return false;
            }

            if (!pawn.CanReach(preferStoreCell, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return false;
            }

            if (cargo.Spawned && !pawn.CanReserve(cargo))
            {
                return false;
            }

            // Container / ASF shelf occupying the remembered cell.
            List<Thing> things = preferStoreCell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building building
                    && building.Faction == Faction.OfPlayer
                    && building is IHaulDestination haulDest
                    && haulDest.HaulDestinationEnabled
                    && haulDest.Accepts(cargo)
                    && building.TryGetInnerInteractableThingOwner() != null)
                {
                    job = JobMaker.MakeJob(JobDefOf.HaulToContainer, cargo, building);
                    job.count = cargo.stackCount;
                    job.haulMode = HaulMode.ToContainer;
                    return true;
                }
            }

            if (!StoreUtility.IsGoodStoreCell(preferStoreCell, map, cargo, pawn, pawn.Faction))
            {
                return false;
            }

            job = HaulAIUtility.HaulToCellStorageJob(pawn, cargo, preferStoreCell, fitInStoreCell: true);
            return job != null;
        }

        private static bool TryFindStorage(
            Pawn pawn,
            Thing cargo,
            StoragePriority currentPriority,
            out IntVec3 cell,
            out IHaulDestination dest)
        {
            cell = IntVec3.Invalid;
            dest = null;
            try
            {
                return StoreUtility.TryFindBestBetterStorageFor(
                    cargo,
                    pawn,
                    pawn.Map,
                    currentPriority,
                    pawn.Faction,
                    out cell,
                    out dest,
                    needAccurateResult: true);
            }
            catch
            {
                return false;
            }
        }

        private static long Key(Map map, ThingDef def)
        {
            return ((long)map.uniqueID << 32) ^ (uint)def.shortHash;
        }
    }
}
