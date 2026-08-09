# Deep Colony — update ideas

**Status:** triage complete — Batch A (20) + Batch B (22) = **42 ideas**, assigned to Phase 0–5.  
**Mod:** [Deep Colony](../../Deep%20Colony/) — perk trees, trauma/therapy, apprenticeship, generational inheritance, living faction reputation.  
**Lane rule:** colonist / colony-facing only. World chronicle, settlement morph, NPC wars → [Living World](living-world.md). Named hunters → [Nemesis](../../Nemesis/ROADMAP.md). Farm brand → Homesteader.  
**Roadmap:** [Deep Colony/ROADMAP.md](../../Deep%20Colony/ROADMAP.md).

**Sources:** Cursor Batch A; Claude pool from worktree `claude/deep-colony-ideas-554ceb`.

---

## Pitch reminder

Your colonists are more than their stats. Updates should deepen **identity, memory, teaching, bloodlines, and faction goodwill you earned** — not add furniture, raid timers, or off-map politics.

---

## Non-goals

- Off-map news letters, settlement flip, NPC faction wars (Living World)
- Named personal antagonists / hunt sites (Nemesis)
- Farmstand / harvest festival flavor (Homesteader)
- Weather disasters (Stormproof), multi-level columns (Strata), romance schedule (Date Night)
- A second goodwill buffer that duplicates Living World’s event bus

---

## Phases (summary)

| Phase | Theme | IDs | Count |
|-------|-------|-----|-------|
| 0 | Foundation | A19, A20, B01 | 3 |
| 1 | Quick wins | A04, A09, A18, B03, B08, B09, B19, B22 | 8 |
| 2 | Mentoring & generations | A10–A15, B11, B15, B16, B20 | 10 |
| 3 | Trauma depth | A05–A08, B05–B07, B13, B17, B18, B21 | 11 |
| 4 | Reputation | A16, A17, B12 | 3 |
| 5 | Power (gated) | A01–A03, B02, B04, B10, B14 | 7 |

---

## Dedupe report (Claude → Batch A)

Claude’s original 20: **6 hard duplicates** dropped/folded; **12 kept** as B01–B12; **2 over-drops recovered** as B21–B22; **8 fresh** as B13–B20.

| Claude # | Claude title | Matched Batch A | Action |
|----------|--------------|-----------------|--------|
| 1 | Mod settings panel | DC-A19 | Folded into A19 |
| 3 | Perk respec | DC-A03 | Dropped |
| 4 | Capstone L20 | DC-A01 | Dropped |
| 12 | Trauma-aware work/combat | *(≠ A05)* | Recovered → B21 |
| 13 | Graduation | DC-A09 | Dropped (nuances in A09) |
| 14 | Choose taught skill | DC-A11 | Dropped (nuances in A11) |
| 18 | Dynasty + surnames | DC-A13 | Tab = A13; surnames → B22 |
| 19 | Reputation ledger | DC-A16 | Dropped |

---

## Batch A — 20 ideas (Cursor)

| ID | Phase | Idea | System | Size | Notes |
|----|-------|------|--------|------|-------|
| DC-A01 | 5 | **Third perk tier (skill 20)** — capstone node per skill | Perks | L | Settings-gated |
| DC-A02 | 5 | **Branching perk choice** — L15 pick A *or* B | Perks | L | Settings-gated |
| DC-A03 | 5 | **Respec / forget perk** | Perks | M | Settings for cost |
| DC-A04 | 1 | **Perk inspect tooltips show numbers** | Perks | S | QoL |
| DC-A05 | 3 | **Trauma-linked combat habits** — cover / avoid melee | Trauma | M | ≠ B21 |
| DC-A06 | 3 | **Specialty trauma: fire / toxic / insect** | Trauma | M | |
| DC-A07 | 3 | **Group counseling** | Trauma | M | |
| DC-A08 | 3 | **Trauma scars after full heal** | Trauma | M | |
| DC-A09 | 1 | **Apprentice graduation** | Mentoring | M | |
| DC-A10 | 2 | **Lineage mentors** | Mentoring | S | |
| DC-A11 | 2 | **Skill-focus apprenticeship** | Mentoring | M | |
| DC-A12 | 2 | **Blackboard mentoring boost** | Mentoring | S | Biotech blackboard in room |
| DC-A13 | 2 | **Founder legacy screen** | Generations | M | Pairs with B22 |
| DC-A14 | 2 | **Family skill traditions** | Generations | M | |
| DC-A15 | 2 | **Adoptive inheritance soft pass** | Generations | M | |
| DC-A16 | 4 | **Reputation ledger UI** | Reputation | M | |
| DC-A17 | 4 | **Personal envoy reputation** | Reputation | M | |
| DC-A18 | 1 | **Grudge / favor memory labels** | Reputation | S | Flavor only |
| DC-A19 | 0 | **Settings pack** — presets + per-system on/off + sliders | QoL | S | Phase 0 |
| DC-A20 | 0 | **Dev tools expansion** | QoL | S | Phase 0 |

---

## Batch B — 22 ideas (Claude)

| ID | Phase | Idea | System | Size | Notes |
|----|-------|------|--------|------|-------|
| DC-B01 | 0 | **Retroactive perk points on join** | Perks | S | Phase 0 |
| DC-B02 | 5 | **Cross-skill archetypes** | Perks | M | Settings-gated |
| DC-B03 | 1 | **Colony perk overview window** | Perks | S/M | |
| DC-B04 | 5 | **Chronic trauma → hediff** | Trauma | L | After Phase 3 playtest |
| DC-B05 | 3 | **Triggers and flashbacks** | Trauma | M | |
| DC-B06 | 3 | **Resilience / post-traumatic growth** | Trauma | M | |
| DC-B07 | 3 | **Therapy quality scaling** | Trauma | S/M | |
| DC-B08 | 1 | **Confidant relation** | Trauma | S | |
| DC-B09 | 1 | **Teaching lineage record** | Mentoring | S | |
| DC-B10 | 5 | **Heirlooms** | Generations | M | Settings-gated |
| DC-B11 | 2 | **Deeper inheritance** | Generations | M | |
| DC-B12 | 4 | **Attitude states with consequences** | Reputation | L | Settings-gated |
| DC-B13 | 3 | **Betrayal trauma family** | Trauma | M | |
| DC-B14 | 5 | **Recruits arrive pre-perked** | Perks | M | Conservative default |
| DC-B15 | 2 | **Perk apprenticeship** | Mentoring | M | Tier-1 only |
| DC-B16 | 2 | **Professional rivalry** | Mentoring | M | |
| DC-B17 | 3 | **Grudges remember the faction** | Trauma × Rep | M | |
| DC-B18 | 3 | **Days of remembrance** | Trauma | M | |
| DC-B19 | 1 | **Skill rust and muscle memory** | Perks | S/M | |
| DC-B20 | 2 | **Elders** | Generations | M | |
| DC-B21 | 3 | **Trauma draft / work penalties** | Trauma | M | Default off |
| DC-B22 | 1 | **Founder surnames** | Generations | S/M | |

---

## Merge / triage notes

- Prefer **S/M** polish that players feel in the first 20 hours (Phase 1) after Foundation (Phase 0).
- Capstone / branching (A01–A02), chronic trauma (B04), attitude consequences (B12) stay Phase 5 / settings-gated.
- Reputation ideas must keep using `AddFactionDrift` / `FactionRepUtility`; no parallel buffer.
- Further idea rounds must avoid **A01–A20 and B01–B22**.
