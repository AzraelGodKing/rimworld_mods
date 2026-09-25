# Changelog

## [1.0.0]

Player-facing version **1.0.0** (`About.xml` `modVersion`). Stamp `road-hazard-v1`.

AZR-84 AZR-85 AZR-205

### Added
- **Chronicle tab** (AZR-84) — world history UI with faction and hear-channel filters. CN/RU keys.
- **Rumour distortion** (AZR-85) — far news can arrive wrong and later correct in the chronicle.
- **The road** (AZR-205) — letter after eight unique caravan tiles; way-camps on a stretch they already walked; road-worn hediff on long trips. Route hazards (mud/heat/cold/wind) on that trail; camps make them rarer.

### Fixed
- **Guarded Harmony** — each patch class is applied on its own; one missing target logs and skips instead of aborting the rest of Living World.

### Changed
- **CI zip** — Living World is not release-ready; CI no longer builds or publishes `LivingWorld.zip`.

### Also
- **Chinese and Russian** — Keyed packs plus DefInjected for the Listening Post scenario, incidents, and world objects (parity with English).
- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Unlisted docs page** — `docs/living-world.html` with `noindex` (not linked from the public hub).
- **Languages README** — translator stub for Keyed packs (`repo-hygiene-1-6`).
- **Listening Post scenario** — off-map world showcase start; locks Azrael when Homesteader is loaded.
- **Phase 2 — Wars and fallout** (`living-world-phase2`) — NPC faction diplomacy and player-facing fallout.
  - `FactionPairState` Peace / Tension / War / Alliance; skirmish, battle, white peace, rare pact/betrayal write the chronicle and nudge settlement prosperity.
  - Concurrent war cap, war-rate slider, Mod Options for diplomacy / fallout / trade blackout / war sites / warbands (warbands default off).
  - Refugee incident after severe visible wars; optional pass-through warband (points-capped).
  - Short-lived generic war sites on the world map (tile-avoid vs Nemesis by name).
  - Trade blackout soft-blocks trader caravans for factions in total war.
  - Dev tools: dump pairs, force diplomacy / war / battle / refugees.
- **Phase 1 foundation** (`living-world-phase1`) — off-map chronicle + settlement morph.
  - Slow sim pulse with Mod Options (interval, verbosity, letter/morph caps, proximity).
  - Hear-rules: major always; medium = contact / nearby / allies; high adds comms + more noise.
  - Morphs: prosperity drift, ownership flip, abandon, outpost, epithet; inspector / label cues.
  - `LivingWorldSignals` soft bus for Deep Colony / Nemesis / Homesteader (no hard deps yet).
  - Dev tools: dump chronicle, force morph kinds, fake skirmish letter.
