using Verse;

namespace Niceties
{
    /// <summary>
    /// Host-authoritative snapshot of Niceties settings that affect deterministic sim.
    /// Scribed with the game so Multiplayer joiners receive the host values even when
    /// their local Mod Options file differs. Outside a Multiplayer session the snapshot
    /// follows Mod Options on every load and edit; inside one it stays as the host baked it,
    /// because a local Mod Options edit would only change this client's sim.
    /// </summary>
    public class GameComponent_NicetiesSim : GameComponent
    {
        private NicetiesSettings snapshot;
        private bool loadedFromSave;

        public GameComponent_NicetiesSim(Game game)
        {
            // Starting pawns are generated before FinalizeInit; drop the previous game's tags.
            ApparelGender.Apply(NicetiesMod.Settings == null || NicetiesMod.Settings.wearAnyGender);
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
            if (snapshot == null || !loadedFromSave || !NicetiesMultiplayer.InSession)
            {
                PullFromModSettings();
            }

            ApparelGender.ApplyEffective();
            CryptosleepBar.MarkDirty();
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

        public bool DiffersFromModSettings()
        {
            NicetiesSettings src = NicetiesMod.Settings;
            return snapshot != null && src != null && snapshot.SimFingerprint() != src.SimFingerprint();
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

        internal static bool CanApplyModSettingsNow => !NicetiesMultiplayer.InSession;

        internal static void SyncFromModSettings()
        {
            if (!CanApplyModSettingsNow)
            {
                return;
            }

            GameComponent_NicetiesSim.Get()?.PullFromModSettings();
        }
    }
}
