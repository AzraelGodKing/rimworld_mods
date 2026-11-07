#nullable enable
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
        /// <summary>
        /// 1 = Niceties 1.x (no version key). 2 = Niceties 2.0: adds
        /// <c>wearAnyGenderGenerated</c> and drops the weapon-care flag (AZR-551).
        /// </summary>
        internal const int CurrentSnapshotVersion = 2;

        private static Game? cachedGame;
        private static GameComponent_NicetiesSim? cached;

        private NicetiesSimFlags snapshot;
        private bool hasSnapshot;
        private bool loadedFromSave;
        private int snapshotVersion = CurrentSnapshotVersion;

        public GameComponent_NicetiesSim(Game game)
        {
            // Until FinalizeInit (or a load) fills the snapshot, patches read Mod Options.
            // On a new game that is the host's own file; a Multiplayer joiner always loads
            // a save, so starting pawns never depend on a joiner's local options (AZR-653).
            cachedGame = game;
            cached = this;
        }

        internal bool HasSnapshot => hasSnapshot;

        internal NicetiesSimFlags Snapshot => snapshot;

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                snapshotVersion = CurrentSnapshotVersion;
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                loadedFromSave = true;
                snapshot = NicetiesSimFlags.Defaults;
            }

            // Missing key = a 1.x save.
            Scribe_Values.Look(ref snapshotVersion, "nicetiesSnapshotVersion", 1);
            snapshot.Expose();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                hasSnapshot = true;
                UpgradeSnapshot();
            }
        }

        /// <summary>
        /// A 1.x snapshot has no key for the 2.0 fields, so they took their defaults while
        /// loading. Fixed up here so the next new field has an obvious place to go.
        /// </summary>
        private void UpgradeSnapshot()
        {
            if (snapshotVersion < 2)
            {
                // 1.x cleared apparel gender tags for every pawn, generated ones included.
                // Keep that for a host-baked Multiplayer save so its generation does not change
                // under the players; outside Multiplayer FinalizeInit pulls Mod Options anyway.
                snapshot.wearAnyGenderGenerated = snapshot.wearAnyGender;
            }

            snapshotVersion = CurrentSnapshotVersion;
        }

        public override void FinalizeInit()
        {
            if (!hasSnapshot || !loadedFromSave || !NicetiesMultiplayer.InSession)
            {
                PullFromModSettings();
            }

            CryptosleepBar.MarkDirty();
            OpenBuilds.ClearCache();
            SharedRooms.ClearCache();
        }

        public void PullFromModSettings()
        {
            NicetiesSimFlags src = NicetiesMod.Settings.sim;
            src.Clamp();
            snapshot = src;
            hasSnapshot = true;
            Patch_Apparel_GetInspectString.Invalidate();
        }

        public bool DiffersFromModSettings()
        {
            return hasSnapshot && snapshot != NicetiesMod.Settings.sim;
        }

        /// <summary>
        /// Cached per game. Patches read settings on hot paths, so this avoids a
        /// <c>Game.GetComponent</c> list scan on every call (AZR-549).
        /// </summary>
        public static GameComponent_NicetiesSim? Get()
        {
            Game? game = Current.Game;
            if (game == null)
            {
                return null;
            }

            if (!ReferenceEquals(game, cachedGame))
            {
                cachedGame = game;
                cached = game.GetComponent<GameComponent_NicetiesSim>();
            }

            return cached;
        }
    }

    /// <summary>
    /// Effective settings for patches: in-game prefer the GameComponent snapshot.
    /// </summary>
    internal static class NicetiesSim
    {
        internal static NicetiesSimFlags Settings
        {
            get
            {
                GameComponent_NicetiesSim? gc = GameComponent_NicetiesSim.Get();
                return gc != null && gc.HasSnapshot ? gc.Snapshot : NicetiesMod.Settings.sim;
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
