# Deep Colony — Batch D (post–Batch C)

**Status:** shipped (`batch-d-v1`, family tree `family-tree-v1`, family join `family-join-v1`, family loyalty `family-loyalty-v1`, family beats `family-beats-v1`, family life `family-life-v1`). Do not reuse D01–D24 or E01–E05.  
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
| D16 | Family tree on the Character inspect tab — click a relative to jump to that pawn | Generations | Inspect tab + Bio-card button; dead included |

Dropped on purpose: extra perk tiers, caravan-forming envoy visits, farmstand, named hunters, world chronicle UI, school furniture.

---

## D17 / D18 (shipped `family-join-v1`)

Colony identity: family on the map can switch sides; ex-lovers do not defect, they reconcile.

| ID | Idea | System | Notes |
|----|------|--------|-------|
| D17 | Kin join / defect on the player home map | Generations + reputation | Spouse 1.00, fiancé 0.80, parent/child 0.70, lover 0.55, sibling 0.50, grandparent 0.30. **Ex-lover / ex-spouse = 0%** for this path. Hostile raid kin may defect (vanilla goodwill hit + `FactionRepReason.FamilyDefect`). Neutral/allied kin may join with **no** goodwill penalty. Roll once per pawn. Skip leaders, lodgers, prisoners, babies. |
| D18 | Ex-lovers get back together; 4th time is toxic | Trauma | Separate from D17. Same-map ex-lover / ex-spouse may become `Lover` again (MTB ~8 days). After more than 3 reunions, both get `DC_Trauma_ToxicRelationship` (counseling / therapy eases it). Date Night still owns romance schedules. |

Settings (default on): family join, raid defect chance, visit join chance, ex-lover reconcile, reconcile MTB. Soft raises join chances / slows reconcile; Hard lowers join chances / hastens reconcile drama.

| ID | Idea | System | Notes |
|----|------|--------|-------|
| D19 | Unwavering prisoners can be broken by family only | Generations | Kin (`KinWeight` > 0, so not ex-lovers) who like each other (mutual opinion, default 20 both ways) can talk the prisoner out of `guest.Recruitable == false`. Chance scales with relation (spouse highest). Then vanilla recruit is available. Non-family still cannot. |

---

## D20–D24 (shipped `family-beats-v1`)

More colony-facing family beats. Same `enableFamilyJoin` toggle. Date Night still owns romance schedules.

| ID | Idea | System | Notes |
|----|------|--------|-------|
| D20 | In-law welcome | Generations | On colonist–colonist marriage, living parent/sibling colonists on the map get `DC_Thought_InLawWelcome`. Dedupe with `thingIDNumber` order (marriage `AddDirectRelation` fires twice). |
| D21 | Kin homecoming | Generations | `DeSpawn` on a player home map stamps `leftColonyMapTick`. `SpawnSetup` (not after load): if away ≥ 8h and cooldown 10 days, returning colonist + best kin (`KinWeight`) get `DC_Thought_KinHomecoming`. Distinct from D02 parent reunion (once, parent-only; first-time recruit spawn does not fire). |
| D22 | Kin died other side | Trauma-adjacent | `Pawn.Kill`: victim is not a player colonist; colonist kin **or ex** on the map get `DC_Thought_KinDiedOtherSide` + letter. Skips if an execution is in progress. Does not replace opinion≥40 violent-loss trauma. |
| D23 | Breakup wound | Trauma-adjacent | `AddDirectRelation` ExLover/ExSpouse: both get `DC_Thought_BreakupWound` if at least one is a colonist. Skip first 600 ticks (world gen). If toxic-relationship trauma is already present, renew it. Reconcile removes Ex then adds Lover, so it does not fire breakup. |
| D24 | Execute family | Trauma | Harmony `ExecutionUtility.DoExecutionByCut(Pawn executioner, Pawn victim, …)`: Prefix/Postfix. Colonist kin/ex get `DC_Thought_KinExecuted` + `DC_Trauma_Betrayal`. Letter. ThreadStatic so the Kill path does not also fire D22. |

---

## E01–E05 (shipped `family-life-v1`)

Next family life-cycle beats. Same `enableFamilyJoin` toggle. Date Night still owns romance schedules. Spouse/lover is not blood for last-of-the-line.

| ID | Idea | System | Notes |
|----|------|--------|-------|
| E01 | Grandchild born | Generations | Colony birth: living grandparent / great-grandparent colonists get `DC_Thought_GrandchildBorn` + a Legacy letter. Distinct from D20 in-law (marriage) and D03 coming of age. |
| E02 | Kin taken / returned | Trauma-adjacent | Kidnap (`PreKidnapped`) or enemy `CapturedBy`: colonist kin **or ex** get `DC_Thought_KinTaken` + letter. Rescue / recruit / return clears it and applies `DC_Thought_KinReturned`. Dedupe 2500 ticks. Not a Nemesis hunt. |
| E03 | Tended by family | Generations | `TendUtility.DoTend`: colonist (or prisoner kin) tended by colonist kin (`KinWeight` > 0, not ex). Patient `DC_Thought_TendedByFamily`, doctor `DC_Thought_TendedFamily`. 1-day cooldown. No new medical job. |
| E04 | Last of the line / line continues | Generations | Comp flags `sawColonyBloodKin` + `lastOfTheLine`. Situational `DC_Thought_LastOfTheLine` only after they have had living colony blood kin (parent/child/sibling/grand). Birth, join, or return that restores blood kin applies `DC_Thought_LineContinues` + letter. |
| E05 | Step-family | Generations | On colonist–colonist marriage, living children of either spouse who do not already have the other as parent get `DC_Thought_StepFamily`. Distinct from D20 (parents/siblings of the couple). |

---

## Non-goals (still)

- Off-map news, settlement flip, NPC wars → Living World
- Named personal antagonists → Nemesis
- Farmstand / harvest festival / aging cheese → Homesteader
- New architect furniture, raid incident packs, a second goodwill store
