# Nemesis — ROADMAP

Playable core is in. Remaining fantasy (N1–N4 sub-IDs, sizes, build order):
[docs/ideas/nemesis-updates.md](../docs/ideas/nemesis-updates.md).

**Ownership:** personal antagonists, hunt arcs, and hunt-keyed world sites stay in **Nemesis**. Off-map faction politics / settlement morph / generic war sites belong to **[Living World](../docs/ideas/living-world.md)**. Nemesis may *listen* to Living World signals fail-open; it does not own the world sim.

---

## Hybrid captain progression (shipped — first pass)

- [x] Progression levels on escape (skills, gear quality, battle-hardened hediff)
- [x] Combat focus at create (Destroyer / Berserker / Sniper / Psycho / Survivor / Mechanitor)
- [x] Post-escape action bias toward army returns; petty sabotage downweighted
- [x] Soft animal escorts + Biotech mech retinue (Mechanitor)
- [ ] Warcaskets (VFE Pirates) / vehicles (VRF) — later
- [ ] Full tactic matrix (Siege / Breach / Commander) — later
- [ ] Cheat-death — **out of scope** (keep corner → killable)

---

## Hunt base / false-lead arc (N1)

Acceptance-oriented checklist for later implementation. Sub-IDs: [nemesis-updates.md](../docs/ideas/nemesis-updates.md).

- [ ] **NM-A01 Aggression gate** — camp / quest content only above hunt aggression threshold X (Mod Options).
- [ ] **NM-A02 Nemesis camp world site / quest** — offer at higher aggression; resolving may be:
  - **Real** — confrontation with the nemesis (and retinue), or
  - **False lead** — empty camp, planted evidence, or trap.
- [ ] **NM-A03 Progressive intel** — scrap / rumor → last-known world tile → site reveal; each step requires an active hunt.
- [ ] **NM-A04 Caravan-route ambush** — encounter map tied to the active nemesis pawn / faction (not a Living World warband).
- [ ] **NM-A05 Taunt cache** — abandoned stockpile / note on a route; do **not** reuse Living World generic war-site defs.

Shared tile rule (when Living World exists): if a LW war site already occupies a cell, offset or skip; Nemesis sites remain hunt-keyed.

---

## Multi-faction antagonists (N2)

- [ ] **NM-A06** Allow **one active nemesis per hostile faction**, with a **global cap of 1–2** hunts.
- [ ] Reuse the same hunt component / letter pipeline; faction-colored taunt strings.
- [ ] **Not** a Living World “warlord table” — still personal fixation targets.
- [ ] **NM-A07 Living World listen (N3, fail-open)** — if LW reports the nemesis’s faction crushed / fled the region: escalate aggression **or** end/dormant hunt (option). If LW absent, behavior unchanged.

---

## More personal systems (N4)

- [ ] **NM-A09** Obsession thought / social memory with the fixation target (opinion; social fight chance already exists).
- [ ] **NM-A08** Comms console interaction: reply options (taunt back / offer truce / demand surrender).
- [ ] **NM-A12** Trophy memento after escapes (intel crumb, not a reputation sheet).
- [ ] Apparel / weapon tint polish (focus gear upgrades already ship).

## Assault polish

- [ ] **NM-A10** Dedicated `LordJob` that prioritizes the fixation pawn, then flees to map edge when raid points collapse.
- [ ] **NM-A11** Shuttle drop + extract when Odyssey present (soft).
- [ ] **NM-A13** Light focus tactics (not full Siege matrix).

## Soft compat depth

- [x] **Rimesis / BFV exclusive claim** — `NemesisCompatApi` + foreign-antagonist skip (shipped). Spec: [nemesis-rimesis-compat.md](../docs/ideas/nemesis-rimesis-compat.md)
- [x] **Deep Colony capture / truce goodwill** — reviewed with DC ledger; no conflict (Execute/Release = vanilla goodwill only; Truce = timer only). Same spec.
- [x] **Rimesis Availability / Missing (design + stub)** — Font owns Availability (`Available` vs busy: AwaitingInvestigation / LocatedCampsite / LocatedSettlement / IncomingRaid / DispatchingRaid / EncounterActive). Nemesis exposes `ShouldReportMissingToRimesis` (`IsNemesisPawn`) for Font to mark Missing; soft-read of Availability via reflection still TBD (fail-open; need Font type/method names). Same spec.
- [ ] **NM-S01 Rimesis Availability soft-read** — when Font publishes the API, fail-open reflection in `SoftCompat` so Nemesis never steals a busy Rimesis pawn; no hard require.
- [ ] **Rimesis leader-raid handoff (Font)** — when Nemesis fires a vengeance / “leader” army return, call into Rimesis raid injection so Rimesis combat style/tactics apply. More work than coexistence; not scheduled until Font confirms packageId + public inject surface.
- [ ] **NM-S03** Stormproof: optional ion-storm baiting when aggression is high (still fail-open).
- [ ] **NM-S04** Strata: harassment on underground levels via stairs awareness; don’t break pocket maps.
- [ ] **NM-S02** Homesteader: target pantry / smokehouse stacks by defName list.
- [ ] **NM-A07** Living World: consume faction crushed / victory chronicle signals only (see N3).

## Content / UX

- [ ] Preview.png art pass.
- [ ] Scenario / storyteller hints.
- [ ] Dev mode force-spawn / force-end debug actions.
- [ ] Steam description + screenshots.

## Balance

- [ ] Playtest trigger rates mid-game vs late.
- [ ] Cap concurrent storyteller threat when nemesis raid just fired.
- [ ] Multi-faction hunt cap playtest (1 vs 2 global).

## Series / further vision

- [Series roadmap](../ROADMAP.md) — soft-compat web, Azrael storyteller, “The Deep Homestead”, Living World
- [Living World design](../docs/ideas/living-world.md) — world news, settlement morph, NPC diplomacy (not personal hunts)
- [Strata V3](../Strata/V3_ROADMAP.md) · [Homesteader](../Homesteader/ROADMAP.md) · [Stormproof](../Stormproof/ROADMAP.md)
