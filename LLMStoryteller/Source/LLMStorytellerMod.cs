using UnityEngine;
using Verse;

namespace LLMStoryteller
{
    public class LLMStorytellerMod : Mod
    {
        public static LLMStorytellerSettings Settings;

        public LLMStorytellerMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LLMStorytellerSettings>();
        }

        public override string SettingsCategory() => "LLMStoryteller_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
    }
}
