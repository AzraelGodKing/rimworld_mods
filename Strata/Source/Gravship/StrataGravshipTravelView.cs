using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Strata
{
    // Travelling underdeck/tower maps stay in Find.Maps during flight. Viewing one
    // (colonist-bar portrait) makes MapDrawer regenerate that pocket while land
    // then mass-dirties terrain/things — classic RGB neon section corruption that
    // save/load does not heal. Keep the camera off travelling floors when another
    // map exists; on land, paint with the host as CurrentMap and force a full
    // MapDrawer regen afterward.
    public static class StrataGravshipTravelView
    {
        public static bool IsTravelling(Map map)
        {
            var stacks = WorldComponent_StrataGravshipStacks.Get();
            return stacks != null && stacks.IsTravelling(map);
        }

        public static bool ShouldBlockView(Map map)
        {
            if (map == null || !IsTravelling(map))
            {
                return false;
            }
            // If every remaining map is travelling, the player has nowhere else
            // to look — allow the view (land path still switches away to paint).
            return FindNonTravellingMap(except: map) != null;
        }

        public static Map FindNonTravellingMap(Map except = null)
        {
            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return null;
            }
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || map == except || IsTravelling(map))
                {
                    continue;
                }
                return map;
            }
            return null;
        }

        // Call after MarkTravelling so takeoff does not leave the camera on a pocket.
        public static void LeaveTravellingView(Map preferHost = null)
        {
            Map current = Find.CurrentMap;
            if (current == null || !IsTravelling(current))
            {
                return;
            }
            Map fallback = preferHost != null && Find.Maps.Contains(preferHost) && !IsTravelling(preferHost)
                ? preferHost
                : FindNonTravellingMap(except: current);
            if (fallback != null)
            {
                Current.Game.CurrentMap = fallback;
                Messages.Message(
                    "Strata_GravshipTravelViewBlocked".Translate(),
                    MessageTypeDefOf.NeutralEvent,
                    historical: false);
                return;
            }
            // No safe map left — show the planet until land.
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            Messages.Message(
                "Strata_GravshipTravelViewBlocked".Translate(),
                MessageTypeDefOf.NeutralEvent,
                historical: false);
        }

        // Land paint must not run while a travelling/linked pocket is the viewed map.
        public static Map BeginLandPaintSafeView(Map newHost, List<Map> pockets)
        {
            Map previous = Find.CurrentMap;
            if (newHost == null || previous == null || previous == newHost)
            {
                return null;
            }
            bool viewingPocket = false;
            if (pockets != null)
            {
                for (int i = 0; i < pockets.Count; i++)
                {
                    if (pockets[i] == previous)
                    {
                        viewingPocket = true;
                        break;
                    }
                }
            }
            if (!viewingPocket && !IsTravelling(previous))
            {
                return null;
            }
            if (!Find.Maps.Contains(newHost))
            {
                return null;
            }
            Current.Game.CurrentMap = newHost;
            return previous;
        }

        public static void EndLandPaintSafeView(Map previousView, Map newHost, List<Map> pockets)
        {
            HealMapDrawer(newHost);
            if (pockets != null)
            {
                for (int i = 0; i < pockets.Count; i++)
                {
                    HealMapDrawer(pockets[i]);
                }
            }
            if (previousView != null
                && Find.Maps.Contains(previousView)
                && Find.CurrentMap != previousView)
            {
                Current.Game.CurrentMap = previousView;
            }
        }

        public static void HealMapDrawer(Map map)
        {
            if (map?.mapDrawer == null || map.Disposed)
            {
                return;
            }
            try
            {
                map.mapDrawer.RegenerateEverythingNow();
            }
            catch (System.Exception e)
            {
                StrataLog.Warning("[Strata] MapDrawer regen after gravship land failed on map "
                    + map.uniqueID + ": " + e.Message);
            }
        }
    }

    // Colonist bar / CameraJumper set CurrentMap directly — block travelling pockets
    // when another map is available.
    [HarmonyPatch(typeof(Game), "set_CurrentMap")]
    public static class Patch_BlockTravellingMapView
    {
        public static bool Prefix(ref Map value)
        {
            if (!StrataGravshipTravelView.ShouldBlockView(value))
            {
                return true;
            }
            Map fallback = StrataGravshipTravelView.FindNonTravellingMap(except: value);
            if (fallback == null)
            {
                return true;
            }
            value = fallback;
            Messages.Message(
                "Strata_GravshipTravelViewBlocked".Translate(),
                MessageTypeDefOf.RejectInput,
                historical: false);
            return true;
        }
    }
}
