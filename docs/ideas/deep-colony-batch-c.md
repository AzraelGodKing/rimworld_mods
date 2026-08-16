# Deep Colony — Batch C (post-2.0)

**Status:** ideas only — Phases 0–5 shipped. Do not start until 2.0 has some playtime.  
**Mod:** [Deep Colony](../../Deep%20Colony/). Prior pool: [deep-colony-updates.md](deep-colony-updates.md) (A01–A20, B01–B22 — **do not reuse**). Batch C = **20 ideas**, Phase 6–9.  
**Lane rule:** colonist / colony-facing only. No furniture packs, raid timers, off-map politics, farm brand, or a second goodwill buffer.

**Pitch reminder:** deepen identity, memory, teaching, bloodlines, and earned faction goodwill — especially where 2.0 still ignores DLC and sibling mods.

---

## Why a new batch

2.0 filled perk/trauma/mentor/generation/reputation depth. Workshop only asked about **mid-save performance** (answered: light). Remaining gaps:

- No Royalty / Ideology / Biotech-child / Anomaly / Odyssey hooks (grep is empty).
- No sibling flavor beyond Living World goodwill + Nemesis “don’t double-buffer.”
- QoL around the three new tabs (alerts, filters) is thinner than the systems behind them.

Prefer **S/M** that players feel without another Hard-gated power layer.

---

## Phases (summary)

| Phase | Theme | IDs | Count |
|-------|-------|-----|-------|
| 6 | QoL after 2.0 | C01, C02, C04, C16 | 4 |
| 7 | Memory & kin | C11, C12, C17, C18, C21 | 5 |
| 8 | DLC + sibling hooks | C03, C05–C10, C24 | 8 |
| 9 | Gated / larger | C13, C15, C23 | 3 |

---

## Batch C — 20 ideas

| ID | Phase | Idea | System | Size | Notes |
|----|-------|------|--------|------|-------|
| DC-C01 | 6 | **Untreated-trauma alert** — same pattern as unspent perk points (1-day grace) | Trauma | S | Workshop asked FPS; this is opt-in UI, not a tick tax |
| DC-C02 | 6 | **Counseling history** — last counselor, session count, confidant progress on inspect | Trauma | S | Flavor + debug; no new job |
| DC-C16 | 6 | **Perks tab filter** — by skill / unspent / Hard-only nodes | Perks | S | Tab already exists |
| DC-C04 | 6 | **Royalty titles as envoy bias** — titled pawns suggested first; optional tiny goodwill from title when envoy | Reputation | S | Fail-open if no Royalty |
| DC-C11 | 7 | **Family letters** — rare birthday / anniversary / “first harvest as a family” notes on the Legacy tab | Generations | S/M | Cap like LW letters; not world news |
| DC-C12 | 7 | **Deathbed lesson** — dying mentor can finish one last teach (or pass a tier-1 perk) if an apprentice is on the map | Mentoring | M | Pairs with heirlooms; default on |
| DC-C17 | 7 | **Childhood memories** — colony-born kids keep a short “I grew up here” thought into adulthood | Generations | M | Biotech children; skip growth vats |
| DC-C18 | 7 | **Funerals ease violent loss** — burying / burning the body (Ideology funeral if present) ages trauma faster | Trauma | S/M | Vanilla grave is enough without Ideology |
| DC-C21 | 7 | **Sibling bond** — colony-raised siblings get a small opinion + teach-gap discount (not professional rivalry) | Mentoring | S | ≠ B16 |
| DC-C03 | 8 | **Ideology precepts** — “Counseling is sacred” vs “Stoic: skip therapy, slower natural fade” | Trauma | M | Fail-open; no hard Ideology dep |
| DC-C05 | 8 | **Anomaly horror trauma** — void / entity exposure; counseling still works | Trauma | M | DLC-gated def; ≠ insect/fire |
| DC-C06 | 8 | **Odyssey crash / isolation trauma** — gravship wreck or long solo orbit | Trauma | M | DLC-gated; no shuttle furniture |
| DC-C07 | 8 | **Kids at the raid** — child witnesses get a lighter, shorter thought (not full combat shock) | Trauma | S/M | Biotech; default on, slider |
| DC-C08 | 8 | **Date Night confidants** — lovers reach confidant in 2 counsel sessions instead of 3 | Trauma | S | Fail-open; Date Night optional |
| DC-C09 | 8 | **Homesteader meals + cooking perk** — Grand Chef extra mood only on Homesteader pantry foods | Perks | S | Fail-open; farm brand stays Homesteader |
| DC-C10 | 8 | **Sibling disaster flavor** — cave-in (Strata) / ion-storm down (Stormproof) can apply existing combat/fire/toxic trauma with a keyed reason | Trauma | S | Reasons only; no new TraumaDefs required |
| DC-C24 | 8 | **Gene vs blood** — Biotech xenogene passion vs inherited family tradition: a one-line inspect conflict, not a stat war | Generations | S | Flavor; don’t nerf genes |
| DC-C13 | 9 | **Prisoner counsel** — optional recruitment path via counseling (slow, Social-gated); default off | Trauma | M | Must not replace Warden chat |
| DC-C15 | 9 | **Apology / tribute** — float menu: spend silver or a gift to write a ledger row (still `AddFactionDrift`) | Reputation | M | No parallel buffer |
| DC-C23 | 9 | **Envoy visit** — assigned envoy may form a short caravan to an allied settlement for a goodwill pulse | Reputation | L | Settings-gated; skip if caravans are a mess |

Dropped on purpose: extra perk tiers, respec variants, chronic-trauma 2, farmstand, named hunters, world chronicle UI, guest teachers, school furniture, remembrance-calendar UI (B18 is enough).

---

## Suggested build order

1. **Phase 6** — C01, C02, C16 (players feel the 2.0 tabs immediately).
2. **Phase 8 lite** — C08, C09, C10, C04 (fail-open one-liners).
3. **Phase 7** — C18, C12, C17, C21 (kin memory).
4. **Phase 8 DLC** — C03, C05, C06, C07 (only if you want DLC callouts on the Workshop page).
5. **Phase 9** — only after playtest; C23 last.

---

## Non-goals (still)

- Off-map news, settlement flip, NPC wars → Living World
- Named personal antagonists → Nemesis
- Farmstand / harvest festival / aging cheese → [Homesteader](homesteader-updates.md)
- New architect furniture, raid incident packs, a second goodwill store
