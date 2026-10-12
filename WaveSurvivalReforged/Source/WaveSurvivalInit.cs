using AzraelCommon;
using Verse;

namespace WaveSurvivalReforged
{
    [StaticConstructorOnStartup]
    public static class WaveSurvivalInit
    {
        static WaveSurvivalInit()
        {
            LongEventHandler.ExecuteWhenFinished(() => ModVersionLog.Write("[Wave Survival Reforged]"));
        }
    }
}
