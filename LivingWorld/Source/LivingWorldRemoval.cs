using RimWorld;
using Verse;

namespace LivingWorld
{
    /// <summary>"Prepare this save for removal" from Mod Options.</summary>
    public static class LivingWorldRemoval
    {
        /// <summary>UI entry: also turns the player's own Mod Options off, then runs the synced part.</summary>
        public static void Confirm()
        {
            if (LivingWorldMod.Global != null)
            {
                LivingWorldMod.Global.enabled = false;
                LivingWorldMod.Global.Write();
            }
            Run();
        }

        /// <summary>Changes the game; a Multiplayer sync method.</summary>
        public static void Run()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            int removed = comp.PrepareForRemoval();
            if (comp.SimSettings != null)
            {
                comp.SimSettings.enabled = false;
            }
            Messages.Message(
                "LivingWorld_Settings_PrepareRemovalDone".Translate(removed),
                MessageTypeDefOf.TaskCompletion,
                historical: false);
        }
    }
}
