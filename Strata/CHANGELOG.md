# Changelog

All notable changes to Strata are documented here.

## [2.0] — 2026-07-16

**Strata V2** — dig down, build up, breathe deep, and (with Odyssey) take linked floors with your gravship.

### Added
- **Building Up** — tower stairwells and elevators open outdoor **Level +N** roof decks; Page Up/Down and Levels tab support upper floors.
- **Gravship stack (Odyssey)** — A+/B+ pocket maps on ship substructure travel on launch and reattach on landing; gravship stairwells, metal underdeck, synced substructure for Odyssey placement.
- **Living Below** — shoring pillar, gas airlock, ore hoist, fungus farm, sump pump, mine lamp, shaft bellows, lime scrubber; cave fungus and lime items; flood seep incident.
- **Deep threats & quest sites** — gas firestorm, deep siege, cave breakthrough, early prospector; collapsed mine, sealed vault, and geothermal vent multi-level sites.
- **Mine atmosphere** — methane, black damp, fungal spores, and steam channels with counter buildings; dedicated Strata research tab.
- **Breathable deep** — O₂/CO₂ simulation, hypoxia/CO₂ exposure, oxygen and CO₂ pumps, shaft gas exchanger, canary and bird cages, full gas overlay.
- **Gas infrastructure** — linked gas pipe on the conduit layer (smoke and all Strata channels), hidden gas pipe, smoke hole, exhaust fan, updraft filter.
- **Fluid shafts** — cross-level junctions for DBH plumbing, DCH heating/air-con, Rimatomics coolant, VHGE helixien, VEF/Rimefeller chemfuel and crude.
- **One colony column** — cross-level work, food, rest, medical, joy, and schedule relays; level roles; storage priority and construction/bill ingredient pull; ritual escorts; caravan pull from below; raid pursuit across floors.
- **World & gen** — rich ore nodes, ancient colony surface stairwell (optional), native warren or Biomes! Caverns layouts, hidden geothermal and gas chambers, deep gas economy (well + generator).
- **Stairwell art** — rotatable directional handrail stairs (default); optional **Multifloor Stairs** setting swaps bundled MultiFloors art (off by default).

### Changed
- New levels match the **parent map size** (1:1 stack under the level above).
- A+ maps are **roof decks** (buildable only where roofed below + shaft plaza), not full concrete pads.
- **Performance** — background occupied levels, vacant hibernation, throttled alerts and gravship deck sync (settings toggles).
- Elevators research gates on *digging down*, not deep excavation.

### Fixed
- Gravship launch UI collection-modified crash, landing/onboard NREs, and empty-substructure placement preview.
- Multi-level save load: roof grid, `WorldGrid` tile repair, pocket-map plant growth, room HUD on A+/B+.
- Handrail stair defs: `rotatable=true` required for `Graphic_Multi` draw offsets (RimWorld 1.6 validation).
- Landings inherit entrance rotation; dig-shaft blueprints respect landing facing.
- Underground storyteller and incident targeting on pocket maps; ancient stairwell enter/generation crashes.
- B2 power (dig-shaft transmitter, stairwell overlay), oxygen pump rate/registration/ventilated rooms.
- Gas pipe linking (`Custom10`), cross-mod duct conflicts, fluid junction graphics and VEF/Rimatomics transfer.
- Cross-level haul spam, bill ingredients, Misc. Robots on shafts, relay session state on load, sealed-shaft siege on indestructible landings.
- Dozens of startup/XML/Harmony 1.6 compat fixes across incidents, canary, mine lamp, lime scrubber, and dev tools.

---

## [1.1] — 2026-07-12

Level sharing, cross-level storage and construction, smoke ventilation overhaul, pursuing raids, deep insect eruptions, shaft power and legacy landing migration. See git history for full 1.1 notes.

## [1.0] — Strata V1

Initial release: excavated levels, relays, smoke sim, Levels tab, elevators, shaft power, raid pursuit.
