# Changelog

Detailed notes for **Niceties** only.

## [1.3.1]

Player-facing version **1.3.1** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.3.1 loaded from ...; options-fold-v1` in Player.log.

AZR-218 AZR-219

### Changed
- **Options fold** (`options-fold-v1`, AZR-219) — Mod Options sections collapse/expand.
- **Non-partner bed share** (AZR-218) — opt-in toggle under Shared bedrooms skips `ThoughtWorker_SharedBed` in marked shared rooms.

## [1.3.0]

Player-facing version **1.3.0** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.3.0 loaded from ...; openbuilds-hard-v1` in Player.log.

### Fixed

- **Leave-a-way-out re-entry** (AZR-216) — step-aside / stop is queued for the next `GameComponent` tick instead of calling `StartJob`/`EndCurrentJob` from inside `Frame.CompleteConstruction`.
- **Enclose-check cache** (AZR-215) — multi-slot cache so concurrent builders do not thrash a single global entry.
- **Apparel care Poor quality** (AZR-217) — Poor daily wear is vanilla (`1f`), matching the settings tip (was `0.8f`).
- **Share Rooms incompatible** (AZR-214) — `LWM.ShareRooms` listed alongside Smarter Construction; both patch bedroom/barracks scoring.

## [1.2.1]

Player-facing version **1.2.1** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.2.1 loaded from ...; openbuilds-hard-v1` in Player.log.

### Fixed

- **Leave-a-way-out re-entry** (AZR-216) — step-aside / stop is queued for the next `GameComponent` tick instead of calling `StartJob`/`EndCurrentJob` from inside `Frame.CompleteConstruction`.
- **Enclose-check cache** (AZR-215) — multi-slot cache so concurrent builders do not thrash a single global entry.
- **Apparel care Poor quality** (AZR-217) — Poor daily wear is vanilla (`1f`), matching the settings tip (was `0.8f`).
- **Share Rooms incompatible** (AZR-214) — `LWM.ShareRooms` listed alongside Smarter Construction; both patch bedroom/barracks scoring.

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
