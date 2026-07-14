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

SHIPPED (see CHANGELOG "Pillar 1: The Living Deep"): multi-gas atmosphere
sim (smoke = one channel, unchanged tuning), hidden geothermal chambers with
working vanilla geothermal, persistent pressurized gas pockets with ignition
explosions, deep vents + gas well + canisters + smokeless deep-gas generator,
reflection-based Helixien pipe feed, and the gas pocket incident recast as a
breach of the persistent system. Levels now generate fogged so chambers are
discovered by mining.

Remaining polish (backlog): dedicated art for the vent/well/generator/canister
(current art is generated placeholder in the mod's flat style).

## Pillar 2 — Fluid shafts (pipe mod compatibility)

Cross-level pipes for water, oil, gas, paste, chemfuel — WITHOUT merging any
mod's pipe network across maps. The power shaft tie is the proven template:
paired nodes, each an ordinary member of its own per-map net, with a
demand-driven metered transfer every interval. O(1) per shaft per interval,
nothing to desync, no patching of net internals (the source of MF-style jank).

- **Shaft pipe building pair**: build the upper node by a shaft; it
  self-extends its lower junction (same pattern as the shaft conduit).
- **Adapters as soft dependencies** (reflection-based, no hard refs), one per
  pipe framework:
  - **VEF PipeSystem** — one adapter covers the whole VE family: nutrient
    paste (VNPE), Helixien gas, chemfuel pipes, resource tank addons.
  - **Dubs Bad Hygiene** — water/sewage; also seed DBH groundwater on Strata
    levels (aquifers: deeper = richer) so underground wells work at all.
  - **Rimefeller** — oil/chemfuel network.
- Transfer model: read both nets' stored amount / demand through the
  framework's own API, move min(cap, need, available). Same brownout-proof
  demand-driven shape as `CompPowerShaft.DriveTie`.

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
