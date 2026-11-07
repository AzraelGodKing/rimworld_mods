using System;
using RimWorld;
using Verse;

namespace Stormproof
{
    // Public surface for sister mods and other modders. Resolve it once by name
    // ("Stormproof.StormproofCompatApi"), check ApiVersion, and fall back to
    // "no Stormproof" answers when it is missing. Common/Sibling/StormproofClient.cs
    // is the reference client the Azrael mods use.
    //
    // Version 2 (Stormproof 2.0.0) is the first version that ships. Members are
    // only ever added; a member's meaning never changes within a version.
    public static class StormproofCompatApi
    {
        public const int ApiVersion = 2;

        private static float? dampenerRadius;

        // Brownout on the thing's power grid, 0 (none) to 1 (full). 0 with
        // brownout disabled in settings or when the thing has no grid.
        public static float BrownoutFor(Thing thing) => BrownoutUtility.For(thing);

        // True when a powered EMP dampener covers the thing right now.
        public static bool IsEmpProtected(Thing thing) => CompEmpDampener.Shielded(thing);

        // True when a surge protector on the map would eat the next Zzzt.
        public static bool MapHasReadySurgeProtector(Map map) =>
            map != null && ZzztUtility.ReadyProtector(map) != null;

        // The EMP dampener's cover radius in cells, from its def.
        public static float DampenerRadius
        {
            get
            {
                if (dampenerRadius == null)
                {
                    ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail("Stormproof_EmpDampener");
                    dampenerRadius = def?.GetCompProperties<CompProperties_EmpDampener>()?.protectRadius
                        ?? new CompProperties_EmpDampener().protectRadius;
                }
                return dampenerRadius.Value;
            }
        }

        // How hard the weather is driving water and charge into the ground,
        // 0 to 1: rain rate, more under lightning weather, at least half during
        // an ion storm. Pass the surface map; underground maps have no weather.
        public static float StormSurgeIntensity(Map map)
        {
            WeatherManager weather = map?.weatherManager;
            if (weather == null)
            {
                return 0f;
            }
            float surge = weather.RainRate * 0.7f;
            if (CompWeatherForecaster.BringsLightning(weather.curWeather))
            {
                surge += 0.3f;
            }
            if (IonStormActive(map))
            {
                surge = Math.Max(surge, 0.5f);
            }
            return Math.Min(1f, surge);
        }

        // Ticks until the current weather may change, or -1 when unknown.
        public static int ForecastRemainingTicks(Map map)
        {
            if (map?.weatherDecider == null
                || !WeatherDeciderAccess.TryGetDuration(map.weatherDecider, out int duration))
            {
                return -1;
            }
            return Math.Max(0, duration - map.weatherManager.curWeatherAge);
        }

        // True when the weather the map is in, or fading into, makes lightning.
        // RimWorld picks the following weather only when this one ends, so this
        // is as far ahead as anyone can see.
        public static bool WeatherBringsLightning(Map map) =>
            CompWeatherForecaster.BringsLightning(map?.weatherManager?.curWeather);

        // Ion storm or dry lightning front on the map.
        public static bool IonStormActive(Map map)
        {
            GameConditionManager conditions = map?.gameConditionManager;
            if (conditions == null)
            {
                return false;
            }
            return Active(conditions, StormproofDefOf.Stormproof_IonStorm)
                || Active(conditions, StormproofDefOf.Stormproof_DryLightning);
        }

        // Raised once per affected map when an ion storm starts or ends there.
        public static event Action<Map> IonStormStarted;
        public static event Action<Map> IonStormEnded;

        internal static void NotifyCondition(GameCondition condition, Map map, bool started)
        {
            if (condition?.def != StormproofDefOf.Stormproof_IonStorm || map == null)
            {
                return;
            }
            Action<Map> handler = started ? IonStormStarted : IonStormEnded;
            if (handler == null)
            {
                return;
            }
            // One broken subscriber must not stop the condition from starting or ending.
            foreach (Action<Map> one in handler.GetInvocationList())
            {
                try
                {
                    one(map);
                }
                catch (Exception e)
                {
                    Log.ErrorOnce("[Stormproof] Ion storm subscriber " + one.Method.DeclaringType + " threw: " + e,
                        one.Method.GetHashCode());
                }
            }
        }

        private static bool Active(GameConditionManager conditions, GameConditionDef def) =>
            def != null && conditions.ConditionIsActive(def);
    }
}
