using AzraelCommon;
using HarmonyLib;
using Verse;

namespace Niceties
{
    [StaticConstructorOnStartup]
    public static class NicetiesInit
    {
        static NicetiesInit()
        {
            SafePatchAll.Apply(new Harmony("azraelgodking.niceties"), "[Niceties]");
            SharedRooms.InjectComps();
            ApparelGender.Capture();
            ApparelGender.ApplyEffective();
            NicetiesMultiplayer.RegisterSyncMethod(typeof(SharedRooms), nameof(SharedRooms.SetMarkedForBed));
            LongEventHandler.ExecuteWhenFinished(() =>
                ModVersionLog.Write("[Niceties]", extra: "openbuilds-roommate-v1"));
        }
    }
}
