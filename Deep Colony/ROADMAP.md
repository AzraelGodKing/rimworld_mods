# Deep Colony — ROADMAP

Playable core is in (perks, trauma, mentoring, inheritance, living faction reputation).

Series vision: [../ROADMAP.md](../ROADMAP.md).  
Idea pool (42, shipped): [docs/ideas/deep-colony-updates.md](../docs/ideas/deep-colony-updates.md).  
Post-2.0 pool (20): [docs/ideas/deep-colony-batch-c.md](../docs/ideas/deep-colony-batch-c.md).  
Batch D: [docs/ideas/deep-colony-batch-d.md](../docs/ideas/deep-colony-batch-d.md).

## Ownership vs Living World

Deep Colony owns **player↔faction goodwill memory and drift** (raids, trades, gifts, shared-enemy kills, idle ally decay tuning). It does **not** own:

- Off-map chronicles / world news letters → [Living World](../docs/ideas/living-world.md)
- Settlement morph / NPC faction wars → Living World
- Named antagonists / hunt sites → [Nemesis](../Nemesis/ROADMAP.md)
- Farmstand / harvest festival → [Homesteader](../Homesteader/ROADMAP.md)

### When Living World ships (consumer only)

- [x] Register fail-open for Living World world-event signals
- [x] Map **visible** wars / ally disasters to existing `AddFactionDrift` / `FactionRepUtility` paths
- [x] Do **not** duplicate a second goodwill buffer inside Living World
- [ ] Keep current idle ally / enemy drift behavior unless playtests say otherwise

## Update phases (colony identity — not world sim)

### Phase 0 — Foundation

- [x] **A19** Settings pack (per-system on/off, soft/default/hard, sliders)
- [x] **A20** Dev tools expansion
- [x] **B01** Retroactive perk points on join

### Phase 1 — Quick wins

- [x] **A04** Perk numeric tooltips
- [x] **B03** Colony perk overview + idle-points alert
- [x] **B19** Skill rust / muscle memory
- [x] **A18** Grudge / favor epithets
- [x] **B22** Founder surnames
- [x] **A09** Apprentice graduation
- [x] **B08** Confidant relation
- [x] **B09** Teaching lineage flavor

### Phase 2 — Mentoring & generations

- [x] **A11** Skill-focus teach
- [x] **A10** Lineage mentor preference
- [x] **A12** Mentoring XP bonus from Biotech blackboard (same room)
- [x] **A13** Founder legacy screen
- [x] **A14** Family skill traditions
- [x] **A15** Adoptive passion echo
- [x] **B11** Deeper inheritance
- [x] **B20** Elders
- [x] **B15** Perk apprenticeship (tier-1)
- [x] **B16** Professional rivalry

### Phase 3 — Trauma depth

- [x] **B07** Therapy quality scaling
- [x] **A07** Group counseling
- [x] **A06** Specialty trauma (fire/toxic/insect)
- [x] **B13** Betrayal trauma family
- [x] **A08** Trauma scars after heal
- [x] **B06** Resilience / growth
- [x] **B05** Flashbacks
- [x] **B21** Draft/work penalties (default off)
- [x] **A05** Combat AI habits
- [x] **B17** Grudges remember faction
- [x] **B18** Days of remembrance

### Phase 4 — Reputation transparency

- [x] **A16** Reputation ledger UI
- [x] **A17** Personal envoy
- [x] **B12** Attitude consequences (settings-gated)

### Phase 5 — Power systems (gated)

- [x] **A01** Skill-20 capstone
- [x] **A02** Branching L15 pick
- [x] **A03** Respec / reflection
- [x] **B02** Cross-skill archetypes
- [x] **B14** Recruits pre-perked
- [x] **B10** Heirlooms
- [x] **B04** Chronic trauma hediff

### Soft-compat / playtest

- [x] Soft-compat notes with Nemesis capture / truce goodwill — reviewed; no gap fix. Nemesis Execute/Release use vanilla `TryAffectGoodwillWith`; DC ledger does not double-buffer. Spec: [nemesis-rimesis-compat.md](../docs/ideas/nemesis-rimesis-compat.md)

### Close-out

- [x] Workshop / docs polish — About.xml + `docs/deep-colony.html` 2.0 blurb; Soft/Default keep power systems & attitude consequences off, Hard enables heavier set
- [x] CN/RU language spot-check — 2.0 tabs (Perks/Legacy/Reputation) + Phase 5 Keyed/DefInjected fill

## Batch C (Phases 6–9)

Spec: [deep-colony-batch-c.md](../docs/ideas/deep-colony-batch-c.md). Build stamp `batch-c-v1`.

- [x] **Phase 6 QoL** — untreated-trauma alert, counseling history, Perks tab filter, Royalty envoy bias
- [x] **Phase 7 memory & kin** — family letters, deathbed lesson, childhood memories, funerals ease loss, sibling bond
- [x] **Phase 8 DLC + siblings** — Ideology precepts; Anomaly/Odyssey trauma; child witnesses; Date Night confidants; Homesteader chef meals; Strata/Stormproof trauma reasons; gene vs blood flavor
- [x] **Phase 9 gated** — prisoner counsel (default off), apology/tribute, envoy visit (settings-gated; goodwill pulse, no caravan form)

## Batch D

Spec: [deep-colony-batch-d.md](../docs/ideas/deep-colony-batch-d.md). Build stamp `batch-d-v1`.

- [x] **Leftovers** — first harvest + marriage letters (C11), gift/float-menu tribute (C15), Ideology funeral (C18)
- [x] **Fixes** — rivalry interval, player-only deathbed, funeral only-when-eased, counseling restarts trauma-alert grace, sibling/caravan kin, Date Night confidant inspect
- [x] **Identity** — family meal, parent reunion, coming of age, classroom extra, envoy-at-trade, surface isolation ease, quiet-room therapy, spouse remembrance
- [x] **Tabs / inspect** — Legacy trauma + remembrance, Reputation ally/hostile filter, Perks archetype column, inspect extras, prisoner-counsel float menu, Strata gas/firestorm reasons
- [x] **D16 Family tree** — Character inspect tab + Bio-card button; click a relative to select and jump to that pawn
- [x] **D17 Kin join / defect** — spouse highest chance, ex-lover 0% for this path; no goodwill hit unless hostile (grudge / `FamilyDefect`)
- [x] **D18 Ex-lover reconcile** — get back together on the map; 4th time marks a toxic relationship that counseling can ease (`family-join-v1`)
- [x] **D19 Family vs unwavering** — only kin the prisoner likes (and who like them back) can break unwavering loyalty (`family-loyalty-v1`)
- [x] **D20 In-law welcome** — parents and siblings on the map get a short mood when colonists marry
- [x] **D21 Kin homecoming** — returning after ≥8h away (10-day cooldown) cheers the returner and their closest kin; distinct from D02 parent reunion
- [x] **D22 Kin died other side** — colonist kin/ex mourn a relative who dies while not a colonist (letter; skipped during execution)
- [x] **D23 Breakup wound** — colonists gain a breakup thought when ExLover/ExSpouse is written; toxic trauma renews if already present
- [x] **D24 Execute family** — executing kin/ex hits remaining colonist family with executed thought + betrayal trauma (`family-beats-v1`)
- [x] **E01 Grandchild born** — grandparents / great-grandparents on the map get a thought + Legacy note
- [x] **E02 Kin taken** — kidnap or enemy capture hurts kin/ex on the map; return/recruit clears it
- [x] **E03 Tended by family** — kin doctor tend gives both a short comfort thought (1-day cooldown)
- [x] **E04 Last of the line** — last living colony blood kin gets a lasting thought; a new blood relative is “the line continues”
- [x] **E05 Step-family** — children of a colonist marriage get a step-parent thought (`family-life-v1`)
- [x] **E06 Prison visit** — family at a kin prisoner’s cell gives mood even if loyalty holds
- [x] **E07 Release kin** — releasing kin/ex is the inverse of execution (relief, no betrayal)
- [x] **E08 Tradition teach** — one-shot letter when the household skill is passed on at a perk gate
- [x] **E09 Kin downed beside you** — adult sees family collapse in a fight (not death, not child raid-witness)
- [x] **E10 Empty nest** — last child leaves the home map; staying parent gets a quiet-house thought (`family-echo-v1`)
