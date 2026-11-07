# Changelog

## [1.0.0]

First public release, for RimWorld 1.6 and 1.7. Player-facing version **1.0.0** (`About.xml` `modVersion`). Stamp `waylith-v1`.

### Changed
- **RimWorld 1.7 build.** 1.7 loads its own .NET 10 DLL from `1.7/Assemblies`, built against the 1.7 references; 1.6 keeps `1.6/Assemblies`. Same source, same features.
- **Planet layers.** Chronicle entries, settlement moods and pending fallout remember which planet layer their tile is on, so jump-to-tile and distance checks stay right with Odyssey orbits. Older entries load as surface tiles. Inter-settlement caravans only travel between surface settlements.
- **Sturdier patches.** The settlement label and trader-caravan patches bind to the exact game methods; if a game update moves them, the patch logs an error instead of silently patching the wrong method.
- **Settlements in use stay put** (AZR-513) — abandon and faction flips skip a settlement with a generated map, a player caravan, an open trade request, or a quest look target. New outposts need a valid settlement tile at least 10 tiles from a player base.
- **Refugee and warband fallout** (AZR-514) — refugees visit, then leave, and a hostile losing faction does not arrive as a neutral event. Warbands are a combat group scaled by threat points (capped at 600). Fallout lands on the player home nearest the event, later when the trip is longer. News counts as nearby if any colony is in range.
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6 (AZR-457).
- **Workshop preview** — added `About/Preview.png` (settlements, roads, and a caravan).
- **Wars move borders** — settlements no longer change hands at random. A faction only takes a settlement after winning a battle in a hard-fought war, and it takes the loser's settlement closest to its own lands. The Empire and factions in an open quest never lose their last settlement.
- **Battles favour the strong** — bigger, more prosperous factions win more often, but upsets still happen.
- **Relations start from vanilla** — factions that are hostile in vanilla start tense and never cool below tension, vanilla allies start allied, and pirates, permanent enemies and hostile factions never form pacts.
- **Lighter on big worlds** — rumour corrections, letter budgets and the debug queue no longer do work every tick; faction pairs are looked up by key and only rebuilt when factions or settlements change; settlement counts are computed once per tick; the settlement label no longer allocates; way-camp rest checks the cheap conditions first; distance checks are bounded and skip unreachable tiles; caravan routes check reachability before pathing.

### Fixed
- **Master switch** — turning Living World off now also stops way-camp founding, the road letter, the road-worn condition and the way-camp rest bonus.
- **Fallout is not lost** — refugees and warbands stay queued until they actually arrive. They are only dropped when the faction is gone (or, for refugees, hostile to you) or after 60 days.
- **Friendly warbands leave** — a non-hostile warband now crosses the map and exits instead of loitering.
- **War sites** are skipped when a battle has no settlement to anchor them.
- **Debug commands** queued in one game no longer run in the next one you load.

### Added
- **Prepare for removal** — a Mod Options button (in game) that removes Living World war sites, way-camps, caravans and the road-worn condition, then turns the sim off, so the save loads clean without the mod.
- **Save versioning** — saves carry a Living World data version for future upgrades. Moods for settlements that no longer exist and stale fallout are pruned on load and every sim pulse.

Not released yet: Living World stays at 1.0.0 until its first Workshop / Nexus / GitHub release. The 1.1.0–1.2.0 numbers below were internal pre-release bumps that never shipped; their notes are kept as development history.

### Development history (after formerly 1.2.0)

AZR-326

#### Changed
- **Version reset** - `modVersion` and `mod.json` go back to 1.0.0 (download `LivingWorld-1.0.0.zip`) until the first release; `About/changelog.txt` is now a first-release summary.

#### Internal
- Removed dead code: `TryConsumeMorphBudget`, `LivingWorldWarSites.TickExpire`, `LivingWorldSignals.ResetSession`.
- Merged four identical `FindFaction(int)` helpers into one on `WorldEvent`.
- Dropped unused Founding letter Keyed pair (EN/RU/CN); `WorldEventKind.Founding` stays for scribed saves.
- Removed redundant `HasComms()` short-circuit in `PlayerShouldHear` (same as `return verbosity >= 2`).
- Dropped unused `IsStableHookKind` (`StableHookKinds` stays). `ChannelFor` calls `IsNearby` / `HasComms` directly.
- Stripped AZR-326–333 ticket-id prefixes from source comments.
- Languages README no longer implies DefInjected packs are future work.

### Development history (formerly 1.2.0)

AZR-334 AZR-417 AZR-418 AZR-419 AZR-421 AZR-422 AZR-423 AZR-424 AZR-425 AZR-426

#### Added
- **Inter-settlement caravans** (LW9) — NPC caravans now travel between friendly settlements on the world map. Arrivals go in the chronicle without a letter and sometimes lift the destination's prosperity. Rarely, one passing near your colony turns aside to trade. Toggle in Mod Options.
- **World state view** — a button on the Chronicle tab shows current wars, tensions, and alliances between NPC factions, with intensity and trade blackouts.
- **Severity filter** — the Chronicle tab can show all news, normal and major, or major only.
- **War scar timer** — war-site inspect text says how long until the site fades.
- **Sibling-mod hooks** — `LivingWorldSignals.StableHookKinds` lists the event kinds other mods can safely listen for.
- Debug action: spawn a traffic caravan.

#### Fixed
- **Refugees and warbands** no longer fire early with a random faction. They wait for the intended delay and always come from the war you heard about.
- **Rumour corrections** still arrive when the original news has aged out of the chronicle.
- Debug "Force" morph actions now ignore the per-settlement cooldown, as intended.

#### Changed
- Settlement label and inspect lookups are now constant-time on large worlds.

### Initial build

Stamp `road-hazard-v1`.

AZR-84 AZR-85 AZR-205

#### Added
- **Chronicle tab** (AZR-84) — world history UI with faction and hear-channel filters. CN/RU keys.
- **Rumour distortion** (AZR-85) — far news can arrive wrong and later correct in the chronicle.
- **The road** (AZR-205) — letter after eight unique caravan tiles; way-camps on a stretch they already walked; road-worn hediff on long trips. Route hazards (mud/heat/cold/wind) on that trail; camps make them rarer.

#### Fixed
- **Guarded Harmony** — each patch class is applied on its own; one missing target logs and skips instead of aborting the rest of Living World.

#### Changed
- **CI zip** — Living World is not release-ready; CI no longer builds or publishes `LivingWorld.zip`.

#### Also
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
