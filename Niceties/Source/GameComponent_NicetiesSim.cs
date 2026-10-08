using Verse;

namespace Niceties
{
    /// <summary>
    /// Host-authoritative snapshot of Niceties settings that affect deterministic sim.
    /// Scribed with the game so Multiplayer joiners receive the host values even when
    /// their local Mod Options file differs. Mid-session Mod Options edits still need
    /// Multiplayer.API SyncField for live cross-client sync — without that dependency
    /// this covers join-time / save-bake only (AZR-292).
    /// </summary>
    public class GameComponent_NicetiesSim : GameComponent
    {
        private NicetiesSettings snapshot;
        private bool loadedFromSave;

        public GameComponent_NicetiesSim(Game game)
        {
        }

        public NicetiesSettings Snapshot => snapshot;

        public override void ExposeData()
        {
            if (snapshot == null)
            {
                snapshot = new NicetiesSettings();
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                loadedFromSave = true;
            }

            snapshot.ExposeData();
        }

        public override void FinalizeInit()
        {
            if (snapshot == null || !loadedFromSave)
            {
                PullFromModSettings();
            }
        }

        public void PullFromModSettings()
        {
            NicetiesSettings src = NicetiesMod.Settings;
            if (src == null)
            {
                return;
            }

            if (snapshot == null)
            {
                snapshot = new NicetiesSettings();
            }

            snapshot.CopyFrom(src);
        }

        public static GameComponent_NicetiesSim Get()
        {
            return Current.Game?.GetComponent<GameComponent_NicetiesSim>();
        }
    }

    /// <summary>
    /// Effective settings for patches: in-game prefer the GameComponent snapshot.
    /// </summary>
    internal static class NicetiesSim
    {
        internal static NicetiesSettings Settings
        {
            get
            {
                GameComponent_NicetiesSim gc = GameComponent_NicetiesSim.Get();
                if (gc?.Snapshot != null)
                {
                    return gc.Snapshot;
                }

                return NicetiesMod.Settings;
            }
        }

        internal static void SyncFromModSettings()
        {
            GameComponent_NicetiesSim.Get()?.PullFromModSettings();
        }
    }
}
