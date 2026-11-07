#nullable enable
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Maple sap runs hard in cool spring weather, slows in fall and winter, and nearly stops in summer.
    /// </summary>
    internal static class MapleSeason
    {
        internal static float YieldFactor(Map? map)
        {
            if (map == null)
            {
                return 1f;
            }

            SapSeason season;
            switch (GenLocalDate.Season(map))
            {
                case Season.Spring:
                    season = SapSeason.Spring;
                    break;
                case Season.Winter:
                case Season.PermanentWinter:
                    season = SapSeason.Winter;
                    break;
                case Season.Fall:
                    season = SapSeason.Fall;
                    break;
                default:
                    season = SapSeason.Summer;
                    break;
            }

            return HomesteaderPure.MapleYieldFactor(season, map.mapTemperature.OutdoorTemp);
        }
    }

    public class CompProperties_MapleSap : CompProperties
    {
        public CompProperties_MapleSap()
        {
            compClass = typeof(CompMapleSap);
        }
    }

    public class CompMapleSap : ThingComp
    {
        public override string? CompInspectStringExtra()
        {
            Map? map = parent.MapHeld;
            if (map == null)
            {
                return null;
            }

            float f = MapleSeason.YieldFactor(map);
            if (f >= 1.1f)
            {
                return "Homesteader_MapleSeason_Running".Translate();
            }

            if (f <= 0.35f)
            {
                return "Homesteader_MapleSeason_Dry".Translate();
            }

            return "Homesteader_MapleSeason_Slow".Translate();
        }
    }
}
