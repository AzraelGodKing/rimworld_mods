# Azrael — ROADMAP

## Release

- First public release is **1.0.0** for RimWorld **1.6 and 1.7** from one source (decided 2026-11-07, AZR-586). No frozen 1.6 build: `1.6/Assemblies` (net472) and `1.7/Assemblies` (net10) come from the same code. The "Azrael 2" Linear tickets are delivered in 1.0.0.
- Multiplayer: soft support like Date Night (decided 2026-11-07). The safe-removal Deconstruct runs through a Multiplayer sync method registered by reflection; no Multiplayer DLL ships.

## Shipped (this package)

- [x] Scenario **The Deep Homestead** (forced Azrael, MayRequire Homesteader/Strata)
- [x] Standalone **Azrael** storyteller inject when Homesteader absent (canonical teller lives in Homesteader)
- [x] Series hub (Mod Options → Azrael): loaded mods + versions, live bridges, named conflicts, copy-report
- [x] Hub health: DLC, build stamps, Dubs Bad Hygiene bridges, failed Harmony patch classes
- [x] Safe removal wizard: buildings, items, caravan cargo, blockers (Nemesis hunt or truce, colonists on a Strata pocket floor), queued deconstruction
- [x] Load-order conflict warning (series mod loaded after Azrael)
- [x] World-state removal scan: Deep Colony family letters, parked Nemesis world pawn, Deep Colony hediffs on map, caravan, and world pawns
- [x] RU/CN DefInjected for the scenario, forced-storyteller part, and storyteller
- [x] Optional mountain start for The Deep Homestead (default and random starting site favor large hills or mountains)
- [x] Multiplayer notice in Conflicts; safe-removal deconstruct queued and applied on game tick

## Later

- [ ] Custom Azrael portraits (stop reusing Cassandra art) — preferably once, in Homesteader
- [x] Per-incident chance weights for series events under the Azrael storyteller (`Azrael_SeriesIncidentWeights`)
- [ ] `XBuildInfo.BuildStamp` in Homesteader, Stormproof, Nemesis, Living World, Date Night, Niceties (hub already reads the convention; mods without a typed stamp stay blank)
- [ ] Living World traffic weights tied to Azrael (see series ROADMAP)
