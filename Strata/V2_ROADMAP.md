# Strata V2 Roadmap

Not committed scope — a backlog for planning. Updated after the 1.1 release
with community suggestions (Thundercraft, dognamedKats, Hat).

## Pillar 0 — V1 carry-over (polish & hardening)

Smaller items inherited from the V1 backlog; good between-pillar work.

- **Elevator haul priority** — prefer powered elevator over stairs for heavy hauls.
- **Multi-shaft routing** — relay picks shortest portal path, not first BFS link.
- **Haul + seal race fix** — re-check portal seal mid `JobDriver_HaulToLevel`.
- **Burrower telegraph** — tremor warning before a deep raid erupts.
- **Sealed-shaft siege** — raiders attempt to unseal or find alternate entry.
- **Depth-scaled threat table** — richer ore ↔ more bugs tradeoff pass.
- **Dedicated exhaust-fan research** — Strata gate instead of bare Electricity.
- **Placement helpers** — fan intake arrow, duct outdoor-exit hint.
- **Unit tests** — pure logic: `LevelGraph`, `StrataDepth`, smoke math, relays.
- **In-repo README** — full install/compat doc beyond `About.xml`.
- **Strata-specific alerts** — smoke on empty level, colonists below sealed shaft.
- **Cross-level rituals: prisoners & animals** — currently colonists only.
- **Raid coordinator** — surface lord's retreat/loot decision broadcast to
  sub-level pursuit groups (one raid, one story). Build if playtests show
  split-decision weirdness.
- **Dedicated art** — smoke hole and updraft filter reuse other textures.

## Pillar 1 — The Living Deep (geothermal + gas)

The deep becomes a place with resources and an atmosphere, not just rock.

- **Hidden geothermal chambers**: steam geysers seeded per level in small
  fogged chambers sealed in rock — discovered by mining, like ore. Chance and
  count scale with depth. Vanilla geothermal generator just works; its heat
  feeds the existing stairwell temperature exchange.
- **Atmosphere generalization**: refactor the smoke sim into a multi-gas
  room-density sim (smoke = one channel). Per-gas flags: harms pawns,
  flammable, extractable. All existing ventilation tools (vents, louvers,
  smoke holes, ducts, seals, the ventilation guarantee) apply to every gas
  for free. Step must ship with zero behavior change for smoke.
- **Persistent gas pockets**: hidden fogged chambers of toxic/flammable gas
  found by mining. Flammable rooms above an ignition density explode from
  open flames — torches become dangerous mining equipment; electric light
  becomes a safety upgrade. Venting keeps pawns safe but wastes the resource.
- **Gas extraction economy**: gas well on a deep vent + deep-gas generator
  (self-contained, no dependencies). Guarded patch: with Vanilla Helixien Gas
  Expanded loaded, deep vents can feed its pipe network instead.
- **Recast the gas pocket incident** as a breach of the persistent system.

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

## Pillar 3 — Rival system (nemesis-like, discussed)

- Escaped raiders tracked in a world component; return in later raids with
  their scars, a grudge, a name in the letter, and knowledge of the colony
  (e.g. arrive already heading for the stairwell they escaped through).
- Kill closes the storyline; repeat escapes escalate.
- Deliberately NOT the patented ensemble: single rivals with memory, no
  procedural promotion hierarchy, no faction-politics screen.

## Pillar 4 — Building Up (above-ground floors)

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
