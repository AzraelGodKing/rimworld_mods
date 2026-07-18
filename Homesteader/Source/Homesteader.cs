using HarmonyLib;
using Verse;

namespace Homesteader
{
    [StaticConstructorOnStartup]
    public static class HomesteaderHarmony
    {
        static HomesteaderHarmony()
        {
            new Harmony("azraelgodking.homesteader").PatchAll();
        }
    }
}
