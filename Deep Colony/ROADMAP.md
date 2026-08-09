# Deep Colony — ROADMAP

Playable core is in (perks, trauma, mentoring, inheritance, living faction reputation).

Series vision: [../ROADMAP.md](../ROADMAP.md).  
Idea pool (42): [docs/ideas/deep-colony-updates.md](../docs/ideas/deep-colony-updates.md).

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