# Changelog

All notable changes to Strata are documented here.

## [Unreleased]

### Added
- **Level sharing**: a second stairwell or elevator built on the same floor now breaks through into the *same* level below instead of opening a parallel pocket dimension. Its landing is carved roughly beneath where it stands (sealed in rock until mined out — pawns can always ride back up). A shared level only collapses when its last entrance is deconstructed.
- **Cross-level storage priority**: hauling now honors stockpile priority across the whole level graph. Items go directly to whichever level's storage has the highest accepting priority (ties stay local), running just above vanilla `HaulGeneral` so a Critical freezer downstairs beats a Low pile by the door. Items already sitting in storage get upgraded to better storage on other levels too. Cargo is only shipped to a level where the storage is actually walkable from the arrival landing.
- **Haul designations travel**: "Haul things" designations (stone chunks etc.) move with the cargo through stairwells and elevators, and items pulled out of storage for a cross-level upgrade get designated on arrival — nothing needs re-marking.
- **Self-extending shaft conduit**: build one shaft power conduit within a few tiles of a stairwell or elevator and it automatically extends a matching junction down to the landing below and drives the tie itself. The lower end lives and dies with the one you built, is replaced automatically if destroyed, and existing hand-built pairs are adopted as-is.
- **Legacy landing migration**: on load, old rope "cave exit" landings (from before Strata spawned its own) are swapped in place for the proper stairwell/elevator landing, complete with power shaft comp.

### Fixed
- **Smoke killed colonists at fueled workbenches in seconds** (reported: "my smithy and smelter killed my colonists in like 15 seconds"). Three compounding bugs:
  - The burner auto-patch tagged *every* refuelable building as a full-rate burner — fueled smithies, stoves, and smelters smoked like generators, **constantly, even while idle**, and a passive cooler would have smoked too. Fueled workbenches now emit gently and **only while a pawn is actually working them**; other refuelables only smoke with real evidence of combustion (flame overlay or heat output).
  - Open exterior doors vented too little to matter — a single worked bench out-emitted the maximum door bonus. Door venting is roughly doubled: one open exterior door now keeps a worked bench's room below the harm threshold.
  - Smoke inhalation severity climbed so fast that full smoke killed in about a minute. Retuned: a pawn in 100% smoke starts coughing after roughly an in-game hour and dies only after several — a hazard you can see and react to.
- **Landings were never Strata buildings**: vanilla's `GenStep_PlaceCaveExit` ignores `exitDef` and always spawns its 3×3 rope cave exit — so no level ever had a real stairwell/elevator landing, and the exit-side power shaft never existed. New `GenStep_PlaceLevelExit` spawns the entrance's actual `exitDef`.
- **Power tie brownout death spiral**: the shaft tie used to push a flat 2,000W toward the emptier grid regardless of demand, draining the source, getting shed by vanilla brownout logic, and locking off until the grid could afford the full draw. The tie is now demand-driven (a grid asks for its running deficit plus a battery-equalization trickle that tapers to zero), flows both ways, respects the elevator's flick switch and breakdowns, and recovers on its own after a brownout. Batteries on each level are optional now.
- **Stairwell-up texture** was an unprocessed 1536×1024 image with a fake checkerboard background baked into the pixels; it rendered squashed with the chevrons reading as a lightning bolt. Cropped to true 256×256 with real transparency.

### Changed
- **Ventilation is now a guarantee**: a room with any working smoke outlet — open sky, an open exterior door, a fan or louver whose exhaust (or duct run) reaches outdoors, or a powered updraft filter beside an unsealed shaft — is hard-capped at a light haze below the harm threshold. No amount of burners can give pawns smoke inhalation in a properly ventilated room; lose the outlet (power cut, door closed, duct broken, shaft sealed) and smoke builds again.
- **Emission retune**: torch lamps (and modded torches/candles) barely smoke now (1.0 → 0.1) — mood lighting, not a hazard. Braziers and other always-lit flames drop from generator level to below campfire level (3.5 → 2.0), so ideoligion rooms don't smoke themselves out.
- GitHub Pages catalog (`docs/strata.html`) updated for level sharing, priority hauling, the demand-driven power tie, and the self-extending shaft conduit; download zip rebuilt.

## [1.0] — Strata V1

### Changed
- GitHub Pages catalog (`docs/strata.html`) updated with smoke ventilation buildings, Levels tab, 200×200 levels, stairwell power pooling, and structured events list.

### Added
- **Raid pursuit**: hiding downstairs no longer ends a fight. Raiders with nobody left to shoot at on their level find an unsealed stairwell or powered elevator and come down (or up) after your colonists, re-forming into an assault once they arrive - underground they fight to the end, since there's no map edge to flee across. Sealing a stairwell stops pursuit cold, making the seal toggle a real defensive decision. A warning message fires when pursuers start using a stairwell.
- **Levels tab**: a new bottom-bar tab (appears once the first level is excavated) listing every floor of the colony with colonist count, hostile count, and temperature. Click a row to jump the camera to that level's stairwell; **Rename** assigns custom labels (e.g. "Freezer floor", "Workshop B-2").
- **Exhaust fan (one-way wall vent)**: wall-mounted, rotatable, powered — pulls smoke from the intake room and pushes it out the facing side (another room, outdoors, or a duct run). Does not work in reverse.
- **Smoke louver**: passive one-way wall vent — slow unpowered bleed, same placement rules as the fan.
- **Smoke duct**: floor tiles that chain together so a fan/louver can vent a distant room through a duct run that reaches outdoors.
- **Smoke rises through stairwells**: combustion smoke in an unsealed stairwell or elevator landing naturally convects upward to the level above (like heat). Sealing the shaft stops it.
- **Updraft filter**: powered fan built in a stairwell room — actively pulls smoke from the landing and pushes it up the shaft faster than passive rise alone.
- **Stairwell power shaft**: excavated stairwells now tie both levels' power grids (same pooling as elevators) when each floor is wired into the stairwell. Shaft power conduit remains for a separate tie point beside the stairs.
- **Larger underground levels**: new excavations generate **200×200** pocket maps (was 75×75), with a slightly roomier arrival chamber.
- **Door-based natural venting**: open exterior doors now materially clear combustion smoke (as advertised).
- **Auto-patch mod burners**: at load, fuel-fired and power-generating buildings from other mods get `CompExhaust` when they look like burners (refuelable flames or negative power draw).
- Strata page on the docs site, with hub card, download zip, and comparison entry alongside the other mods.
- **Mod preview** image (`About/Preview.png`) for the in-game mod list and Steam Workshop.

### Changed
- **Smoke duct** is a Structure floor building again (not a conduit draw layer) — still uses linked duct art, but renders on `FloorEmplacement` and is placed tile-by-tile like other buildings.
- **Underground landing art** for `StairsUp` redrawn to match the top stairwell — clearly a staircase, not a rope/ladder.

### Fixed
- **Smoke duct** failed to load at startup (`Could not load Texture2D at 'Things/Building/Linked/Conduit'`) — RimWorld 1.6 power conduits use `PowerConduit_Atlas`, not `Conduit`. Duct now uses a linked floor atlas (`SmokeDuct_Atlas`, 256×256) with thin embedded pipes that connect at corners.
- Underground biome failed to load because `baseWeatherCommonalities` used list-item syntax instead of the vanilla `<Clear>100</Clear>` element format. The broken biome def made every excavated level generate with a null biome, crashing map generation (VacuumComponent, temperature, wild plants, weather, water, glow grid) with endless null-reference errors the moment a colonist went downstairs.
- **Sealed stairwells / elevators block tox gas** through the portal footprint — sealing actually isolates gas pockets on that level instead of letting fumes seep through the shaft tiles.
- **MapComponent registration** — smoke simulation and raid pursuit now attach to every map reliably.
- **Empty-level throttle** no longer skips `MapPostTick`, so smoke and pursuit stay live on vacant underground floors (generators left running still vent correctly).
