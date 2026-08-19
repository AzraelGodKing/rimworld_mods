# Next content — series slate

**Status:** planning for Stormproof / Date Night / Strata / Nemesis N1. **Homesteader HS-Q04 through HS-S04 is implemented** on this branch.  
**When:** after the Steam-first / general-fixes ship. Living World Phase 1–2, Strata V3, Deep Colony 2.0, and Date Night date hours are in.

Homesteader and Deep Colony already have numbered pools. This pass fills the same shape for **Stormproof**, **Nemesis**, **Date Night**, and **Strata post-V3**, then picks a first slice that stays in each mod’s lane.

---

## Full pools

| Mod | Pool | Lane |
|-----|------|------|
| Homesteader | [homesteader-updates.md](homesteader-updates.md) | Yard, pantry, table |
| Deep Colony | [deep-colony-batch-c.md](deep-colony-batch-c.md) | Identity, memory, goodwill (wait for 2.0 playtime) |
| Stormproof | [stormproof-updates.md](stormproof-updates.md) | Grid, weather, lightning, EMP |
| Nemesis | [nemesis-updates.md](nemesis-updates.md) | Named hunt (N1–N4) |
| Date Night | [date-night-updates.md](date-night-updates.md) | Schedule, dates, couple life |
| Strata | [strata-post-v3.md](strata-post-v3.md) | Column after V3 |
| Living World | [living-world.md](living-world.md) §8b Phase 3–4 | Chronicle already shipped; HS1 / N3 / LW9 later |

Do **not** start Deep Colony Batch C until 2.0 has some playtime (that pool says so). Animal poop is already a draft PR — not this slate.

---

## Slice 1 — feel-now QoL (S)

No new map types, no world sites, no endgame turrets. Players notice these in Mod Options, alerts, and inspect text.

| Mod | IDs | Why first |
|-----|-----|-----------|
| Homesteader | **HS-Q04** settings pack | **Shipped** with the rest of the Homesteader pool |
| Stormproof | **SP-Q01** settings, **SP-Q02** Odyssey DeepFreeze offset | Stormproof has no options menu; DeepFreeze is a shipped gap |
| Date Night | **DN-Q01** couple sync, **DN-Q02** stood-up nuance | Date hours just landed; finish the schedule UX |
| Strata | **ST-Q01** Levels-tab threat badges | Uses the existing tab; Workshop screenshot-adjacent |
| Nemesis | **NM-A09** obsession thought, **NM-S02** pantry defNames | Personal flavor without hunt-site art; HS-S04 lives here |

Optional tiny sibling: **HS1** Homesteader famine/refugee keyed lines — **shipped** as HS-S01.

---

## Slice 2 — one signature per lane (S/M)

Start after Slice 1, or skip a row if that mod is not the priority.

| Mod | IDs | Signature |
|-----|-----|-----------|
| Homesteader | **HS-A02** larder mood → **HS-A01** harvest festival (full pool through S04) | **Shipped** |
| Stormproof | **SP-A03** lightning divertor, then **SP-A01** substations | Early roof safety, then grid isolation |
| Nemesis | **N1** camp / intel / taunt cache (**NM-A01–A05**) | Hunt has a place on the world map |
| Strata | **ST-A01** dumbwaiter | Early item shaft; magma/flood wait |
| Date Night | **DN-A01** anniversary streak | Couple memory without Deep Colony bloodlines |
| Living World | leave **LW9** traffic parked | Not required to sell Phase 1–2 |

---

## Explicitly later / not this planning pass

- Magma layer, flooded level type, stack panel, fog-of-war underground
- Weather dominator, lightning gun
- Multi-hunt (N2) before N1 camps exist
- Deep Colony envoy caravan (C23), prisoner counsel (C13)
- Azrael custom portraits; **S1** teller weights until sibling incidents need them
- Farmstand (HS-A04), dairy shed, guard geese — **shipped** with the Homesteader pool
- Restoring `Homesteader_GoatPen`

---

## Suggested execution

1. **This planning branch** — docs for Stormproof / Nemesis / Date Night / Strata (done). Homesteader code landed here because that lane was the first implementation ask.
2. **Next code** — Slice 1 for Stormproof, Date Night, Strata, Nemesis NM-A09. Do not batch remaining mods in one go.
3. **Hunt-fantasy branch** — Nemesis N1 when the personal antagonist is the Workshop story.
4. **Homesteader** — pool implemented; wait for playtime. Animal poop stays PR #74.
