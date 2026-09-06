# Changelog

Detailed notes for **Niceties** only. ## [Unreleased]

Player-facing version **1.1.1** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.1.1 loaded from ...; update-news-v1` in Player.log.

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
