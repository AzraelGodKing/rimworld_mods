#nullable enable
using AzraelCommon;
using HarmonyLib;
using Verse;

namespace Niceties
{
    [StaticConstructorOnStartup]
    public static class NicetiesInit
    {
#if RIMWORLD_1_7
        internal const string BuildStamp = "niceties2-net10, 1.7 build";
#else
        internal const string BuildStamp = "niceties2, 1.6 build";
#endif

        static NicetiesInit()
        {
            SafePatchAll.Apply(new Harmony("azraelgodking.niceties"), "[Niceties]");
            SharedRooms.InjectComps();
            ApparelGender.Capture();
            ApparelGender.ApplyEffective();
            NicetiesMultiplayer.RegisterSyncMethod(typeof(SharedRooms), nameof(SharedRooms.SetMarkedForBed));
            LongEventHandler.ExecuteWhenFinished(() =>
                ModVersionLog.Write("[Niceties]", extra: BuildStamp));
        }
    }
}
