# Nemesis — update ideas

**Status:** ideas only. Playable hunt core is shipped. This numbers the remaining fantasy as **N1–N4** (same IDs as [living-world.md](living-world.md) §8b) plus implementable sub-IDs.  
**Mod:** [Nemesis](../../Nemesis/). Checklist: [Nemesis/ROADMAP.md](../../Nemesis/ROADMAP.md).  
**Lane rule:** named personal antagonist, hunt arc, hunt-keyed world sites. Off-map faction wars → Living World. Player goodwill math → Deep Colony (Execute/Release stay vanilla goodwill). Farm brand → Homesteader (defName list only). Rimesis/BFV coexistence stays fail-open — never steal Font’s pawn.

**Sources:** Nemesis ROADMAP; living-world Phase 3 (N1–N4); [nemesis-rimesis-compat.md](nemesis-rimesis-compat.md).

---

## Pitch reminder

Updates should make the **same person** come back smarter, leave traces on the world map, and talk to you — not spawn a war table or a second reputation sheet.

---

## What’s already in (don’t rebuild)

Persistent hunt, escape-until-cornered, captain levels + combat foci, vengeance army inject, assault / waste / fake-signal / caravan / sabotage / food / Anomaly bait, capture dialog (Execute / Release / Keep / Truce), Marked scenario, `NemesisCompatApi`, Rimesis/BFV skip, Stormproof EMP respect, Homesteader cellar/favorite flavor, Dev force actions, CN/RU.

**Not shipped:** world sites, intel chain, multi-hunt, interactive comms replies, fixation `LordJob`, Odyssey shuttle. `TrophyHunt` is an unused enum. `FactionRetaliation` is debug-only. Dev tools and social-fight multiplier already exist — ROADMAP “dev force-spawn” / “social memory” bullets are partly stale.

---

## Phases (summary)

| Phase | Parent | Theme | IDs | Count |
|-------|--------|-------|-----|-------|
| 0 | N4 (lite) | Personal flavor + lists | A09, S02 | 2 |
| 1 | **N1** | Hunt space | A01–A05 | 5 |
| 2 | **N4** | Personal systems | A08, A10–A12 | 4 |
| 3 | **N2** / **N3** | Multi-hunt + LW listen | A06, A07 | 2 |
| 4 | N4 compat | Soft-compat depth | S01, S03, S04, A13 | 4 |

---

## Phase 0 — Feel the hunt without new sites

| ID | Parent | Idea | Size | Notes |
|----|--------|------|------|-------|
| NM-A09 | N4 | **Obsession thought** — named mood/opinion while the nemesis is on the map (both sides) | S | Deeper than social-fight ×2.4; not a goodwill ledger |
| NM-S02 | N4 | **Homesteader pantry list** — smokehouse / cellar / farmstand defNames for food raids | S | **Shipped** (HS-S04; `IsOnHomesteaderPantryTarget`) |

---

## Phase 1 — N1 hunt space

Tile-avoid Living World generic war sites when both load (LW8 already names the rule).

| ID | Parent | Idea | Size | Notes |
|----|--------|------|------|-------|
| NM-A01 | N1 | **Aggression gate** — camp / quest content only above a settings threshold | S | Slider next to existing pacing |
| NM-A02 | N1 | **Camp site / quest** — real confrontation **or** false lead (empty, planted evidence, trap) | L | First WorldObject / SitePart in the package |
| NM-A03 | N1 | **Progressive intel** — scrap → last-known tile → site reveal; each step needs an active hunt | M | Feeds A02; no site if hunt ended |
| NM-A04 | N1 | **Caravan-route ambush** — encounter map tied to the nemesis pawn / faction | M | Not a Living World warband |
| NM-A05 | N1 | **Taunt cache** — abandoned stockpile + note on a route | S/M | Distinct defNames from LW war sites |

---

## Phase 2 — N4 personal systems

Independent of camps. Do not wait on Living World.

| ID | Parent | Idea | Size | Notes |
|----|--------|------|------|-------|
| NM-A08 | N4 | **Interactive comms** — reply: taunt back / offer truce / demand surrender | M | Truce stays a timer; no Deep Colony `AddFactionDrift` |
| NM-A10 | N4 | **Fixation `LordJob`** — prioritize the marked colonist, then flee to edge when raid points collapse | M | Replaces stock `LordJob_AssaultColony` for NemesisAssault |
| NM-A11 | N4 | **Odyssey shuttle** — soft drop + extract when DLC present | M | Fail-open; Marked already has an orbit layer |
| NM-A12 | N4 | **Trophy memento** — after escapes, a named token on the map as an intel crumb | S | Feeds A03; not a second reputation sheet |

---

## Phase 3 — N2 multi-hunt and N3 listen

| ID | Parent | Idea | Size | Notes |
|----|--------|------|------|-------|
| NM-A06 | N2 | **One nemesis per hostile faction**, global cap 1–2 | M | Reuse hunt component; faction-colored taunts. After N1 core is stable |
| NM-A07 | N3 | **Living World listen** — faction crushed / fled → escalate **or** dormant (option) | S | Fail-open; bus already exists |

---

## Phase 4 — Soft-compat depth

| ID | Parent | Idea | Size | Notes |
|----|--------|------|------|-------|
| NM-S01 | N4 | **Rimesis Availability soft-read** — never steal a busy Font pawn once type names land | S | Spec already; no hard require |
| NM-S03 | N4 | **Stormproof ion bait** — high aggression, optional lure into an EMP window | S | Needs SP-Q03 / SP-S03; fail-open |
| NM-S04 | N4 | **Strata stairs** — harassment can chase underground; don’t break pocket maps | M | Prefer surface today; expose stair awareness from Strata |
| NM-A13 | N4 | **Light focus tactics** — Sniper holds range / Berserker charges / Mechanitor peels | M | Not VFE warcaskets, not a full Siege/Breach matrix |

---

## Explicitly later / probably never

- Cheat-death after max escapes
- Warcaskets (VFE Pirates) / VRF vehicles
- Full Siege / Breach / Commander matrix
- Rimesis leader-raid **inject** (Font public API; beyond coexistence)
- Natural `FactionRetaliation` trigger (keep debug unless playtests want it)
- Owning generic war sites or goodwill drift

---

## Suggested build order

1. **Phase 0** — A09 + S02 (Slice 1).
2. **N1** — A01 gate, A05 cache, A03 intel, then A02 camp (the expensive one), A04 ambush with it.
3. **N4 combat** — A10 LordJob before shuttle.
4. **N2 / N3** — only after one hunt + camp feels good.
5. **S01** whenever Font publishes names.
