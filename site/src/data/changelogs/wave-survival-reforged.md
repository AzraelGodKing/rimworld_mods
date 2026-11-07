# Changelog

Detailed notes for **Wave Survival Reforged** only.

## [Unreleased]

## [2.0.0]

Player-facing version **2.0.0** (`About.xml` `modVersion`). Stamp `wsr2-waves`. Ships for RimWorld **1.6** and **1.7** from the same source; 1.6 is not frozen. Multiplayer is not supported.

AZR-584 AZR-592 AZR-593 AZR-594 AZR-599 AZR-600 AZR-607 AZR-608 AZR-609 AZR-610 AZR-613 AZR-614 AZR-615

### Added
- **Data-driven wave table** (AZR-600) — `WaveTypeDef`s in `Defs/WaveTypes.xml` with `pool` (Normal / Special / Boss), `kind` (Raid / Incident), strategy, arrival mode, faction filter, `minWave`, `weight`, `pointsFactor`, `bonusPoints`, `canSplit`. New: drop pod raids, drops in the base, breachers, mechanoids; Royalty mech clusters; Anomaly shamblers, fleshbeasts, sightstealers; boss waves every 10th wave. Game defs are resolved by name without errors, so a missing one turns that type off.
- **Points curve** (AZR-600) — `pointsByWave` `SimpleCurve`, optional `wealthBlend` toward `StorytellerUtility.DefaultThreatPointsNow`, `breatherDays` after special and boss waves, range checks for every config value in `ConfigErrors`.
- **Pre-rolled waves and HUD** (AZR-609) — the next wave (type, faction, points, split) is rolled when scheduled and saved; the alert and a toggleable HUD show it, and it runs as announced. `WaveTracker` sends a wave report (killed, downed, fled).
- **Presets** (AZR-610) — Casual / Standard / Brutal / Custom, copied into the save at run start (`RunSettings`). Covers wave strength, first-wave and between-wave days, special interval, supply chances, the still-alive mood, wanderer chances. The Wave Survival scenario locks the storyteller.
- **Run stats and best runs** (AZR-615) — saved `RunStats`; on colony loss a letter and a local top-10 list in the mod settings.
- **1.7 wave vignette** (AZR-614) — GLSL `WSR_WaveVignette` in `1.7/Shaders`, ramps over the last 6000 ticks and flashes on arrival, coloured by pool. Settings toggle.
- **No-beacon fallback drop** (AZR-594) — after `noBeaconFallbackDays` (3) a half-size drop, no weapon, near the colony.
- **Build stamp** (AZR-608) — `WaveSurvivalReforgedBuildInfo.BuildStamp`, log prefix `[WaveSurvivalReforged]` for the Azrael hub.

### Changed
- **Two builds, one source** (AZR-599) — 1.6 loads `1.6/Assemblies` (net472). 1.7 loads `1.7/Assemblies`, a .NET 10 build against `Krafs.Rimworld.Ref` 1.7.4998-unstable. Released through the normal workflow and docs-site zip.
- **Full difficulty scaling** (AZR-593) — threat scale applies to every point, including the starting points and bonuses.
- **Supply drops** (AZR-594) — loose items near the beacon count toward `maxStockpile`; weapon weight is commonality × √price; the beacon is cached.
- **Alerts** (AZR-609) — text built once per check and translated on use.

### Fixed
- **Phantom waves** (AZR-593) — announced only after spawning; fallback to a plain raid, then a manhunter pack, else postponed with a message and the wave number kept. Split raids only when each half meets the faction minimum.
- **Trade ship without comms** (AZR-593) — the trader is gated on `CanFireNow`.

### Saves
- `saveVersion` 2 (AZR-613). A 1.0 save loads with the Standard preset, the same wave number and countdown, stats seeded from the wave number, and the next wave pre-rolled on the first check.

## [1.0.0]

First release, RimWorld 1.6. A from-scratch rebuild of Wave Survival [Game Mode] by Justin C, storyteller art by Philbro.

### Added
- Wave Survival storyteller: random events off, raid waves on a timer that grow each time, every fourth wave a siege, sappers, manhunter pack or mass animal insanity.
- Daily supply drops at the orbital trade beacon; Wave Survival scenario; wave countdown and beacon alerts.

### Fixed
- Component drops checked silver instead of components; drops due before a beacon existed were lost; a special wave no faction could run was skipped.
