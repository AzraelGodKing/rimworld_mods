# Changelog

## [Unreleased]

Stamp `removal-queue-v1`.

AZR-344 AZR-427 AZR-428 AZR-429 AZR-430 AZR-431 AZR-432 AZR-433 AZR-434 AZR-435 AZR-436

### Added
- **Mountain start for The Deep Homestead** - the default and "Random" starting site now favor large hills or mountains. You can still pick any tile, and the part can be removed in the scenario editor. EN/CN/RU.
- **Safe removal summary** - the removal report opens with one line: how many things to deconstruct, consume or sell, need attention, and are safe.
- **Multiplayer notice** - with the Multiplayer mod active, Conflicts warns that safe-removal deconstruct only acts on your client.
- **New series mods show up on their own** - any other active AzraelGodKing mod is listed in the hub (with its own "Prepare to remove" button) without waiting for an Azrael update.

### Fixed
- **Living World ↔ Deep Colony goodwill** - the hub only says "live" when Deep Colony's goodwill listener actually registered with Living World, not just when both mods are loaded.
- **Hub scrolling** - the settings page no longer cuts off the last "Prepare to remove" buttons when the failed-patches section is shown.
- **Safe removal off-map pawns** - Deep Colony perks and trauma on kidnapped colonists, quest guests, and other off-map pawns now count in the report.

### Changed
- **Safe removal deconstruct** - the button now queues the designations and applies them on the next game tick (series multiplayer-safer pattern) instead of editing the map straight from Mod Options. If the game is paused, they appear when you unpause.
- **Build stamps** - the hub reads `BuildStamp` from any series mod that follows the `XBuildInfo` convention (Azrael now does too) before falling back to the log.
- **Release metadata** - `mod.json` download now names the 1.3.0 zip; ROADMAP lists everything shipped through 1.3.0.

## [1.3.0]

Player-facing version **1.3.0** (`About.xml` `modVersion`).

See `About/changelog.txt` for the full player-facing notes for this ship.
## [1.2.0]

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

## [1.1.0]

Player-facing version **1.1.0** (`About.xml` `modVersion`). Stamp `removal-wizard-v1`.

AZR-137 AZR-136

### Added
- **Safe removal wizard** (`removal-wizard-v1`, AZR-137) — hub action lists orphan buildings/items/blockers and can queue deconstruction. Not a save scrubber. CN/RU keys for the hub.
- **Hub health** (`hub-health-v1`, AZR-136) — Mod Options → Azrael now shows DLC, build stamps, Dubs Bad Hygiene bridges (not loaded vs type missing), and failed Harmony patch classes with the exception summary. Copy report includes RimWorld version, all nine series mods, DLC, bridges, conflicts, and patch failures. Works from the main menu. SafePatchAll logs the full exception and reports into Azrael after startup.

## [1.0.0]

Player-facing version **1.0.0** (`About.xml` `modVersion`). Startup writes `[Azrael] v1.0.0 loaded from ...` in Player.log (`update-news-v1`).

### Added
- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Series hub** — Mod Options → Azrael lists loaded series mods and About versions, live soft-compat bridges (root cellars, wells, Stormproof rooms, Living World goodwill), named conflicts (AASB / MultiFloors with Strata), and failed Harmony patch classes from this session's log. Copy report for support. Read-only; missing mods show as not loaded. EN/CN/RU.
- **The Deep Homestead** scenario — 3 settlers, farm kit, mountain-foothills opener; forces Azrael; MayRequire Homesteader / Strata start research and rock salt.
- **Standalone storyteller fallback** — injects `StorytellerDef` Azrael only when Homesteader is not loaded (Homesteader owns the canonical copy). `PatchOperationFindMod` matches the Homesteader display name so both mods together do not duplicate the def.
- **CI zip** — ~~`Azrael.zip` with the compiled DLL is packed on `latest` so Deep Homestead's forced-storyteller part loads.~~ **Superseded:** Azrael is not release-ready; CI no longer builds or publishes `Azrael.zip`.
- **Soft series load order** — loadAfter Homesteader / Strata / Stormproof / Nemesis / Deep Colony / Living World / Date Night without hard Workshop deps (Harmony only).

### Fixed
- **RimWorld 1.6 storyteller comps** — Cassandra Classic structure + `CassandraClassic` portraits (standalone patch + Homesteader canonical).
- **Deep Homestead Strata research** — start research → `Strata_DiggingDown` (was incorrectly the research tab defName).
