# Strata V2 Roadmap

Planning backlog for Strata. Last updated for **Pillars 4–5** on
`feature/strata-v2-away-and-living` (art pass intentionally last).

**Current focus:** **Pillar 3 — Building Up** on `Feat/Final_V2_Feature_Going_UP`
(surface → A1 tower stairwell MVP). Pillars 4–5 remain code-complete / playtest.

---

## Pillar 0 — V1 carry-over (polish & hardening)

**Status: SHIPPED** (dedicated art still deferred — see Art Pass below)

Routing, seal race, siege battering, raid telegraph, depth scaling, ventilation
research, placement guides, alerts, self-tests, README, raid coordinator,
cross-level ritual escorts, smarter shaft routing, level size parity, vacant-level
throttle.

---

## Pillar 1 — The Living Deep (geothermal + gas + exploration)

**Status: FEATURE-COMPLETE on branch — playtest + merge sign-off pending**

Atmosphere v2, deep gas economy, chambers, dig-down progression, sunken ruins,
fluid adapters (DBH / DCH / Rimatomics / VHGE / Rimefeller). See CHANGELOG.

---

## Pillar 2 — Fluid shafts (pipe mod compatibility)

**Status: CORE SHIPPED — VEF umbrella expanding**

Cross-level junctions for DBH, DCH, Rimatomics, VHGE, Rimefeller. VEF PipeSystem
umbrella (VNPE / chemfuel / other nets) continues under Compat.

---

## Pillar 3 — Building Up (above-ground floors)

**Status: MVP IN PROGRESS** (`Feat/Final_V2_Feature_Going_UP`)

Shipped in this slice:
- [x] Research `Strata_BuildingUp`
- [x] Tower stairwell (`Strata_StairsBuildUp`) + upper landing
- [x] Tower elevator (`Strata_ElevatorBuildUp`) + landing (power gate mirrors dig elevator)
- [x] `Strata_UpperLevel` outdoor map gen (concrete pad, open sky)
- [x] Levels tab + hotkeys for Level +N
- [x] Join existing upper level from a second tower

Polish landed:
- [x] Dig portals never join upper pockets; no excavate-from-A1
- [x] Tower shaft gas direction (rise up / sink down)
- [x] Raid pursuit, sealed-off alert, caravan pull, abandon copy for A+
- [x] Upper pad outdoor weather; dig elevator PlaceWorker
- [x] Docs / About / research tab mention building up
- [x] A+ buildable only on roof-supported deck (+ shaft plaza); open sky elsewhere; live roof sync

Still open:
- [ ] A2+ stacking playtest / balance
- [ ] Upper-floor dedicated content (greenhouses, sky bedrooms fantasy)
- [ ] Art pass for tower stairwell / landing / elevator

---

## Pillar / V3 — Gravship Strata (Odyssey) — INVESTIGATION

**Status: DESIGN NOTES ONLY** (not in V2 ship scope; no code yet)

Goal: let a multistory Strata colony ride with a gravship — or at least not explode when the player launches.

### What Odyssey exposes (from 1.6 `Assembly-CSharp` strings)
- Core types: `Building_GravEngine`, `GravshipUtility`, `GravshipController`, `CompGravshipFacility` / `Thruster` / `ShieldGenerator`, `GravshipComponentTypeDef`, `GravShipCanLandOn`, `GravAnchor`, `GravFieldExtender`, `GravlitePanel`.
- Capture/launch pipeline names: `GravshipCapture`, `GravshipCapturer`, `GravshipCells`, launch/land audio + cutscene hooks.
- Implication: the ship is a **captured cell set** around the grav engine / substructure, not a pocket-map stack. Strata B/A levels are **separate pocket maps** linked by `MapPortal` — they will **not** auto-travel with a capture unless we teach the capture pass about them.

### Hard problems for Strata
1. **Pocket maps are not on the ship footprint** — launching the surface map abandons / orphans linked B1+/A1+ unless we migrate or destroy them deliberately.
2. **Size** — Strata levels match the full colony map; packing entire B1 into a gravship is almost never what Odyssey wants (ship is a sub-rect).
3. **Atmosphere / shafts / fluid junctions** — cross-map comps assume a stable `sourceMap` parent chain; launch rewires world parents.
4. **Landing** — after touchdown on a new tile, portals must rebind to the new surface map and re-align landings.

### Recommended V3 slices (in order)
1. **Safe launch policy (compat MVP)** — detect Gravship launch from a map that has Strata links; block launch with a clear message *or* auto-abandon empty linked levels and evacuate pawns to the ship map (player choice). Never silent orphan.
2. **Ship cellar (small)** — optional compact underground pocket (fixed small size, e.g. 50×50) opened from a gravship-floor hatch, destroyed or hibernated cleanly on launch/land. Not full B1.
3. **True stack migration (hard)** — serialize linked level maps with the ship, reparent on land; only if capture API allows attaching extra maps or we store them on `GravshipController` / custom WorldComponent.

### Non-goals for first Gravship patch
- Full A/B tower traveling at colony map size.
- Quest-site pocket maps hitching a ride.
- Gravship-only research tree until MVP #1 works in playtests with Odyssey loaded (`MayRequire` / LoadFolders).

---

## Pillar 4 — Away Into the Dark (exploration)

**Status: CODE-COMPLETE — playtest pending**

Away-from-home and mid-game discovery content so depth is not only basements.

### Scope (shipped)

- More quest site types: collapsed mine, sealed vault, geothermal vent
- Underground biome themes on site warrens (fungal, flooded, frozen, volcanic)
- Home-colony **cave breakthrough** incident (natural cave off a rock face)
- Prospector tip → short excavation dig site

### Done criteria

- [x] Each site type fires, generates surface entrance + pocket level, rewards loot
- [x] Themed warren variants (fungal / flooded / frozen / volcanic)
- [x] Cave breakthrough on colony underground levels
- [ ] Playtest pass

---

## Pillar 5 — Living Below (deep colony life)

**Status: CODE-COMPLETE — playtest pending**

Engineering and economy so living underground is a design puzzle, not only dig +
vent.

### Scope (shipped)

- Structural **shoring pillars** (reduce cave-in risk)
- **Gas airlocks** (room-to-room seal without sealing the whole shaft)
- **Ore skip hoist** (bulk vertical logistics)
- **Fungus farms** / underground agriculture interacting with O₂/CO₂
- **Flooding / sump pumps** (water-table pressure on deep levels)
- **Mine lamps** (safe light near deep gas)
- **Passive life support** (bellows, lime scrubbers) before powered pumps
- Cross-level **medical** and **joy** relays
- **Caravan packing** from the whole column
- **Level roles** (freezer / barracks / workshop bias)

### Done criteria

- [x] Buildings research-gated and placeable
- [x] Relays + roles toggleable in mod settings
- [ ] Playtest pass

---

## Threats & story (supports 4–5)

**Status: SHIPPED (code)** — gas firestorm, tremor escalation, deep siege, lost miners,
prospector dig, cave breakthrough.

---

## Compat & tools

**Status: SHIPPED (code)** — VEF chemfuel junction + PipeNet discovery, sister-mod
bridges, Royalty deep-bedroom thought, hibernation / Levels-tab perf readout,
exploration + flood settings.

---

## Art Pass (ALWAYS LAST)

**Status: SHIPPED** — dedicated sprites for Living Below buildings, quest
stairheads, O₂/CO₂ pumps, smoke hole, updraft filter, gas exchanger, fluid
junctions, and lime. Deep-gas vent/well/generator already had prior art.

---

## Release checklist

1. `dotnet build Strata/Source/Strata.csproj -c Release`
2. Fresh game: sites, cave breakthrough, shoring, airlock, hoist, farms, flood, relays
3. Pipe-mod loadout still works; new VEF junctions appear when mods present
4. Dev-mode self-tests pass
5. Art pass complete
6. Update README + CHANGELOG; merge → `Feat/Strata_V2` / `main` per release plan
