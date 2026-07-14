# Strata V2 Roadmap

Not committed scope — a backlog for planning. Updated after the 1.1 release
with community suggestions (Thundercraft, dognamedKats, Hat).

## Pillar 0 — V1 carry-over (polish & hardening)

SHIPPED post-1.1 (routing, seal race, siege, telegraph, depth scaling,
ventilation research, placement guides, alerts, self-tests, README, raid
coordinator, cross-level ritual escorts for prisoners and animals).

## Backlog (not Pillar 0)

- **Dedicated art** — smoke hole and updraft filter reuse other textures until
  custom assets exist.

## Pillar 1 — The Living Deep (geothermal + gas)

**Shipped on `feature/pillar1-fluid-adapters`:** fluid shaft adapters (DBH, DCH,
Rimatomics, VHGE); DBH groundwater on new levels; hidden geothermal + gas
pockets; atmosphere channels (toxic + natural gas); deep gas well/generator
with VHGE pipe hookup; gas pocket incident recast as pocket breach.

**Fluid shaft playtest status:**

| Adapter | Status |
|---------|--------|
| DBH plumbing (water/sewage) | Playtested — confirmed working cross-level |
| DCH heating + air-con | Playtested — confirmed working cross-level |
| VHGE helixien gas | Playtested — confirmed working cross-level |
| Rimatomics coolant | Code-complete — community playtest pending |

Rimatomics uses the same tap-into-net pattern as the other Dub adapters, with
a junction buffer + post-tick loop-ratio hook (Rimatomics recomputes net
fields every tick). Needs someone with a reactor/cooling-tower setup to
validate in a real colony.

**Pillar 1 done criteria (not yet complete):**

- Cross-level **water + gas** in a real colony loadout — **water and helixien
  gas done**; **oil** (Rimefeller adapter) still open.
- Smoke sim evolved into a full **multi-gas atmosphere**: CO₂, O₂, and other
  channels with per-gas density/behavior — not just cosmetic overlays.
- **Vertical stratification:** CO₂ sinks, O₂ rises; deep levels need active
  oxygen supply (pumped down shafts / vented from above) or colonists suffocate
  while lighter/toxic gases pool where physics says they should.
- Existing ventilation buildings (fans, louvers, ducts, shaft rise) apply to
  every gas channel without one-off special cases.

**Next implementation slice:**

- **Atmosphere v2** — O₂ / CO₂ room fractions per level, cross-level diffusion
  through open shafts, `CompOxygenPump` or shaft-side gas junctions for
  life-support plumbing.
- **Rimefeller oil** shaft adapter (last major fluid gap for Pillar 1 “done”).
- Hazard tuning (after atmosphere v2): higher ignition risk for natural gas
  pockets, torch/mining danger, slower well payoff so breaching feels scary
  before it feels profitable.

**Backlog:** VEF chemfuel umbrella adapter; richer pocket art; dedicated fluid
junction art; retroactive pocket seeding (explicitly out of scope for now).

## Pillar 2 — Fluid shafts (pipe mod compatibility)

Core pillar work **shipped with Pillar 1** on `feature/pillar1-fluid-adapters`.
The power-shaft tie template is proven: paired junction nodes, each a member of
its own per-map net, demand-driven metered transfer every 60 ticks — no net
merging across maps, O(1) per shaft per interval.

**Shipped:**

- Shaft fluid junction building pair (upper builds beside shaft; lower auto-spawns
  on the landing below — same pattern as shaft conduit).
- Soft-dependency adapters (reflection, no hard refs): DBH, DCH (heat + air),
  Rimatomics coolant, VHGE helixien gas.
- DBH groundwater seeding on new Strata levels.
- **Fluid shafts** research (1,200 pts, Industrial, after Shaft power).

**Still planned:**

- **Rimefeller** — oil/chemfuel network.
- **VEF PipeSystem umbrella** — nutrient paste (VNPE), chemfuel pipes, other
  VE resource networks beyond VHGE (which already has a dedicated adapter).

## Pillar 3 — Building Up (above-ground floors)

Sky is the new rock: an up-level is a full-size pocket map that is open sky
everywhere — impassable, unbuildable void — except above supported structure.
The buildable region grows as the base below grows (maps never resize; the
usable area does, exactly like mining expands a down-level).

- **1:1 spatial mapping**: generate the upper map at exactly the surface
  map's size so cell (x,z) upstairs is directly above cell (x,z) downstairs.
  Support checks, stair alignment, and collapse all become trivial cross-map
  lookups. (Down-levels keep proportional mapping; rock forgives imprecision,
  floors don't.)
- **Support projection**: "upper floor" terrain placeable only where the map
  below has a wall / constructed roof under that cell (PlaceWorker with a
  cross-map read).
- **Ceiling/floor bookkeeping**: laying floor on level N sets a "floor above"
  roof on level N-1's cell (blocks sun/weather below), mirroring how
  down-levels sit under thick rock roof. Cross-map writes on build/destroy.
- **Collapse**: destroying the supporting wall below drops the floor above —
  cave-in machinery is the template.
- **Real sky**: mirror surface weather onto up-levels, sunlight works (solar,
  greenhouses, sun lamps unnecessary), and drop-pod raids can hit rooftops —
  the inverse of the underground incident suppression, not a reuse of it.
- **Stairs up** are the existing portal tech pointed the other way.
- Biggest pillar (~5-7 sessions) and the biggest payoff: up AND down makes
  Strata the complete vertical-base mod.

## Done in 1.1 (removed from backlog)

- Shaft conduit inspect UI; deep raid lord tuning (obsolete — deep raids are
  insect eruptions now); cargo lift (superseded by cross-level storage
  priority + construction demand pull); cross-level gas containment via
  sealed portals (shipped with the smoke system).
