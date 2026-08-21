# Deep Colony — Batch D (post–Batch C)

**Status:** shipped (`batch-d-v1`). Do not reuse D01–D25.  
**Mod:** [Deep Colony](../../Deep%20Colony/). Prior pools: [deep-colony-updates.md](deep-colony-updates.md) (A01–A20, B01–B22), [deep-colony-batch-c.md](deep-colony-batch-c.md) (C01–C24). **Do not reuse those IDs.**  
**Lane rule:** colonist / colony-facing only. No furniture packs, raid timers, off-map politics, farm brand, or a second goodwill buffer.

**Pitch reminder:** deepen identity, memory, teaching, bloodlines, and earned faction goodwill.

---

## Why Batch D

Batch C shipped Phases 6–9. Players asked for **as much as possible** from leftovers, QoL, and the next identity layer:

- C11 first harvest / marriage notes were still missing.
- C15 tribute was Reputation-tab silver only (spec asked for gift + float menu).
- C18 Ideology funerals were still missing.
- Inspect was thin (no trauma types / teach / envoy / rival / Date Night confidant count).
- Rivalry scan, deathbed, funeral spam, and untreated-trauma grace had real bugs.

---

## Fixes (shipped with this stamp)

| ID | Fix |
|----|-----|
| D-fix rivalry | Rivalry pair scan is interval-gated (2500 ticks) and reuses a scratch list |
| D-fix deathbed | Last lesson only for player-side mentor and apprentice |
| D-fix funeral | Message only if someone actually had loss eased; spouses get extra ease |
| D-fix alert | Counseling restarts the 1-day untreated-trauma grace if trauma remains |
| D-fix kin | Birthday / family checks use implied sibling workers + caravan colonists |
| D-fix confidant | Inspect bond progress uses the last counselor (Date Night lovers = 2) |

---

## Batch C leftovers (completed here)

| ID | Idea |
|----|------|
| C11 | First harvest as a family + marriage letters on the Legacy tab |
| C15 | Right-click silver / gold / jade / a valuable gift as tribute |
| C18 | Ideology funeral ritual eases violent loss (fail-open `Lord.Cleanup`) |

---

## Batch D ideas

| ID | Idea | System | Notes |
|----|------|--------|-------|
| D01 | Family meal thought when eating in the same room as kin | Generations | 2-day cooldown |
| D02 | Parent reunion thought when an adult child returns to a parent’s map | Generations | Once per pawn |
| D03 | Coming-of-age letter with the childhood thought | Generations | Biotech adults; skip vats |
| D04 | Classroom extra XP: 2+ apprentices + Biotech blackboard | Mentoring | Stacks on the existing 1.15 |
| D05 | Assigned envoy present at a successful trade → extra ledger drift | Reputation | Still `AddFactionDrift` |
| D06 | Returning to a surface map eases Odyssey isolation | Trauma | DLC-gated def |
| D07 | Quiet indoor room (no work benches) therapy bonus | Trauma | +10% |
| D08 | Spouse / lover extra on funeral + remembrance day | Trauma | Extra ease + short thought |
| D09 | Legacy tab: traumatized count + remembrance names | UI | |
| D10 | Reputation tab ally / hostile filter | UI | |
| D11 | Perks tab archetype column | UI | |
| D12 | Right-click counsel prisoner | Trauma | Settings-gated |
| D13 | Strata gas / firestorm keyed reasons on existing toxic/fire trauma | Trauma | Fail-open |
| D14 | Inspect: trauma types, teach progress, envoy, rival | QoL | |
| D15 | Marriage letter | Generations | Colonist–colonist spouse |

Dropped on purpose: extra perk tiers, caravan-forming envoy visits, farmstand, named hunters, world chronicle UI, school furniture.

---

## Non-goals (still)

- Off-map news, settlement flip, NPC wars → Living World
- Named personal antagonists → Nemesis
- Farmstand / harvest festival / aging cheese → Homesteader
- New architect furniture, raid incident packs, a second goodwill store
