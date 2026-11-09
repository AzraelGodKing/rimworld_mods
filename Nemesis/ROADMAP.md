# Nemesis — ROADMAP

Playable core is in. Remaining fantasy for later passes.

**Ownership:** personal antagonists, hunt arcs, and hunt-keyed world sites stay in **Nemesis**. Off-map faction politics / settlement morph / generic war sites belong to **[Living World](../docs/ideas/living-world.md)** (design only). Nemesis may *listen* to Living World signals fail-open; it does not own the world sim.

---

## Hybrid captain progression (shipped — first pass)

- [x] Progression levels on escape (skills, gear quality, battle-hardened hediff)
- [x] Combat focus at create (Destroyer / Berserker / Sniper / Psycho / Survivor / Mechanitor)
- [x] Post-escape action bias toward army returns; petty sabotage downweighted
- [x] Soft animal escorts + Biotech mech retinue (Mechanitor)
- [x] Warcaskets (VFE Pirates) — level 5+ Destroyer / Survivor escort
- [ ] Vehicles (VRF) — later
- [x] Full tactic matrix (Siege / Breach / Commander / Assault)
- [ ] Cheat-death — **out of scope** (keep corner → killable)

---

## Hunt base / false-lead arc (Nemesis-owned)

Acceptance-oriented checklist for later implementation:

- [ ] **Aggression gate** — camp / quest content only above hunt aggression threshold X (Mod Options). (AZR-304 — still open.)
- [x] **Nemesis camp world site** — a bought lead places `Nemesis_HuntSite` near the faction's settlement. Real: the nemesis waits with the camp; false lead: their people and a calling card (AZR-555).
- [x] **Progressive intel** — dossier + bought leads (tile / gear / warning / false lead / camp).
- [ ] **Caravan-route ambush** — encounter map tied to the active nemesis pawn / faction (not a Living World warband).
- [ ] **Taunt cache** — abandoned stockpile / note on a route; do **not** reuse Living World generic war-site defs.

Shared tile rule (when Living World exists): if a LW war site already occupies a cell, offset or skip; Nemesis sites remain hunt-keyed.

---

## Multi-faction antagonists (Nemesis-owned)

- [x] Allow **one active nemesis per hostile faction**, with a **global cap of 1–3** hunts (default 2).
- [x] Reuse the same hunt component / letter pipeline; faction-colored letter titles.
- **Not** a Living World “warlord table” — still personal fixation targets.
- [x] **Living World listen (fail-open)** — `Collapse` escalates or goes dormant; `DecisiveVictory` emboldens or delays (AZR-556). If LW absent, behavior unchanged.

---

## More personal systems

- [x] Nemesis relationship / social memory with the fixation target (opinion, social fight chance) — `NemesisSocial` + social-fight Harmony patch.
- [x] Apparel tint + aura (level 3+ gear colour; battle-hardened aura) — AZR-536.
- [x] Comms console interaction: reply options (taunt back / offer truce / demand surrender) — AZR-301.
- [x] Colony mood fallout on repeat escapes (fixation target hit hardest) — AZR-388.

## Assault polish

- [x] Dedicated `LordJob_NemesisHunt` that prioritizes the fixation pawn, then flees when the raid collapses.
- [x] Kidnap-attempt action for Pawn-mode hunts (vanilla kidnap duty; kidnapped target ends the hunt) — AZR-389.
- [x] Shuttle drop + extract when Odyssey present (soft) — AZR-706.

## Soft compat depth

- [x] **Rimesis / BFV exclusive claim** — `NemesisCompatApi` + foreign-antagonist skip (shipped). Spec: [nemesis-rimesis-compat.md](../docs/ideas/nemesis-rimesis-compat.md)
- [x] **Deep Colony capture / truce goodwill** — reviewed with DC ledger; no conflict (Execute/Release = vanilla goodwill only; Truce = timer only). Same spec.
- [x] **Rimesis Availability / Missing (design + stub)** — Font owns Availability (`Available` vs busy: AwaitingInvestigation / LocatedCampsite / LocatedSettlement / IncomingRaid / DispatchingRaid / EncounterActive). Nemesis exposes `ShouldReportMissingToRimesis` (`IsNemesisPawn`) for Font to mark Missing; soft-read of Availability via reflection still TBD (fail-open; need Font type/method names). Same spec.
- [ ] **Rimesis Availability soft-read** — when Font publishes the API, fail-open reflection in `SoftCompat` so Nemesis never steals a busy Rimesis pawn; no hard require.
- [ ] **Rimesis leader-raid handoff (Font)** — when Nemesis fires a vengeance / “leader” army return, call into Rimesis raid injection so Rimesis combat style/tactics apply. More work than coexistence; not scheduled until Font confirms packageId + public inject surface. (AZR-60 — deferred, blocked on public hook.)
- [ ] Stormproof: optional ion-storm baiting when aggression is high (still fail-open).
- [ ] Strata: harassment on underground levels via stairs awareness; don’t break pocket maps.
- [x] Homesteader: target pantry / smokehouse stacks by defName list (AZR-303).
- [x] Living World: consume `Collapse` / `DecisiveVictory` chronicle signals (see above).

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
- [Homesteader](../Homesteader/ROADMAP.md) · [Stormproof](../Stormproof/ROADMAP.md)
