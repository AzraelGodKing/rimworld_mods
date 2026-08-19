# Changelog

Detailed notes for **Stormproof** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **Update idea pool** — settings, DeepFreeze gap, substations, divertor, sky events, lightning gun, emit queries for Homesteader / Nemesis / Strata / Living World. Spec: [docs/ideas/stormproof-updates.md](../docs/ideas/stormproof-updates.md).
- **Stormfront scenario** — hard-weather showcase start; unlocks storm protection research; locks Azrael when Homesteader is loaded.

### Changed
- **Workshop preview** — compressed `About/Preview.png` (~3.0 MB → ~0.94 MB) via palette PNG so Steam uploads stay under size pressure.

### Fixed
- **Building sprites re-verified (SP1)** — re-ran pack (~90% fill, Armored conduit skipped) + full alpha clean on all Power buildings so Workshop installs match the fixed art.
- **Building architect icons tiny** — sprites only filled ~10–25% of each PNG; packed content to ~90% canvas fill so Power-tab icons and in-world draw match normal RimWorld scale (Armored conduit atlas unchanged).
- **Building textures baked backgrounds** — alpha-cleaned white/checkerboard plates, then stripped leftover light editor pads (near-black / light-gray connected to transparent) so in-world draw isn’t a solid cell square around the art.
- **Building textures white backgrounds** — batch alpha-cleaned all `Textures/Stormproof/Buildings/*.png` (baked white/checkerboard → transparent).
- **Surge protector eats no-op spire Zzzt** — storm spire short-circuit rolls call `CanFireNow` first; surge Prefix also refuses `Absorb()` when vanilla would find nothing shortable.
- **Static pylon charge bleed** — capacitor drain is all-or-nothing: if the bank cannot cover a zap, nothing is taken.
- **Volcanic ash Harmony startup crash** — `GiveOrUpdateHediff` second arg is `target` in 1.6 (was patched as `pawn`); optional Odyssey ash barrier prefix now applies.
- Research tab XML parse failure: escaped `&` in `Storm & grid research` (`&amp;`) so `Research_Stormproof.xml` loads again (was breaking all Stormproof research defs and prerequisites).
- **Ion storm tick cost** — dampener shield checks and EMP candidate picks use plain loops instead of LINQ allocations.
- Solar shield's electricity patch now targets `GameConditionManager.ElectricityDisabled(Map)`, which RimWorld 1.6 changed from a property to a method. The previous getter patch failed to apply and aborted the whole mod's Harmony initialization on startup.
- Armored conduit: replaced the removed-in-1.6 `placingDraggableDimensions` field with `drawStyleCategory` (Conduits), keeping drag placement.

### Added
- **Hazard hardening** (Spacer research) — late-game natural-incident defense layer:
  - Atmospheric barrier — map-wide toxic fallout / toxic surge / volcanic ash / noxious haze suppression while powered
  - Climate stabilizer — cancels heat wave, cold snap, volcanic winter, heat dome, and polar front temperature offsets
  - Sky restorer — restores usable daylight during eclipse, volcanic winter/ash, and darkened skies
  - Fire suppressor — extinguishes fires in radius; ramps draw during flashstorms
  - Drought condenser — cancels drought plant-growth penalties (Odyssey)
- **New events:** dry lightning front, heat dome (+28°C), polar front (−28°C), toxic surge — each countered by the matching hazard building (plus existing spires / scrubbers / storm caller).
- `Languages/English/Keyed/Stormproof.xml` — all C# player strings (messages, gizmos, inspect panels) with `.Translate()` wiring.
- `Languages/README.md` — translator guide (Keyed + DefInjected layout, package id).

### Changed
- **Vanilla pixel building remake** — all Stormproof building textures redrawn as Core-like top-down pixel art (correct 128/256 canvases; charcoal steel + amber/cyan accents; no UI-badge frames). Armored conduit segment, menu icon, and 512×512 link atlas rebuilt with continuous amber channels. Workshop `About/Preview.png` updated to match.
- **Workshop preview makeover** — cinematic painted `About/Preview.png` in the Strata style (thunderstorm grid defense scene; Flares • Lightning • Surges • EMP), replacing the old icon-row banner.
- Replaced empty `Languages/English/.gitkeep` scaffolding with real Keyed files.
- **Dedicated research tab** — all Stormproof projects live under their own *Stormproof* tab (no longer on Main).
- Load shedder: an automatic breaker for wiring between the main grid and a low-priority sub-grid. Transmits like a conduit while the supply side's batteries hold charge; trips open and sheds the sub-grid when they fall below an adjustable cutoff (default 20%, gizmo-adjustable 5–45%), then reconnects on its own once charge recovers past the cutoff plus a 20-point margin. Requires storm protection research.
- Grid monitor console: inspect panel shows the power net's live production, consumption, net gain, battery charge, and an estimate of time until batteries run empty (or full). Sends a caution message when stored charge drops below 25% and a negative alert below 10%, re-arming once charge recovers above 35%. Requires storm protection research.
- Ion storm: a new small-threat event (0.75–1.5 days). While active, batteries bleed roughly 20% of their stored charge per day, random EMP bursts pop at powered colony buildings (~3/day), and extra "Zzzt!" short circuits fire through the vanilla incident (~1.5/day). EMP dampeners shield batteries and buildings in their radius from the bleed and bursts, surge protectors absorb the extra surges, and storm capacitor banks are immune to the bleed.
- Storm capacitor bank: a lightning-only battery. Only spire-caught strikes can charge it, it never self-discharges, and "Zzzt!" surges can't drain it. Automatically discharges up to 2,000W to cover grid deficits. Requires storm protection + batteries research.
- Weather forecaster: shows how long the current weather will hold, announces incoming thunderstorms, and warns an hour before the weather breaks. Requires storm protection research.
- Static discharge pylon: periodically stuns and burns hostile pawns and mechanoids within ~7 tiles (6 burn damage per shock, high armor penetration), draining 50 Wd of stored strike energy from a storm capacitor bank on its power net per shock. Each shock draws a visible electric arc from the pylon to the victim with sparks, a flash, a crackling electricity overlay for the stun duration, and a zap sound. Ignores downed targets and stops firing without capacitor charge. Requires flare shielding research.
- Fallout scrubber: slowly removes toxic buildup from pawns and animals in the enclosed room it's placed in. Requires flare shielding research.
- Storm caller: summons a rainy thunderstorm on demand (half-day duration, five-day recharge) - feeds storm spires and douses wildfires. Requires new atmospheric control research (Spacer, 3,500 pts).
- Atmospheric control research project gating the storm caller.
- Spire-caught lightning now charges storm capacitor banks first, then batteries.
- Perfect grounding research (Spacer tech, requires flare shielding and advanced fabrication): eliminates the "Zzzt!" surge risk from grid-connected storm spires entirely.
- Armored conduit sprite redrawn as segmented steel armor over visible copper wire and a gold power core, reading as armored cable rather than a floor plate.
- Armored conduit sprite changed from cross junction to horizontal line segment to match vanilla conduit draw style.
- Armored conduit redrawn as a thick continuous armored band (no segment gaps) so it stays visible on terrain and does not tile into a ladder pattern.
- Fixed armored conduit alpha processing (white-only border flood after resize) so dark armor is preserved; docs and mod textures restored.
- Armored conduit now uses a 16-cell linked atlas with Transmitter linkType (like vanilla) so corners and T-junctions bend correctly.
- Armored conduit atlas: L-corners use closed outer armor and culled dead quadrants; T-junctions use flat center hubs and end nubs.
- Regenerated armored conduit as atlas-native pixel art (16-cell linked sheet + segment/menu icon); removed composited segment-stitching approach.
- Regenerated heavy armored conduit art: riveted segment + junction hub block at L/T/cross bends (matches in-game armored look).
- Fixed armored conduit atlas seams: wires now drawn as one continuous band per axis (no more plate-pattern restarts or gaps at tile edges); broken junction cells repaired.
- Fixed armored conduit def: replaced nonexistent `Graphic_LinkedConduit` graphicClass with `Graphic_Single` + `linkType Transmitter` and `Custom1` linkFlags (was crashing map rendering with null-reference errors on every placed conduit).
- Fixed armored conduit atlas orientation: link cells are sampled with UV origin at the bottom-left of the sheet, so rows are now written bottom-to-top (index 0 at bottom-left). Straight runs previously drew junction-hub cells on every tile.
- Removed thing categories from the load shedder (not minifiable, caused a config error).
- Batch alpha-cleaned all Stormproof building PNGs to strip baked white/gray backgrounds and edge halos.
- Storm spire is now completely fireproof (flammability 0), and any fires ignited by a caught lightning strike within 3 cells of the spire are snuffed out. The spire keeps sweeping the area for 5 seconds after the strike, since the flame explosion expands over several ticks rather than instantly - a grounded rod shouldn't burn your base down.
- Storm spire surge chance per caught strike lowered from 25% to 5%.

## [1.0.0] — Initial release

### Added
- Solar shield, storm spire, surge protector, EMP dampener, armored conduit, storm vane.
