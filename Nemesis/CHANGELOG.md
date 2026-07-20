# Nemesis — Changelog

Foundation by **Dredd (Misakabob)** — original design, persistent antagonist pawn, escape/capture loop, aggression pacing, assaults, waste drops, fixation/prison-break triggers, resolution dialog, and settings. Credited with gratitude; this monorepo package extends that work.

## Unreleased (monorepo integration)

- **Brought into** `rimworld_mods/Nemesis` as `AzraelGodKing.Nemesis` (Harmony 1.6, sibling csproj pattern).
- **Credit** — Dredd / Misakabob named in About + this changelog as original author of the foundation.
- **New harassment** — fake signal → delayed ambush; caravan harassment; EMP / grid sabotage; food-store raids; Anomaly bait (DLC, fail-open).
- **New triggers** — wounded-and-escaped cinematic survival; Ideology slave rebellion (when present).
- **End conditions** — hunt also ends if a fixation target dies or is handed over (nemesis “wins”).
- **Flee-when-losing** — on-map assaults use flee-capable lords; low-HP escape retained from foundation.
- **Personal taunts** — keyed English strings; Homesteader favorite-food / cellar lines and Stormproof ion flavor when those mods are active.
- **Soft compat (fail-open)** — Stormproof EMP dampeners / surge protectors; Strata surface-map preference; Homesteader cellar / favorites via packageId + defName / reflection.
- **Mod-local performance** — nemesis/target pawn registry cache; staggered health checks (faster on viewed map); defer actions during large raids; skip action fire while nemesis is on-map; no LINQ on subdue hot path; dirty flags for resolution / end checks.
- **Safe mid-save add.** Removal: resolve active hunt first so WorldPawn keep-forever pins are released via capture outcomes.

### Inherited from Dredd 1.4.x (summary)

- Persistent named antagonist; cannot be killed until cornered/captured path; escalating taunts/raids/assaults/waste; settings for triggers, pacing, action mix; truce; rogue on peace treaty; fixation + prison break + killed-ally triggers; resolution Execute / Release / Keep / Truce.
