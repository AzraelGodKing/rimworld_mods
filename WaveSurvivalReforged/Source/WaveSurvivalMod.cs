using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    public class WaveSurvivalSettings : ModSettings
    {
        public bool showHud = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showHud, "showHud", true);
        }
    }

    public class WaveSurvivalMod : Mod
    {
        public static WaveSurvivalSettings Settings { get; private set; }

        public WaveSurvivalMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WaveSurvivalSettings>();
        }

        public override string SettingsCategory() => "WSR_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("WSR_SettingsShowHud".Translate(), ref Settings.showHud, "WSR_SettingsShowHudTip".Translate());
            list.End();
        }
    }
}
