using System;
using RimWorld;
using Verse;

namespace Strata
{
    // A+ decks share the sky with the map below — copy curWeather so rain/snow
    // overlays match what you see through open sky / see-below.
    public class MapComponent_WeatherSync : MapComponent
    {
        private const int SyncIntervalCurrent = 150;
        private const int SyncIntervalBackground = 600;

        private int nextSyncTick;

        public MapComponent_WeatherSync(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!StrataMapUtility.IsUpperLevel(map))
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            int interval = Find.CurrentMap == map ? SyncIntervalCurrent : SyncIntervalBackground;
            if (now < nextSyncTick)
            {
                return;
            }
            nextSyncTick = now + interval;

            try
            {
                SyncFromSource();
            }
            catch (Exception e)
            {
                Log.WarningOnce("[Strata] Weather sync failed: " + e.Message, map.uniqueID ^ 0x5E47E01);
            }
        }

        private void SyncFromSource()
        {
            Map source = UpperDeckUtility.SourceMapFor(map);
            if (source == null || source.Disposed || source.weatherManager == null)
            {
                return;
            }

            WeatherDef want = source.weatherManager.curWeather;
            if (want == null || map.weatherManager == null)
            {
                return;
            }

            if (map.weatherManager.curWeather != want)
            {
                map.weatherManager.TransitionTo(want);
            }
        }
    }
}
