using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Dig/ancient pocket maps are full colony size and fill solid rock — with a
    // large mod list that can freeze the main thread for minutes looking like a
    // crash. Tower decks skip rock fill, which is why they still feel fine.
    // Queue generation as a LongEvent so the loading overlay stays alive, and
    // refuse EnterPortal until the pocket exists.
    public static class StrataPocketMapOpen
    {
        private static readonly HashSet<int> generatingPortalIds = new HashSet<int>();

        private static readonly HashSet<int> failedPortalIds = new HashSet<int>();

        public static bool IsGenerating(MapPortal portal)
        {
            return portal != null && generatingPortalIds.Contains(portal.thingIDNumber);
        }

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
                delegate
                {
                    try
                    {
                        if (portal.Destroyed || !portal.Spawned)
                        {
                            return;
                        }
                        Map map = portal.PocketMap;
                        if (map == null)
                        {
                            failedPortalIds.Add(id);
                            Log.Error("[Strata] Pocket map generation returned null for "
                                + portal.LabelCap + " (" + portal.def?.defName + ").");
                        }
                    }
                    catch (Exception e)
                    {
                        failedPortalIds.Add(id);
                        Log.Error("[Strata] Pocket map generation failed for "
                            + portal.LabelCap + ": " + e);
                    }
                    finally
                    {
                        generatingPortalIds.Remove(id);
                    }
                },
                "GeneratingMap",
                doAsynchronously: false,
                exceptionHandler: null);
            return false;
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
