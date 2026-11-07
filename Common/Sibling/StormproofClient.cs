using System;
using System.Reflection;
using Verse;

namespace AzraelCommon
{
    /// <summary>
    /// Sister-mod view of <c>Stormproof.StormproofCompatApi</c> (API version 2 or later).
    /// The delegates are resolved once, on first use. If Stormproof is missing, older,
    /// or anything fails to bind, every member returns the "no Stormproof" answer.
    /// Linked only by the mods that read Stormproof (Strata, Nemesis, Deep Colony).
    /// </summary>
    internal static class StormproofClient
    {
        private const string ApiTypeName = "Stormproof.StormproofCompatApi";
        private const int MinApiVersion = 2;

        private static bool resolved;
        private static Func<Thing, float> brownoutFor;
        private static Func<Thing, bool> isEmpProtected;
        private static Func<Map, bool> readySurgeProtector;
        private static Func<Map, float> stormSurge;
        private static Func<Map, int> forecastRemaining;
        private static Func<Map, bool> ionStormActive;

        public static bool Available
        {
            get
            {
                Resolve();
                return brownoutFor != null;
            }
        }

        public static float BrownoutFor(Thing thing)
        {
            Resolve();
            return brownoutFor == null || thing == null ? 0f : brownoutFor(thing);
        }

        public static bool IsEmpProtected(Thing thing)
        {
            Resolve();
            return isEmpProtected != null && thing != null && isEmpProtected(thing);
        }

        public static bool MapHasReadySurgeProtector(Map map)
        {
            Resolve();
            return readySurgeProtector != null && map != null && readySurgeProtector(map);
        }

        public static float StormSurgeIntensity(Map map)
        {
            Resolve();
            return stormSurge == null || map == null ? 0f : stormSurge(map);
        }

        /// <summary>-1 when unknown or Stormproof is missing.</summary>
        public static int ForecastRemainingTicks(Map map)
        {
            Resolve();
            return forecastRemaining == null || map == null ? -1 : forecastRemaining(map);
        }

        public static bool IonStormActive(Map map)
        {
            Resolve();
            return ionStormActive != null && map != null && ionStormActive(map);
        }

        private static void Resolve()
        {
            if (resolved)
            {
                return;
            }
            resolved = true;
            try
            {
                Type api = GenTypes.GetTypeInAnyAssembly(ApiTypeName);
                FieldInfo version = api?.GetField("ApiVersion", BindingFlags.Public | BindingFlags.Static);
                if (version == null || !(version.GetValue(null) is int v) || v < MinApiVersion)
                {
                    return;
                }
                isEmpProtected = Bind<Func<Thing, bool>>(api, "IsEmpProtected");
                readySurgeProtector = Bind<Func<Map, bool>>(api, "MapHasReadySurgeProtector");
                stormSurge = Bind<Func<Map, float>>(api, "StormSurgeIntensity");
                forecastRemaining = Bind<Func<Map, int>>(api, "ForecastRemainingTicks");
                ionStormActive = Bind<Func<Map, bool>>(api, "IonStormActive");
                // Last: Available reads this one, so it only turns true once the rest bound.
                brownoutFor = Bind<Func<Thing, float>>(api, "BrownoutFor");
            }
            catch (Exception e)
            {
                brownoutFor = null;
                isEmpProtected = null;
                readySurgeProtector = null;
                stormSurge = null;
                forecastRemaining = null;
                ionStormActive = null;
                Log.Warning("[" + Assembly.GetExecutingAssembly().GetName().Name
                    + "] Stormproof compat API could not be bound; carrying on without it. " + e.Message);
            }
        }

        private static T Bind<T>(Type api, string name) where T : class =>
            Delegate.CreateDelegate(typeof(T), api, name, ignoreCase: false, throwOnBindFailure: false) as T;
    }
}
