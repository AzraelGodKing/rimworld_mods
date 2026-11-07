# Changelog

Detailed notes for **Stormproof** only.

## [Unreleased]

## [2.0.0]

Player-facing version **2.0.0** (`About.xml` `modVersion`). Stamp `stormproof2-gridtools`. Ships for RimWorld **1.6** and **1.7** from the same source; 1.6 is not frozen.

AZR-492 AZR-493 AZR-494 AZR-495 AZR-497 AZR-498 AZR-500 AZR-501 AZR-505 AZR-667 AZR-668 AZR-669 AZR-670 AZR-671 AZR-672 AZR-713 AZR-715 AZR-716 AZR-717 AZR-718

### Added
- **Smart hold on the load shedder** (AZR-671) — a fourth hold mode after Auto. It works like Auto, but when the charge is back above the reconnect level it runs the grid forecast as if the sub-grid came back now, and stays shed if the supply would fall to the cutoff again within 8 hours. Needs a weather forecaster on the supply grid; without one it acts like Auto. The inspect pane says when and why it is holding. The hold gizmo is tinted per mode.
- **Almanac on the grid monitor** (AZR-716) — the grid monitor has its own Open almanac gizmo. The charge sparkline shows when a weather forecaster shares its grid.
- **Pylon zaps and EMP bursts in the almanac** (AZR-670) — each entry and the lifetime totals count static pylon shocks and ion storm EMP bursts.
- **Undefended grid warning** (AZR-672) — once per map, the first thunderstorm or dry lightning over a powered colony with no storm spire and no surge protector shows a reminder (which research to do, or what to build). Setting, on by default.
- **Storm caller settings** (AZR-718) — sliders for recharge (1 to 15 days, default 5) and storm length (2 to 24 hours, default 12). The building no longer has `cooldownTicks` / `stormDurationTicks` in its def.
- **EMP dampener for mechanoids** (AZR-717) — with Biotech, an opt-in setting lets the dampener shield your own mechanoids from EMP stuns. Enemy mechanoids are never shielded.
- **Gizmo icons** (AZR-507) — the almanac, fulgurite, schedule and hold gizmos use Stormproof's own art instead of vanilla's launch report and temperature icons.

### Changed
- **Two builds, one source** (AZR-493) — official 1.6 loads `1.6/Assemblies` (net472, 1.6 refs). 1.7 loads `1.7/Assemblies`, now a .NET 10 build against the 1.7 refs (`Krafs.Rimworld.Ref` 1.7.4998-unstable, Harmony 3.0.0-rc.4).
- **Weather timer access** (AZR-500) — on 1.7 `WeatherDecider.curWeatherDuration` is read with `[UnsafeAccessor]` instead of reflection. A missing field logs one warning and the forecaster and storm caller degrade quietly, as on 1.6.
- **Hazard defenders** (AZR-495) — the barrier, stabilizer, sky restorer and condenser share one base class. The heat dome and polar front check the stabilizer in their own `TemperatureOffset` instead of through two Harmony patches. Def-name strings in code are `[DefOf]` fields. Ties between spires, protectors and capacitors break by thing id, the same on every load.
- **Code cleanup** (AZR-500, AZR-668) — one battery-total helper replaces four copies; mod settings are never null, so the 20+ null checks are gone; incident chances use `[DefOf]` fields instead of def-name strings and are applied once defs exist; simulation code uses `System.Math`; the fire suppressor and the grid forecast no longer share static scratch lists between calls.
- **ROADMAP** (AZR-667) — dropped the dead AZR-109 line and the deleted Strata V4 roadmap link.
- Dropped unused `docs/img/ArmoredConduit.png`. Atlas script segment output is opt-in (`-OutSegment`); default writes atlas + menu icon only. No gameplay change.
- **Brownout off the hot getter** (AZR-497, AZR-501, AZR-713) — brownout now trims a grid's energy gain in `PowerNet.CurrentEnergyGainRate`, once per grid, instead of postfixing `CompPowerTrader.PowerOutput` for every consumer. The .NET 10 JIT can inline that tiny getter, which would have switched brownout off on 1.7 without an error. With brownout disabled, nothing is looked up at all. The grid monitor shows the trimmed consumption so it matches the net gain.
- **Workbench brownout as a stat part** (AZR-501) — `StatPart_Brownout` is added to `WorkTableWorkSpeedFactor` by `Patches/Stormproof_Brownout.xml` and shows in the stat tooltip. The global `StatExtension.GetStatValue` patch is gone.
- **Hazard checks** (AZR-501) — the toxic, plant and temperature patches read one cached answer per map per tick instead of rescanning defenders for every cell or plant. Defender hazard lists are built once, not per call.
- **Ticking** (AZR-501) — surge protector, EMP dampener and storm caller no longer tick; weather forecaster, fallout scrubber and grid monitor tick rarely (every 250 ticks, as they already acted). The scrubber checks for toxic buildup before the room lookup. Load shedders reuse their flood-fill collections, and grid forecasts on one map share one sun curve per 250 ticks.
- **Load shedder junctions** (AZR-669) — checked the shed-side split for breakers touching three or more sides: every non-supply side drops off together when the breaker opens, and sides joined elsewhere are counted once. No change; the reasoning is now in a code comment.
- **1.7 patch safety** (AZR-497) — Player.log's load line now says how many patch classes applied. A missing Odyssey or Biotech target logs a warning when that DLC is active.

### Fixed
- **Storm caller vs dry lightning** (AZR-494) — calling a storm during a dry lightning front now ends the front first (logged in the almanac), so the rainy thunderstorm actually arrives. Before, the front's forced dry weather undid the call on the next weather tick.
- **Ion storm sky** (AZR-494) — the purple tint now shows, fading in and out over 300 ticks. The condition never set a sky lerp factor, so vanilla's 0 hid it.
- **Ion storm underground** (AZR-494) — can no longer be registered on underground maps, matching every other Stormproof condition.
- **Bolt on the spire** (AZR-495) — a caught bolt is now drawn on the spire. Only the damage moved before; the bolt was drawn from the event's own cell. Strikes with no cell set pick one up front, so the drawn bolt and the damage always agree.
- **Disabled defenders** (AZR-495) — a barrier, stabilizer, sky restorer, condenser or fire suppressor switched off in mod settings stays at idle draw during its hazard and says so in its inspect pane.
- **Heater and cooler thermostat** (AZR-495) — the brownout shift is undone in a Harmony finalizer, so an error inside `TickRare` can't leave the target changed. A missing heater or cooler target no longer fails the whole patch class.
- **Surge protector** (AZR-495) — absorbs every Zzzt that has something to short, including forced ones from the ion storm and storm spires. The storyteller's refire check no longer lets them slip past.
- **Missed strikes** (AZR-495) — strikes on a map with no storm spire are counted as missed in the almanac.
- **Temporary maps** (AZR-495) — building registries are kept per map and dropped when a map is removed, so quest and ambush maps no longer leave stale entries.
- **Almanac after load** (AZR-498) — the open weather entry keeps its real start time; loading no longer resets it to the load tick.
- **Weather forecaster after load** (AZR-498) — keeps the weather it last saw, so a change during the save still warns.
- **Save version** (AZR-498) — maps save `stormproofSaveSchema` (2). Saves without it are treated as version 1; every existing `stormproof*` key is unchanged.
- **Translations** (AZR-505, AZR-715) — Russian and Chinese now cover the five condition end messages, the research tab's title and description, and the whole Stormfront scenario (name, description, summary, start text). The scenario's start text is addressed by its part's def handle, so the optional Odyssey and Homesteader parts can't shift it.
- **Translation checks** (AZR-505) — `scripts/validate_mods.py` (run by CI) now fails when a Stormproof def's label, description, end message, letter, research-tab text or scenario text is missing from a shipped language, and when a Stormproof game condition doesn't say whether it can run underground. Well-formed XML, no BOM and Keyed parity were already checked for every folder.

## [1.7.0]

Player-facing version **1.7.0** (`About.xml` `modVersion`). Stamp `forecast-schedule-v1`.

### Changed
- **Workshop preview** — new `About/Preview.png` (grid under a flare and lightning). Docs card synced.
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6 (AZR-457). Existing 1.6 DLL; no gameplay change.
- Dropped the unused `ArmoredConduit.png` texture (defs use `_Atlas` and `_MenuIcon` only). Atlas script writes the segment preview to `docs/img` instead of `Textures/` and no longer copies the unused atlas PNG into `docs/img`. No gameplay change.
- Dropped flavor-text class headers on atmospheric barrier, climate stabilizer, sky restorer, drought condenser, fire suppressor, and natural-hazard game conditions. No gameplay change.

## [1.6.1]

Player-facing version **1.6.1** (`About.xml` `modVersion`).

AZR-457

### Changed
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6 (AZR-457). Existing 1.6 DLL; no gameplay change.

## [1.6.0]

Player-facing version **1.6.0** (`About.xml` `modVersion`). Stamp `forecast-schedule-v1`.

AZR-325 AZR-408 AZR-409 AZR-410 AZR-411 AZR-412 AZR-413 AZR-414 AZR-415 AZR-416

### Added
- **Load schedule forecast** (`forecast-schedule-v1`) — with a weather forecaster on the supply grid, each hour in the schedule shows a bottom bar where the forecast dips: amber for brownout, orange where the cutoff would trip. Hover an hour for its lowest predicted charge.
- **Forecast sparkline** — the weather almanac opened from a forecaster draws the next 8 hours of predicted battery charge for its grid.
- **Longest clean stretch** — the almanac shows the longest run without a missed strike or a suffered Zzzt, plus the current one.

### Fixed
- **Almanac lifetime totals** — totals are kept as running counts and no longer shrink when old almanac entries roll off. Existing saves start from the entries still in the almanac.
- **Brownout per grid** — brownout strength is tracked per power grid object, so one grid's brownout can never land on another.
- **Grid monitor** — "empty in" / "full in" on a nearly balanced grid no longer shows a garbled or negative time.
- **Weather forecaster and storm caller** — if a RimWorld update changes the weather timer they rely on, the forecast lines go quiet instead of throwing errors.

### Changed
- **Grid forecast** — the monitor's forecast now follows load shedders on the grid: scheduled shed hours, holds, and the battery cutoff drop or restore the sub-grid's draw over the 8-hour window.
- **Code cleanup** — dropped the unused fire-suppressor registry, unused incident-enabled helper, unread weather-decider `Available` flag, and flavor-text class headers that restated building copy.

## [1.5.1]

Player-facing version **1.5.1** (`About.xml` `modVersion`). Stamp `forecast-ramp-v1`.

### Fixed
- **Grid forecast** (`forecast-ramp-v1`) — solar shield, fire suppressor, atmospheric barrier, climate stabilizer, sky restorer, and drought condenser are counted at their hazard draw, not the idle nameplate.
- **Short-circuit absorb** — the handoff uses Harmony `__state` instead of a static flag.

### Improved
- **Settings** — the window writes to disk only when a value changes.
- **Registries** — comp lists clear when a new game starts, so a previous session's buildings are not kept.
- **Compat hooks** — `StormproofCompatApi` exposes ion storm, grid surge, and drought for other mods. Missing Stormproof stays a no-op for the caller.
- **Grid monitor** — inspect shows the predicted brownout at the forecast low point.
- **Almanac** — lifetime totals across recorded storms.

## [1.5.0]

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

## [1.3.1]

Player-facing version **1.3.1** (`About.xml` `modVersion`). Stamp `storm-call-tick-v1`.

AZR-229

### Fixed
- **Storm caller tick** (`storm-call-tick-v1`, AZR-229) — weather TransitionTo runs on MapComponentTick, not the gizmo click.

## [1.3.0]

Player-facing version **1.3.0** (`About.xml` `modVersion`). Startup writes `[Stormproof] v1.3.0 loaded from ...` with stamp `schedule-ledger-v1`.

### Added
- **Load schedule** — the load shedder gets a 24-hour run/shed timetable, a forecast override that sheds before a storm or flare lands, and a one-click Hold Auto / Run / Shed. The battery cutoff still trips first. Old saves keep the old breaker until you set a schedule.
- **Almanac ledger** — each recorded storm now tracks strikes caught vs missed, Zzzt absorbed vs suffered, fires the suppressor snuffed, and wear hits on unhardened wiring.

### Changed
- **Building art and draw size** — Power-tab sprites redrawn and canvases set to 128 px per cell so tall buildings are no longer squashed square textures. Storm caller, sky restorer, fire suppressor, and drought condenser use matching square draw sizes.

### Fixed
- **Grid forecast brownout** — the 8-hour projection uses nameplate draw and models brownout from projected charge, instead of reading the already-dimmed live `PowerOutput`.
- **Night solar forecast** — after dark the monitor reuses the last daytime sky multiplier (or the weather's own factor). It no longer hardcodes 0.5 and halves the next sunrise.
- **Forecast every frame** — inspect and warnings cache on the monitor's 250-tick interval instead of recomputing the whole 8-hour walk while selected.
- **Forecast alarms and full-in** — warnings re-arm from current charge (same as the live low/critical lines). A grid that is already full shows held charge, not "full in 4 minutes".
- **XML load** — the three Nexus crash files (`GameConditions`, `Incidents`, `Research`) are well-formed UTF-8 with no BOM. Research already escapes `&amp;` in the tab title. A re-upload from this branch should not hit `unknown parse failure` from encoding.

## [1.2.0]

Player-facing version **1.2.0** (`About.xml` `modVersion`). Startup writes `[Stormproof] v1.2.0 loaded from ...` with stamp `radius-forecast-v1`.

### Added
- **Coverage rings** (`radius-forecast-v1`, AZR-143) — place and select draw the live CompProperties radius on the storm spire, static pylon, EMP dampener, and fire suppressor. The fallout scrubber highlights its enclosed room (outdoors: no outline).
- **Grid forecast** (`radius-forecast-v1`, AZR-144) — grid monitor projects battery trajectory for 8 hours using a powered weather forecaster on the same net (solar/wind vs current weather, then a typical sky after it breaks). 25% / 10% alarms can fire on the projection. Falls back to the naive empty-in line with an inspect note if no forecaster is present.

## [1.1.1]

Player-facing version **1.1.1** (`About.xml` `modVersion`). Startup writes `[Stormproof] v1.1.1 loaded from ...` with stamp `update-news-v1`.

### Added
- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Graded brownout** — when batteries fall below 40%, lights and benches degrade and coolers lag; grid draw drops so the remaining charge lasts longer. Load shedders still cut the sub-grid at the trip point.
- **Storm wear** — thunderstorms, ion storms, and high wind slowly chip ordinary conduit and batteries. Armored conduit and storm capacitor banks are immune. Vanilla repair fixes the damage.
- **Weather almanac** — weather forecaster gizmo + inspect log of this map's seasons and game conditions.
- **Fulgurite** — every 8 strikes on a storm spire, fused glass appears at the base (trade / sculpture stuff).
- **Mod settings** — Soft / Default / Hard, five incident toggles + frequency, suppressor toggles, brownout strength, wear, almanac, fulgurite, spire Zzzt factor.

### Changed
- **Storm vane (SP2)** — sprite packed to ~90% canvas fill so the decorative vane reads at building scale.
- **Workshop description** — Homesteader-style building lists, grid stress, research, settings, and which-building notes.

### Fixed
- **Brownout cooler Harmony** — `CompTempControl` has no `CompTickRare` in 1.6, so the patch hit a null method. Brownout now prefixes `Building_Heater.TickRare` and `Building_Cooler.TickRare`.

## [Unreleased]


## [1.5.0]

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

Player-facing version **1.0.1** (`About.xml` `modVersion`). Startup writes `[Stormproof] v1.0.1 loaded from ...` in Player.log.

### Added
- **Stormfront scenario** — hard-weather showcase start; unlocks storm protection research; locks Azrael when Homesteader is loaded.

### Changed
- **Workshop preview** — compressed `About/Preview.png` (~3.0 MB → ~0.94 MB) via palette PNG so Steam uploads stay under size pressure.

### Fixed
- **Guarded Harmony** — each patch class is applied on its own; one missing target logs and skips instead of aborting the rest of Stormproof.
- **Chinese Keyed XML** — closing tag for `Stormproof_NotConnectedPowerNet` now matches the opening tag so the Simplified Chinese pack loads instead of failing the whole file.
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
