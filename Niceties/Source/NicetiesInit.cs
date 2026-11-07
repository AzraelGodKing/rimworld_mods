#nullable enable
using AzraelCommon;
using HarmonyLib;
using Verse;

namespace Niceties
{
    /// <summary>
    /// Series convention (AZR-588): the Azrael hub reads <c>Niceties.NicetiesBuildInfo.BuildStamp</c>
    /// by reflection. The stamp names the build folder (AZR-531).
    /// </summary>
    public static class NicetiesBuildInfo
    {
#if RIMWORLD_1_7
        public const string BuildStamp = "niceties2-net10, 1.7 build";
#else
        public const string BuildStamp = "niceties2, 1.6 build";
#endif
    }

    [StaticConstructorOnStartup]
    public static class NicetiesInit
    {
        static NicetiesInit()
        {
            SafePatchAll.Apply(new Harmony("azraelgodking.niceties"), "[Niceties]");
            SharedRooms.InjectComps();
            NicetiesMultiplayer.RegisterSyncMethod(typeof(SharedRooms), nameof(SharedRooms.SetMarkedForBed));
            LongEventHandler.ExecuteWhenFinished(() =>
                ModVersionLog.Write("[Niceties]", extra: NicetiesBuildInfo.BuildStamp));
        }
    }
}
