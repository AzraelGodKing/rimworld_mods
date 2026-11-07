using Verse;

namespace NanoHiveReforged
{
    /// <summary>
    /// Save-level colony defaults that new hives copy. Seeded once per save from
    /// the mod settings (for a 1.x save, those are the old global values, so its
    /// hives keep behaving the same). After that the save owns them.
    /// </summary>
    public class GameComponent_NanoHive : GameComponent
    {
        private HiveConfig defaults;

        public GameComponent_NanoHive(Game game)
        {
        }

        public static GameComponent_NanoHive Instance => Current.Game?.GetComponent<GameComponent_NanoHive>();

        public HiveConfig Defaults
        {
            get
            {
                if (defaults == null)
                {
                    defaults = NanoHiveReforgedMod.Settings.NewColonyDefaults();
                }
                return defaults;
            }
        }

        public void SetDefaults(HiveConfig config)
        {
            defaults = config.Copy();
        }

        // Seed before the first save, so a Multiplayer host sends its own
        // values and clients never seed from their local settings.
        public override void FinalizeInit()
        {
            _ = Defaults;
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref defaults, "defaults");
        }
    }
}
