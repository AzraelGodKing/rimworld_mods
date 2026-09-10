# Repo review — 5 new mods, 10 fixes + 10 ideas per existing mod

**Date:** 2026-09-10 · **Branch:** `claude/zen-goodall-ss5zuj` · **Scope:** full-repo code + def + docs review

## How to read this

Every row is written to become **one Linear ticket**. Approve or deny per row.

- **Fixes** = defects, robustness gaps, perf, and hygiene found in the current code. Each cites the file it lives in.
- **Ideas** = new features for an existing mod, scoped to that mod's declared ownership boundary in [`ROADMAP.md`](../../ROADMAP.md).
- **New mods** = five candidates that fill gaps the series does not cover today.

**Severity:** `P1` crash / save corruption · `P2` wrong behavior a player will notice · `P3` performance or reproducibility · `P4` hygiene, maintainability, coverage
**Effort:** `S` under a day · `M` a few days · `L` a real project

**Verification key:** ✅ = I read the code and the defect is in the file as described. 🔎 = the risk is real and the code path is as described, but the player-visible impact needs a playtest or a ref-pack check to confirm.

Nothing here duplicates an existing Linear `Todo` (AZR-60, 59, 84, 85, 103, 108, 136–142, 145, 147) or a checked box in a per-mod roadmap. Items that *extend* an open ticket say so.

---

## Part 1 — Five new mod ideas

Each one is picked because (a) no existing mod in the series owns it, (b) it consumes systems the series already ships, and (c) it can start small and stay useful.

### NEW-01 — **Upkeep** — buildings wear out and need servicing

The colony's infrastructure stops being fire-and-forget. Every powered or mechanical building accumulates a hidden **wear** stat from runtime, load, weather, and abuse. Wear degrades efficiency before it breaks anything: a worn cooler holds a few degrees warmer, a worn conduit drops a little power, a worn workbench runs slower. At high wear, breakdown chance climbs sharply.

The counter-play is a **maintenance job type**: a colonist with a toolkit and Construction services a building on a schedule, consuming a small part cost. Add a **maintenance ledger** building that lists every asset by wear, sorts by "due next", and lets the player set a service interval per building category — preventive maintenance instead of reactive repair. Ship a **spare parts** item line and a **workshop bench** that fabricates them from steel/components.

*Why it fits:* Stormproof already models storm wear on unhardened conduit (AZR-78) and graded brownout (AZR-77); Upkeep generalizes that into a colony-wide system Stormproof can feed. Strata's deep levels are the natural difficulty spike (everything down there is hard to reach). Homesteader's off-grid generators are the natural early-game teaching case.

*Start small:* wear stat + inspect line + one maintenance WorkGiver. Everything else is additive.
**Effort: L**

### NEW-02 — **Wayfare** — overland routes, waystations, and supply lines

Caravanning today is a straight line between two dots. Wayfare makes the space between settlements real. **Roads you build** on the world map cut caravan time and raise carry capacity. **Waystations** are tiny player-owned world objects that let a caravan rest, resupply, and cache goods without a full colony. A **standing supply route** lets you assign a caravan to run a repeating loop — sell preserves at a settlement, buy components, come home — with an approval prompt at each leg instead of manual re-forming.

Route danger scales with distance from home and with faction hostility along the path: bandit tolls, washed-out roads in the wet season, a broken axle that needs a spare.

*Why it fits:* Living World's roadmap explicitly parks **LW9 inter-settlement traffic** as "later" and Living World owns the *NPC* side of the world sim — Wayfare owns the **player** side, consuming LW's war and prosperity signals fail-open to price routes. Homesteader's farmstand (HS-A04) and Deep Colony's envoy/reputation both gain a real destination.
**Effort: L**

### NEW-03 — **Ward** — patrols, sensors, and a colony alarm state

A defensive-posture mod. Colonists can be assigned **patrol routes** (a drawn path plus a schedule slot) that they walk while otherwise idle, giving early warning instead of the player scrubbing the map. **Watchtowers** extend vision and mark map edges. A **sensor line** (tripwires, motion posts, seismic pads) fires a silent alert rather than a letter.

The centerpiece is a **colony alert state**: Green / Amber / Red, set manually or auto-escalated by a sensor trip. Each state carries a saved override of work priorities, door locks, and turret power — so "Red" is one click instead of fifteen. Add **lockdown doors** and a **muster point** colonists run to.

*Why it fits:* Nemesis's assaults and Strata's `RaidPursuit` cross-level chase both currently surprise the player with no counter-play; Ward is the answer. Stormproof's grid monitor already proves the "one console, live colony state" UI pattern.
**Effort: M–L**

### NEW-04 — **Convalescence** — illness, quarantine, and real recovery

Vanilla medicine is binary: tended or not. Convalescence adds the middle. Diseases become **contagious** with a transmission model (shared rooms, shared meals, water source), so an outbreak is a spatial problem. A **quarantine zone** designation and an **isolation ward** room role let you contain it. Recovery is staged — bed rest, then light duty, then full — with a **light duty** work state that caps a pawn's hours and job types instead of the all-or-nothing "downed" flag.

Add **medical records** per pawn (past illnesses, immunity, drug tolerance) and a **ward** room role that scores on cleanliness, separation, and doctor proximity.

*Why it fits:* Deep Colony explicitly owns *psychological* trauma and therapy and does **not** own physical medicine — that lane is open. Strata's sealed underground levels and Stormproof's toxic surge are ready-made outbreak pressure. The light-duty state is exactly the kind of thing Niceties users ask for.
**Effort: L**

### NEW-05 — **Provenance** — where things came from, and what that's worth

Every crafted item quietly remembers **who made it, when, where, and from what**. That history surfaces as an inspect line and drives three systems: a **maker's mark** (a legendary smith's work carries a name and a trade premium), **collections** (a set of items from one maker or one era displayed together gives a room bonus), and **relics** (an item that survived a raid, a famine, or a nemesis hunt gains a story and real mood value).

Traders pay for provenance. So do factions — gifting a piece with history is worth more goodwill than its market value.

*Why it fits:* Deep Colony already ships **B10 Heirlooms** and a Legacy screen but heirlooms have no *material* history; Provenance is the substrate under them, consumed fail-open. Homesteader's aging cheese/ham (HS-A03) is the food-side version of the same idea. Living World's chronicle gives events for a relic to have survived. This is the cheapest of the five to prototype: one comp, one inspect line, one trade-value hook.
**Effort: M**

---
## Part 2 — Per-mod fixes and ideas

Nine mods ship or live in this repo, so this section covers all nine. **Living World** and **Azrael** are not release-ready and are not in the GitHub Release zips — their rows are marked accordingly, and it is reasonable to deny the whole pair for now and revisit when they ship.

---

## Homesteader — `Homesteader/`  ·  v1.0.3

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-HS-F01** | Reorder the `AmbientTemperature` postfix so the cheap test runs first ✅ | `PassiveCooling.cs` postfixes `Thing.AmbientTemperature` — one of the hottest properties in the game — and does two map lookups (`__instance.Map`, `CoolingLookup.For(map)`, `HasAnyCooling`) **before** the cheap `RottableDefs.Contains(__instance.def)` HashSet test. Moving the def test first short-circuits the overwhelming majority of calls for the cost of one hash. | P3 | S |
| **RV-HS-F02** | `CoolingLookup` has no game-load reset ✅ | The static `byMapId` dictionary is keyed on `map.uniqueID`, which restarts at 0 for each new `Game`. `Unregister` only runs from `MapRemoved()`, which does not fire when the player quits to menu and loads a different save. A stale component for map 0 can then be served for the new game's map 0. Strata solves exactly this with `[StrataSessionReset]` — copy that pattern. | P2 | S |
| **RV-HS-F03** | Move own-mod def lookups from `GetNamedSilentFail` to `[DefOf]` ✅ | `FavoriteFood.cs` resolves `Homesteader_AllergicReaction` and `Homesteader_AteAllergen` by string on every allergen ingestion. Two costs: a repeated `DefDatabase` lookup on a warm path, and — more importantly — a packaging or XML failure makes allergies silently stop working instead of erroring. Given **AZR-108** (`unknown parse failure` on Nexus) is still open, loud failure is worth more than silent. | P2 | S |
| **RV-HS-F04** | `IsPreserveBill` classifies recipes by defName substring ✅ | `SpoilageTriage.cs` decides "is this a preservation bill?" with `n.IndexOf("Smoke")`, `IndexOf("Jam")`, `IndexOf("Pickl")` etc. and one hand-carved exception for `RockSalt`. Any future recipe whose name happens to contain one of those fragments is silently misclassified. Replace with a `DefModExtension` on the recipe, or an explicit def list. | P3 | M |
| **RV-HS-F05** | `FavoriteFood.cachedPool` is never invalidated ✅ | It is a process-lifetime static built once from `DefDatabase`. If the player changes Homesteader settings that affect what counts as a favorite candidate, the old pool keeps serving for the rest of the session. Add an invalidation hook on `WriteSettings`. | P3 | S |
| **RV-HS-F06** | Favorite pool ordering is not reproducible ✅ | `GetFavoritePool()` returns `set.ToList()` from a `HashSet<ThingDef>`, then favorites are drawn from it by index. Enumeration order depends on insertion order, which depends on mod load order — so the same colony seed rolls different favorites on different setups. Sort by `defName` before caching. | P3 | S |
| **RV-HS-F07** | `CompRootCellarCooling.PostSpawnSetup` can no-op during map generation 🔎 | `CoolingLookup.For(parent.Map)?.MarkDirty()` returns null if the map component does not exist yet. A cooler placed by a scenario or map-gen step therefore never marks the grid dirty and stays inert until something else dirties it. Fall back to queueing a dirty flag, or mark dirty unconditionally in `FinalizeInit`. | P2 | S |
| **RV-HS-F08** | `RootCellarCoolingMapComponent.MapRemoved()` does not call `base.MapRemoved()` ✅ | Harmless today (the vanilla body is empty) but it is the kind of omission that breaks silently on a game update. Same audit should cover the other overrides in the file. | P4 | S |
| **RV-HS-F09** | Add a ModChecks rule for own-mod `GetNamedSilentFail` ✅ | `Tests/ModChecks` already validates defName literals against the def XML. It does not flag the *pattern* of resolving your own mod's defs silently. A rule that fails CI when a `GetNamedSilentFail("Homesteader_…")` has no matching def would have caught F03's class of bug at build time. | P4 | S |
| **RV-HS-F10** | Audit the six settings that gate behavior but do not gate cost 🔎 | `SpoilageTriage.Enabled`, allergy and favorite-food paths all check their settings flag *inside* the work rather than before the hook runs. When a player turns a system off in Mod Options the patch still runs, allocates, and early-outs. Move the toggles into Harmony `Prepare()` or an early gate so "off" really means zero cost. | P3 | M |

### Ideas

| ID | Title | What | Est |
|---|---|---|---|
| **RV-HS-I01** | **Root cellar humidity** | A second hidden axis beside temperature. Cured meat wants dry, root vegetables want damp. A cellar that is too dry or too damp for what is stored in it slows preservation instead of speeding it — a reason to build two cellars. | M |
| **RV-HS-I02** | **Seed vault** | A building that banks landrace seed lines (`CompLandrace` already tracks yield/frost/drought bonuses and generations). Survives crop failure, blight, and toxic fallout; lets a player rebuild a 20-generation crop line after a disaster instead of starting over. | M |
| **RV-HS-I03** | **Preserve labels and dating** | Every preserved item carries the season and year it was put up. A "best by" inspect line, an oldest-first hauling bias, and a cellar readout of what is aging out. Pairs directly with **HS-A03 aging** on the roadmap. | S |
| **RV-HS-I04** | **Crop rotation memory** | Growing zones remember what was planted; repeating the same crop in the same soil drops yield, rotating or fallowing restores it. Makes the irrigated soil and planter upgrades matter and gives the farm a real layout problem. | M |
| **RV-HS-I05** | **Community kitchen ritual** | A shared cook-and-eat at the homestead hearth: colonists who eat together at the same table in the same hour get a bonus, larger with more distinct Homesteader dishes present. A cheap, warm counterpart to **HS-A01 harvest festival**. | M |
| **RV-HS-I06** | **Water quality** | Well, cistern, rain barrel, and solar-still water are not identical. Untreated well water carries a small sickness chance; boiled and filtered water do not. Gives the solar still and the boiled-water line a purpose beyond flavor. | M |
| **RV-HS-I07** | **Pantry planner tab** | Extend the existing pantry main tab into a forward projection: at current colony size and consumption, this pantry lasts N days, and here is the first thing that runs out. Complements the open **AZR-147** ("what can I make") from the other direction. | M |
| **RV-HS-I08** | **Grafting and cuttings** | Take a cutting from a high-quality orchard tree to propagate its landrace line rather than replanting from seed. Turns the apple/cherry/maple orchard into something you cultivate across generations. | M |
| **RV-HS-I09** | **Winter stores alert** | An alert that fires in late autumn if preserved food will not cover the colony through winter at current headcount, with a breakdown of the shortfall by food type. Turns the preservation chain into a deadline the player can plan against. | S |
| **RV-HS-I10** | **Barter-quality preserves** | Preserve quality (from cook skill and ingredient freshness) becomes a real trade stat: a masterwork jar of jam is worth meaningfully more to a trader. Gives the crafting skill a payoff outside mood, and feeds **HS-A04 farmstand**. | M |

---

## Strata — `Strata/`  ·  v3.3.2

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-ST-F01** | `AtmosphereMapComponent` is the only static-state holder missing `[StrataSessionReset]` ✅ | Every other cross-map static in Strata (`PawnRelay`, `RelayClaims`, `RaidPursuit`, `PortalRelayChain`, `StrataCrossLevelCombat`, `RaidCoordinator`, `WorkRelayAntiLoop`, `Patch_ConstructAcrossLevels`, `StrataCrossLevelAutoEngage`, `StrataRoomUtility`, `StrataPawnUtility`, `StrataIncidents`) carries the attribute. `AtmosphereMapComponent.pendingSeedsByMap` does not — and it is keyed on `map.uniqueID`, which restarts per `Game`. Load save A, quit to menu, load save B: queued gas seeds from A can drain into B's map with the same ID. | **P1** | S |
| **RV-ST-F02** | Fault-guard trip message mixes languages ✅ | `StrataFaultGuard.ApplyTrip` builds `label` from hardcoded English (`"work / haul relay"`, `"atmosphere / gases"`, `"shaft fluid junctions"`, `"robot soft-compat"`) and then passes it as the argument to the translated key `Strata_FaultGuardTripped`. CN and RU players get a translated sentence with an English noun in it. Use per-system Keyed labels. | P2 | S |
| **RV-ST-F03** | Fault-guard settings changes are never persisted ✅ | `ApplyTrip` sets `s.workRelayEnabled = false` (etc.) directly on the live settings object but never calls `Write()`. The auto-disable therefore lasts only for the session — **unless** the player happens to open and close Mod Options, which silently persists a change they never made. Decide one behavior and make it explicit. | P2 | S |
| **RV-ST-F04** | A tripped fault guard leaves no stack trace ✅ | Every `catch` that feeds `StrataFaultGuard.Report` passes `e.GetType().Name + ": " + e.Message` and discards `e`. When the guard finally trips, neither the player's Player.log nor a bug report contains a stack — the one thing needed to fix it. Log the full exception once per system on first occurrence. | P2 | S |
| **RV-ST-F05** | `Log.WarningOnce` keyed on `e.GetHashCode()` defeats the "once" ✅ | `WorkRelaySignals.cs:238` uses the exception instance's default hash as the dedupe key. Every throw is a new instance with a new hash, so a soft-compat probe that throws every relay scan spams the log unbounded — the exact failure the call was written to prevent. Key on the message, or a constant. | P2 | S |
| **RV-ST-F06** | Simulation rate depends on which floor the player is looking at ✅ | `StrataLevelPerfUtility.ShouldThrottleAmbient` / `ShouldReduceAtmosphere` and `AtmosphereMapComponent.ShouldSkipOverlayRebuild` all branch on `Find.CurrentMap != map`. This is a deliberate perf trade, but it means gas buildup and ambient sim on an occupied level literally run up to 4× slower while you are not watching it — so looking away is mechanically safer. Add catch-up on view change, or make the throttle rate view-independent. | P3 | M |
| **RV-ST-F07** | Shaft-fluid pairing tie-break uses `GetHashCode()` ✅ | `ShaftFluid/VefPipeSystemBackend.cs:276` orders a net pair with `net.GetHashCode() > partner.GetHashCode()`. Default object hashes are not stable across runs, so which net is treated as the "primary" of a pair can differ between two loads of the same save. Use a stable identity (net ID, or the lower `thingIDNumber` of the two anchor buildings). | P3 | S |
| **RV-ST-F08** | Reflection-discovered patch targets have no CI coverage ✅ | `Patch_ExternalJobGiverRelay.DiscoverTargetMethods()` and `Patch_RobotReturnBaseRelay.DiscoverTargetMethods()` resolve foreign methods by string at runtime. `Tests/ModChecks` validates *typed* Harmony targets against the Krafs ref pack but skips anything behind `TargetMethods()`. Add a manifest of the strings these discoverers look for plus a smoke test that each one still resolves against the mods it claims to support. | P4 | M |
| **RV-ST-F09** | Every map pays a `try`/`catch` on every atmosphere tick ✅ | `AtmosphereMapComponent.MapComponentTick` wraps `MapComponentTickInner` unconditionally. With a deep stack (surface + several pocket levels) that is one exception frame per map per tick even when the whole atmosphere system is toggled off. Gate the wrapper on the system actually being active, and let `IsAtmosphereSimulationIdle()` return before the `try`. | P3 | S |
| **RV-ST-F10** | Auto-disabled systems are invisible in Mod Options ✅ | When the fault guard trips, the corresponding checkbox in the settings window simply appears unchecked, with no indication that Strata turned it off or why. Add an inline "auto-disabled after repeated errors — see Player.log" note next to any flag the guard has flipped, with a re-enable button that also clears the trip. | P3 | S |

### Ideas

Roadmap items already listed in [`V3_ROADMAP.md`](../../Strata/V3_ROADMAP.md) (magma layer, flooded level, seismograph, noise, dumbwaiter, collapse trap, stack panel, lost floor) and open Linear tickets AZR-138/139/140/141 are **excluded** — these are additive to them.

| ID | Title | What | Est |
|---|---|---|---|
| **RV-ST-I01** | **Shaft integrity** | Stairwells and elevators accumulate structural strain from the weight of levels above and from nearby mining. Unshored shafts in deep stacks risk partial collapse. Adds a **shoring** buildable and a reason not to dig ten levels straight down. | M |
| **RV-ST-I02** | **Level naming and colour coding** | Let the player name each level ("Freezer", "Barracks", "The Deep") and pick a colour; names appear on the level switcher, in letters, in alerts, and on portal gizmos. Cheap, and every screenshot and bug report gets clearer. Natural companion to **AZR-140** level purpose tags. | S |
| **RV-ST-I03** | **Emergency ascent** | A one-shot lever on any shaft that orders every colonist on this level and below to drop what they are doing and climb. The counterpart to Strata's threat model — gas pocket, infestation, cave-in — where the current answer is manual drafting. | M |
| **RV-ST-I04** | **Rock strength by stratum** | Deeper strata mine slower and yield more, and specific rock types resist collapse differently. Gives the geothermal gradient a mechanical sibling and makes depth a real cost curve rather than only a danger curve. | M |
| **RV-ST-I05** | **Ventilation planning overlay** | A build-mode overlay that shows, before you place anything, which rooms on this level would be sealed and which have a path to a vent or the surface. Turns the smoke system from a punishment into a design problem. | M |
| **RV-ST-I06** | **Deep water reservoir** | A cistern carved into a wet stratum, filled by the water table, pumped up the shaft. Ties **AZR-139** (water table) to Homesteader's water ladder without either mod hard-depending on the other. | M |
| **RV-ST-I07** | **Level evacuation drill** | A scheduled drill that measures how long a full ascent actually takes, and reports the bottleneck shaft. Pure information, but it makes a tall base legible and it is a great Workshop screenshot. | S |
| **RV-ST-I08** | **Cargo lift automation** | Extend the ore hoist / cargo lift with a standing order: keep level N stocked with X of item Y, pulling from level M. Removes the "put a stockpile near the stairs and hope" pattern for the things that actually matter. | M |
| **RV-ST-I09** | **Bioluminescent cultivation** | A deep-level light source grown rather than powered — glowing fungus that lights a room at the cost of humidity and slow growth. Gives an unpowered deep base a real early game and pairs with the existing fungus farm comp. | M |
| **RV-ST-I10** | **Shaft traffic report** | A readout on any shaft showing pawn-trips per day, average queue time, and the top three relay reasons. The fluidity engine is Strata's headline feature and is currently invisible; this makes it observable — and makes relay bugs reportable. | M |

---
## Stormproof — `Stormproof/`  ·  v1.2.0

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-SP-F01** | Storm capacitors are invisible to every "does the grid have power?" check ✅ | Six systems gate on `PowerNet.CurrentStoredEnergy() > 1f` — `CompSolarShield`, `CompAtmosphericBarrier`, `CompClimateStabilizer`, `CompDroughtCondenser`, `CompSkyRestorer` — and `CompGridMonitor` / `CompLoadShedder` / `MapComponent_Stormproof.RefreshBrownoutCache` all sum `net.batteryComps`. `CompStormCapacitor` is deliberately **not** a `CompPowerBattery`, so a grid running purely on caught lightning reports **zero stored energy**: the solar shield refuses to protect during a flare, the grid monitor alarms at 0%, the load shedder trips, and brownout applies at full severity — while the capacitors sit full and discharging. This is the mod's flagship building silently failing to satisfy the mod's own checks. Introduce one `StormproofPower.EffectiveStored(net)` helper and route all eight call sites through it. | **P2** | M |
| **RV-SP-F02** | A switched-off or broken-down solar shield still drains 2,500 W ✅ | `CompSolarShield.CompTick` sets `powerComp.PowerOutput = -Props.activePowerConsumption` whenever `FlareActive`, with no check on `flickComp.SwitchIsOn` or `breakdownComp.BrokenDown` — even though `Protecting` checks both. During a flare a shield you turned off drains the grid at full rate and protects nothing. | **P2** | S |
| **RV-SP-F03** | Brownout postfixes two of the hottest methods in the game ✅ | `Patch_Brownout_PowerOutput` postfixes `CompPowerTrader.get_PowerOutput` and `Patch_Brownout_WorkSpeed` postfixes `StatExtension.GetStatValue`. Both call `BrownoutUtility.For(thing)`, which does `thing.Map.GetComponent<MapComponent_Stormproof>()` — a linear scan of the map's component list — on *every* invocation. `GetStatValue` is called constantly across the whole game, not just for worktables. Cache the map component per map, and check the cheap conditions before touching the map. | **P3** | M |
| **RV-SP-F04** | Brownout can permanently corrupt a heater/cooler's target temperature ✅ | `Patch_Brownout_TempControl` overwrites `control.targetTemperature` in a `Prefix` and restores it in a `Postfix`. Harmony does not run postfixes when the original method throws, so a single exception out of `Building_Heater.TickRare` — from vanilla or any other mod — leaves the player's target temperature permanently overwritten, and it saves that way. Use a `[HarmonyFinalizer]`, or compute the derating without mutating shared state. | **P1** | S |
| **RV-SP-F05** | Brownout cache is keyed on `PowerNet.GetHashCode()` ✅ | `MapComponent_Stormproof.RefreshBrownoutCache` stores `brownoutByNetId[net.GetHashCode()]`. RimWorld destroys and recreates `PowerNet` objects constantly as conduits change, and the default object hash is a recycled runtime value — collisions apply one net's brownout to another. Use a `Dictionary<PowerNet, float>` (reference equality) or the net's own stable identity. | **P2** | S |
| **RV-SP-F06** | The weather almanac stores localized labels ✅ | `TickAlmanac` records `cur?.label` and `NotifyCondition` records `condition.def.label` — the *translated* strings — into saved history. Change the game language and the almanac shows a mixed-language log of past seasons forever. Store `defName` and translate at draw time. | **P2** | S |
| **RV-SP-F07** | Load shedder scribes a hardcoded default instead of the def's ✅ | `Scribe_Values.Look(ref cutoffFraction, "stormproof_cutoffFraction", 0.20f)` hardcodes `0.20f` while `Initialize` uses `Props.defaultCutoffFraction`. The two silently disagree the moment the def changes. | P4 | S |
| **RV-SP-F08** | Load shedder recomputes its supply net every UI frame ✅ | `CompInspectStringExtra` calls `SupplyNet()`, which — when the breaker is open — iterates four adjacent cells and runs a LINQ `Sum` over each net's battery comps. That runs every frame the building is selected. Cache per tick. | P3 | S |
| **RV-SP-F09** | Fulgurite is silently destroyed if placement fails ✅ | `CompStormSpire.CollectFulgurite` zeroes `fulguriteReady` and then calls `GenPlace.TryPlaceThing` without checking the result. If the spire is fully enclosed or the area is packed, the player's accumulated fulgurite vanishes. Only clear the counter on a successful place. | P2 | S |
| **RV-SP-F10** | Strike-harvest messages have no rate limit ✅ | `Notify_Struck` fires a `Messages.Message` per strike per spire with no cooldown. A thunderstorm over a spire farm buries the message log. Batch per storm, or add the same cooldown pattern Strata's `RaidPursuit` already uses. | P3 | S |

### Ideas

Excludes open tickets **AZR-142** (scheduled load profiles) and **AZR-145** (per-storm damage report).

| ID | Title | What | Est |
|---|---|---|---|
| **RV-SP-I01** | **Grid topology view** | An overlay that draws each power net in its own colour, marks where a conduit run is the single point of failure, and highlights unbatteried sub-grids. The grid monitor tells you *how much*; this tells you *where*. | M |
| **RV-SP-I02** | **Lightning rod network** | Multiple spires wired together share a single attraction field and split caught energy, instead of each competing in its own radius. Makes a spire farm a design rather than a stack. | M |
| **RV-SP-I03** | **Storm shelter room role** | A room role that scores on being sealed, powered, stocked, and away from the map edge. Colonists route there automatically during a heat dome, polar front, or toxic surge. Gives the hazard family a defensive answer. | M |
| **RV-SP-I04** | **Seasonal climate report** | Extend the almanac from a log into a forecast: at this map's latitude and biome, here is the expected storm frequency and temperature band per quadrum, refined by what you have actually observed. Directly useful for planning power capacity. | M |
| **RV-SP-I05** | **Generator maintenance and derating** | Generators lose output as they run without service, and run hotter. A natural home for the wear model, and the obvious bridge if **NEW-01 Upkeep** ships. | M |
| **RV-SP-I06** | **Faraday room** | A room built from armored conduit and shielding is immune to EMP and ion effects entirely — a hardened core for the things that must not go down, and a build goal past the EMP dampener. | M |
| **RV-SP-I07** | **Battery bank health** | Batteries degrade with deep-discharge cycles and lose peak capacity; a maintenance action restores them. Makes the load shedder's cutoff a real decision rather than a preference. | M |
| **RV-SP-I08** | **Weather manipulation costs** | The storm caller currently costs only a five-day recharge. Give it a real price — grid energy, or a small chance of overshooting into a worse storm — so summoning weather is a gamble. | S |
| **RV-SP-I09** | **Power priority tiers** | Tag buildings Critical / Normal / Deferrable. During deficit the grid sheds by tier automatically, before the load shedder's blunt sub-grid cut. Complements **AZR-142** rather than duplicating it. | M |
| **RV-SP-I10** | **Storm damage insurance quest** | A faction offers grid-hardening contracts: keep your colony powered through the next N storms for a reward, lose it if you brown out. Turns the whole mod into a scored objective. | M |

---

## Nemesis — `Nemesis/`  ·  v1.1.0

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-NM-F01** | `GameComponentTick` does per-tick map work outside its interval guard ✅ | In `GameComponent_Nemesis.GameComponentTick`, `Map home = SoftCompat.PreferHarassmentMap(Find.AnyPlayerHomeMap)` runs **every tick** while a hunt is active — only its *result* is used inside `if (tick % healthInterval == 0)`. `Find.AnyPlayerHomeMap` walks the map list, and `PreferHarassmentMap` walks it twice more calling `IsStrataUnderground` / `IsStrataUpper` per map when Strata is loaded. Move the whole computation inside the interval check. | **P3** | S |
| **RV-NM-F02** | Four separate patch classes target the same `Pawn.Kill` ✅ | `Patch_Pawn_Kill_Nemesis`, `Patch_Pawn_Kill_TriggerNemesis`, `Patch_Pawn_Kill_FixationTrigger`, and `Patch_Pawn_Kill_WoundedEscape` each resolve and patch `Pawn.Kill` independently. The two postfixes both roll `Rand.Chance` and both can call `CreateNemesis`; their relative order comes from `AccessTools.GetTypesFromAssembly` reflection order, which is not a guaranteed-stable ordering. Consolidate into one prefix and one postfix with explicit precedence. | **P2** | M |
| **RV-NM-F03** | Trigger rolls consume RNG even when the hunt is already claimed ✅ | Both trigger postfixes call `Rand.Chance(...)` before `CreateNemesis`, and `CreateNemesis` then bails on `IsEngaged`. Combined with F02's ordering, the same kill can consume one or two rolls depending on patch order, so the effective trigger rate is not the configured rate. Check `IsEngaged` immediately before the roll, and roll once. | P2 | S |
| **RV-NM-F04** | Aggression climb depends on hitting an exact tick ✅ | `if (tick % 60000 == 0)` only advances aggression on the precise day boundary. Any path that leaves `_data.active` false across that tick — a truce that resumes at tick 60001, a hunt created mid-day — silently skips a full day of escalation. Track `lastAggressionTick` and advance by elapsed days instead. | P2 | S |
| **RV-NM-F05** | Health-check interval flips between 120 and 300 ticks ✅ | `healthInterval` changes based on whether the map is currently being viewed, and the check is `tick % healthInterval == 0`. Switching maps mid-hunt can skip a check window or double one. Use an explicit `nextHealthCheckTick` counter. | P3 | S |
| **RV-NM-F06** | Deep Colony's packageId is probed under two casings ✅ | `SoftCompat.cs:226-227` calls `GetActiveModWithIdentifier` for both `"azraelgodking.DeepColony"` and `"AzraelGodKing.DeepColony"` because the series is inconsistent about packageId casing (see **RV-AZ-F01**). Fix the root cause and delete the second probe. | P4 | S |
| **RV-NM-F07** | A rescued hostile in a colony bed can never become a nemesis ✅ | `NemesisTriggers.IsColonyInternedOrExecution` returns true for any pawn in a bed owned by the player faction. That correctly suppresses execution-in-a-cell, but it also suppresses the legitimate case of a hostile downed and tended in your hospital who then dies. Narrow the check to prisoner/slave/guest status plus `ExecutionCut`, and drop the bed-faction clause. | P2 | S |
| **RV-NM-F08** | `IsBuildingEmpProtected` does a def lookup and a full thing scan per call ✅ | `SoftCompat.cs` resolves `Stormproof_EmpDampener` via `DefDatabase.GetNamedSilentFail` and then walks `listerThings.ThingsOfDef` on every query. Cache the def once and the dampener positions per map with invalidation on spawn/despawn. | P3 | S |
| **RV-NM-F09** | Patch target resolution has no `Prepare()` guard ✅ | The `Pawn.Kill` patches use `AccessTools.Method(typeof(Pawn), "Kill", new[]{ typeof(DamageInfo?), typeof(Hediff) })`. If that signature ever changes, `TargetMethod` returns null and Harmony throws — caught by `SafePatchAll`, which is good, but the resulting player-facing letter is generic. Add a `Prepare()` that returns false with a specific log line naming the missing signature. | P4 | S |
| **RV-NM-F10** | No dev-mode force-spawn / force-end actions ✅ | Already on the Nemesis roadmap under Content/UX and still unchecked. Without it, every fix in this section costs a multi-hour playtest to reproduce. Worth doing **first**, as the enabler for F02–F07. | P3 | S |

### Ideas

Excludes the roadmap's hunt-base/false-lead arc, multi-faction antagonists, and open tickets **AZR-59** / **AZR-60**.

| ID | Title | What | Est |
|---|---|---|---|
| **RV-NM-I01** | **Nemesis reputation across factions** | Other hostile factions react to your nemesis — some respect them, some want them gone. A rival faction may offer intel or even a temporary truce to help you end the hunt. | M |
| **RV-NM-I02** | **Counter-intelligence** | Feed the nemesis false information through a captured informant: bait an assault into a killbox, or send them to an empty tile. The player-side mirror of the mod's existing false-lead mechanic. | M |
| **RV-NM-I03** | **Escalation ladder made visible** | A dossier readout of exactly what rises with aggression and what the next threshold unlocks. The hunt currently escalates invisibly; showing the ladder makes the pressure legible and the mod easier to tune. | S |
| **RV-NM-I04** | **Nemesis legacy** | When a hunt ends, the nemesis's faction may raise a successor with an inherited grudge and some of the dead one's gear and tells. Turns a resolved hunt into a story beat instead of a full stop. | M |
| **RV-NM-I05** | **Hostage exchange** | If you hold someone the nemesis cares about, the hunt gains a negotiation track: exchange, ransom, or refuse — each with different aggression and goodwill consequences. | M |
| **RV-NM-I06** | **Sabotage forensics** | After a sabotage event, a colonist with Intellectual can investigate the scene and recover intel — method, timing, sometimes the next target. Gives the sabotage beats a response other than repairing. | M |
| **RV-NM-I07** | **Nemesis in the world map** | The nemesis physically moves between world tiles between assaults, visible if you have intel. Turns tracking them into a caravan objective and makes the hunt feel spatial. | L |
| **RV-NM-I08** | **Personal grudge duel** | At high aggression the nemesis challenges the fixation target directly: accept for a one-on-one with high stakes, or refuse and take an aggression spike. | M |
| **RV-NM-I09** | **Colony paranoia** | Long hunts erode morale. Colonists gain a low-grade unease that a resolved hunt clears — and specific counters (Ward-style patrols, a resolved false lead) ease. Makes the hunt cost something between assaults. | M |
| **RV-NM-I10** | **Nemesis-authored letters** | Taunts arrive as in-fiction written notes with the nemesis's voice and tells, rather than system letters. Pure presentation, high impact on a mod whose whole premise is personality. | S |

---
## Deep Colony — `Deep Colony/`  ·  v1.6.7

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-DC-F01** | Toggling family-tree style can wipe the player's settings file ✅ | `Generations/FamilyTreeDrawer.cs:38-46` does `var settings = DeepColonyMod.Settings ?? DeepColonySettings.Get;` then `settings.Write()` on button click. `DeepColonySettings.Get` is itself `DeepColonyMod.Settings ?? new DeepColonySettings()` — so when `DeepColonyMod.Settings` is null the fallback chain hands back a **brand-new default object** and calls `Write()` on it. Best case that is a no-op on an unattached `ModSettings`; worst case it persists an all-defaults file over the player's configuration. The `??` is also redundant with `Get`. | **P2** | S |
| **RV-DC-F02** | `DeepColonySettings.Get` allocates on the hottest path in the mod ✅ | `DeepColonySettings.cs:84` returns `DeepColonyMod.Settings ?? new DeepColonySettings()`. `Get` is read several times per `SkillRecord.Learn` call — which is per-tick, per-working-pawn. If settings ever fail to load, that is a fresh allocation per read, and every read silently returns defaults rather than surfacing the failure. Return a single cached fallback instance and log once. | **P2** | S |
| **RV-DC-F03** | `SkillRecord.Learn` prefix + postfix each re-resolve the pawn comp ✅ | `Patches/Patch_SkillRecord_Learn.cs` calls `pawn.TryGetComp<Comp_DeepColony>()` in both the prefix and the postfix, on a method invoked every tick for every learning pawn. `TryGetComp` walks the comp list. Cache the comp on the pawn (or key it once per Learn via `__state`). | **P3** | M |
| **RV-DC-F04** | `RivalryUtility.HasRivalBoost` walks every direct relation per XP grant ✅ | Called unconditionally from the `Learn` prefix whenever mentoring is enabled. It iterates `pawn.relations.DirectRelations` and does two `GetSkill` lookups per rival relation — every tick, per pawn. Precompute the rival set per pawn and invalidate on relation change. | **P3** | S |
| **RV-DC-F05** | `MentorshipUtility.TryGraduate` runs on every positive XP grant ✅ | The `Learn` postfix calls it whenever `xp > 0f && comp.mentor != null`. `TryGraduate` re-resolves the comp, resolves the focus skill, and can fall through to `MentorLeadsInAnySkill`, which scans all skills. Graduation is a once-per-apprenticeship event; gate it behind `TickPhase` (the mod already has a staggered clock in `Data/TickPhase.cs`) or trigger it only on a level-up. | **P3** | S |
| **RV-DC-F06** | Tribute takes silver from whichever map the player is looking at ✅ | `FactionRep/TributeUtility.cs:54` — `Map map = Find.CurrentMap ?? Find.Maps[0];`. With two colonies, sending tribute from the world view or from colony B charges whichever map `Find.CurrentMap` happens to be. It also drops the null guard that the sibling `CanTribute` (line 32) correctly has, so `Find.Maps[0]` can throw when no map exists. Pick the map explicitly and show it in the confirmation. | **P2** | S |
| **RV-DC-F07** | Founder surname is picked from the currently-viewed map ✅ | `Data/GameComp_DeepColony.cs:55` — `EnsureFounderSurname` reads `Find.CurrentMap ?? Find.Maps[0]` and takes the first colonist's surname. Which colony is "the founding one" therefore depends on where the camera was the first time the value was needed. Resolve from the oldest colonist across all player maps instead. | **P2** | S |
| **RV-DC-F08** | Silent `catch { }` with no comment in the family-tree dev path ✅ | `Generations/FamilyTreeDevUtility.cs:229` swallows every exception from `Find.WorldPawns.RemovePawn` with no comment and no log. The neighbouring catches in the same file all carry an explanatory comment; this one hides a real world-pawn bookkeeping failure. | P4 | S |
| **RV-DC-F09** | `modVersion` is `1.6.7`, which reads as a RimWorld version ✅ | Every other mod in the series uses semver from `1.0.0`. Deep Colony's `1.6.7` looks like "RimWorld 1.6, patch 7" on the Workshop, on the docs site, and in the release tag `DeepColony-v1.6.7`. Decide whether to re-base to `2.x` (the roadmap already calls the current state "2.0 shipped") and document it in `docs/VERSIONING.md`. | P4 | S |
| **RV-DC-F10** | No perf regression guard on the `Learn` patch ✅ | F03–F05 are all in one method that runs thousands of times per second in a large colony. Before changing it, add a bench harness (or at minimum a dev-mode counter of Learn-prefix microseconds) so the fixes are provably improvements and future additions to this patch are visibly costed. | P4 | M |

### Ideas

Excludes everything checked in the Deep Colony roadmap (Batches A–E, touch-need, identity pack) — this is genuinely new ground.

| ID | Title | What | Est |
|---|---|---|---|
| **RV-DC-I01** | **Colony charter** | The founders write a charter at game start — three principles picked from a list. Colonists who see the colony act against its own charter take a mood hit; acting in line with it gives a small bond. Ideology-aware, works without it. | M |
| **RV-DC-I02** | **Mentor lineage tree** | A visual tree of who taught whom, across generations, in the same style as the shipped family tree. The teaching-lineage flavour (B09) exists but is invisible; this makes a 40-year colony's craft heritage legible. | M |
| **RV-DC-I03** | **Skill specialization branches** | At skill 15, a branching pick already exists (A02). Extend it: a specialization changes *how* the pawn works, not just numbers — a "field medic" tends faster but worse, a "surgeon" the reverse. | M |
| **RV-DC-I04** | **Reputation consequences at trade** | Faction attitude already exists; let it visibly move prices, caravan quality, and what a trader is willing to sell you. Currently the ledger is informative but weakly consequential. | M |
| **RV-DC-I05** | **Colonist ambitions** | Each pawn carries a private goal — reach skill 20 in X, marry, have a child, own a great bedroom. Meeting it gives a large lasting mood; ignoring it for years erodes. Turns the perk system into a character system. | L |
| **RV-DC-I06** | **Trauma triggers as map features** | A pawn who was ambushed in a specific room avoids that room. Ties the existing flashback system to colony geography and gives the player a concrete thing to change. | M |
| **RV-DC-I07** | **Written wills and estate disputes** | The estate system (AZR-70) exists; add the drama — a contested will, a sibling who disagrees, a mediation event that costs time and mood. | M |
| **RV-DC-I08** | **Generational skill drift** | Colonies that repeatedly train one skill and neglect another slowly shift what newborns are naturally good at. Makes a 100-year colony demonstrably *different* from a fresh one. | L |
| **RV-DC-I09** | **Envoy negotiation minigame** | Sending an envoy is currently a goodwill pulse. Give it a short decision — three approaches, faction-dependent outcomes — so the personal envoy feels like a person doing a job. | M |
| **RV-DC-I10** | **Colony anniversary** | An annual event that reads back the year: births, deaths, arrivals, the biggest fight, the best meal. Uses the chronicle export (AZR-71) that already exists. Cheap, and it is the emotional payoff the whole mod is building toward. | M |

---

## Date Night — `DateNight/`  ·  v1.1.1

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-DN-F01** | Pawns on a Date block refuse to sleep until Exhausted ✅ | `Patch_GetRest_GetPriority` returns `__result = 0f` for the whole Date schedule unless `rest.CurCategory >= RestCategory.Exhausted`. `Tired` and `VeryTired` pawns therefore keep dating through a long Date block and accumulate rest debt. Escalate at `VeryTired`, or scale priority with the rest need rather than cliff-edging at the last tier. | **P2** | S |
| **RV-DN-F02** | `RendezvousBedIds` static survives a save reload ✅ | `DateNightUtility.cs:15` keys a static `Dictionary<long,int>` on a couple key built from `thingIDNumber`s and stores a bed `thingIDNumber`. Nothing resets it between games in one session, and thing IDs restart per `Game` — so after quit-to-menu and load, a couple can be pointed at whatever thing now holds that ID. Strata's `[StrataSessionReset]` is the pattern to copy. | **P2** | S |
| **RV-DN-F03** | Lovin-job detection re-walks the type hierarchy on every check ✅ | `LooksLikeLovinJob` does up to four case-insensitive `IndexOf` scans per type name, walking the driver's whole base-type chain, and `IsBusyWithLovin` calls it on a warm path. The *approach* is right for soft-compat with other romance mods; the cost is not. Memoize per `JobDef` + concrete driver type in a dictionary. | **P3** | S |
| **RV-DN-F04** | Job-name sniffing can false-positive on unrelated mods ✅ | `NameLooksLikeLovin` matches any type or namespace containing `"Lovin"`, `"JobDriver_Sex"`, `"SexBase"`, or `"JoinInBed"`. A mod with an unrelated `JobDriver_JoinInBed…` gets treated as a lovin job. Add an explicit opt-in allow/deny list in Mod Options and log what was matched, so a player can correct a misdetection. | P3 | M |
| **RV-DN-F05** | Four `JobGiver` prefixes hard-return `false` ✅ | `JobGiver_GetRest.GetPriority`, `JobGiver_GetRest.TryGiveJob`, `JobGiver_Work.GetPriority`, `ThinkNode_Priority_GetJoy.GetPriority`, and `JobGiver_GetJoy.TryGiveJob` are all skipped outright during Date/Lovin hours. Any other mod postfixing those never runs during those hours. The `GetJoy` one is justified and documented (vanilla throws on custom `TimeAssignmentDef`s); the others should be audited for whether a return-value override would do. | P2 | M |
| **RV-DN-F06** | `Patch_Work_GetPriority` returns a magic `3f` ✅ | The value is uncommented and unconfigurable. It decides how much colony work happens during Date and Lovin blocks — the single biggest balance lever in the mod. Make it a setting with the current value as default, and comment where 3 sits relative to vanilla priorities. | P3 | S |
| **RV-DN-F07** | Schedule-grid layout guesses at other mods' button rects ✅ | `Patch_TimeAssignmentSelector_DrawGrid.DrawCombo` reconstructs its cell position from `rect.width * 0.5f`, a scanned "extra column index", and `Patch_AllowedArea_DoHeader.ButtonRect`, with `52f` / `24f` / `2f` fudge constants. It is careful work, but it is unverifiable and will drift with any UI mod or game update. Add a settings override for the column index and a "reset layout" action. | P3 | M |
| **RV-DN-F08** | `GameComponent_DateNight` persists only the news version ✅ | Its `ExposeData` scribes `lastNewsVersion` and nothing else, while per-couple date state lives in statics (F02) and in per-pawn lookups. Anything a player would notice resetting — last date tick, missed-date state, anniversaries — should be explicitly on the component and saved. Audit and document what is intentionally session-only. | P2 | M |
| **RV-DN-F09** | Alert and schedule strings need a CN/RU parity pass 🔎 | `Alert_ScheduleMismatch`, the date activity lines, and the double-date flow are the newest surfaces and the most string-heavy. `scripts/validate_mods.py` checks for Keyed drift; confirm it actually covers the 1.1.x additions and fill any gaps. | P4 | S |
| **RV-DN-F10** | No debug action to force a date or a lovin window ✅ | `DateNightDebug.cs` exists; confirm it can start a date between two named pawns and jump the schedule, because every fix above needs a repro that does not involve waiting for a timetable block. If it cannot, that is the first ticket. | P3 | S |

### Ideas

| ID | Title | What | Est |
|---|---|---|---|
| **RV-DN-I01** | **Date variety and memory** | Couples remember what they did last time; repeating the same date loses value, varying it gains. Turns the activity list into a small system instead of a random pick. | M |
| **RV-DN-I02** | **Date venues** | Specific buildings and room roles make better dates — a dining room, a garden, a rooftop. Gives the player a reason to build something purely for the couples. | M |
| **RV-DN-I03** | **Courtship stages** | Pre-relationship dating: colonists who like each other can go on dates *before* becoming lovers, with success feeding into whether the relationship forms. Currently romance is binary. | M |
| **RV-DN-I04** | **Anniversary and milestone events** | Extend the existing anniversary hooks into real events — a gift, a small ritual, a colony-wide mood bump for a long marriage. | M |
| **RV-DN-I05** | **Relationship maintenance need** | A slow-moving "connection" value per couple that dates raise and neglect lowers, feeding breakup chance. Makes the Date slot mechanically load-bearing rather than optional flavour. | M |
| **RV-DN-I06** | **Jealousy and rivalry** | Dating someone your ex or a hopeful admirer can see has social consequences. Small, cheap, and it makes the schedule a real decision. | M |
| **RV-DN-I07** | **Group and family outings** | Extend double dates to family outings — parents and children — feeding Deep Colony's family systems fail-open. | M |
| **RV-DN-I08** | **Date planning gizmo** | Right-click a partner to schedule a specific date at a specific time and place, instead of relying entirely on the timetable. Direct player agency over the mod's core loop. | M |
| **RV-DN-I09** | **Seasonal romance events** | A midwinter dance, a spring festival — colony-scale versions of the couple date. Pairs with Homesteader's maypole without either depending on the other. | M |
| **RV-DN-I10** | **Relationship history tab** | A per-couple timeline: met, first date, became lovers, married, the fights. Deep Colony ships a family tree; this is the romance-side equivalent. | M |

---
## Niceties — `Niceties/`  ·  v1.1.2

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-NC-F01** | Apparel care prefixes `Thing.TakeDamage` for the whole game ✅ | `Patch_ApparelCare.Patch_Thing_TakeDamage_Deterioration` is a prefix on `Thing.TakeDamage` — every bullet, every fire tick, every explosion, on every thing on every map — in order to intercept the tiny fraction that are `Deterioration` on worn apparel. The early-outs are cheap but the patch frame is not free at that call volume. Patch the narrower vanilla path that applies wear to worn apparel instead. | **P3** | M |
| **RV-NC-F02** | Colonist-bar filter does reflection every UI frame ✅ | `Patch_CryptosleepBar.FilterEntries` calls `AccessTools.Field(typeof(ColonistBar), "cachedEntries")` and then `.GetValue(bar)` from a `CalculateDrawLocs` prefix — i.e. once per frame the bar draws. Resolve the `FieldInfo` once into a static (or build an accessor delegate) instead of on every call. | **P3** | S |
| **RV-NC-F03** | The filter mutates the bar's live entry cache ✅ | `FilterEntries` removes from `ColonistBar.cachedEntries` in place. Anything that reads `ColonistBar.Entries` after draw-loc calculation — vanilla's own hit-testing, or another mod — sees a list that is short the cryptosleeping pawns. The comment acknowledges the layout hazard; the *consumer* hazard is not covered. Filter into a copy, or restore the list in a postfix. | **P2** | M |
| **RV-NC-F04** | Masterwork and legendary apparel never deteriorates at all ✅ | `ApparelCare.ChanceForQuality` returns `0f` for both, and `DailyWearChance <= 0f` makes the prefix swallow **all** `Deterioration` damage on that item permanently. That may well be the intent of "well-kept apparel", but it is an absolute rather than a scale, and it is not spelled out in the toggle's description. Either document it plainly in the setting text or give it a floor. | P2 | S |
| **RV-NC-F05** | The melee-hunt patch targets an inherited method name ✅ | `[HarmonyPatch(typeof(WorkGiver_HunterHunt), nameof(WorkGiver_Scanner.HasJobOnThing))]` names the *base* class member while targeting the derived type. It resolves today because `WorkGiver_HunterHunt` overrides it — but if that override is ever removed the attribute silently resolves against the base and the patch applies to **every work giver in the game**. Use `nameof(WorkGiver_HunterHunt.HasJobOnThing)` and add an explicit argument-type list. | **P2** | S |
| **RV-NC-F06** | ModChecks does not validate `nameof(...)` Harmony targets ✅ | `Tests/ModChecks/Program.cs:203-206` skips any match where group 2 (the quoted string form) did not capture — so every `nameof(...)` target in the repo is unchecked against the Krafs ref pack. That is the majority of the patches in this repo, including F05. Extend the regex and resolve `Type.Member` names. This is a repo-wide safety net, filed here because Niceties is where it bites first. | **P2** | M |
| **RV-NC-F07** | `unarmedHunting` grants a hunting weapon with no verb check ✅ | `Patch_HasHuntingWeapon` returns `__result = true` for an unarmed pawn purely on the setting, while the armed branch correctly checks `verb.HarmsHealth()`. A pawn whose melee capacity is fully disabled can therefore be assigned a hunt they cannot execute. Apply the same verb check to the unarmed path. | P2 | S |
| **RV-NC-F08** | `CompSharedRoom.PostExposeData` does not call `base` ✅ | Harmless today, and the same omission appears in a few other comps across the repo. Worth one sweep with a Roslyn analyzer or a ModChecks rule rather than six separate tickets. | P4 | S |
| **RV-NC-F09** | Prey body-size cap is checked but never surfaced before assignment ✅ | `PreyFitsMelee` sets a `JobFailReason` at job-assignment time, so the player sees the hunter refuse rather than understanding the rule. Add the cap to the melee-hunting setting description, and show it on the hunt designator. | P3 | S |
| **RV-NC-F10** | Toggles gate behavior but not patch installation ✅ | Every nicety checks `NicetiesMod.Settings.<flag>` inside the patch body. A player who turns all six off still pays for six installed patches, including the two hot ones above. Add Harmony `Prepare()` gating with a restart notice — the mod's own README promises each nicety "can be turned off", and off should mean off. | P3 | M |

### Ideas

Niceties is explicitly a QoL companion, not a content pack, and its README rule is that **every nicety ships with a master toggle and must no-op when off**. All of these respect that.

| ID | Title | What | Est |
|---|---|---|---|
| **RV-NC-I01** | **Auto-rearm traps in safe zones** | Traps inside a designated zone rearm without a manual re-designation, so a killbox does not need re-clicking after every raid. | S |
| **RV-NC-I02** | **Remember last stockpile settings** | New stockpiles inherit the filter of the last one you configured, instead of starting from the default. | S |
| **RV-NC-I03** | **Bulk medical operation queue** | Queue the same operation on multiple pawns from one dialog. | M |
| **RV-NC-I04** | **Don't drop equipment on draft-undraft** | Optional toggle to stop pawns dropping carried items when undrafted mid-haul. | S |
| **RV-NC-I05** | **Persistent camera bookmarks** | Save and jump to named camera positions per map, with hotkeys. Compounds well with Strata's multi-level bases. | S |
| **RV-NC-I06** | **Prisoner tab quick actions** | Set recruit/convert/release/execute for a whole cellblock at once. | M |
| **RV-NC-I07** | **Better bill target ordering** | Bills default to "nearest" ingredient rather than the vanilla scan order — a common friction point that fits the mod's remit exactly. | M |
| **RV-NC-I08** | **Show real hediff durations** | Inspect lines for hediffs show remaining ticks in a readable form instead of a severity bar. | S |
| **RV-NC-I09** | **Quiet alerts** | Per-alert-type mute, so an alert you have deliberately accepted stops shouting. | M |
| **RV-NC-I10** | **Wall-mounted furniture placement helper** | A placement preview that snaps decor to walls and shows the beauty radius while placing. | M |

---

## Living World — `LivingWorld/`  ·  v1.0.0  ·  **not release-ready**

Living World is in-repo but excluded from the GitHub Release zips. Phases 1–2 are done; the remaining roadmap items are LW9 traffic, sibling flavour, and Azrael weights. **These rows are mostly release-readiness gates rather than defects — it is entirely reasonable to deny the whole section and revisit when the mod ships.**

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-LW-F01** | `Log.WarningOnce` keyed on `e.GetHashCode()` ✅ | `LivingWorldSignals.cs:49` — same defect as **RV-ST-F05**. Every exception instance has a different default hash, so a consumer that throws on every signal spams the log without limit, defeating the entire point of `WarningOnce`. Key on the message. | **P2** | S |
| **RV-LW-F02** | `GameComponent_LivingWorld.ExposeData` never calls `base.ExposeData()` ✅ | Nemesis's equivalent component does. The vanilla body is empty today, so this is latent rather than live — but it is the kind of thing that breaks quietly on a game update. | P4 | S |
| **RV-LW-F03** | `LivingWorldSignals.ResetSession()` is a comment, not a reset ✅ | Its body is `// Registrations are process-lifetime; nothing to clear per save.` That is a defensible design, but it means a consumer registered by a mod that is later disabled keeps receiving signals for the rest of the session. Add explicit unregistration and a debug listing of who is currently subscribed. | P3 | S |
| **RV-LW-F04** | Cross-mod consumer contract is undocumented ✅ | Azrael's hub probes for the *type name* `LivingWorld.LivingWorldSignals` and Deep Colony's for `DeepColony.LivingWorldSoftCompat`. Renaming either class silently breaks the bridge with no build error and no test. Publish the signal API surface as a documented, frozen contract in `docs/ideas/living-world.md` and add a ModChecks rule that asserts the type names still exist. | **P2** | M |
| **RV-LW-F05** | Chronicle capacity is a hardcoded constant ✅ | `ChronicleCapacity = 96` with no setting. Open ticket **AZR-84** ("readable world history") will make players want more, and long games will want less. Make it a setting before the tab ships, not after. | P3 | S |
| **RV-LW-F06** | Letter and morph budgets are quadrum/year-scoped with no visibility ✅ | `lettersThisQuadrum` and `morphsThisYear` silently suppress events once spent, with nothing telling the player or a debugger. Add a dev readout of the current budget state. | P3 | S |
| **RV-LW-F07** | No Steam/Nexus release checklist ✅ | The mod is absent from the release zips and from the docs hub (`living-world.html` is on the legacy keep-list "until the mod is on the hub"). One ticket should enumerate the gates: About polish, preview art, changelog, `validate_mods.py` clean, hub page, release workflow entry. | P3 | M |
| **RV-LW-F08** | CN/RU coverage is unverified for a pre-release mod ✅ | `ENGLISH_ONLY_MODS` in `scripts/validate_mods.py` is empty "after AZR-134", which means Living World is already being held to the full-translation bar. Confirm that is actually true and either fill the gaps or re-add the exemption until launch. | P3 | S |
| **RV-LW-F09** | No debug forcing for wars, morphs, or refugee fallout 🔎 | `LivingWorldDebug.cs` exists at 236 lines; confirm it can force each of LW6/LW7/LW8's outcomes on demand. World-sim mods are otherwise untestable in reasonable time. | P3 | S |
| **RV-LW-F10** | Diplomacy state is a flat `List<FactionPairState>` ✅ | `LivingWorldDiplomacy.cs` is the largest file in the mod and pairs are scanned linearly. With many factions plus Ideology/Biotech additions that is O(n²) per sweep. Index by faction pair before LW9 traffic adds more consumers. | P3 | M |

### Ideas

Excludes open tickets **AZR-84** (chronicle tab) and **AZR-85** (rumours arrive wrong).

| ID | Title | What | Est |
|---|---|---|---|
| **RV-LW-I01** | **Trade goods flow** | Settlements specialize and their stock reflects it; a war upriver changes what the trader two tiles away is carrying. | M |
| **RV-LW-I02** | **Faction leaders as people** | Named leaders with traits who die, get replaced, and whose successor has a different disposition toward you. | M |
| **RV-LW-I03** | **Migration and refugee waves** | LW7 fallout exists; make it spatial — refugees move along routes toward safety, and you are on one of those routes. | M |
| **RV-LW-I04** | **World map weather and seasons** | Regional drought or a hard winter that shifts prosperity and drives the events above. Pairs with Stormproof fail-open. | L |
| **RV-LW-I05** | **Reputation propagation** | What you do to one faction is heard about by its allies, at a delay and with distortion. The natural partner to AZR-85. | M |
| **RV-LW-I06** | **Settlement sieges you can join** | An NPC war produces a real map you can caravan to and take a side on. | L |
| **RV-LW-I07** | **World news console** | An in-colony radio/comms building that surfaces chronicle entries as they arrive rather than as letters. Gives AZR-84 a diegetic home. | M |
| **RV-LW-I08** | **Historical map layers** | Toggle the world map to show ownership as of N years ago. Cheap once the chronicle exists, and a strong screenshot. | M |
| **RV-LW-I09** | **Faction quests from the chronicle** | A faction that just lost a settlement asks you for help retaking it. Turns passive world news into player-facing content. | M |
| **RV-LW-I10** | **Player notoriety** | The world tracks what you are *known for* — slaver, healer, warlord — independently of per-faction goodwill, and NPC behavior keys off it. Feeds Deep Colony fail-open. | M |

---

## Azrael — `Azrael/`  ·  v1.0.0  ·  series hub, **not release-ready**

Azrael is the series showcase package: storyteller, scenarios, and the cross-mod hub. Same caveat as Living World — several rows are readiness gates.

### Fixes

| ID | Title | What & why | Sev | Est |
|---|---|---|---|---|
| **RV-AZ-F01** | PackageId casing is inconsistent across the series ✅ | Five mods use `AzraelGodKing.*` (Homesteader, Strata, Stormproof, Nemesis, Niceties) and four use `azraelgodking.*` (Azrael, DeepColony, livingworld, DateNight) — and `livingworld` is also all-lowercase where the rest are PascalCase. Nemesis already works around this by probing Deep Colony under **both** casings (`SoftCompat.cs:226-227`). Pick one convention, update every `About.xml`, `loadAfter`, `MayRequire`, and C# literal, and add a ModChecks rule. This is the root cause of **RV-NM-F06**. | **P2** | M |
| **RV-AZ-F02** | Series packageIds are hardcoded in three places in one file ✅ | `SeriesHub.cs` lists them in the `Series[][]` roster (lines 40-50), again in `Bridges()` (lines 72-77), and again in `Conflicts()`. Any casing or id change has to be made in all of them. Hoist to constants and have the roster reference them. | P3 | S |
| **RV-AZ-F03** | Cross-mod bridges are detected by reflected type-name strings ✅ | `TypePresent("LivingWorld.LivingWorldSignals")` and `TypePresent("DeepColony.LivingWorldSoftCompat")` scan loaded assemblies for a name. A rename in either mod silently turns the bridge off with no error anywhere. Pair with **RV-LW-F04**: freeze these as a documented contract and assert them in ModChecks. | **P2** | M |
| **RV-AZ-F04** | Bridge liveness is a boolean with no diagnosis ✅ | `Bridge(...)` reports live/not-live. When a bridge is dark the player cannot tell whether a mod is missing, a def is missing, or a type moved. Extend each row with the specific failing precondition. This overlaps open ticket **AZR-136** (hub health panel) — file it as a sub-task of that rather than a duplicate. | P3 | M |
| **RV-AZ-F05** | The hub roster is hand-maintained ✅ | Adding a tenth mod to the series means editing `SeriesHub.Series` by hand and remembering the three other spots in F02. The ModChecks suite already validates the Series hub roster per the README — extend that check to assert the roster matches the set of `*/About/About.xml` in the repo. | P3 | S |
| **RV-AZ-F06** | Storyteller def is injected only when Homesteader is absent ✅ | Per `ROADMAP.md`, Homesteader is canonical for the Azrael storyteller and the standalone package injects the same def only as a fallback. That is a genuinely fragile arrangement — two sources of one def, keyed on load order. Move the def to a single owner (Azrael) and have Homesteader depend on it softly. | **P2** | M |
| **RV-AZ-F07** | No conflict detection for known-incompatible mods ✅ | `Conflicts()` exists but the list is small and static. The docs site already has a `/compat` route with compatible/incompatible views — the in-game hub and the site should read from one shared data file rather than drifting. | P3 | M |
| **RV-AZ-F08** | Series version skew is not surfaced ✅ | The hub shows each mod's version but does nothing with the combination. Ship a compatibility matrix so a player running Strata 3.3.2 with a Deep Colony from six months ago gets told, not left to find out. | P3 | M |
| **RV-AZ-F09** | No release checklist for the package ✅ | Azrael is not in the release zips. Same gate list as **RV-LW-F07**: About, preview, changelog, hub page, workflow entry — plus a decision on whether the showcase scenario ships enabled by default. | P3 | M |
| **RV-AZ-F10** | Scenario defs have no automated validation ✅ | `Azrael/Defs/ScenarioDefs` and `ScenPartDefs` reference defs from other mods via `MayRequire`. A renamed building in Homesteader or Strata breaks the showcase scenario silently at scenario-select time. Add a ModChecks rule that resolves every def referenced by a scenario part against the owning mod's XML. | **P2** | M |

### Ideas

Excludes open tickets **AZR-136** (hub health panel) and **AZR-137** (safe removal wizard).

| ID | Title | What | Est |
|---|---|---|---|
| **RV-AZ-I01** | **Series-wide settings profile** | One Soft / Default / Hard control that sets the matching preset in every loaded series mod at once. Each mod already ships presets; this is the missing front door. | M |
| **RV-AZ-I02** | **Guided first-run tour** | On first load with two or more series mods, a short in-game walkthrough of what each one added and where to find it. | M |
| **RV-AZ-I03** | **More showcase scenarios** | The roadmap ships six; add scenarios that require *three* mods together, to show the bridges rather than the parts. | M |
| **RV-AZ-I04** | **Series achievement log** | Track milestones across mods — first deep level, first nemesis resolved, first hundred-year lineage — as a shared trophy page. | M |
| **RV-AZ-I05** | **Cross-mod event weaving** | The Azrael storyteller deliberately schedules series events to land together: a storm during a nemesis assault, an infestation while the pantry is empty. This is the whole reason a series storyteller exists. | L |
| **RV-AZ-I06** | **Bridge toggle panel** | Let the player turn individual cross-mod bridges off without disabling either mod. Directly useful for bug triage. | M |
| **RV-AZ-I07** | **In-game series changelog** | Show what changed in each loaded series mod since the player last played, in one panel — replacing the per-mod `UpdateNews` letters that every mod currently ships separately. | M |
| **RV-AZ-I08** | **Save health report** | Scan a loaded save for series-mod orphans: buildings whose mod is gone, hediffs with no def, dangling references. The diagnostic sibling of AZR-137. | M |
| **RV-AZ-I09** | **Recommended load order check** | Verify the actual load order against the series' declared `loadAfter` graph and offer a one-click fix. | M |
| **RV-AZ-I10** | **Shared debug console** | One dev window with every series mod's debug actions, grouped by mod. Nine mods currently ship nine separate debug surfaces. | M |

---

## Appendix — cross-cutting themes

Several findings repeat across mods. If you would rather approve **themes** than individual rows, these are the four that matter most:

1. **Static state keyed on per-`Game` IDs, without a session reset.** `RV-ST-F01` (Strata, the worst case), `RV-HS-F02`, `RV-DN-F02`. Strata's `[StrataSessionReset]` attribute is the right pattern — promote it into `Common/` and adopt it everywhere.
2. **Hot vanilla methods patched with the expensive check first.** `RV-SP-F03`, `RV-HS-F01`, `RV-NC-F01`, `RV-NC-F02`, `RV-DC-F03/F04/F05`, `RV-NM-F01`. A shared rule — cheapest, most-selective test first; cache map/pawn component lookups — would fix all of them.
3. **Cross-mod contracts held together by strings.** `RV-AZ-F01/F03`, `RV-LW-F04`, `RV-NM-F06`, `RV-AZ-F10`, `RV-ST-F08`. PackageIds, reflected type names, and scenario def references all break silently. One ModChecks expansion covers the lot.
4. **Test coverage gaps that let the above through.** `RV-NC-F06` is the big one — `Tests/ModChecks` skips every `nameof(...)` Harmony target, which is most of the patches in this repo.

**Suggested first slice** if you want a single high-value sprint: `RV-ST-F01`, `RV-SP-F04`, `RV-SP-F02`, `RV-SP-F01`, `RV-DC-F01`, `RV-NC-F06`, `RV-AZ-F01`. That is one save-corruption bug, one settings-corruption bug, two flagship features not working as documented, and the two coverage gaps that would have caught them.
