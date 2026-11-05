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
        /// <summary>
        /// Bumped when a member is added (minor) or a signature changes. Check
        /// it through reflection before calling newer members. 2 added levels,
        /// labels, travel cost, work probes and the level events.
        /// </summary>
        public const int ApiVersion = 2;

        private static readonly List<Action<Map, Map, MapPortal>> linkCallbacks =
            new List<Action<Map, Map, MapPortal>>();

        private static readonly List<Action<Map>> createdCallbacks = new List<Action<Map>>();

        private static readonly List<Action<Map>> removedCallbacks = new List<Action<Map>>();

        /// <summary>True when the map is a Strata underground pocket (B+).</summary>
        public static bool IsUnderground(Map map) => StrataMapUtility.IsUnderground(map);

        /// <summary>True when the map is a Strata upper / roof deck (A+).</summary>
        public static bool IsUpperLevel(Map map) => StrataMapUtility.IsUpperLevel(map);

        /// <summary>True when the map is linked under an Odyssey gravship stack.</summary>
        public static bool IsGravshipLinkedLevel(Map map) =>
            StrataGravshipUtility.IsGravshipLinkedLevel(map);

        /// <summary>True for any Strata level: underground, upper deck or gravship stack level.</summary>
        public static bool IsLevel(Map map) =>
            map != null && (IsUnderground(map) || IsUpperLevel(map) || IsGravshipLinkedLevel(map));

        /// <summary>
        /// Signed stack index: +N for the Nth upper floor, 0 for the surface,
        /// -N for the Nth level underground.
        /// </summary>
        public static int LevelIndex(Map map) => StrataDepth.Altitude(map);

        /// <summary>The player's name for the level, or the map's own label.</summary>
        public static string LevelLabel(Map map)
        {
            if (map == null)
            {
                return null;
            }
            string custom = StrataLevelLabels.Get?.GetLabel(map);
            return !custom.NullOrEmpty() ? custom : map.Parent?.LabelCap ?? map.ToString();
        }

        /// <summary>
        /// Cost of the cheapest route from <paramref name="from"/> to <paramref name="to"/>,
        /// in walked cells: the walk between portals on the floors in between
        /// plus each stair or elevator ride (a stair counts as 20 cells, an
        /// elevator as 8). The walk to the first portal is not included. 0 for
        /// the same map, -1 when not linked.
        /// </summary>
        public static float TravelCost(Map from, Map to)
        {
            if (from == null || to == null)
            {
                return -1f;
            }
            if (from == to)
            {
                return 0f;
            }
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(from);
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].map == to)
                {
                    return links[i].travelCost;
                }
            }
            return -1f;
        }

        /// <summary>
        /// Stair and elevator rides on that cheapest route: 0 for the same map,
        /// -1 when not linked.
        /// </summary>
        public static int Hops(Map from, Map to)
        {
            if (from == null || to == null)
            {
                return -1;
            }
            if (from == to)
            {
                return 0;
            }
            List<LevelGraph.LevelLink> links = LevelGraph.ReachableLevels(from);
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].map == to)
                {
                    return links[i].depth;
                }
            }
            return -1;
        }

        /// <summary>
        /// Lets the work relay send pawns to a floor for your mod's work. Return
        /// true when <paramref name="map"/> has work this pawn could do there.
        /// Called during job search, so keep it cheap.
        /// </summary>
        public static void RegisterWorkProbe(WorkRelaySignals.WorkProbe probe) =>
            WorkRelaySignals.RegisterWorkProbe(probe);

        public static void UnregisterWorkProbe(WorkRelaySignals.WorkProbe probe) =>
            WorkRelaySignals.UnregisterWorkProbe(probe);

        /// <summary>
        /// Treat a modded ThinkNode_JobGiver whose type name contains
        /// <paramref name="typeNameFragment"/> as a work-seeking giver, so the
        /// work relay runs after it finds nothing.
        /// </summary>
        public static void RegisterWorkSeekingJobGiverMarker(string typeNameFragment) =>
            WorkRelaySignals.RegisterWorkSeekingJobGiverMarker(typeNameFragment);

        /// <summary>Called once a new Strata level has finished generating.</summary>
        public static void RegisterLevelCreatedListener(Action<Map> callback) => Add(createdCallbacks, callback);

        public static void UnregisterLevelCreatedListener(Action<Map> callback) => createdCallbacks.Remove(callback);

        /// <summary>Called just before a Strata level is removed from the game. The map is still valid.</summary>
        public static void RegisterLevelRemovedListener(Action<Map> callback) => Add(removedCallbacks, callback);

        public static void UnregisterLevelRemovedListener(Action<Map> callback) => removedCallbacks.Remove(callback);

        /// <summary>True when <paramref name="from"/> has at least one stair/elevator link.</summary>
        public static bool AnyLinkFrom(Map from) => LevelGraph.AnyLinkFrom(from);

        /// <summary>
        /// Linked destination maps reachable from <paramref name="from"/>, cheapest route first.
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
        /// Called when a stair or elevator links two maps: Strata's own when
        /// one is built or a level is created (not on load), and custom links
        /// another mod announces with <see cref="NotifyVerticalLink"/>.
        /// </summary>
        public static void RegisterVerticalLinkListener(Action<Map, Map, MapPortal> callback) =>
            Add(linkCallbacks, callback);

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

        internal static void NotifyLevelCreated(Map map) => Raise(createdCallbacks, map, "level-created");

        internal static void NotifyLevelRemoved(Map map) => Raise(removedCallbacks, map, "level-removed");

        private static void Add<T>(List<T> list, T callback) where T : class
        {
            if (callback != null && !list.Contains(callback))
            {
                list.Add(callback);
            }
        }

        private static void Raise(List<Action<Map>> list, Map map, string what)
        {
            if (list.Count == 0 || map == null)
            {
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    list[i]?.Invoke(map);
                }
                catch (Exception e)
                {
                    Log.Warning("[Strata] Modder " + what + " listener threw: " + e);
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
