using Verse;

namespace DeepColony
{
    public class DeepColonyMod : Mod
    {
        public DeepColonyMod(ModContentPack content) : base(content)
        {
            Log.Message($"[{content.Name}] loaded.");
        }
    }
}
