# Living World — ROADMAP

Design: [../docs/ideas/living-world.md](../docs/ideas/living-world.md) (§8b completion sequence).

## Release

- First public release is **1.0.0**, for RimWorld 1.6 and 1.7 from one source (Azrael's call, 2026-11-07). The "Living World 2" tickets (1.7 port, fixes, API, overlays) ship inside 1.0.0; there is no separate frozen 1.6 build because no 1.6 build ever shipped.
- `1.6/Assemblies` is built from `LivingWorld.csproj` (net472, 1.6 refs); `1.7/Assemblies` from `LivingWorld.1.7.csproj` (net10, 1.7 refs). `LoadFolders.xml` loads exactly one of them.
- Multiplayer: soft support (Azrael's call, 2026-11-07).

## Phase 1 — Foundation (done)

- [x] LW0 Mod stub
- [x] LW1 Chronicle component + tick
- [x] LW2 Hear-rules + letters + spam caps + debug
- [x] LW3 Morph prototype (prosperity + ownership flip)
- [x] LW4 Soft event bus
- [x] LW5 Morph polish (abandon, outpost, epithet, options, inspector)

## Phase 2 — Wars (done)

- [x] LW6 FactionPairState diplomacy
- [x] LW7 Refugee fallout
- [x] DC1 Deep Colony goodwill consumer
- [x] LW8 War polish + generic sites

## Later

- [ ] Sibling flavor (Homesteader HS1)
- [x] Nemesis N3 listen (`Collapse` / `DecisiveVictory` in `NemesisLivingWorld`)
- [x] LW9 Inter-settlement traffic (v1: world-map caravans, chronicle arrivals, rare divert to the player)
- [x] Azrael storyteller weights (series): refugees ×4, warband ×3 in Azrael's `Azrael_SeriesIncidentWeights`
