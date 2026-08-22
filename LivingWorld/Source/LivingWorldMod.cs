using HarmonyLib;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    public class LivingWorldMod : Mod
    {
        public static LivingWorldSettings Settings;

        public LivingWorldMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LivingWorldSettings>();
            ModVersionLog.Write("[LivingWorld]", content);
            new Harmony("azraelgodking.livingworld").PatchAll();
        }

        public override string SettingsCategory() => "LivingWorld_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            LivingWorldSettings.DoSettingsWindow(inRect);
        }
    }
}
