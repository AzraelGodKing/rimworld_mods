# Changelog

All notable changes to Strata are documented here.

## [Unreleased]

### Added
- **Raid pursuit**: hiding downstairs no longer ends a fight. Raiders with nobody left to shoot at on their level find an unsealed stairwell or powered elevator and come down (or up) after your colonists, re-forming into an assault once they arrive - underground they fight to the end, since there's no map edge to flee across. Sealing a stairwell stops pursuit cold, making the seal toggle a real defensive decision. A warning message fires when pursuers start using a stairwell.
- **Levels tab**: a new bottom-bar tab (appears once the first level is excavated) listing every floor of the colony with colonist count, hostile count, and temperature. Click a row to jump the camera to that level's stairwell.
- Strata page on the docs site, with hub card, download zip, and comparison entry alongside the other mods.

### Fixed
- Underground biome failed to load because `baseWeatherCommonalities` used list-item syntax instead of the vanilla `<Clear>100</Clear>` element format. The broken biome def made every excavated level generate with a null biome, crashing map generation (VacuumComponent, temperature, wild plants, weather, water, glow grid) with endless null-reference errors the moment a colonist went downstairs.
