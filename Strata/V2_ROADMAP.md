# Strata V2 Roadmap

Not committed scope — a backlog for planning. Updated after integrating
**Claude Fable 5** Pillar 1 ([PR #21](https://github.com/AzraelGodKing/rimworld_mods/pull/21))
with **Cursor** fluid shafts on `feature/pillar1-integrated`.

## Pillar 0 — V1 carry-over (polish & hardening)

SHIPPED post-1.1 (routing, seal race, siege, telegraph, depth scaling,
ventilation research, placement guides, alerts, self-tests, README, raid
coordinator, cross-level ritual escorts for prisoners and animals).

## Backlog (not Pillar 0)

- **Dedicated art** — smoke hole, updraft filter, and deep-gas buildings use
  placeholder/generated textures until custom assets exist.

## Pillar 1 — The Living Deep (geothermal + gas)

**Shipped on `feature/pillar1-fluid-adapters` (Claude + Cursor integrated):**

- Multi-gas **AtmosphereMapComponent** (`StrataGasDef` channels; smoke unchanged)
- Hidden **geothermal + gas chambers** (`GenStep_HiddenChambers`), level **fog**
- **Deep gas** hazard (pools, poisons, ignites near open flames)
- **Gas economy**: vent, well, canisters, 1400W smokeless generator
- **GasNetAdapter** VHGE soft bridge on wells
- Gas pocket incident recast as persistent-system breach

**Shipped (Cursor — same branch):**

- Shaft fluid junctions: DBH, DCH heat/air, Rimatomics coolant, VHGE helixien
- **DBH groundwater** genstep on new levels
- **Fluid shafts** research

**Playtest status:**

| Feature | Status |
|---------|--------|
| DBH / DCH / VHGE shaft adapters | Playtested — confirmed cross-level |
| Rimatomics coolant adapter | Code-complete — community playtest pending |
| Claude Pillar 1 (atmosphere, chambers, gas economy) | Merged — needs playtest |

**Pillar 1 done criteria (not yet complete):**

- Cross-level **water + gas** in a real loadout — shaft adapters done; **oil**
  (Rimefeller) still open
- **Atmosphere v2**: O₂ / CO₂ stratification, cross-level diffusion, life-support
  plumbing
- Hazard tuning after v2 (torch/mining danger, slower well payoff)

**Backlog:** Rimefeller oil adapter; VEF chemfuel umbrella; richer pocket art;
dedicated fluid junction art; retroactive pocket seeding (out of scope).

## Pillar 2 — Fluid shafts (pipe mod compatibility)

Core work **shipped on `feature/pillar1-fluid-adapters`**. Still planned:

- **Rimefeller** oil/chemfuel
- **VEF PipeSystem umbrella** beyond VHGE (VNPE, chemfuel, etc.)

## Pillar 3 — Building Up (above-ground floors)

Unchanged — see prior roadmap. Biggest future pillar (~5–7 sessions).

## Done in 1.1 (removed from backlog)

Shaft conduit inspect UI; deep raid lord tuning (obsolete); cargo lift
(superseded); cross-level gas containment via sealed portals.
