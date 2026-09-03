using HarmonyLib;
using Verse;

namespace Niceties
{
    [StaticConstructorOnStartup]
    public static class NicetiesInit
    {
        static NicetiesInit()
        {
            HarmonyPatchAll.Apply(new Harmony("azraelgodking.niceties"), "[Niceties]");
            SharedRooms.InjectComps();
            ApparelGender.Capture();
            ApparelGender.Apply(NicetiesMod.Settings?.wearAnyGender ?? true);
            LongEventHandler.ExecuteWhenFinished(() =>
                ModVersionLog.Write("[Niceties]", extra: "shared-rooms-v2"));
        }
    }
}
