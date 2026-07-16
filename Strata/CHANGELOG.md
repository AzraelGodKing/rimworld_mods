# Changelog

All notable changes to Strata are documented here.

## [Unreleased]

### Added — Ancient colony stairwell
- **Surface scatter** — some new colony maps can spawn a pre-built *ancient stairwell* away from the landing zone (toggle and spawn chance in Strata mod settings). Descending opens a normal mineable B1 stratum **without digging-down research**. The shaft has **no power transmitter** — each level must be wired separately unless you later build an excavated stairwell or shaft power conduit for cross-level grids.

### Added — Work / Schedule cross-level colonists
- **Strata Levels toggle** — Work and Schedule tabs show a *Strata Levels* checkbox when the colony has excavated levels. When enabled, both tabs list free colonists (and schedule subhumans) from every portal-linked level, sorted surface-first then by depth. Default on; persists in save and syncs with Strata mod settings.

### Added — Native cave generation
- **Strata cave networks (B1+)** — excavated colony levels generate native chamber-and-tunnel layouts via `GenStep_CarveWarren` when Biomes! Caverns is not loaded (or its compat toggle is off). Depth scales chamber count; hidden gas/geothermal pockets still generate in the surrounding rock. Toggle: *Natural cave layout* in Strata settings.

### Changed — Underground O₂ / CO₂ simulation
- **Localized oxygen pumps** — life-support emitters enrich air in a 16-tile radius with falloff in natural caverns; sealed colony-built rooms still pressurize uniformly.
- **Roof-column ambient** — natural map (non-colony) cells under thick roof supported by rock columns drift toward 21% O₂; farther from pumps or unsupported roof, levels drop until diffusion or ventilation catches up.
- **B2+ breathing** — removed the B1-wide perpetual O₂ top-up; sealed player rooms accumulate gas from pumps, pawns, and plants. Colonists and animals consume O₂ and exhale CO₂; living plants slowly restore O₂ and scrub CO₂.
- **Overlay** — O₂ and CO₂ tints now follow per-cell density in caverns instead of a flat room average.

### Fixed — Underground gas open-air classification
- **B1+ enclosed rock** — rooms on underground Strata levels (B1 and deeper) no longer inherit Biomes! cavern `UsesOutdoorTemperature` / psychologically-outdoors flags, so the gas system treats them as sealed chambers (dispersion, overlay, breathing, and vent routing) instead of open sky.

### Fixed — Ancient colony stairwell entry
- **EnterPortal crash** — ancient stairwells bypass digging-down research for entry, but pocket-map generation still enforced that gate and returned no B1 map, so `JobDriver_EnterPortal` hit a null map and crashed. Generation and `OpenLevelBelow` now honor the same research bypass as `IsEnterable`.

### Fixed — Biomes! arrival landing safety
- **Lava/magma near stairwell** — after Biomes! Caverns terrain generation, Strata now replaces lava, magma (Deep Lava), and impassable water in a 14-tile disc around the vertically aligned stair landing with walkable stone floor, and clears mineables in the arrival chamber. Applies on new B1+ pocket maps and when spawning landings on existing levels.

### Fixed — Ancient colony stairwell generation
- **Player start spot timing** — the ancient stairwell scatter step now runs after vanilla `FindPlayerStartSpot` (order 880, was 650) and uses `PlayerStartSpotValid` before reading the landing cell, fixing `Accessing player start spot before setting it` on new colony maps.

### Fixed — Cross-level construction haul spam
- **Install blueprints** — `LevelDemand` no longer calls `TotalMaterialCost` on `Blueprint_Install` (reinstall/move blueprints), which vanilla rejects and logged every storyteller/haul tick. Construction shortfalls still track build blueprints and frames only.

### Fixed — Underground storyteller crashes
- **Invalid pocket map tiles** — storyteller `CanFireNow` no longer crashes on `WorldGrid` when an underground level still carries a stale `PlanetTile` after load. Tile repair runs before each check; if the tile cannot be resolved, Strata skips the vanilla world-tile lookup and evaluates incident gates plus `CanFireNowSub` only (fixes repeated `ArgumentOutOfRangeException` during `StorytellerTick` on multi-level saves).

### Fixed — B2 power and life support
- **Dig-down shaft power** — dig-down extensions now include a `CompPowerShaft` transmitter so B1→B2 ties run through the shaft instead of the hub landing (which was fighting the surface↔B1 tie). Wire the B1 landing or the dig shaft into your local grid.
- **Stairwell power overlay** — stair shaft transmitters no longer flash the blinking unpowered lightning bolt when cross-level power is working. Consolidated tie updates to landings only (no duplicate drive from both StairsDown and StairsUp), set excavated stairwell draw to 0W like landings, and suppress the NeedsPower overlay on wired shaft nodes.
- **Oxygen pumps in ventilated rooms** — life-support emitters (oxygen pump) bypass the smoke ventilation cap so O₂ can reach breathable levels (0.21) in rooms with open shafts or vents.
- **Oxygen pump emission rate** — life-support pumps now apply their full per-cycle concentration gain instead of dividing by room size (which made O₂ gains vanishingly small in normal rooms and could not offset colonist breathing). CO₂ scrubbers use the same concentration model. Inspect text reports enrichment status while powered.
- **Oxygen pump registration** — pumps placed before the atmosphere component initializes on a map are picked up on the first simulation tick.

### Changed — Gas overlay readout
- **Cursor-attached gas panel** — with the gas overlay on, move the mouse over any room to see O₂, CO₂, and other gas percentages in a panel that follows the cursor and stays on screen.
- **Gas overlay room labels (mod option)** — optional per-room percentage labels on the map (off by default). Enable under Strata mod settings → Deep-level breathing → *Gas overlay room labels*.

### Changed — Gas pipe (formerly smoke duct)
- **Conduit-layer pipe** — gas pipe now renders as a linked metal pipe on the conduit layer (like power conduits), with drag-to-lay placement and corner/split auto-linking. Carries every Strata gas channel, not just smoke: connected rooms equalize all gases on the same run, outdoor terminals drain the network, and fans/louvers can push into dead-end runs toward other hooked-up rooms.
- **Hidden gas pipe** — nearly invisible variant (`Strata_HiddenGasPipe`) links with ordinary gas pipe for the same all-gas routing. Existing saves keep `Strata_SmokeDuct` buildings; the label is now *gas pipe*.
- **Pipe linking fix** — child defs now specify full `graphicData` (`linkType: Basic`, `Custom10` link flag). Partial overrides had cleared link metadata, so every segment drew as a standalone cap instead of joining into runs.
- **Gas pipe XML** — removed obsolete `placingDraggableDimensions` (not used in RimWorld 1.6; drag placement comes from `drawStyleCategory: Conduits`). Fixed hidden gas pipe inheriting a duplicate `CompGasPipe`.

### Fixed — Smoke ducts
- **Cross-links with other pipe mods** — smoke ducts used `Custom7`, the same link flag as VEF chemfuel/helixien pipes, so adjacent runs visually merged and routing looked broken. Ducts now use an exclusive `Custom10` flag and only link to other smoke ducts.
- **Paintable** — smoke ducts can be painted like other structures.
- **Conduit-style placement** — drag-to-lay line placement (`drawStyleCategory: Conduits`), can overlap zones, and duct graph traversal ignores unrelated buildings sharing the same tile (power/gas conduits on other layers).

- **Startup crash without Biomes! loaded** — lazy-bind `Map.Biome` for cavern generation instead of a static `FieldRefAccess` on the removed `Map.biome` field (RimWorld 1.6). Fixes `TypeInitializationException` poisoning `StrataMapUtility.IsUnderground` during surface map generation and every tick.
- **Cavern layout with Biomes! loaded** — swap biome via `Map.pocketTileInfo.PrimaryBiome` (RimWorld 1.6 read-only `Map.Biome`). Successful cavern generation keeps the picked BMT profile so later feature scatter (plants, fauna, crystals) runs correctly.
- **Cavern rock placement** — resolve `GenStep_CavernRocksFromGrid` from `BiomesCore.MapGeneration` (Biomes! Framework 1.6 moved it out of `BiomesCore.GenSteps`).
- **B2+ pocket map generation crash** — copy the surface colony `PlanetTile` onto pocket `MapParent` before map generation and again during `Map.FinalizeInit` so existing saves load. Plant-growth fallback still runs when a pocket tile cannot be resolved (broken portal refs). Underground incident checks short-circuit blocked events before `CanFireNowSub` runs (fixes Polux tree / `WorldGrid` index errors).
- **Existing save load (New Arrivals7)** — pocket maps could keep a `Valid` but out-of-range world tile after broken `MapParent` cross-refs, crashing `MapPlantGrowthRateCalculator.BuildFor` during `FinalizeLoading`. Tile repair now checks `WorldGrid` bounds (not just `PlanetTile.Valid`), runs at `FinalizeLoading`, and the plant-growth fallback applies to any map with a bad tile. Colony tile resolution falls back to player settlements on the world layer when portal chains are broken mid-load.

### Added — Biomes! Caverns compatibility
- **Natural cave generation (B1+)** — when Biomes! Caverns, Biomes! Core/Framework, and Geological Landforms are loaded, excavated underground levels generate Biomes! cavern layouts (tunnel networks, chambers, and tubes) instead of Strata's native warren. Depth picks the cavern profile: earthen depths near the top, fungal forest mid-deep, crystal caverns deeper down. Without Biomes!, Strata's own warren carving runs instead.
- **Cave content** — carved levels spawn Biomes! Caverns wild plants and animals, stalagmites, and crystal scatter in open cave space. Hidden Strata chambers (geothermal / gas pockets) still generate in the surrounding rock.
- **Strata systems preserved** — arrival chamber, B1 oxygen seed, fog-of-war, stairwell landings, atmosphere, incidents, and relay logic still treat these levels as Strata underground maps.
- **Mod option** — *Biomes! Caverns layout* toggle in Strata settings (shown only when the mod stack is present).

### Fixed — Startup / config errors
- **Incidents XML** — removed stray `</IncidentDef>` that prevented all Strata incidents from loading; renamed duplicate Anomaly lost-miners def to `Strata_LostMinersAnomaly`.
- **Harmony** — caravan pull tick patch now targets `TickManager.DoSingleTick` (RimWorld 1.6 removed `Game.GameTick`).
- **Mine canary** — inherits from `AnimalThingBase` / `AnimalKindBase` instead of missing `Chicken` parent.
- **Mine lamp** — dropped invalid `CompGlowerPowered` comp class (uses vanilla powered glower behavior).
- **Lime scrubber** — added `tickerType` Normal so refuelable fuel consumption works.
- **Collapsed mine site** — shell blocks reference `BlocksLimestone` instead of nonexistent `BlocksWoodLog`.
- **Cave fungus** — inherits from `PlantFoodRawBase` with vanilla fungus graphics and ingestible rules (concrete `RawFungus` parent was unreliable in some load orders).
- **Homesteader root-cellar bridge** — rot slowdown patch retargeted to `CompRottable.TickInterval` for RimWorld 1.6 (`RotProgressPerInterval` removed).
- **Ambient O₂ replenish** — skip rooms with no regions during new-game init (fixes `ArgumentOutOfRangeException` in `ReplenishAmbientOxygen`).
- **Gas pocket incident** — stronger initial burst, immediate overlay refresh, spawns a deep gas vent at the breach, and deep gas now throws visible motes like smoke.
- **Dev mode menu** — Strata debug actions now use `Strata/Incidents` and `Strata/Gas` category paths with ASCII-only labels, fixing `DebugActionNode.LabelNow` index errors when opening the debug menu. `ToolMap` gas tools use parameterless methods with `UI.MouseCell()` (RimWorld 1.6 binds `ToolMap` as `Action`, not `Action<IntVec3>`).
- **Dev mode incidents** — every Strata incident (including world quest sites, flood seep, and Anomaly lost miners) is spawnable from `Strata/Incidents`; world-target incidents resolve `Find.World` automatically and dev mode can force past settings gates when `TryExecute` is blocked. World quest-site buttons no longer use `WorldRenderedNow` (that flag requires a world-tile method signature and crashed the debug menu).
- **Deep gas ignition** — uncapped deep gas vents and other non-combustion emitters no longer count as open flames; gas only detonates when a real fire or fuel burner is in the **same room**.
- **Gas overlay labels** — labels now average the on-screen projection of the room's **visible** bounds (fixes long tunnels where one room spans off-screen) and clamp the full label box inside the viewport. On-map labels use readable line spacing (`GameFont.Small`, 20px rows) instead of vertically squished tiny text.
- **Shoring pillar** — uses the vanilla stone column art (build from marble or any stone blocks), sets `holdsRoof`, and supports **13.8 tiles** (2× vanilla column) for both roof collapse and cave-in protection.
- **B1 oxygen** — the first underground level always starts with ambient O₂ in every enclosed room (arrival chamber seeded at generation, ongoing top-up each atmosphere cycle after shaft/door transfer, no hidden gas pockets on B1). Deeper levels still use one-time seeding and can roll gas chambers. Overlay percentages now show absolute density (21% O₂ at ambient fill, not 100% of a near-empty mix).
- **Pending gas seeds** — generation-time gas pockets retry until their room exists instead of being discarded on the first tick.
- **Rest relay on bedless floors** — colonists on a level with no reachable bed now commute to their assigned bed or a free bed elsewhere when they need sleep, instead of giving up when an unreachable bed exists on the current floor.
- **Need colonist beds alert** — counts beds and colonists across linked Strata levels (patches vanilla's per-map surface-only check; RimWorld 1.6 returns a boolean alert with no culprit list).

### Added — Mine atmosphere channels
- **Methane (firedamp)** — buoyant, ignites earlier than deep gas; pockets in hidden chambers and geothermal sites.
- **Black damp** — heavy, non-flammable; displaces oxygen; left after gas ignitions; pools in flooded warren pits.
- **Fungal spores** — illness/mood channel from fungal warrens and fungus farms (not explosive).
- **Steam** — buoyant scald haze from geysers and volcanic chambers.
- **Counters** (research: *mine atmosphere*): methane flare, black-damp scrubber (+O₂ restore), spore filter, steam condenser.
- Helixien room gas, refrigerant, and Anomaly miasma still deferred.
- **Dedicated research tab** — all Strata projects live under their own *Strata* tab (no longer on Main).

### Added — Work relay coverage
- **Broader work-relay signals** — growing/sowing & harvestable grow zones, cleaning, firefighting, hunting, research benches, warden (hungry/recruitable/injured prisoners), animal handling (tame/slaughter/training/hungry animals), flick designations, and childcare needs, in addition to construction/mining/plant-cut/hauling/bills.
- **Mod extension API** — `PawnRelay.RegisterWorkProbe` / `UnregisterWorkProbe` for custom “has work on this map?” checks; `RegisterWorkSeekingJobGiverMarker` so mods with their own `JobGiver` (not `JobGiver_Work`) opt into cross-level work relay via `Patch_ExternalJobGiverRelay`.
- **Misc. Robots** — broader work detection (farm/mine/construct/fire/omni) using the same signals.
- **Escape through shafts** — when vanilla exit-map AI has no map edge (underground levels), prisoners, slaves, guests, and other flee jobs take unsealed stairwells/elevators toward the surface on their own — so prison breaks and slave escapes keep working without escorts. Sealed shafts still block them. Colony work relays stay closed to **prisoners**; **slaves** commute for work/food/rest like colonists.

### Added — Pillars 4–5 (Away Into the Dark / Living Below)
- **Medical & joy relays** — patients and doctors commute to linked levels with medical beds or tending work; recreation-starved colonists seek joy food or recreation buildings elsewhere (cap 2, toggles in mod settings).
- **Level roles** — assign Freezer, Barracks, Workshop, Hospital, or Storage per level in the Levels tab; relays softly prefer matching roles.
- **Caravan pull from below** — while forming a surface caravan, colonists haul high-value goods up from linked underground levels (toggle; alert when goods remain below).
- **Threat incidents** — gas firestorm, enhanced tremors (unseal shafts, underground roof damage, gas seeps), deep siege (surface raiders batter stairwells), cave breakthrough, early prospector dig (after Digging Down), lost miners underground.
- **Pillar 4 quest sites** — three new multi-level world sites gated behind deep excavation and the underground gas / exploration toggles: **collapsed mine** (fungal warren), **sealed vault** (frozen chambers, mechanoids or insects), **geothermal vent** (volcanic warren with geysers and deep gas). Sunken ruins now use the **flooded** warren theme. Shared `QuestSiteUtility` + `GenStep_WarrenTheme`.
- **Living Below (Pillar 5)** — research for rock shoring, passive life support, deep infrastructure, and deep agriculture. Buildings: shoring pillar, gas airlock, ore skip hoist, fungus farm, sump pump, mine lamp, shaft bellows, lime scrubber. Items: cave fungus, lime. Flood seep incident + sump drainage.
- **Compat & tools** — VEF chemfuel shaft junction (auto-discovery for VEF PipeSystem nets); sister-mod bridges (Homesteader / Wellspring / Stormproof); Royalty deep-bedroom thought; Levels-tab hibernation readout; exploration-sites and flood-events settings.
- **Dedicated art pass** — new sprites for Living Below buildings, quest stairheads, life-support pumps, smoke hole, updraft filter, gas exchanger, fluid junctions, and lime.

### Changed
- **Gas overlay differentiation** — rooms tint by dominant hazardous gas (smoke gray, deep gas green, CO₂ blue-gray, methane amber, spores lime, steam pale cyan, etc.) instead of averaging all channels into one muddy color; normal O₂ is suppressed on the tint. **On-map labels** at each room center list the top three gases with color-coded percentages plus total load; cursor readout shows the full mix for the hovered room.
- **Stairs up art** — redrawn to match the industrial stairwell frame style (riveted square border, ascending steps, up chevrons) consistent with StairsDown, elevators, and exhaust fan buildings.
- **Lime item art** — redrawn as a burlap sack of white crushed limestone (matches lime scrubber / Strata item style).
- **Mine canary art** — custom canary sprites (north/south/east + dessicated) replace the yellow-tinted chicken placeholder.
- **Canary cage vs bird cage** — canary cages accept **mine canaries only** (assign or hay acquire). New **bird cage** furniture holds any tame vanilla bird. **Default feeding:** stock hay/kibble in the cage via **Stock food** gizmo; toggle **Sustain caged bird hunger** in mod settings to freeze hunger instead.
- **Mine canary animal** — custom **mine canary** pawn kind (chicken-based stats, dedicated canary art) for cage acquire/spawn. Gas harm for caged birds uses canary thresholds instead of colonist hediffs.

### Added
- **Digging down** research (600 pts, Medieval): unlocks the first basement from the surface.
- **Dig down** gizmo on underground stairwell landings and down-stair entrances: extend the shaft to the next level without placing a new stairwell elsewhere on the floor (surface excluded; B2+ requires deep excavation research).
- **Rimefeller shaft junctions** — crude-oil and chemfuel pipe ties between levels when [Rimefeller](https://steamcommunity.com/sharedfiles/filedetails/?id=1321849735) is loaded.
- **Sunken ruin quest sites** ([PR #20](https://github.com/AzraelGodKing/rimworld_mods/pull/20)) — world incident after deep excavation reveals an ancient stairhead and insect warren with a hoard below; pocket-map safety when abandoning settlements or despawning sites.
- **Underground gas** mod option — master toggle for deep gas pockets and sunken ruin incidents (sealed stairwell gas containment stays on).
- **Combined abandon warning** mod option — one dialog listing pawns left on surface and underground when abandoning a settlement.

### Fixed
- **Startup: stale `Buildings_DeepGas.xml`** — replaced the pre-merge stub with an empty deprecated file so old installs stop referencing removed types (`CompProperties_DeepGasVent`, `Strata_DeepGasExtraction`, etc.); gas defs live in `Buildings_StrataGas.xml`.
- **Startup: `Strata_GasWell` config** — set `fillPercent` to 1 and `blockWind` so the impassable wellhead passes RimWorld's build validation.

### Added — Atmosphere v2 (O₂ / CO₂)
- **Breathable air on deep levels**: underground maps seed enclosed rooms with ambient oxygen when finalized. Colonists and animals consume O₂ and exhale CO₂ each atmosphere cycle.
- **Gas stratification**: oxygen is buoyant and rises through unsealed stairwells and elevators; carbon dioxide is heavy and sinks to the level below. Surface stairwell landings and open-to-sky rooms stay topped up with fresh O₂.
- **Hypoxia and CO₂ exposure** hediffs when O₂ falls too low or CO₂ builds up. Toggle in mod settings: *O₂ / CO₂ simulation* (clears both gases when off).
- **Life support buildings**: powered **oxygen pump** (releases O₂ into a sealed room), **CO₂ pump** (scrubs exhaled carbon dioxide from a room), and **shaft gas exchanger** (boosts O₂ rise and CO₂ sink through an open stairwell). Unlocked by *deep life support* research (900 pts, after forced ventilation).
- **Canary cage**: a furnished warning for sealed rooms — assign or acquire a **mine canary** only. The bird sickens or dies from smoke, deep gas, hypoxia, or CO₂ **before** colonists do, triggering a high-priority alert. Hunger is sustained while caged. Unlocked by *forced ventilation* research.
- **Bird cage**: hold any tame bird for display; hunger is sustained while caged.
- **Gas overlay**: play-settings toggle (bottom-right row) now drives the full Strata gas overlay — each room tints by its **dominant visible gas** (not a muddy average), with a secondary gas blended in when present. Normal breathable O₂ is hidden; depleted O₂ shifts blue → red. Cursor readout uses matching **color-coded** gas labels and percentages.

### Fixed
- **Dig down power overlap** — dig-down now spawns a **`dig shaft`** extension with no power comp beside the landing; the landing remains the sole transmitter and ties power to the level below.
- **Level excavation gating** — two-step research progression: **digging down** opens surface → B1; **deep excavation** (now requires digging down first) opens B2 and every level deeper. Applies to excavated stairwells and the **Dig down** gizmo; extra stairwells that join an already-open level below are still allowed.
- **Stairwell build work** — excavated stairwells now take **12,000** work from the surface and **20,000** below B1 (+8,000 deep offset). **Dig down** designates a dig-shaft blueprint with the same work and materials; the level below opens only after colonists finish construction (matching architect-placed stairwells).

### Fixed
- **Startup: stairwell `statParts` XML** — removed invalid `statParts` nodes from stairwell defs; depth-scaled work is applied via a Harmony patch on `WorkToBuild` instead.
- **Startup: stairwell work Harmony patch** — resolve `StatExtension.GetStatValue` overloads at patch time so RimWorld 1.6 no longer throws on mod init.
- **Dig shaft blueprints ignored** — `Strata_DigDownShaft` now has the construction fields RimWorld requires (`designationCategory`, terrain affordance, place worker); it stays hidden from the architect menu and is only placed via **Dig down**.
- **Gas overlay on world tab** — the room tint overlay no longer draws while the world map is open (it was sticking on screen after leaving the colony view).
- **Gas overlay readout** — mix and load are one line at the map cursor position so text no longer stacks/overlaps when pawns are on the level.

### Added — Pillar 1: The Living Deep
- **Multi-gas atmosphere simulation**: the smoke sim is now a general room-density simulation with per-gas channels (`StrataGasDef`). Each gas declares its own color, buoyancy, persistence, harm, flammability, and extractability; every existing ventilation tool — vents, louvers, smoke holes, ducts, fans, updraft filters, door flow, shaft seals, and the ventilation guarantee — applies to every gas automatically. Smoke keeps its exact tuning as one channel. Gas clouds now survive save/load, and clouds re-anchor mass-conservingly when rooms merge or grow (a breached pocket dilutes into the corridor that opened it).
- **Hidden geothermal chambers**: freshly opened levels seed small fogged chambers sealed in the rock, discovered by mining like ore. Geothermal chambers hold a steam geyser — the vanilla geothermal generator just works underground, and its heat feeds the existing stairwell temperature exchange. Chance and count scale with depth.
- **Persistent gas pockets**: other hidden chambers hold pressurized foul deep gas (often with a **deep gas vent** that seeps forever). Deep gas is heavier than air — it pools on the level it leaks into instead of riding shafts up — poisons pawns who breathe it, and **explodes when a room past ignition density holds an open flame**: torches become dangerous mining equipment, electric light a safety upgrade. Venting keeps pawns safe but wastes the resource. New levels are now generated fogged outside the arrival chamber so discovery works.
- **Gas extraction economy**: cap a deep vent with the powered **gas well** (stops the seep, pumps **deep gas canisters**) and burn them in the **deep-gas generator** — 1400W with no smoke at all, the natural power plant for sealed levels. New research: *deep gas extraction* (1200 pts, after forced ventilation). With **Vanilla Helixien Gas Expanded** loaded, the well gains a pipe connector and feeds the helixien gas network directly instead (reflection-based soft dependency; canister fallback if the network is full or absent).
- **Gas pocket incident recast** as a breach of the persistent system: digging splits a seam at a real rock face and floods the adjacent workings with deep gas — the same gas the chambers hold, cleared by the same tools — instead of conjuring vanilla tox gas out of thin air.
- **Alerts**: "Flammable gas near open flame" (critical) while gas pools around a lit flame below ignition density; "Smoke building underground" generalized to any harmful gas.
- **Dev tools**: saturate rooms with deep gas, list hidden chambers, per-gas cloud logging, and self-test coverage for the gas defs, economy defs, and gensteps.

### Added — Fluid shafts (Cursor)
- **Fluid shaft junctions**: optional shaft-side ties for **Dubs Bad Hygiene** plumbing, **Dubs Central Heating** hot-water and air-con pipes, **Dubs Rimatomics** reactor coolant, and **Vanilla Helixien Gas Expanded** helixien gas — adapters bound against real Dub/VEF APIs (`PlumbingNet.PushWater`, `CompHeatStore.HeaterTemp`, `CoolingNet` capacity, VEF `PipeNet` storage).
- **DBH groundwater on Strata levels**: new underground maps attempt to seed Dubs Bad Hygiene groundwater when that mod is loaded, scaling with depth so deep wells work downstairs.
- **Fluid shafts research** (1,200 pts, Industrial, after Shaft power): unlocks shaft fluid junction buildings when a supported pipe mod is loaded (DBH, DCH, Rimatomics, or VHGE).

### Added when the surface raid lord gives up, loots, or kidnaps, pursuit groups on other levels mirror that decision instead of fighting on alone — one raid, one story across the whole column.
- **Cross-level ritual escorts**: prisoners and colony animals assigned to a ritual on another level are listed in the dialog from every linked floor and escorted through stairwells by a warden, handler, or bonded master before joining the ceremony. Wardens walk to the prisoner in their cell and lead them to the stairwell — no prison beds at the landing required.
- **Smarter shaft routing**: pawns heading to another level now take the portal nearest to *them* — powered elevators preferred — instead of the first shaft the level graph found. Applies to work/food/rest relays, cross-level hauling, ritual travel, and raid pursuit.
- **Forced ventilation research** (700 pts, Industrial, after Electricity): the exhaust fan and updraft filter now sit behind a proper Strata research project instead of bare Electricity. Passive vents (louver, smoke hole) are unchanged.
- **Placement guides**: one-way vents (exhaust fan, louver, smoke hole) show their intake side in orange and exhaust side in green while placing; smoke ducts highlight the network they'd join — green if the run reaches outdoors, red if it's a dead pipe.
- **Alerts**: "Smoke building underground" when smoke accumulates on a level with nobody on it, and a high-priority "Colonists sealed below" when pawns are on a level whose every exit shaft is sealed.
- **Sealed-shaft siege**: pursuing raiders who hit a sealed stairwell now batter it down instead of shrugging. Breaking the entrance removes the way down — sealing is still the counter-play, but it buys time now, not immunity.
- **Deep raid telegraphing & depth scaling**: the screen shakes as the tunneler digs in, and swarm points scale up with level depth — richer ore, meaner bugs.
- **Self-tests**: a dev-mode action that runs invariant checks over the live colony (component registration, level-graph consistency, routing) and reports pass/fail.
- **README** with install, compatibility (including the Ancient Urban Ruins verdict), performance notes, and dev-tool docs.

### Changed
- **Levels now match the parent map's size** (previously a fixed 200×200). Landings, shaft conduits, and the level hotkeys stack exactly 1:1 beneath the level above instead of proportionally squeezed toward the center of a smaller map. Applies to newly opened levels only — existing levels keep their size and the proportional alignment still handles them. **Heads-up on big maps**: each opened level now costs what another map of your chosen size costs, so 300×300+ colonies pay noticeably more per level (the vacant-level throttle softens it).

### Fixed
### Fixed — Fluid shafts (Cursor)
- **Startup: `Strata_FluidShafts` cross-reference** — fluid shafts research now lives in core defs instead of a conditional patch, so junction `researchPrerequisites` always resolve.
- **Startup: VEF helixien pipe bind** — PipeSystem exposes `pipeNet` and `parent` as fields; reflection bind no longer fails at startup (which disabled same-cell helixien linking).
- **Helixien gas shaft transfer** — VEF nets meter stored gas and pipe overflow (not live production/consumption rates), resolve the local net from the pipe grid at the junction cell, and merge isolated junction nets into adjacent pipe nets reliably. Fixed reflection binds for VEF 1.6 (`DrawAmongStorage` is 4-parameter only; `DistributeAmongStorage` uses 2-parameter).
- **Rimatomics coolant shaft transfer** — Rimatomics recomputes `CoolingCapacity` / `CoolingLoopRatio` every tick from connected coolers and turbines, so the tie now pools cross-level spare capacity in a junction buffer and reapplies it after `CoolingNet.Tick` instead of writing net fields that vanish on the next tick. Pipe-grid regen runs when coolant junctions spawn or load.
- **Helixien pipe placement crash** — the VEF register hook no longer rewrites every helixien connector (which threw when reading props from the wrong type and could corrupt pipe nets); it only relinks shaft junctions and pulls junctions onto newly placed pipe segments.
- **Shaft fluid junctions tap into pipe nets like shaft power conduits.** Each junction now gets the mod's pipe comp (`CompProperties_Pipe` / `CompProperties_Resource`) via conditional patches — the same role as `CompPowerShaft` + `transmitsPower` on shaft power conduits. VHGE helixien junctions also link same-cell gas pipes (VEF only checks cardinal neighbours by default) and transfer via pipe-net overflow, not just storage tanks.
- **Fluid shafts research cross-reference** when no pipe mods loaded — prerequisite was on the abstract junction base; moved to each mod-gated junction def.
- **Strata startup crash** from invalid Harmony patch on `Thing.PostSpawnSetup` (removed; pipe rebuild uses deferred comp tick instead).
- **Shaft fluid junction graphic load crash** — junctions incorrectly used `Graphic_Linked` with a single-tile art file, which broke def init and placement ghosts; they now use `Graphic_Single` with pipe link flags (matching VEF PipeSystem convention).
- **Helixien shaft junction** now appears in VHGE's **Pipe Networks** architect tab (not buried in Structure), uses the gas-pipe menu icon, and only loads when VHGE is active. Requires both **Fluid shafts** and **Gas extraction** research.
### Fixed
- **Elevators never actually required power to descend.** The shaft comp is a power transmitter, so it always has a grid (its own, if unwired) and stays switched on so the tie can feed a dark level — which meant the "has power" check for the car was always true, and the advertised power gate never engaged. Descending now requires the elevator's grid to be genuinely live: producing at least what it consumes, or holding battery charge. Riding up is unchanged (always free, nobody gets trapped), and shaft routing's elevator preference now uses the same real check.
- **Clean power producers no longer smoke.** The burner auto-detection tagged *every* non-refuelable power producer as a combustion generator — including vanilla's watermill, the vanometric power cell, and the ship reactor, plus modded solar arrays, reactors, and turbines. Wall one in and it would gas the room. Producers now need actual evidence of combustion (fuel-ish name, flame overlay, or heat output) before they get an exhaust comp, and the vanilla clean producers are explicitly excluded.
- **Raiders sealed in from below no longer batter the indestructible landing forever.** Bottom landings have no hit points, so pursuers trapped under a sealed shaft would melee it endlessly while "raiders are battering the sealed stairwell!" messages promised progress that could never come. Only the destructible top-side entrance can be sieged; raiders sealed in below are simply trapped until the player unseals.
- **Combined-resources performance and memory**: with the combined readout on, every `GetCount` call (resource readout each frame, "make until X" bills) re-walked the whole level graph; cross-level totals are now cached per tick. The caches also prune entries for collapsed levels instead of retaining dead maps forever.
- **Stale cross-save state**: relay cooldowns, relay claims, and pursuit assault credit are keyed by pawn ID and tick-stamped, so loading an earlier save (or a different one) could silently suppress relays or miscount claim caps for a while. All session state is now cleared on game load.
- **Arrival chamber shrunk to respect roof physics**: the freshly carved landing chamber (radius 8) was wider than vanilla's thick-roof support distance (6.9), leaving its center cells unsupported — mining or building near the landing could trigger a genuine roof collapse right on the arrival point. The chamber is now radius 6.5, safely inside support range.
- **Smoke rising up a shaft** now anchors the transferred cloud to the entrance's own cell on the upper map instead of a lower-map coordinate that happened to be re-interpreted upstairs.
- **Dev-mode log spam**: the deep-raid "can't fire" breadcrumbs now only log for deliberate attempts (debug buttons) or at most once per ~2,500 ticks, instead of on every routine storyteller probe.
- **Cross-level bill ingredients**: production bills on every `IBillGiver` bench (stoves, stonecutters, smithies, smelters, tailor benches, drug labs, fabrication benches, research benches, etc.) only searched their own map for ingredients. Active bills now register ingredient shortfalls in the same demand ledger construction uses — respecting each bill's ingredient filter, recipe fixed filter, and fixed-ingredient slots — haulers ship matching stacks through stairwells/elevators automatically, and work relay only sends a crafter once the bench's level actually has enough to start. Covers all standard DoBill work types (cooking, crafting, smithing, tailoring, art, smelting, fabrication, doctor, research).
- **Misc. Robots on stairs/elevators**: player bots (Haplo's Misc. Robots and similar) were excluded from level relays because only `IsFreeColonist` pawns could use portals. Work relay, cross-level hauling, and a dedicated hook on Misc Robots' own job givers now send haulers and cleaners through stairwells and elevators when linked levels have work waiting — without pulling them mid-recharge.
- **Vertical alignment**: the first stairwell or elevator on a level now opens its landing directly beneath the shaft on the level above (same relative map position), instead of always at the center of the new underground map. Existing saves realign misplaced landings once on load — no repeated destroy/spawn loop. Shaft power conduits extend their lower junction to the same spot under where you built them, not beside the stairwell landing.
- **Shaft power conduit bootstrap**: the cross-level tie no longer waits for both grids to already be powered before it can start moving watts — auto-spawned lower junctions draw 0W like stairwell landings, the flick switch is gone so the junction is live as soon as it is built and wired, and shaft comps stay on whenever they are connected to a grid. Fixed a partner check that was destroying and re-spawning the lower junction every few seconds (spamming the extension message and flickering power). Cross-level flow now matches each grid's live surplus and battery discharge headroom instead of a flat 2000W cap. Shaft conduit pairs now save by thing ID and re-link after reload — vanilla cross-map references were breaking the tie on every load. Stairwells and elevator landings now transmit power across their full footprint so a single wire hook-up actually joins the grid.
- **Haul + seal race**: sealing a shaft mid-haul now cancels the haul job instead of the pawn walking cargo to a door that won't open.

## [1.1] — 2026-07-12

One home, many floors — and now they behave like one home. This release makes storage priority, construction, rituals, the build menu, and the resource readout work across the whole level column; rebuilds the smoke system around real airflow with a hard safety guarantee for ventilated rooms; makes pursuing raids commit to the fight like proper raids; turns deep raids into the insect eruptions they were always meant to be; and fixes a batch of deep-rooted bugs — including several systems that had never actually worked (door venting, underground storyteller events, real stairwell landings).

### Added
- **Level sharing**: a second stairwell or elevator built on the same floor now breaks through into the *same* level below instead of opening a parallel pocket dimension. Its landing is carved roughly beneath where it stands (sealed in rock until mined out — pawns can always ride back up). A shared level only collapses when its last entrance is deconstructed.
- **Cross-level storage priority**: hauling now honors stockpile priority across the whole level graph. Items go directly to whichever level's storage has the highest accepting priority (ties stay local), running just above vanilla `HaulGeneral` so a Critical freezer downstairs beats a Low pile by the door. Items already sitting in storage get upgraded to better storage on other levels too. Cargo is only shipped to a level where the storage is actually walkable from the arrival landing.
- **Haul designations travel**: "Haul things" designations (stone chunks etc.) move with the cargo through stairwells and elevators, and items pulled out of storage for a cross-level upgrade get designated on arrival — nothing needs re-marking.
- **Self-extending shaft conduit**: build one shaft power conduit within a few tiles of a stairwell or elevator and it automatically extends a matching junction down to the landing below and drives the tie itself. The lower end lives and dies with the one you built, is replaced automatically if destroyed, and existing hand-built pairs are adopted as-is.
- **Legacy landing migration**: on load, old rope "cave exit" landings (from before Strata spawned its own) are swapped in place for the proper stairwell/elevator landing, complete with power shaft comp.
- **Cross-level build menu**: the stuff dropdown now offers materials that exist on *any* linked level, so a wood wall is placeable on a level with no wood — demand pull fetches the wood once the blueprint is down. Always on.
- **Level hotkeys**: Page Up / Page Down flip the camera one level up or down, keeping the same relative position over the base — no more scrolling to a staircase to peek downstairs. Rebindable in Strata's mod settings (click the key button, press the key you want, Reset to go back to the defaults).
- **Resizable Levels tab**: drag the corner handle to resize; the row list scrolls when it doesn't fit, and the chosen size sticks for the session.
- **Mod options**: a Strata settings page (Options > Mod settings) with toggles for the work/food/rest relays, the smoke simulation (plus a severity slider from harmless to double), raid pursuit, and the vacant-level performance throttle.
- **Smoke flows through open doors**: an open exterior door flushes a smoky room fast and *visibly* (about 40% per second — a full room clears in ~5 seconds), and open interior doors **equalize** smoke between rooms, volume-weighted and mass-conserving: a small smithy venting into a great hall thins to a light haze, while a smoky hall pours into an open closet fast. Smoke spreads room to room toward an exit instead of sitting still, exits always take priority as pure drains, and the ventilation guarantee changed shape to match: burners and inflows can never push a properly ventilated room past a light haze, but thick pre-existing smoke drains through the outlets on screen instead of vanishing to the cap in one tick. (Also fixed smoke transferred between rooms being invisible — it anchored to the door's own one-cell room instead of the receiving room. And fixed the deepest-rooted bug of the batch: doors form their own one-cell "doorway room" in RimWorld and never appear in a room's region list, so the door-detection used since the smoke system's first release **never found a single door** — the real reason "open doors don't matter" was reported. Door discovery now scans the room's border cells.)
- **Vanilla wall vents carry smoke**: the stock temperature vent now flows smoke the same way it flows heat. Between two rooms it equalizes (volume-weighted, slightly slower than an open door); facing open air it drains the room nearly empty and counts as proper ventilation for the safety guarantee. Flicking the vent off stops the flow. A normal base's existing vent network just works for smoke, no new buildings needed.
- **Smoke hole**: a primitive passive wall vent for tribal starts — 25 wood, no research, no power. A sod-and-wicker flue that slowly bleeds fumes out of the room, and like every working vent it guarantees the room stays below harmful smoke levels. Tribes can run fires, torches, and fueled benches indoors from day one.
- **Cross-level rituals**: the ritual menu now lists free colonists from every linked level, and participants on other levels walk to the ritual on their own — through as many stairwells as it takes — joining with their assigned role the moment they arrive. The ritual lord itself stays strictly one-map (mixed-map lords break duty AI); off-level pawns are held out at start and added on arrival. Degrades safely: if the ritual ends early, the route breaks, or you draft the pawn, they just go back to normal AI. Prisoners and animals still need to be on the ritual's level. Toggleable in mod settings.
- **Combined level resources (toggle)**: a new play-settings toggle makes the resource readout — and every other count-based reader (designator cost labels, "make until X" bills) — show colony-wide totals across all linked levels instead of just the current one. Off by default; pure vanilla per-level counts when off.
- **Construction demand pull**: a level whose blueprints and frames are short of materials now pulls them from other levels automatically — including when the demanding level has no stockpiles at all (a fresh dig with nothing but wall blueprints pulls its wood down on its own) — no stockpile setup needed. Haulers ship matching materials (loose or stored, any priority) through the stairs; once on the right level, vanilla construction delivery takes over. A level's own shortage always keeps first claim on its materials, shipments only go where a construction site is actually walkable from the landing, and the shortage ledger refreshes on a slow cadence so over-shipping self-corrects.

### Fixed
- **Smoke killed colonists at fueled workbenches in seconds** (reported: "my smithy and smelter killed my colonists in like 15 seconds"). Three compounding bugs:
  - The burner auto-patch tagged *every* refuelable building as a full-rate burner — fueled smithies, stoves, and smelters smoked like generators, **constantly, even while idle**, and a passive cooler would have smoked too. Fueled workbenches now emit gently and **only while a pawn is actually working them**; other refuelables only smoke with real evidence of combustion (flame overlay or heat output).
  - Open exterior doors vented too little to matter — a single worked bench out-emitted the maximum door bonus. Door venting is roughly doubled: one open exterior door now keeps a worked bench's room below the harm threshold.
  - Smoke inhalation severity climbed so fast that full smoke killed in about a minute. Retuned: a pawn in 100% smoke starts coughing after roughly an in-game hour and dies only after several — a hazard you can see and react to.
- **Landings were never Strata buildings**: vanilla's `GenStep_PlaceCaveExit` ignores `exitDef` and always spawns its 3×3 rope cave exit — so no level ever had a real stairwell/elevator landing, and the exit-side power shaft never existed. New `GenStep_PlaceLevelExit` spawns the entrance's actual `exitDef`.
- **Power tie brownout death spiral**: the shaft tie used to push a flat 2,000W toward the emptier grid regardless of demand, draining the source, getting shed by vanilla brownout logic, and locking off until the grid could afford the full draw. The tie is now demand-driven (a grid asks for its running deficit plus a battery-equalization trickle that tapers to zero), flows both ways, respects the elevator's flick switch and breakdowns, and recovers on its own after a brownout. Batteries on each level are optional now.
- **Pursuing raiders no longer turn around and run back upstairs.** Three stacked causes, all fixed:
  - A pawn stepping through a portal leaves its lord behind, and the vanilla portal job strips it *after* the arrival hook runs — so even instant re-enrollment was silently undone, and lordless raiders took vanilla's "leave the map" job (the stairs they came down). Arrivals are now enrolled one tick later, past the strip, with any escape job force-interrupted.
  - Strays get enrolled even on levels with no colonists, so multi-hop pursuits keep moving instead of bouncing home at the first landing.
  - The killer: pursuers were put in vanilla's assault lord, whose **steal impulse** triggers the moment high-value things are around — and pursuers arrive at a landing *inside* the base, so they grabbed loot and marched straight back out without a fight. Pursuers now get Strata's own assault lord that mimics a regular raid in full — they can give up and retreat, leave satisfied after enough destruction, kidnap a downed colonist, or turn to looting — but the steal impulse is delayed by about an in-game hour of assault, standing in for the approach walk vanilla raiders get for free. They fight first; the raid ends the way raids end. The give-up clock also **carries across levels**: time spent assaulting upstairs counts against the pursuit's retreat window downstairs, so chasing the colony through three stairwells doesn't hand the raiders three fresh timers.
- **Deep raid reworked into an insect eruption**: human raiders crawling out of solid rock never fit the fiction — the deep belongs to the bugs. Deep raid now uses vanilla's tunnel-spawner (rubble, dust, then the floor bursts open) to erupt a points-scaled insect swarm near your colonists, no hive left behind. Also fixed the event silently fizzling on low-wealth levels (points now floor at a real swarm and cap so a treasure vault doesn't spawn an army), Strata's debug incident buttons bypass storyteller pacing gates (mercy windows, cooldowns), and the incident logs a dev-mode reason whenever it refuses to fire.
- **Underground events never fired — at all**: pocket maps have no incident target tags in vanilla, so the storyteller (and even the debug incident menu) never targeted underground levels with anything. Deep raids, cave-ins, gas pockets, deep veins — and the advertised extra infestation pressure — were all dead code. Strata levels are now tagged as player-home incident targets, with the existing sealed-rock filter deciding what can actually reach them: infestations, diseases, and Strata's own events fire; raids, drop pods, and sky weather still can't.
- **Force-hauling works again — locally AND across levels**: the cross-level haul workgiver shared vanilla hauling's right-click label and, running at higher priority, shadowed the normal "prioritize hauling" option (the menu dedups by label) — so forced hauls tried to go cross-level or nowhere. It now has its own wording: right-clicking shows the normal local "prioritize hauling" *and*, whenever storage on another level genuinely wants the item, a separate "prioritize hauling to another level" order.
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
