using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Stable soft-compat surface for other mods. Prefer these over digging into
    // LevelGraph / StrataIncidents internals — signatures here stay intentional.
    public static class StrataModderApi
    {
        private static readonly List<Action<Map, Map, MapPortal>> linkCallbacks =
            new List<Action<Map, Map, MapPortal>>();

        /// <summary>True when the map is a Strata underground pocket (B+).</summary>
        public static bool IsUnderground(Map map) => StrataMapUtility.IsUnderground(map);

        /// <summary>True when the map is a Strata upper / roof deck (A+).</summary>
        public static bool IsUpperLevel(Map map) => StrataMapUtility.IsUpperLevel(map);

        /// <summary>True when the map is linked under an Odyssey gravship stack.</summary>
        public static bool IsGravshipLinkedLevel(Map map) =>
            StrataGravshipUtility.IsGravshipLinkedLevel(map);

        /// <summary>True when <paramref name="from"/> has at least one stair/elevator link.</summary>
        public static bool AnyLinkFrom(Map from) => LevelGraph.AnyLinkFrom(from);

        /// <summary>
        /// Linked destination maps reachable from <paramref name="from"/> (BFS order).
        /// Fresh list each call — safe to store or mutate.
        /// </summary>
        public static List<Map> ReachableMaps(Map from)
        {
            var result = new List<Map>();
            if (from == null)
            {
                return result;
            }
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(from);
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].map != null)
                {
                    result.Add(links[i].map);
                }
            }
            return result;
        }

        /// <summary>
        /// Best first stair/elevator step from <paramref name="from"/> toward
        /// <paramref name="target"/> near <paramref name="near"/> for <paramref name="pawn"/>.
        /// </summary>
        public static MapPortal BestFirstStep(Map from, Map target, IntVec3 near, Pawn pawn = null) =>
            LevelGraph.BestFirstStep(from, target, near, pawn);

        /// <summary>
        /// Candidate first steps toward <paramref name="target"/> from <paramref name="from"/>.
        /// </summary>
        public static List<MapPortal> FirstStepCandidates(Map from, Map target) =>
            LevelGraph.FirstStepCandidates(from, target);

        /// <summary>
        /// Register a callback invoked when another mod announces a custom vertical
        /// link. Strata still discovers MapPortal topology itself; use this to
        /// refresh caches or spawn companion fixtures.
        /// </summary>
        public static void RegisterVerticalLinkListener(Action<Map, Map, MapPortal> callback)
        {
            if (callback != null && !linkCallbacks.Contains(callback))
            {
                linkCallbacks.Add(callback);
            }
        }

        public static void UnregisterVerticalLinkListener(Action<Map, Map, MapPortal> callback)
        {
            if (callback != null)
            {
                linkCallbacks.Remove(callback);
            }
        }

        /// <summary>
        /// Notify Strata (and listeners) that <paramref name="portal"/> links
        /// <paramref name="a"/> and <paramref name="b"/>. Does not create the portal.
        /// </summary>
        public static void NotifyVerticalLink(Map a, Map b, MapPortal portal)
        {
            for (int i = 0; i < linkCallbacks.Count; i++)
            {
                try
                {
                    linkCallbacks[i]?.Invoke(a, b, portal);
                }
                catch (Exception e)
                {
                    Log.Warning("[Strata] Modder vertical-link listener threw: " + e);
                }
            }
        }

        /// <summary>
        /// Whether Strata would block this incident on underground maps (raid
        /// types that need a map edge, etc.). Mirrors internal policy.
        /// </summary>
        public static bool WouldBlockUndergroundIncident(IncidentDef def) =>
            Patch_UndergroundIncidents.WouldBlock(def);
    }
}
