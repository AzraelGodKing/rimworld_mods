using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Strata
{
    // Spreads expensive post-gen work across ticks so opening a level returns
    // control faster. Scheduled during map gen; attached on FinalizeInit.
    public static class StrataDeferredGenUtility
    {
        private static readonly Dictionary<int, List<Action<Map>>> pendingByMapId =
            new Dictionary<int, List<Action<Map>>>();

        public static void ScheduleBiomesCavernFeatures(Map map)
        {
            Schedule(map, m => BiomesCavernsUtility.ScatterCavernFeatures(m, default));
        }

        public static void ScheduleNativeCavernFeatures(Map map)
        {
            Schedule(map, StrataNativeCavernUtility.ScatterFeatures);
        }

        public static void ScheduleDbhGroundwater(Map map)
        {
            Schedule(map, DbhGroundwaterUtility.SeedIfNeeded);
        }

        public static bool HasPending(Map map)
        {
            return map != null
                && pendingByMapId.TryGetValue(map.uniqueID, out List<Action<Map>> work)
                && work.Count > 0;
        }

        public static void AttachPending(Map map)
        {
            if (map == null)
            {
                return;
            }

            MapComponent_StrataDeferredGen comp = map.GetComponent<MapComponent_StrataDeferredGen>();
            if (comp == null)
            {
                return;
            }

            ClaimPendingWork(comp);
        }

        internal static void ClaimPendingWork(MapComponent_StrataDeferredGen comp)
        {
            Map map = comp?.map;
            if (map == null || !pendingByMapId.TryGetValue(map.uniqueID, out List<Action<Map>> work)
                || work.Count == 0)
            {
                return;
            }

            pendingByMapId.Remove(map.uniqueID);
            comp.EnqueueWork(work);
        }

        private static void Schedule(Map map, Action<Map> action)
        {
            if (map == null || action == null)
            {
                return;
            }

            if (!pendingByMapId.TryGetValue(map.uniqueID, out List<Action<Map>> list))
            {
                pendingByMapId[map.uniqueID] = list = new List<Action<Map>>();
            }

            list.Add(action);
        }
    }

    public class MapComponent_StrataDeferredGen : MapComponent
    {
        private const int StageDelayTicks = 45;

        private readonly List<Action<Map>> queue = new List<Action<Map>>();
        private int nextStageTick = -1;

        public MapComponent_StrataDeferredGen(Map map) : base(map)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            StrataDeferredGenUtility.ClaimPendingWork(this);
        }

        internal void EnqueueWork(IEnumerable<Action<Map>> work)
        {
            queue.AddRange(work);
            if (nextStageTick < 0)
            {
                nextStageTick = Find.TickManager.TicksGame + StageDelayTicks;
            }
        }

        public override void MapComponentTick()
        {
            if (queue.Count == 0 || Find.TickManager.TicksGame < nextStageTick)
            {
                return;
            }

            Action<Map> action = queue[0];
            queue.RemoveAt(0);
            nextStageTick = Find.TickManager.TicksGame + StageDelayTicks;
            action?.Invoke(map);

            if (queue.Count == 0)
            {
                nextStageTick = -1;
            }
        }
    }
}
