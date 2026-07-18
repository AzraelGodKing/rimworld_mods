using HarmonyLib;
using Verse;

namespace Homesteader
{
    [StaticConstructorOnStartup]
    public static class HomesteaderMod
    {
        static HomesteaderMod()
        {
            new Harmony("azraelgodking.homesteader").PatchAll();
        }
    }
}
