# Changelog

All notable changes to Stormproof are documented here.

## [Unreleased]

### Added
- Storm capacitor bank: a lightning-only battery. Only spire-caught strikes can charge it, it never self-discharges, and "Zzzt!" surges can't drain it. Automatically discharges up to 2,000W to cover grid deficits. Requires storm protection + batteries research.
- Weather forecaster: shows how long the current weather will hold, announces incoming thunderstorms, and warns an hour before the weather breaks. Requires storm protection research.
- Static discharge pylon: periodically stuns hostile pawns and mechanoids within ~7 tiles, draining 50 Wd of stored strike energy from a storm capacitor bank on its power net per shock. Without capacitor charge it can't fire. Requires flare shielding research.
- Fallout scrubber: slowly removes toxic buildup from pawns and animals in the enclosed room it's placed in. Requires flare shielding research.
- Storm caller: summons a rainy thunderstorm on demand (half-day duration, five-day recharge) - feeds storm spires and douses wildfires. Requires new atmospheric control research (Spacer, 3,500 pts).
- Atmospheric control research project gating the storm caller.
- Spire-caught lightning now charges storm capacitor banks first, then batteries.
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
