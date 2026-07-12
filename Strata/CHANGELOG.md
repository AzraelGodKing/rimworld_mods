# Changelog

All notable changes to Strata are documented here.

## [Unreleased]

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
