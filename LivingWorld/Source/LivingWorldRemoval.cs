using RimWorld;
using Verse;

namespace LivingWorld
{
    /// <summary>"Prepare this save for removal" from Mod Options.</summary>
    public static class LivingWorldRemoval
    {
        public static void Run()
        {
            GameComponent_LivingWorld comp = GameComponent_LivingWorld.Get;
            if (comp == null)
            {
                return;
            }
            int removed = comp.PrepareForRemoval();
            LivingWorldSettings settings = LivingWorldMod.Settings;
            if (settings != null)
            {
                settings.enabled = false;
                settings.Write();
            }
            Messages.Message(
                "LivingWorld_Settings_PrepareRemovalDone".Translate(removed),
                MessageTypeDefOf.TaskCompletion,
                historical: false);
        }
    }
}
