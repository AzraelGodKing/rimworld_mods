using RimWorld;
using Verse;

namespace WaveSurvivalReforged
{
    [DefOf]
    public static class WSR_DefOf
    {
        public static StorytellerDef WSR_WaveSurvival;
        public static WaveSurvivalConfigDef WSR_Config;

        static WSR_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WSR_DefOf));
        }
    }
}
