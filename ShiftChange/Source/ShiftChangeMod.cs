using HarmonyLib;
using UnityEngine;
using Verse;

namespace ShiftChange
{
    public class ShiftChangeMod : Mod
    {
        public static ShiftChangeSettings Settings;

        public ShiftChangeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ShiftChangeSettings>();
            new Harmony("azraelgodking.shiftchange").PatchAll();
        }

        public override string SettingsCategory() => "ShiftChange_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ShiftChangeSettings.DoSettingsWindow(inRect);
        }
    }
}
