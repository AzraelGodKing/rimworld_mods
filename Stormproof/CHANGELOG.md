# Changelog

All notable changes to Stormproof are documented here.

## [Unreleased]

### Added
- Perfect grounding research (Spacer tech, requires flare shielding and advanced fabrication): eliminates the "Zzzt!" surge risk from grid-connected storm spires entirely.

### Changed
- Storm spire is now completely fireproof (flammability 0), and any fires ignited by a caught lightning strike within 3 cells of the spire are snuffed out. The spire keeps sweeping the area for 5 seconds after the strike, since the flame explosion expands over several ticks rather than instantly - a grounded rod shouldn't burn your base down.
- Storm spire surge chance per caught strike lowered from 25% to 5%.

### Fixed
- Solar shield's electricity patch now targets `GameConditionManager.ElectricityDisabled(Map)`, which RimWorld 1.6 changed from a property to a method. The previous getter patch failed to apply and aborted the whole mod's Harmony initialization on startup.
- Armored conduit: replaced the removed-in-1.6 `placingDraggableDimensions` field with `drawStyleCategory` (Conduits), keeping drag placement.

## [1.0.0] — Initial release

### Added
- Solar shield, storm spire, surge protector, EMP dampener, armored conduit, storm vane.
