# Changelog

Detailed notes for **Niceties** only.

## [2.0.0]

Player-facing version **2.0.0** (`About.xml` `modVersion`). Stamp `niceties2` (`niceties2-net10, 1.7 build` or `niceties2, 1.6 build` in Player.log). Ships for RimWorld **1.6** and **1.7** from the same source; 1.6 is not frozen.

AZR-509 AZR-518 AZR-531 AZR-547 AZR-549 AZR-550 AZR-551 AZR-533 AZR-568 AZR-653 AZR-654 AZR-655 AZR-656 AZR-699 AZR-700 AZR-701 AZR-702 AZR-703

### Added
- **Cryptosleep roster** (AZR-568) — with `cryptosleepRoster` (UI-only, default on) the "+N" note is a button: a `FloatMenu` of the hidden pawns, each jumping to and selecting the holding building (`CameraJumper.TryJumpAndSelect`). Local only, no sync.
- **Hide categories** (AZR-568) — UI-only `hideGrowthVats` (`Building_GrowthVat`) and `hidePods` (`CompBiosculpterPod`, `Building_SubcoreScanner`), off by default, shown when the DLC is active. The note reads "+N hidden" when anyone besides casket pawns is hidden.
- **Melee revenge cap** (AZR-568) — sim knob `meleeHuntMaxRevengeChance` (default 1 = off; Hard preset 0.5) against `PawnUtility.GetManhunterOnDamageChance(prey)`. Postfix on `Pawn.GetInspectString` adds the reason on hunt-designated animals melee hunters skip.
- **Reference-animal buttons** (AZR-702) — Cat, Alpaca, Deer, Muffalo (whichever defs exist) set the cap to that animal's `baseBodySize`.
- **Enclose overlay** (AZR-568) — postfix on `Frame.DrawExtraSelectionOverlays` draws `GenDraw.DrawFieldEdges` over the cached enclosed cells with one wall frame selected (UI-only `showEncloseOverlay`). `leaveAWayOutMaxRoom` sim setting (20–400, default 80) replaces the hardcoded `MaxRegionSize`. `OpenBuildsResult.Trapped` names the first trapped colonist in `Niceties_OpenBuilds_WouldEncloseNamed`.
- **Shared room display** (AZR-703) — postfix on `Room.GetRoomRoleLabel` appends "(shared)" from the room cache, so the room stats readout shows it.
- **Multi-select share** (AZR-568) — `Command_ShareRoom.ProcessGroupInput` sets every selected bed's room to the opposite of the first bed's state, once per room (the default grouped toggle flipped twice for two beds in one room).
- **Noble alert** (AZR-568) — `Alert_NicetiesNobleSharedRoom` (UI-only `sharedRoomNobleAlert`, Royalty only): colonists with `HighestTitleWithBedroomRequirements()` whose owned bed is in a marked room and `!HasPersonalBedroom()`.
- **Settings UX** (AZR-533, AZR-656) — collapsible section per nicety with a per-feature Reset (`ResetFeature`, including that feature's UI-only fields) and a status line: missing Royalty+Ideology, Smarter Construction loaded, `PatchTargets.Unavailable(...)`, or host-baked in a Multiplayer session. The listing rect is the real view height instead of `99999f` (AZR-532).

### Not done
- **Prisoner cells** (AZR-655) — vanilla `CheckForDisturbedSleep` returns early for any sleeper not in the player faction, so prisoners are never woken by cellmates in the first place; nothing to add. Listed in the in-game test list to confirm.

### Changed
- **RimWorld 1.7 build** (AZR-518, AZR-531) — `Niceties.1.7.csproj` targets `net10.0` against `Krafs.Rimworld.Ref 1.7.4998-unstable` and Harmony 3. Every Niceties file is `#nullable enable` and both builds compile with no nullable warnings. `NicetiesMod.Settings` and `NicetiesSim.Settings` are never null, so the ~25 defensive null checks are gone. The build stamp names the folder.
- **Removal wording** (AZR-551, AZR-570) — About, ROADMAP: "Safe to add mid-save; removing it logs a few harmless errors once on the next load."
- **ROADMAP** (AZR-654) — header says 1.6 and 1.7.
- **Performance** (AZR-549):
  - Flood fill moved to `Pure/EncloseFill`: cell-index stamps with a generation counter, a fixed `int[]` queue and pooled `Geometry` objects, so a warm check allocates nothing. Regions proven open are stamped, so a second start in the same open area stops on contact. The game side is a `readonly struct MapGrid : IEncloseGrid` (no boxing). Same closed/open rule as 1.x (closed below 80 cells).
  - Step-aside tie-break is nearest, then lowest z, then lowest x (`EncloseFill.PickStepAside`), instead of `IntVec3.GetHashCode`.
  - Disturbed sleep: vanilla's own early outs (no mood need, awake, not player faction) run before any room work; owners, shared mark and inspect names come from a per-room cache (`Room.ID`, 250 ticks, dropped on mark change and new game). Room-role postfixes still scan uncached so a mark shows up in the next recalculation.
  - Throne check walks `listerBuildings.AllBuildingsColonistOfClass<Building_Throne>()` instead of `room.ContainedAndAdjacentThings`.
  - `GameComponent_NicetiesSim.Get()` is cached per `Game`.
  - Apparel inspect line cached for the selected thing (60 ticks, language, settings change). Share-room gizmo icon loaded once in a `[StaticConstructorOnStartup]` holder (AZR-532).
- **Wear any outfit** (AZR-550, AZR-653) — no def edits. Postfix on `ApparelProperties.CorrectGenderForWearing` answers true, and `ThoughtWorker_WrongApparelGender.CurrentStateInternal` returns inactive, while the toggle is on. A `[ThreadStatic]` depth counter around `PawnGenerator.GeneratePawn(PawnGenerationRequest)` (prefix + finalizer) keeps generation vanilla unless the new nested `wearAnyGenderGenerated` is on. Settings are read lazily from the snapshot, so the 1.x constructor-time `ApparelGender.Apply` from local Mod Options (the MP desync) is gone.
- **Settings model** (AZR-533, AZR-700) — simulation fields live in `record struct NicetiesSimFlags`; copy is assignment and change detection is `==`, replacing the hand-written `CopyFrom` / fingerprints. Mod Options keys are unchanged. Presets apply `ApplyKnobsFrom` (nested knobs only); `ResetToDefaults` is the single full reset and includes `showCryptosleepCount`.
- **Well-kept weapons removed** (AZR-550) — vanilla only deals daily `Deterioration` to worn apparel, so `enableWeaponCare` and the weapon inspect postfix on `ThingWithComps.GetInspectString` never had an effect. Wear math moved to `Pure/NicetiesRules.DailyWearChance`.
- **Save snapshot version** (AZR-551) — `nicetiesSnapshotVersion` (missing = 1). A v1 snapshot loaded in Multiplayer keeps 1.x generation (`wearAnyGenderGenerated = wearAnyGender`). Class names and keys are unchanged, so 1.x saves load as-is.

### Fixed
- **Throne and altar** (AZR-699) — both patches have `Prepare() => ModsConfig.RoyaltyActive && ModsConfig.IdeologyActive`. The DLC room classes live in `Assembly-CSharp`, so there was no `TypeLoadException` to guard against; the patches just never matter without both DLCs.
- **Leave-a-way-out tip** — no longer says "original 1.6 code" (EN, CN, RU).
- **Melee cap** (AZR-550) — compares `prey.BodySize`, not `RaceProps.baseBodySize`. Reference label cache is keyed on the active language.
- **Colonist bar filter** (AZR-547) — settings read once, one-pass rebuild instead of `RemoveAt` per entry. A map group left with no visible colonist gets vanilla's pawn-less map entry so the group count passed to `CalculateDrawLocs` stays right. A repeat layout pass over the already-filtered list keeps the hidden count. The "+N" label checks `ColonistBar.Visible` (bound once by reflection), sizes with `Text.CalcSize`, clamps to the screen, restores GUI state in `finally`, caches text by count and language and draws only on Repaint.
- **Leave-a-way-out cache** (AZR-701) — a finished wall drops cached geometry on the same map within `MaxRegionSize + 2` cells only (`ClearCacheNear`), not the whole cache.

## [1.6.1]

Player-facing version **1.6.1** (`About.xml` `modVersion`). Stamp `openbuilds-roommate-v1`.

AZR-548 AZR-567

### Changed
- **Separate 1.6 and 1.7 assemblies.** Official 1.6 loads `1.6/Assemblies`. Unstable 1.7 loads `1.7/Assemblies`.

### Fixed
- **Leave-a-way-out** (AZR-548) — fresh enclose check on CompleteConstruction; reachable safe spots; EndCurrentJob + queue instead of mid-toil StartJob; clear geometry cache on FinalizeInit. Strata stairwell postfix respects `__runOriginal`.
- **Roommate disturbed sleep** (AZR-567) — skip only when disturber owns a bed in the sleeper's room; no outdoor owned-bed fallback; skip medical/prisoner beds. Tooltip updated EN/CN/RU.

## [1.6.0]

Player-facing version **1.6.0** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.6.0 loaded from ...; sim-snapshot-v2` in Player.log.

### Changed
- **Workshop preview** — new `About/Preview.png` (the comforts, without the old options card). Docs card synced.
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6 (AZR-457). Existing 1.6 DLL; no gameplay change.
- Removed unused `CryptosleepBar.HiddenCount` getter and `NicetiesMultiplayer.Loaded` property. No gameplay change.

## [1.5.1]

Player-facing version **1.5.1** (`About.xml` `modVersion`).

AZR-457

### Changed
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6 (AZR-457). Existing 1.6 DLL; no gameplay change.

## [1.5.0]

Player-facing version **1.5.0** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.5.0 loaded from ...; sim-snapshot-v2` in Player.log.

AZR-371 AZR-372 AZR-373 AZR-374 AZR-375 AZR-376 AZR-377 AZR-378 AZR-379 AZR-380 AZR-381

### Added
- **Hidden cryptosleep count** — a small "+N in cryptosleep" note beside the colonist bar says how many colonists the filter is hiding. Own checkbox under Hidden cryptosleep, on by default.
- **Melee hunt size example** — the body-size cap slider names a vanilla reference animal (for example "warg (1.4)") so the number means something.
- **Roommates on inspect** — a shared bedroom's beds now list who sleeps in the room.
- **Save settings notice** — Mod Options says when the loaded save is running on different baked settings, with an "Apply to this save now" button (single-player). In Multiplayer it explains that the host's settings are in charge.

### Fixed
- **Wear any outfit in Multiplayer** — gender tags now follow the save's host-baked settings like every other nicety, instead of each player's own Mod Options.
- **Share room in Multiplayer** — the bed gizmo now syncs to every player when the Multiplayer mod is loaded, so rooms do not end up as a bedroom for one player and barracks for another.
- **Stale save settings** — in single-player, loading a save now picks up your current Mod Options. You no longer have to reopen Mod Options and change something for a save to notice.
- **Patch target renames** — if a RimWorld update renames the private methods behind "no disturbed sleep for roommates" or the cryptosleep bar filter, Niceties skips just that feature and names it in the log.

### Changed
- **Leave a way out performance** — wall enclosure checks are cached per frame, so several builders finishing walls at once no longer redo the same flood fill, and the step-aside check reuses it.
- **Roadmap** — feature table lists Well-kept weapons and the presets.


## [1.4.1]

Player-facing version **1.4.1** (`About.xml` `modVersion`).

### Fixed
- **Settings disk writes** (AZR-275) — Mod Options writes only when a setting actually changed, not every GUI frame.
- **Leave a way out trap-self** (AZR-276) — frames that would trap only the builder with no step-aside cell fail at job assignment with a clear reason instead of retrying forever.
- **Cryptosleep bar reflection** (AZR-291) — hide-cryptosleep uses `ColonistBar.Entries` instead of a private field by string; warns once if null.

### Added
- **Active preset label** (AZR-293) — Soft / Default / Hard / Custom shown under the preset buttons.
- **Well-kept weapons** (AZR-294) — optional toggle under well-kept apparel; equipped weapons reuse the same quality/Crafting wear formula.
- **Sim settings snapshot** (AZR-292) — a GameComponent bakes settings that gate deterministic sim so Multiplayer joiners share the host values from the save.
- **Hunters Use Melee incompat** (AZR-295) — `About.xml` `incompatibleWith` adds `Mlie.HuntersUseMelee` (the original Melee Hunting is discontinued).
## [1.4.0]

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

## [1.2.0]

Player-facing version **1.2.0** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.2.0 loaded from ...; leave-a-way-out-v1` in Player.log.

### Added

- **Leave a way out** (AZR-201, `leave-a-way-out-v1`) — pawns skip finishing a wall that would trap someone or block leftover frames, and step aside before closing themselves in. Vanilla frames and [Replace Stuff - Continued](https://steamcommunity.com/workshop/filedetails/?id=3526354009) frames both count. In-place wall swaps stay solid so a freezer can still be replaced from inside. Right-click force construct still builds it. Inspired by [Smarter Construction](https://steamcommunity.com/sharedfiles/filedetails/?id=2202185773); original 1.6 code — do not run both.

### Changed

- **Update letter** — Common `UpdateNews` always uses this pack's `About.xml` `modVersion` and the matching `changelog.txt` block. Full notes link to `main` on GitHub (not a Workshop copy of the same package id).

## [1.1.2]

Player-facing version **1.1.2** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.1.2 loaded from ...; settings-i18n-v1` in Player.log.

### Added

- **Chinese and Russian** (AZR-134) — Keyed packs for settings, letters, and inspect text (parity with English).

### Fixed

- **Settings scroll** (AZR-124) — the options window grows with its content, so longer translations stay reachable.

## [1.1.1]

Player-facing version **1.1.1**. Startup wrote `[Niceties] v1.1.1 loaded from ...; update-news-v1` in Player.log.

### Added

- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Shared bedrooms** (`azr-106`) — bed gizmo marks the room as shared. It stays a bedroom instead of barracks, so Slept in bedroom and royal bedroom still apply. Pawns who share a room (marked, or another colonist assigned a bed there) skip `SleepDisturbed`. Does not suppress sharing-a-bed-with-a-non-partner. Inspired by Share Rooms [LWM]; original 1.6 code.

### Fixed

- **Colonist bar** — `ColonistBarDrawLocsFinder.CalculateDrawLocs` is the 1.6 three-arg overload (`List<Vector2>`, `ref float`, `int`). The old two-arg patch was `method null` and logged red; hidden cryptosleep pawns now filter before layout.
- **Share-room gizmo** — no cached `Texture2D` static field, so RimWorld does not warn about a missing `StaticConstructorOnStartup`.

## [1.0.0]

Player-facing version **1.0.0**. Startup wrote `[Niceties] v1.0.0 loaded from ...` in Player.log.

### Added

- **Well-kept apparel** — worn clothes skip or scale the daily deterioration tick by quality and Crafting skill. Combat, fire, and outdoor rot are unchanged. Inspect string shows the current daily-wear rule.
- **Throne and altar** — Royalty `RoomRequirement_ForbidAltars` is treated as met; a throne in the room keeps `RoomRoleWorker_WorshipRoom` from stealing the room role.
- **Wear any outfit** — apparel `gender` tags captured at load and cleared (restored if the toggle is off).
- **Hidden cryptosleep** — colonist bar omits pawns with `InCryptosleep`. Recache on casket accept/eject. Does not hide a pawn who is only being carried to a casket.
- **Melee hunting** — `WorkGiver_HunterHunt.HasHuntingWeapon` also accepts a melee weapon (optional unarmed). Body-size cap rejects oversized prey for melee/unarmed hunters.
- **Mod settings** — each nicety is its own on/off. Nested knobs only appear while that nicety is enabled. Soft / Default / Hard change the knobs, not which features you must run.
- **Workshop preview** — options-card collage using the Homesteader straw-hat sprite and chunky pixel icons (no generated painting).
- **Docs site** — hub page at [`/niceties`](https://azraelgodking.github.io/rimworld_mods/niceties); CI packs `Niceties.zip`. Workshop ID `3794727164`.

Inspired by Workshop ideas (Jecrell Everlasting Apparel, Allow Altars in Throneroom, Wear What You Want, Hide Cryptosleep Pawn, Melee Hunting). Clean-room 1.6 code — not a port of those mods.
