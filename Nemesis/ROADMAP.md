# Nemesis — ROADMAP

Playable core is in. Remaining fantasy for later passes.

**Ownership:** personal antagonists, hunt arcs, and hunt-keyed world sites stay in **Nemesis**. Off-map faction politics / settlement morph / generic war sites belong to **[Living World](../docs/ideas/living-world.md)** (design only). Nemesis may *listen* to Living World signals fail-open; it does not own the world sim.

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

## Hunt base / false-lead arc (Nemesis-owned)

Acceptance-oriented checklist for later implementation:

- [ ] **Aggression gate** — camp / quest content only above hunt aggression threshold X (Mod Options).
- [ ] **Nemesis camp world site / quest** — offer at higher aggression; resolving may be:
  - **Real** — confrontation with the nemesis (and retinue), or
  - **False lead** — empty camp, planted evidence, or trap.
- [ ] **Progressive intel** — scrap / rumor → last-known world tile → site reveal; each step requires an active hunt.
- [ ] **Caravan-route ambush** — encounter map tied to the active nemesis pawn / faction (not a Living World warband).
- [ ] **Taunt cache** — abandoned stockpile / note on a route; do **not** reuse Living World generic war-site defs.

Shared tile rule (when Living World exists): if a LW war site already occupies a cell, offset or skip; Nemesis sites remain hunt-keyed.

---

## Multi-faction antagonists (Nemesis-owned)

- [ ] Allow **one active nemesis per hostile faction**, with a **global cap of 1–2** hunts.
- [ ] Reuse the same hunt component / letter pipeline; faction-colored taunt strings.
- [ ] **Not** a Living World “warlord table” — still personal fixation targets.
- [ ] **Living World listen (fail-open)** — if LW reports the nemesis’s faction crushed / fled the region: escalate aggression **or** end/dormant hunt (option). If LW absent, behavior unchanged.

---

## More personal systems

- [ ] Nemesis relationship / social memory with the fixation target (opinion, social fight chance).
- [ ] Apparel / weapon tint polish (focus gear upgrades already ship).
- [ ] Comms console interaction: reply options (taunt back / offer truce / demand surrender).

## Assault polish

- [ ] Dedicated `LordJob` that prioritizes the fixation pawn, then flees to map edge when raid points collapse.
- [ ] Shuttle drop + extract when Odyssey present (soft).

## Soft compat depth

- [ ] Stormproof: optional ion-storm baiting when aggression is high (still fail-open).
- [ ] Strata: harassment on underground levels via stairs awareness; don’t break pocket maps.
- [ ] Homesteader: target pantry / smokehouse stacks by defName list.
- [ ] Living World: consume faction crushed / victory chronicle signals only (see above).

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
