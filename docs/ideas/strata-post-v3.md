# Strata — post-V3 ideas

**Status:** ideas only. Cap, G1–G9, M1–M10, and Polish A1–A5 are done (`V3 Complete`).  
**Mod:** [Strata](../../Strata/). V3 checklist: [docs/strata-roadmap.html](../strata-roadmap.html). Short vision: [Strata/V3_ROADMAP.md](../../Strata/V3_ROADMAP.md).  
**Lane rule:** the column — floors, shafts, depth hazards, vertical logistics. Weather disasters → Stormproof (pair, don’t absorb). Farm wells → Homesteader flavor only. Named hunters → Nemesis (stair awareness API). Fog-of-war underground stays **parked** ([weekend-steam-backlog.md](weekend-steam-backlog.md)).

**Sources:** V3_ROADMAP “After V3”; overlap with shipped flood/sump, ore hoist, cargo lift, tremors, canaries, Levels tab, ancient/quest sites.

---

## Pitch reminder

Updates should make **going deeper** weirder and **moving stuff** cheaper, without a second multi-floor stack or a weather mod. Prefer S/M toys players can build this week over magma-as-a-new-game.

---

## What’s already in (don’t rebuild from scratch)

| Post-V3 idea | Shipped overlap |
|--------------|-----------------|
| Magma | Depth heat, lava arrival avoidance, geothermal **world site** + steam chambers |
| Flooded level | Flood seep, `FloodMapComponent`, sump pump, warren Flooded quest theme |
| Seismograph | Tremor + cave-in incidents; canary / bird cages; **no** predictive building |
| Noise | Depth-scaled infestation weight; **no** player noise economy |
| Dumbwaiter | Ore hoist + cargo lift (heavier logistics) |
| Collapse trap | Cave-in / tremor / shoring (defensive, not a player trap) |
| Stack panel | Levels main tab (list, jump, rename) — not a side-view |
| Lost floor | Ancient colony stairwell, sunken ruins, sealed vault / collapsed mine sites |

---

## Phases (summary)

| Phase | Theme | IDs | Count |
|-------|-------|-----|-------|
| 0 | Workshop UX | Q01 | 1 |
| 1 | Logistics & combat toys | A01–A03 | 3 |
| 2 | Sensing | A04–A06 | 3 |
| 3 | Level types | A07–A09 | 3 |
| 4 | Story / flagship UX | A10–A11 | 2 |
| 5 | Soft-compat | S01–S03 | 3 |

---

## Phase 0 — Workshop UX

| ID | Idea | Size | Notes |
|----|------|------|-------|
| ST-Q01 | **Levels-tab badges** — gas / flood / bugs / cave-in icons + jump-to-threat | S | Flagship screenshot without a full stack panel |

Parked, not in any phase: **fog-of-war underground**.

---

## Phase 1 — Logistics and combat toys

| ID | Idea | Size | Notes |
|----|------|------|-------|
| ST-A01 | **Dumbwaiter** — cheap 1×1 item-only shaft, no pawns; early sibling of the ore hoist | S | Distinct def; tiny stack cap |
| ST-A02 | **Collapse trap** — player-rigged supported-roof drop on pursuers | S/M | Reuse cave-in / shoring rules; sealed stairwells still exist |
| ST-A03 | **Trapdoor seal** — one-tick shaft seal for traps / bug containment | S | Gizmo on stairwell / elevator; not a new pocket |

---

## Phase 2 — Sensing

| ID | Idea | Size | Notes |
|----|------|------|-------|
| ST-A04 | **Seismograph (MVP)** — building: calm / restless / imminent per linked level from existing tremor / cave-in / infestation clocks | M | Pair Stormproof forecaster later (S02) |
| ST-A05 | **Noise meter + dampening floor** — debug + one floor def; inspect “loud / quiet” | S | First slice of A06 |
| ST-A06 | **Noise attracts the dark** — mining/industry noise raises infestation weight; damp walls/floors counter | M | After A05; canaries already exist |

---

## Phase 3 — Level types (large)

| ID | Idea | Size | Notes |
|----|------|------|-------|
| ST-A07 | **Flooded level type** — lake floor on dig; Odyssey fishing optional; pump-failure drama | L | Extends flood/sump; Homesteader wells = flavor (S01) |
| ST-A08 | **Magma endpoint** — hard bottom: geothermal taps, obsidian, brutal heat, “we hit the bottom” | L | Extends depth heat + vent site; last |
| ST-A09 | **Pump failure cascade** — flooded cells + power outage letter / mood without a full lake map | M | Can ship before A07 |

---

## Phase 4 — Story and flagship UX

| ID | Idea | Size | Notes |
|----|------|------|-------|
| ST-A10 | **Lost floor** — rare sealed multi-room story pocket; diary teaches the noise rule | M/L | Cousin of ancient/quest sites, not a duplicate vault |
| ST-A11 | **Stack panel** — side-view column, alerts, click-to-jump | L | Workshop flagship; Q01 is the cheap substitute |

---

## Phase 5 — Soft-compat (fail-open)

| ID | Idea | Size | Notes |
|----|------|------|-------|
| ST-S01 | **Homesteader wells** — keyed inspect when a flooded level sits under a well / cistern | S | Homesteader owns the well; Strata emits “wet below” |
| ST-S02 | **Stormproof pair** — seismograph copy mentions the weather forecaster; surface antenna is **SP-S02** | S | Don’t put ion math in Strata |
| ST-S03 | **Nemesis stair API** — expose linked shafts so NM-S04 can chase without breaking pockets | S | Nemesis owns the hunt |

---

## Explicitly later / probably never

- Fog-of-war underground
- Competing with AASB / MultiFloors (still incompatible)
- Gravship hydroponic homestead (Homesteader parked the same idea)
- Replacing Biomes! Caverns layout when that mod is loaded

---

## Suggested build order

1. **Q01** Levels badges (Slice 1).
2. **A01** dumbwaiter, **A03** trapdoor, **A02** collapse trap.
3. **A05** then **A04** seismograph; **A06** noise economy after both.
4. **A09** pump cascade before a full flooded level.
5. **A07 / A08 / A10 / A11** only as a dedicated content drop.
