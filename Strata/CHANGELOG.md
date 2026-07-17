# Changelog

Short release notes for Strata. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **Off-thread atmosphere work** — O₂/CO₂ cavern diffusion (the per-cell cardinal pass over open chambers) snapshots gas arrays and skip masks on the main thread, computes on a background worker, and applies results on the next atmosphere cycle. Setting: `Off-thread O₂/CO₂ diffusion` under Threats & performance (default ON; requires breathing sim). Startup log: `[Strata] Off-thread work: enabled=...`.
- **Misc. Robots diagnostics** — mod settings section under Threats & performance lists live session counters (total + rate per 60s) for recharge/return relay prefix/postfix fixes, work JobGiver calls, work-relay scans/jobs, EnterPortal relay jobs, and ReachableLevels/BestFirstStep on robot paths; reset button and optional log every 600 ticks (`logRobotDiagnostics`, off by default).
- **Performance mode** master kill-switch: disables colonist work relay, Misc. Robots soft-compat, external JobGiver relay; atmosphere cycles run 4× slower and skip gas motes on non-viewed pocket levels.
- `Languages/English/Keyed/Strata.xml` — C# player strings (mod settings, alerts, messages, gizmos, inspect text, letters) with `.Translate()` wiring.
- `Languages/README.md` — translator guide (Keyed + DefInjected layout, package id).

### Changed
- **Atmosphere hitch reduction** — gas/O₂/CO₂ cycles spread across multiple ticks (prep → transport → sources → breath → finish) instead of one spike; background levels use lite batched room/plant/sync passes; colony-built room cache avoids rescanning thing lists every diffusion tick; overlay rebuild skipped when density unchanged on non-viewed maps; pawn gas harm throttled on background levels (animals skipped unless already affected on Low/performance mode).
- **Atmosphere quality setting** — Low / Medium / High under Threats & performance controls cycle multipliers on background levels (Low: 8× slower off-screen; High: full fidelity everywhere).
- Background **reduce-background** multiplier raised 4× → 8×; multi-level non-viewed multiplier 2× → 4×.
- Performance mode now enables **lite atmosphere** on all levels (batched breath grid, throttled pawn checks, no motes off-screen).
- Colonist **work relay off by default** (`workRelayEnabled`); 7500-tick scan cooldown when enabled so idle colonists do not run expensive cross-level work probes every think pass.
- Replaced empty `Languages/English/.gitkeep` scaffolding with real Keyed files.
- Compressed `About/Preview.png` for smaller Workshop/About footprint.

### Fixed
- **Vented room smoke clearing** — outdoor-vent cluster drain raised (**70%/54%** per cycle, was 58%/42%) and outdoor-facing wall vents add a **20%** extra flush (previously unused constant). Ventilated emission cap lowered **12% → 8%** so a fueled stove in a vented 10–20 cell kitchen clears instead of parking at the old cap. Strata exhaust fan **35%** and smoke louver **12%** per cycle (was 25%/6%). Sealed rooms unchanged.
- **Wall vent smoke exhaust** — open vanilla wall vents, VTE `VTE_WallMountedVent`, and similarly named wall vents again drain smoke outdoors and seed the outdoor-vent cluster. Regression from the exterior-door cluster pass: outdoor detection ignored vent exhaust direction and skipped null outdoor cells, so vented rooms never received cluster drain or the ventilated emission cap.
- **Surface sealed-building oxygen** — closed surface buildings (non-pocket maps) now receive ambient O₂ replenishment each atmosphere cycle instead of reading as 0% / hypoxic like underground voids. Underground and upper-level pocket sim unchanged.
- **Surface smoke venting** — one open exterior door now vents every room reachable through open interior doors (not just the entrance). Direct outdoor openings drain **58%/cycle**; linked rooms **42%/cycle** (~60 ticks). Kitchens and workshops in that cluster respect the **12%** ventilated emission cap again. Sealed underground rooms unchanged.
- **Smoke inhalation pacing** — harm threshold **0.15 → 0.18**, severity gain **0.006 → 0.0035** per atmosphere tick (still scaled by the Smoke inhalation severity setting). Same smoke % builds hediff roughly half as fast at default 100%.
- **Settings migration v1** — one-time upgrade for existing mod profiles turns off colonist work relay and Misc. Robots work relay (old saves kept them enabled despite new defaults); return/recharge soft-compat unchanged. Startup log includes build stamp, DLL path, file modified time, and relay flags.
- **Multi-level atmosphere throttle** — when the colony has more than one Strata pocket level, non-viewed levels run atmosphere cycles 2× slower (stacks with reduce-background and performance mode).
- Smoke/gas ventilation recognizes wall-mounted vents that sit on walls (`isEdifice=false`), including Vanilla Temperature Expanded `VTE_WallMountedVent` and similarly named wall vents (soft-compat, no VTE dependency). Surface rooms vented that way clear smoke like vanilla vents; underground outdoor rules unchanged.
- **Misc. Robots cross-level recharge (follow-up):** soft-compat now patches `Return2BaseRoom`, `RechargeEnergyIdle` (`TryIssueJobPackage`), and `RechargeEnergy` / `Return2BaseAndWait` / `Return2BaseDespawn` (`TryGiveJob`) with a shared relay — cross-map recharge never falls through to vanilla Goto/GoAndWait/GoRecharge (fixes 10-Goto/tick spam and cross-map `Could not reserve` on recharge stations). Startup log lines confirm each patch group. Prefix/postfix safety net when vanilla still emits a foreign-map job.
- Misc. Robots hauler/cleaner bots no longer spam `started 10 Goto jobs/tick` when `Return2BaseRoom` targets a recharge-station room on another Strata level — soft-compat routes via `EnterPortal` instead (no AIRobot assembly reference).
- Misc. Robots return-to-base portal relay no longer hitches every few seconds: cross-level routing uses a prefix (skips vanilla cross-map Goto), 6000-tick per-bot retry cooldown, cached cross-map recharge lookups, shared `BestFirstStep` route cache, and pooled level-graph BFS buffers.
- Misc. Robots work relay throttled (5000-tick scan cooldown) and no longer runs on `Return2BaseRoom`/recharge job givers; low-charge bots skip work scans and prioritize return-to-base. Return-to-base portal jobs no longer consume the general relay cooldown.
- **Robot hitch follow-up:** work relay no longer patches every `ThinkNode_JobGiver.TryIssueJobPackage` in the game (Harmony postfix on all colonist/animal/raider think passes was the remaining periodic hitch). It now targets only AIRobot work JobGivers via dynamic `TargetMethods`. `ReachableLevels` BFS is cached per map until portal topology changes. Robot work relay is **off by default** (`robotWorkRelayEnabled`); master A/B toggle `robotSoftCompatEnabled` disables all Misc. Robots soft-compat (return-base + work relay).
- **Performance pass:** external work relay (`Patch_ExternalJobGiverRelay`) uses the same dynamic JobGiver targeting (no global postfix when no mods register markers). Portal spawn/despawn invalidates level-graph caches. Vacant pocket hibernation keys off colonists and Misc. Robots only (not wild animals); hibernating levels with no gas state skip heavy atmosphere cycles.
- Irrigation bridge uses Homesteader water defs (`Wellspring_*`); standalone Wellspring retired.

## [2.0] — 2026-07-16

**Strata V2** — dig down, build up, breathe deep; Odyssey gravship stacks.

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
- **Steam / About description** — V2 copy with Discord, website, and roadmap links; building up, gravship, living below, atmosphere, fluid shafts, and quest content.

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

Level sharing, cross-level storage/construction, smoke ventilation, pursuing raids, deep insect eruptions, shaft power, legacy landing migration.

## [1.0] — Strata V1

Initial release: excavated levels, relays, smoke sim, Levels tab, elevators, shaft power, raid pursuit.
