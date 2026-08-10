using HarmonyLib;
using Verse;

namespace Wardrobe
{
    public class WardrobeMod : Mod
    {
        public WardrobeMod(ModContentPack content) : base(content)
        {
        }
    }

    [StaticConstructorOnStartup]
    public static class WardrobeInit
    {
        static WardrobeInit()
        {
            new Harmony("azraelgodking.wardrobe").PatchAll();
        }
    }
}
