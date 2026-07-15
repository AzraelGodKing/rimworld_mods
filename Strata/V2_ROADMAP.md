# Strata V2 Roadmap

Planning backlog for Strata. Last updated after **Rimefeller adapters**, **sunken ruin
exploration** ([PR #20](https://github.com/AzraelGodKing/rimworld_mods/pull/20)), and
**Atmosphere v2** on the integration branch (`feature/atmosphere-o2-co2` /
`feature/pillar1-fluid-adapters`).

**Current focus:** **Pillar 3 — Building Up** (above-ground floors), with underground
exploration content continuing to land alongside the vertical stack.

---

## Pillar 0 — V1 carry-over (polish & hardening)

**Status: SHIPPED**

Routing, seal race, siege battering, raid telegraph, depth scaling, ventilation
research, placement guides, alerts, self-tests, README, raid coordinator,
cross-level ritual escorts (prisoners + animals), smarter shaft routing, level
size parity with parent map, vacant-level throttle.

### Backlog (non-blocking)

- **Dedicated art** — smoke hole, updraft filter, deep-gas buildings, and some
  fluid junctions still use placeholder/generated textures.

---

## Pillar 1 — The Living Deep (geothermal + gas + exploration)

**Status: FEATURE-COMPLETE on branch — playtest + merge sign-off pending**

### Shipped — core (Claude + Cursor integrated)

- Multi-gas **AtmosphereMapComponent** (`StrataGasDef` channels; smoke unchanged)
- Hidden **geothermal + gas chambers** (`GenStep_HiddenChambers`), level **fog**
- **Deep gas** hazard (pools, poisons, ignites near open flames)
- **Gas economy**: vent, well, canisters, 1400W smokeless generator
- **GasNetAdapter** VHGE soft bridge on wells
- Gas pocket incident recast as persistent-system breach
- Shaft fluid junctions: DBH, DCH, Rimatomics coolant, VHGE helixien, **Rimefeller**
- **DBH groundwater** genstep on new levels
- **Fluid shafts** research

### Shipped — Atmosphere v2 (Cursor)

- **O₂ / CO₂ simulation** — breathing, consumption, exhalation; toggle in mod
  settings
- **Gas stratification** — O₂ rises, CO₂ sinks through open stairwells
- **Life support** — oxygen pump, CO₂ pump, shaft gas exchanger (*deep life
  support* research)
- **Gas overlay** — play-settings toggle, room tint, cursor mix readout
- **Canary cage** — mine-canary-only gas alarm (assign / hay acquire); real
  pawn; cage food storage (default) or hunger sustain (mod setting)
- **Bird cage** — any tame bird for display; same feeding modes as canary cage

### Shipped — excavation progression (Cursor)

- **Digging down** research (surface → B1)
- **Deep excavation** now requires digging down (B2+)
- **Dig down** gizmo — dig-shaft blueprint, depth-scaled work, no power overlap
- Stairwell build work: 12k surface / 20k underground

### Shipped — underground exploration ([PR #20](https://github.com/AzraelGodKing/rimworld_mods/pull/20))

First multi-level **away-from-home** content: sunken ruin world sites with an
ancient stairhead on the surface map and a pre-carved insect warren below.

- **IncidentWorker_SunkenRuin** — world letter after *deep excavation* research
- **GenStep_SunkenRuinEntrance** — weathered ruin shell + `Strata_RuinStairsDown`
- **GenStep_CarveWarren** + **GenStep_WarrenInfestation** — organic chambers,
  threat-scaled insects, hoard in deepest chamber
- **Patch_PocketMapRemoval** — sites cannot despawn while pawns occupy child maps
- **Patch_AbandonWarning** — warns before abandoning with pawns on lower levels
- Gated by **Underground gas** mod option (with deep gas pockets)

This is the first slice of a broader underground exploration pillar. By the time
**Pillar 3** ships, the vertical stack should feel complete: colony basements and
upper floors at home, plus **caves, biomes, dangers, and loot** off-map and deep
below.

### Playtest status

| Feature | Status |
|---------|--------|
| DBH / DCH / VHGE shaft adapters | Playtested — cross-level confirmed |
| Rimefeller crude + chemfuel adapters | Code-complete — playtest pending |
| Rimatomics coolant adapter | Code-complete — community playtest pending |
| Atmosphere v2 (O₂/CO₂, life support, overlay) | Shipped — needs playtest pass |
| Chambers, deep gas economy, canary cages | Shipped — needs playtest pass |
| Dig-down progression | Shipped — needs playtest pass |
| Sunken ruin sites + abandon warning | Shipped — needs playtest pass |

### Done criteria (remaining before calling Pillar 1 “closed”)

- [ ] One full **playtest session** (see `MERGE_PLAN.md` checklist)
- [ ] **Merge to `main`** and tag release
- [ ] **Hazard tuning** — torch/mining danger, well payoff pacing (balance pass)
- [ ] **Dedicated art** for gas buildings and junctions (cosmetic)

### Backlog (Pillar 1.x — exploration & hazards)

- **Underground biomes** — themed warrens (fungal, flooded, frozen, volcanic)
- **More quest site types** — collapsed mines, sealed vaults, geothermal vents
- **Procedural cave networks** beyond fixed warren templates
- Retroactive pocket seeding on old saves (out of scope)
- Richer chamber / pocket art

---

## Pillar 2 — Fluid shafts (pipe mod compatibility)

**Status: CORE SHIPPED — VEF umbrella still open**

Cross-level fluid junctions for DBH, DCH, Rimatomics, VHGE, and **Rimefeller** are in.

### Shipped

- Shaft-side pipe comps (mirror shaft power conduit pattern)
- DBH groundwater on new Strata levels
- VHGE helixien net overflow + same-cell pipe linking
- Rimatomics coolant buffer across levels
- **Rimefeller** crude-oil and chemfuel junctions (`Strata_ShaftFluid_RimefellerCrude`,
  `Strata_ShaftFluid_RimefellerFuel`)

### Backlog

- **VEF PipeSystem umbrella** beyond VHGE (VNPE, chemfuel, etc.)
- Rimatomics junction — community validation
- Rimefeller junction — community validation
- Dedicated fluid junction art

**Does not block Pillar 3.**

---

## Pillar 3 — Building Up (above-ground floors)

**Status: NOT STARTED — active planning target**

Mirror the basement column upward: stacked maps above the surface, tied into
the existing shaft graph, power conduits, fluid junctions, and atmosphere rules.

When Pillar 3 lands, the full vertical fantasy is: **dig down at the colony,
build up above it, and venture outward into underground caves and ruins** with
Strata's gas, atmosphere, and multi-level safety systems everywhere.

### Open design questions

- Floor numbering and hotkeys (A1, A2… vs negative/positive depth index)
- How **roof / support** works — vanilla roof mechanics, new “build floor slab”
  work, or both?
- **Research gate** — separate *building up* project vs extension of digging down?
- **Stairwell / shaft** placement from surface upward (symmetric to dig down?)
- **Weather & raids** at height — exposed floors, wind, drop-in threats
- **Atmosphere** — open-to-sky rooms on upper floors; stratification direction
- **Map size & performance** — same 1:1 parent sizing as basements

### Proposed scope (first slice)

1. **Level graph** — register above-surface maps in `LevelGraph`, view hotkeys
2. **Build-up stairwell** — architect-placed up-stair + landing (research-gated)
3. **Build up gizmo** — extend shaft upward (symmetry with dig down)
4. **Power / conduit / fluid** — shaft ties work on upward links
5. **Atmosphere** — outdoor rooms on upper maps behave like surface

### Estimate

~5–7 focused sessions for a playable vertical slice (one extra floor + shaft
link); more for polish, art, and edge cases.

---

## Done in 1.1 (removed from backlog)

Shaft conduit inspect UI; deep raid lord tuning (obsolete); cargo lift
(superseded); cross-level gas containment via sealed portals.

---

## Release checklist (Pillars 0–2)

Before a public Pillar 1 release while Pillar 3 is in progress:

1. `dotnet build Strata/Source/Strata.csproj -c Release`
2. Fresh game: chambers, fog, gas economy, O₂/CO₂, canary cage
3. DBH / DCH / VHGE / **Rimefeller** junctions in a modded loadout
4. Sunken ruin incident → warren descent → hoard + abandon warning
5. Dev-mode self-tests pass
6. Update README (pipe-mod cross-level support, atmosphere v2, cages, ruins)
7. Merge integration branch → `main`

See **`MERGE_PLAN.md`** for the detailed integration history and checklist.
