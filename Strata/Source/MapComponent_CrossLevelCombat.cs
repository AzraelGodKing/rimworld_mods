using Verse;

namespace Strata
{
    // Drive cross-level turret fire + auto-engage scans for an A+ / source pair.
    public class MapComponent_CrossLevelCombat : MapComponent
    {
        private const int AutoEngageInterval = 250;
        private const int AutoEngageIntervalPerf = 750;
        private int nextAutoEngageDue = -1;

        public MapComponent_CrossLevelCombat(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!StrataCrossLevelCombat.Enabled) return;
            if (!StrataMapUtility.IsUpperLevel(map)) return;

            Map surface = UpperDeckUtility.SourceMapFor(map);
            if (surface == null || surface.Disposed) return;

            StrataCrossLevelTurret.TickPair(map, surface);

            int now = Find.TickManager.TicksGame;
            if (nextAutoEngageDue < 0) nextAutoEngageDue = now + AutoEngageInterval;
            if (now >= nextAutoEngageDue)
            {
                int interval = StrataMod.Settings?.performanceModeEnabled == true
                    ? AutoEngageIntervalPerf
                    : AutoEngageInterval;
                nextAutoEngageDue = now + interval;
                StrataCrossLevelAutoEngage.ScanPair(map, surface);
            }
        }
    }
}
