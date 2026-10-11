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

- [ ] **Aggression gate** — camp / quest content only above hunt aggression threshold X (Mod Options). (AZR-304 — still open; needs site/quest defs.)
- [ ] **Nemesis camp world site / quest** — offer at higher aggression; resolving may be:
  - **Real** — confrontation with the nemesis (and retinue), or
  - **False lead** — empty camp, planted evidence, or trap.
- [x] **Progressive intel (first pass)** — dossier + bought leads (tile / gear / warning / false lead). Camp / site reveal still later.
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

- [x] Nemesis relationship / social memory with the fixation target (opinion, social fight chance) — `NemesisSocial` + social-fight Harmony patch.
- [ ] Apparel / weapon tint polish (focus gear upgrades already ship).
- [x] Comms console interaction: reply options (taunt back / offer truce / demand surrender) — AZR-301.
- [x] Colony mood fallout on repeat escapes (fixation target hit hardest) — AZR-388.

## Assault polish

- [ ] Dedicated `LordJob` that prioritizes the fixation pawn, then flees to map edge when raid points collapse.
- [x] Kidnap-attempt action for Pawn-mode hunts (vanilla kidnap duty; kidnapped target ends the hunt) — AZR-389. Target-only AI is the item above.
- [ ] Shuttle drop + extract when Odyssey present (soft).

## Soft compat depth

- [x] **Rimesis / BFV exclusive claim** — `NemesisCompatApi` + foreign-antagonist skip (shipped). Spec: [nemesis-rimesis-compat.md](../docs/ideas/nemesis-rimesis-compat.md)
- [x] **Deep Colony capture / truce goodwill** — reviewed with DC ledger; no conflict (Execute/Release = vanilla goodwill only; Truce = timer only). Same spec.
- [x] **Rimesis Availability / Missing (design + stub)** — Font owns Availability (`Available` vs busy: AwaitingInvestigation / LocatedCampsite / LocatedSettlement / IncomingRaid / DispatchingRaid / EncounterActive). Nemesis exposes `ShouldReportMissingToRimesis` (`IsNemesisPawn`) for Font to mark Missing; soft-read of Availability via reflection still TBD (fail-open; need Font type/method names). Same spec.
- [ ] **Rimesis Availability soft-read** — when Font publishes the API, fail-open reflection in `SoftCompat` so Nemesis never steals a busy Rimesis pawn; no hard require.
- [ ] **Rimesis leader-raid handoff (Font)** — when Nemesis fires a vengeance / “leader” army return, call into Rimesis raid injection so Rimesis combat style/tactics apply. More work than coexistence; not scheduled until Font confirms packageId + public inject surface. (AZR-60 — deferred, blocked on public hook.)
- [ ] Stormproof: optional ion-storm baiting when aggression is high (still fail-open).
- [ ] Strata: harassment on underground levels via stairs awareness; don’t break pocket maps.
- [x] Homesteader: target pantry / smokehouse stacks by defName list (AZR-303).
- [ ] Living World: consume faction crushed / victory chronicle signals only (see above).

## Content / UX

- [x] Dossier tab + epitaphs (1.1.0)
- [x] Dossier last-known tile jump + full epitaph archive — AZR-387, AZR-390.
- [x] Tells (voice / weapon / mark / habit)
- [ ] Preview.png art pass.
- [x] Scenario / storyteller hints — `Nemesis_Marked` scenario ships.
- [x] Dev mode force-spawn / force-end debug actions — `NemesisDebug`.
- [ ] Steam description + screenshots.

## Balance

- [ ] Playtest trigger rates mid-game vs late.
- [ ] Cap concurrent storyteller threat when nemesis raid just fired.
- [ ] Multi-faction hunt cap playtest (1 vs 2 global).

## Series / further vision

- [Series roadmap](../ROADMAP.md) — soft-compat web, Azrael storyteller, “The Deep Homestead”, Living World
- [Living World design](../docs/ideas/living-world.md) — world news, settlement morph, NPC diplomacy (not personal hunts)
- [Strata V3](../Strata/V3_ROADMAP.md) · [Homesteader](../Homesteader/ROADMAP.md) · [Stormproof](../Stormproof/ROADMAP.md)
