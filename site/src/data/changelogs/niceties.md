# Changelog

## [Unreleased]

Player-facing version **1.0.0** (`About.xml` `modVersion`). Startup writes `[Niceties] v1.0.0 loaded from ...; azr-105` in Player.log.

### Added

- **Well-kept apparel** — worn clothes skip or scale the daily deterioration tick by quality and Crafting skill. Combat, fire, and outdoor rot are unchanged. Inspect string shows the current daily-wear rule.
- **Throne and altar** — Royalty `RoomRequirement_ForbidAltars` is treated as met; a throne in the room keeps `RoomRoleWorker_WorshipRoom` from stealing the room role.
- **Wear any outfit** — apparel `gender` tags captured at load and cleared (restored if the toggle is off).
- **Hidden cryptosleep** — colonist bar omits pawns with `InCryptosleep`. Recache on casket accept/eject. Does not hide a pawn who is only being carried to a casket.
- **Melee hunting** — `WorkGiver_HunterHunt.HasHuntingWeapon` also accepts a melee weapon (optional unarmed). Body-size cap rejects oversized prey for melee/unarmed hunters.
- **Mod settings** — each nicety is its own on/off. Nested knobs only appear while that nicety is enabled. Soft / Default / Hard change the knobs, not which features you must run.
- **Workshop preview** — options-card collage using the Homesteader straw-hat sprite and chunky pixel icons (no generated painting).

Inspired by Workshop ideas (Jecrell Everlasting Apparel, Allow Altars in Throneroom, Wear What You Want, Hide Cryptosleep Pawn, Melee Hunting). Clean-room 1.6 code — not a port of those mods.
