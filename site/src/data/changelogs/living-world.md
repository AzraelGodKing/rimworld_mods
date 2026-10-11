# Changelog

## [Unreleased]

AZR-334 AZR-417 AZR-418 AZR-419 AZR-421 AZR-422 AZR-423 AZR-424 AZR-425 AZR-426

Stamp `world-traffic-v1`.

### Added
- **Inter-settlement caravans** (LW9) — NPC caravans now travel between friendly settlements on the world map. Arrivals go in the chronicle without a letter and sometimes lift the destination's prosperity. Rarely, one passing near your colony turns aside to trade. Toggle in Mod Options.
- **World state view** — a button on the Chronicle tab shows current wars, tensions, and alliances between NPC factions, with intensity and trade blackouts.
- **Severity filter** — the Chronicle tab can show all news, normal and major, or major only.
- **War scar timer** — war-site inspect text says how long until the site fades.
- **Sibling-mod hooks** — `LivingWorldSignals.StableHookKinds` lists the event kinds other mods can safely listen for.
- Debug action: spawn a traffic caravan.

### Fixed
- **Refugees and warbands** no longer fire early with a random faction. They wait for the intended delay and always come from the war you heard about.
- **Rumour corrections** still arrive when the original news has aged out of the chronicle.
- Debug "Force" morph actions now ignore the per-settlement cooldown, as intended.
- `mod.json` download filename now matches 1.1.1.

### Changed
- Settlement label and inspect lookups are now constant-time on large worlds.

## [1.1.1]

Player-facing version **1.1.1** (`About.xml` `modVersion`).

See `About/changelog.txt` for the full player-facing notes for this ship.
## [1.1.0]

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

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
