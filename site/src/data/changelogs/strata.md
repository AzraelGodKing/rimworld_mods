# Changelog

Player-facing release notes for Strata (Steam Workshop style).
**Version:** `3.3.2` in `About.xml` `modVersion`. Player.log: `[Strata] v3.3.2 Soft-compat build <stamp> loaded from ...`.

**Build stamp:** each DLL logs the stamp after the version. Current stamp is `mp-portal-tick-v1` in `StrataBuildInfo.BuildStamp`.

## [Unreleased]

## [3.3.2]

Player-facing version **3.3.2** (`About.xml` `modVersion`). Startup writes `[Strata] v3.3.2 Soft-compat build mp-portal-tick-v1` in Player.log.

AZR-149 AZR-150

### Fixed
- **Paramedic mechs / allowed areas** (`paramedic-zone-v1`, AZR-149) — Biotech Paramedics are out of work relay and doctor medical relay (they still tend/rescue on their floor, and can climb when drafted or carrying a patient). Auto `EnterPortal` jobs require the landing in the pawn's allowed area. Misc. Robots may still leave a restriction when robot work relay is on.
- **Multiplayer stair jobs** (`mp-portal-tick-v1`, AZR-150) — `PortalRelayChain`, haul-delivery, drafted stair continue, and caravan-pull job creation run on each map's `MapComponentTick` instead of `TickManager.DoSingleTick`. World-clock `MakeJob` desynced UniqueIDs under Multiplayer async time.

## [3.3.1]

Player-facing version **3.3.1** (`About.xml` `modVersion`). Startup writes `[Strata] v3.3.1 Soft-compat build update-news-v1` in Player.log.

### Added
- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.

## [3.3.0]

Player-facing version **3.3.0** (`About.xml` `modVersion`). Startup writes `[Strata] v3.3.0 Soft-compat build harmony-jobdriver-v1` in Player.log.

AZR-101 AZR-100 AZR-64 AZR-61 AZR-63 AZR-57

### Added
- **Elevator call / hold / priority** (`floors-pack-v1`, AZR-64) — gizmos on both elevator landings. Call sends selected colonists through this car. Hold keeps it off transit hops to a third floor. Priority 1–5 weights automatic routing. Soft / Default / Hard do not change this.
- **Level stamp** (`floors-pack-v1`, AZR-61) — **Stamp** on the Levels tab copies walls, doors, floors, and optional stockpiles onto a linked floor as blueprints. Rotate 90° before confirming. Shafts are not copied.
- **Support overlay** (`floors-pack-v1`, AZR-63) — play-settings toggle outlines excavated cells under thick rock that are past a roof holder or shoring pillar. Cave-in incidents pick those cells first.

### Fixed
- **Sleep on the ground** (`floors-pack-v1`, AZR-101) — `JobGiver_GetRest` / `ForceSleepNow` no longer keep a no-bed `LayDown` when the assigned bed is on another linked floor. Arrival tries a rest detour if the landing cannot walk to the bed.
- **Paramedic stair hauls** (`floors-pack-v1`, AZR-100) — mechanoids skip `WorkGiver_HaulAcrossLevels` unless forced. Non-slot haul destinations that are pawns are ignored.
- **Undug rock fog** (`floors-pack-v1`, AZR-57) — underground `FogGrid.Unfog` on a still-mineable cell is put back. Arrival chambers and mined cells stay visible.
- **Harmony JobDriver Cleanup** (`harmony-jobdriver-v1`) — `HarmonyPatchAll` only processes types with `[HarmonyPatch]`. Cross-level `JobDriver` / `LordJob` classes inherit vanilla `Cleanup`, which Harmony treated as a patch auxiliary and logged eight errors. Stair jobs were never patched; they still run as written.

## [3.2.1]

Player-facing version **3.2.1** (`About.xml` `modVersion`). Startup writes `[Strata] v3.2.1 Soft-compat build v321-v1` in Player.log.

### Fixed
- **Guarded Harmony** — each patch class is applied on its own; one missing target logs and skips instead of leaving Strata half-patched.

## [3.2.0]

Player-facing version **3.2.0** (`About.xml` `modVersion`). Startup writes `[Strata] v3.2.0 Soft-compat build v320-v1` in Player.log.

### Added
- **Room air on the stats card** (`v320-v1`) — oxygen, carbon dioxide, and smoke appear on the room stats gizmo with fine / stale / dangerous stages.
- **AASB / MultiFloors detection** — `About.xml` `incompatibleWith` plus a load letter if those mods are still active.

### Changed
- **Verbose log** — routine Player.log chatter is gated by **Verbose log** (off by default). One startup line and real errors stay.
- **See-below DrawPos** — mass `Thing.DrawPos` Harmony patches apply only while see-below is enabled; they unpatch when the setting is off.
- **Exhaust auto-tag allowlist** — C# no longer walks every `ThingDef`. Only a named vanilla/DLC list is auto-tagged; generators, campfires, and Homesteader burners stay on XML patches.

### Fixed
- **Root cellar rot state** — Homesteader root-cellar cooling on underground floors no longer stashes rot progress in statics between Harmony prefix and postfix.
- **Session statics on save load** — `[StrataSessionReset]` sweeps caches (forced hibernate, caged birds, threat letters, deferred gen, off-thread breath jobs, VEF/VTE pipe nets, and the existing relay list) at `Game.FinalizeInit`.
- **CN / RU Keyed** — removed 14 leftover gravship keys that English never had. Russian DefInjected now includes gravship life-support buildings.

### Performance
- **Map kind memoization** — `IsUnderground` / `IsUpperLevel` / depth cache per `map.uniqueID`; biome compared by reference.
- **OpenRoofCount** — unroofed-cell walks on Strata floors cache until `RoofGrid.SetRoof`.
- **Breath diffusion** — per-map array buffers instead of cloning ~1.2 MB each enqueue.
- **Atmosphere cloud keys** — one reused `List<int>` instead of `Keys.ToList()` each cycle.
- **Hibernate presence** — `ColonyPresenceCount` cached ~60 ticks.

## [3.1.0]

Player-facing version **3.1.0** (`About.xml` `modVersion`). Startup writes `[Strata] v3.1.0 Soft-compat build exhaust-allowlist-v1` in Player.log. Cavern floors follow Biomes! Caverns climate/depth instead of locking B1–B2 to Earthen Depths.

### Fixed
- **Exhaust auto-tag allowlist** (`exhaust-allowlist-v1`) — C# no longer walks every `ThingDef` and tags anything that looks combustive. Only a named vanilla/DLC list (fueled stove/smithy, Ideology braziers and darktorches) is auto-tagged; generators, campfires, and Homesteader burners stay on XML patches. **Auto-tag known burners** in settings (on by default) turns the C# list off. **Verbose log** prints the tagged names at startup. Still **3.1.0** until a patch bump.
- **Root cellar rot state** (`rot-state-v1`) — Homesteader root-cellar cooling on underground floors no longer stashes rot progress in statics between Harmony prefix and postfix. Each `CompRottable.TickInterval` keeps its own `__state`, so a throw (or any overlap) cannot make the next stack rot at the wrong rate. Still **3.1.0** until a patch bump.
- **CN / RU Keyed** — added B1 infestation settings and the quest-map shaft warning strings that English already had.
- **Stormproof outdoor-room patch** — one `PsychologicallyOutdoors` Harmony class now covers Strata pocket levels and Stormproof's enclosed-underground rooms, instead of two patches fighting the same getter.
- **Shaft dig-down on underground floors** (`shaft-temp-map-guard-v1`) — digging deeper from B1 (or any Strata underground/upper level) no longer falsely triggers the "temporary quest map" guard. The guard still blocks shafts on actual quest sites, Ancient Urban Ruins, and caravan maps.

### Added
- **Deep Shafts scenario** — mountain/shaft showcase start; unlocks Digging Down + Building Up; locks Azrael when Homesteader is loaded.
- **Cave mods stay out of your column** (`foreign-portal-exclusion-v1`) - Anomaly undercaves, Deep And Deeper caves, and similar portal maps are no longer treated as colony floors for work / food / rest / alerts. Optional setting: *Relay into other mods' portal maps* (off by default).
- **Underground infestations toggle** (`b1-infestations-toggle-v1`) - Mod Options > Threats & performance. Turn off bug infestations on B1+ floors if you want a quieter basement.
- **Stair pair IDs** (`stair-pair-id-v1`) - shafts and landings remember their partner. Broken links auto-repair when possible; Dev tools can relink by hand.
- **Haul across levels** - separate Mod Options toggle (on by default). Work relay no longer pretends to control stair hauling.
- **Work relay on by default** - idle colonists head to other floors that need work. Turn it off in Mod Options if you prefer.
- **Native cavern digs** (`native-cavern-biome-v1`) - without Biomes! Caverns, dig levels get mixed rock, warren tunnels, and cave floors. With Biomes! loaded, Biomes! still owns layout.
- **See-below on roof decks** (`see-below-v1`) - look (and click) through open sky into the map underneath; float menus and selection work through the hole.
- **Cross-level combat** (`cross-level-combat-v2`) - drafted pawns, turrets, and mortars can fight through open sky (settings toggles for combat + auto-engage).
- **Force-build across floors** (`force-build-across-v1`) - prioritize construction even when materials only exist on another linked floor; pawns take the stairs and deliver.
- **Combined resources** (`combined-resources-stockpile-v1`) - optional shared stockpile readout across linked floors (off by default for performance).
- **Vertical utilities** (`a4-one-net-v1`) - shaft power (and soft DBH / VEF / Rimefeller junctions) keep pockets supplied without a battery on every floor.

### Changed
- **Incompatibilities** — listed [As above, So below 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3776015553). The original [As Above, So Below](https://steamcommunity.com/sharedfiles/filedetails/?id=3767572810) is marked deprecated. Do not run either with Strata.
- **Biomes! Caverns floor profiles** (`cavern-biome-depth-v1`) — B1–B2 no longer always generate as Earthen Depths. Picks follow [Biomes! Caverns](https://steamcommunity.com/sharedfiles/filedetails/?id=2969748433): wet surface tiles lean fungal forest, cold tiles lean crystalline caverns, and magma-style earthen depths become common only deeper (Caverns' own workers treat that biome as cave-system depth 2–3+). Already-dug floors are unchanged.
- **Work relay** (`work-relay-board-v1` / `work-relay-smooth-v1`) - smoother job board: less stair looping, faster wake-ups when new designations appear, smarter caps so half the colony does not stampede one blueprint.
- **Faster dig generation** (`map-gen-fast-v1` / `dig-open-yield-v1`) - opening a new underground level freezes the game less; rock fill streams in with a progress bar.
- **Roof decks** (`roofdeck-match-below-v1`) - walkable pads match the floors/rock below instead of blank white concrete.
- **Idle performance** (`idle-tps-v2` / `surface-idle-tps-v1`) - quieter scans when nothing is linked / gases are off; combined resources no longer on by default.
- **Workshop description** - About / Steam text updated for Version 3 features.

### Fixed
- **Combat Extended cross-floor fire** (`ce-cross-level-combat-v2`) — CE guns are not vanilla `Verb_LaunchProjectile`, so drafted orders, auto-engage, and turrets never found a weapon to shoot through open sky. Gap shots now use CE verbs, spawn CE bullets on the paired floor, spend magazine ammo, and launch with CE's ballistic angle (a 0° shot had zero range and died at the muzzle). Vanilla combat unchanged when CE is absent.
- **Omni junction + Helixien/VTE/Chemfuel** (`omnijunction-linkedpipes-v1`) — listing Omni on multiple VEF `pipeDefs` made `PipeSystem.LinkedPipes` throw duplicate-key `Strata_ShaftFluid_Omni`, then every pipe place/print crashed. Omni still joins those nets via CompResource; only dedicated shaft junctions stay in `pipeDefs`.
- **Second stairwell parallel pocket** — if a sibling already opened B1/A+ but no landing cell is free in the 25-cell ring, refuse instead of generating a second pocket map.
- **Orphan upstairs dump** — climbing a broken landing no longer teleports into random rock (`CellFinder.RandomCell`); only standable unfogged cells.
- **Flood seep / sump** — pumping restores the original floor instead of leaving shallow water forever (`SetTerrain` + `RemoveTopLayer` was a no-op). Inspect string says if there is no flood in range (Steam Jul 26).
- **Ore hoist singles** — single-item stacks despawn before placing on the partner level so they actually transfer.
- **Shaft-fluid FindMod** — DBH / VE / Rimatomics / Rimefeller pipe comps match Workshop display names (packageId-only lists never attached).
- **Prioritize haul to B1 storage** (`haul-b1-store-v2` / `haul-complete-path-v1` / `haul-needs-parity-v1`) - float menu "Cannot haul… no accessible spot" only checks the current map. *Haul to another level* finds linked shelves, only takes a shaft with a walk to that stockpile, and auto-haul uses the same vanilla "needs haul" rules (designations / no local spot) then finishes with a normal haul-to-cell into the chosen shelf.
- **Climbing upstairs crash** (`orphan-stairs-enter-v1` / `steam-bugfix-enter-v1`) - going up never calls vanilla `PocketMapExit.OnEntered` (it reads `entrance.Map` / `entrance.def.portal` after the teleport and NREs when the host unlinks). Landings teleport via `Notify_ThingAdded` and play their own traverse sound. Enter is refused if the destination map is gone (Steam Aug 12, Bug Reports #16).
- **Auto haul to another floor** (`haul-export-claim-v1`) - loose loot that only *Force haul* would move downstairs now gets picked up automatically when a better stockpile is on a linked floor.
- **Empty second level under mountains** (`mountain-second-level-rock-v1`) - digs and towers over mountain mass keep mineable rock instead of opening a hollow void.
- **Biomes! mountain digs** (`mountain-biomes-hollow-fallback-v1`) - if Biomes! leaves almost no rock, Strata fills a diggable warren so you are not staring at an empty contour.
- **Storage mods (ASF / Neat / Hauler's Dream)** (`weekend-steam-backlog-v1`) - fewer stair haul loops and failed shelf deliveries; cargo stays carried through stairs.
- **Quest maps** (`weekend-steam-backlog-v1`) - Strata shafts will not plant themselves on ancient urban ruins / world quest sites.
- **Chemfuel junction error** - clears the missing research spam with Vanilla Chemfuel Expanded.
- **Mental breaks** (`mental-break-cross-level-v1`) - tantrums, manhunters, and friends can chase across stairs instead of soft-locking on one floor.
- **Orphan stairs** (`orphan-stair-deconstruct-v1`) - unlinked shafts/landings can be deconstructed again (still blocked while a linked floor has your people).
- **Low Food / Low Medicine** (`alert-food-med-levels-v1`) - alerts count stockpiles on every linked floor, not just the surface.
- **Construction reserves** (`construct-reserve-probe-v1`) - builders no longer steal stacks another pawn already claimed.
- **Stuck carriers** (`haul-stuck-carry-v1` / `haul-construct-to-storage-v1`) - no more Wait loops with invisible cargo after a failed stair haul; leftovers go to storage on that floor.
- **Atmosphere** (`atmosphere-thoughts-dll-v1` / `atmosphere-v35-bugfix`) - suffocation/stuffy thoughts load correctly; sealed-room gas sim (opt-in) no longer treats new rooms as instant vacuum; CN/RU strings filled in.
- **Haulers stuck underground** - colonists and Misc. Robots keep returning upstairs for the next load.
- **Cargo dropped at stairs** - hauling / force-build keeps materials through the portal (Pick Up And Haul soft-compat on arrival).
- **Animal cage feed** - stored hay/kibble drops when the cage is removed instead of vanishing.
- **Force-build / prioritize across floors** (`force-build-commute-v1` / `V3 Complete`) - greyed-out build, mine, deconstruct, and haul options on other floors work; Ideology and turret builds included.
- **Mod Options UI** (`modoptions-column-v1`) - all toggles visible again (no more single "View level above" line); options no longer blank out.
- **Pink UI icons** - shaft power gizmos and bad Mine-icon overrides cleaned up.
- **Gravship / ReGrowth** (`regrowth-splash-v1`) - underdeck open no longer black-screens or null-refs with ReGrowth SimpleFX.
- **Stairs vanishing underfoot** (`portal-travel-no-wipe-v1`) - walking through a portal no longer destroys the landing.
- **Gravship stairs vanish on launch** (`gravship-stairs-launch-v1`) — portal `DeSpawn` immunity swallowed Odyssey’s pack, so shafts stayed spawned or were swept off the GravAnchor map without a packed copy; land then skipped `MakeThing` restore. Host shafts may `DeSpawn(WillReplace)` during takeoff/land; leftover sweep only despawns after a successful pack; land `MakeThing`s a missing shaft.

## Older notes

### [2.0] - 2026-07-16

**Strata V2** - dig down, build up, breathe deep; Odyssey gravship stacks.

#### Added
- Tower stairwells / elevators open outdoor Level +N roof decks (Page Up/Down, Levels tab).
- Odyssey gravship stacks: A+/B+ travel with the ship and reattach on landing.
- Living Below buildings (shoring, airlocks, ore hoist, fungus farm, pumps, lamps, scrubbers) plus flood seep.
- Deep threats and quest sites (firestorm, deep siege, cave breakthrough; mine / vault / vent sites).
- Mine gases and breathable-deep atmosphere (O2/CO2, pumps, cages, overlay) - opt-in where noted.
- Gas pipes and cross-level fluid shafts (DBH, VEF, Rimefeller, and friends).
- One colony column: work, food, rest, medical, joy, haul, and raid pursuit across floors.
- Native warren digs or Biomes! Caverns; optional ancient surface stairwell; rich ore / deep gas economy.
- Rotatable handrail stairs (optional MultiFloors art pack in settings).

#### Changed
- New levels match parent map size (1:1 stack).
- A+ maps are roof decks (build where roofed below), not full concrete pads.
- Performance options for background floors and throttled systems.
- Elevators research gates on Digging Down.
- Steam / About description updated for V2.

#### Fixed
- Gravship launch/landing crashes and empty-substructure previews.
- Multi-level save load (roof grid, plants, room HUD on upper decks).
- Stair rotation / landing facing.
- Underground incidents and ancient stairwell enter crashes.
- Power, oxygen, gas pipes, fluid junctions, and cross-mod duct conflicts.
- Cross-level haul spam, bill ingredients, Misc. Robots on shafts, sealed-shaft sieges.
- Broad RimWorld 1.6 startup / XML / Harmony compat cleanup.

### [1.0] - Strata V1

Initial release: excavated levels, relays, smoke sim, Levels tab, elevators, shaft power, raid pursuit, cross-level storage and construction, smoke ventilation, pursuing raids, deep insect eruptions, legacy landing migration.
