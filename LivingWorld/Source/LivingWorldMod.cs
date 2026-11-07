using AzraelCommon;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace LivingWorld
{
    public class LivingWorldMod : Mod
    {
        /// <summary>The player's Mod Options (config file). Seeds new games; edited by the settings window.</summary>
        public static LivingWorldSettings Global;

        /// <summary>
        /// Settings the simulation runs on: the current game's copy (saved with the
        /// game, the same on every Multiplayer client), or <see cref="Global"/> when
        /// no game is loaded.
        /// </summary>
        public static LivingWorldSettings Settings => GameComponent_LivingWorld.Get?.SimSettings ?? Global;

        public LivingWorldMod(ModContentPack content) : base(content)
        {
            Global = GetSettings<LivingWorldSettings>();
            ModVersionLog.Write("[LivingWorld]", content, extra: "waylith-v1");
            SafePatchAll.Apply(new Harmony("azraelgodking.livingworld"), "[LivingWorld]");
            LivingWorldMultiplayer.RegisterAll();
        }

        public override string SettingsCategory() => "LivingWorld_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            LivingWorldSettings.DoSettingsWindow(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // Closing Mod Options in a game applies the new values to that game.
            LivingWorldMultiplayer.PushSettingsToGame();
        }
    }
}
