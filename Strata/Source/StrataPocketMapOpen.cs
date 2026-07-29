using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Dig/ancient pocket maps are full colony size. Filling them with mineable
    // rock via GenSpawn on the main thread (even inside a LongEvent Action) can
    // freeze Unity for minutes on 250²+ maps. We:
    //  1) Generate the pocket with terrain/roof/warren only (fast)
    //  2) Drain rock GenSpawn in a yielding LongEvent so frames keep pumping
    //  3) Block Enter until both steps finish
    //
    // Important: MapPortal.PocketMap only returns an existing map. Generation is
    // MapPortal.GetOtherMap() → private GeneratePocketMap(). Reading PocketMap
    // alone always "fails" and permanently blacklists the portal id.
    public static class StrataPocketMapOpen
    {
        private static readonly HashSet<int> generatingPortalIds = new HashSet<int>();

        private static readonly HashSet<int> failedPortalIds = new HashSet<int>();

        public static void ResetSession()
        {
            generatingPortalIds.Clear();
            failedPortalIds.Clear();
            StrataRockFillScheduler.Clear();
        }

        public static bool IsGenerating(MapPortal portal)
        {
            return portal != null && generatingPortalIds.Contains(portal.thingIDNumber);
        }

        /// <summary>True while any stair/elevator dig or tower open is mid-LongEvent.</summary>
        public static bool AnyGenerating => generatingPortalIds.Count > 0;

        public static bool HasFailed(MapPortal portal)
        {
            return portal != null && failedPortalIds.Contains(portal.thingIDNumber);
        }

        public static bool TryBeginOpen(MapPortal portal)
        {
            if (portal == null || !portal.Spawned)
            {
                return false;
            }
            if (portal.PocketMapExists)
            {
                return true;
            }
            if (IsGenerating(portal) || HasFailed(portal))
            {
                return false;
            }

            int id = portal.thingIDNumber;
            generatingPortalIds.Add(id);
            LongEventHandler.QueueLongEvent(
                OpenRoutine(portal, id),
                "Strata_GeneratingLevel",
                exceptionHandler: null);
            return false;
        }

        private static IEnumerable OpenRoutine(MapPortal portal, int id)
        {
            try
            {
                // One frame so the overlay can paint before heavy work.
                yield return "Strata_GeneratingLevel_Structure".Translate();

                Map map = null;
                try
                {
                    if (portal.Destroyed || !portal.Spawned)
                    {
                        yield break;
                    }
                    if (portal.PocketMapExists)
                    {
                        map = portal.PocketMap;
                    }
                    else
                    {
                        map = portal.GetOtherMap();
                    }
                    if (map == null)
                    {
                        failedPortalIds.Add(id);
                        Log.Error("[Strata] Pocket map generation returned null for "
                            + portal.LabelCap + " (" + portal.def?.defName + ").");
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    failedPortalIds.Add(id);
                    Log.Error("[Strata] Pocket map generation failed for "
                        + portal.LabelCap + ": " + e);
                    StrataRockFillScheduler.Clear();
                    yield break;
                }

                if (StrataRockFillScheduler.TryBegin(map, out BoolGrid skipMask))
                {
                    yield return "Strata_GeneratingLevel_Rock".Translate();
                    foreach (object step in StrataRockFillScheduler.SpawnMineablesChunked(map, skipMask))
                    {
                        yield return step;
                    }
                }

                Log.Message("[Strata] Opened level under " + portal.LabelCap
                    + " (" + map.Size.x + "×" + map.Size.z + ").");
            }
            finally
            {
                generatingPortalIds.Remove(id);
            }
        }

        public static void ClearFailed(MapPortal portal)
        {
            if (portal != null)
            {
                failedPortalIds.Remove(portal.thingIDNumber);
            }
        }
    }
}
