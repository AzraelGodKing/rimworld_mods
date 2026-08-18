# Homesteader — ROADMAP

Playable core is in. Idea pool (IDs, Steam QoL, build order):
[docs/ideas/homesteader-updates.md](../docs/ideas/homesteader-updates.md).

Series-wide soft-compat, Azrael storyteller, and showcase scenario:
[../ROADMAP.md](../ROADMAP.md).

**Living World:** Homesteader does **not** own off-map faction sims. Farmstand, harvest festival, aging preserves, Diggo supplier, and livestock yard features stay here. If [Living World](../docs/ideas/living-world.md) is loaded, Homesteader may only **consume** famine/refugee chronicle flavor (optional keyed lines) — fail-open, no hard dependency.

**Goat pen:** `Homesteader_GoatPen` was removed. Do not restore it. Dairy livestock = new **dairy shed** (HS-A07).

---

## Phase 0 — Workshop QoL

- [x] **HS-Q01** Homesteader architect tab (storage/cellars off Furniture)
- [x] **HS-Q02** Keep Homestead tab (do not hijack Adaptive Storage Framework — it has no Storage category)
- [x] **HS-Q03** Texture audit (directional drying rack / missing `_north` files are in `Textures/Homesteader/Buildings`)
- [ ] **HS-Q04** Player settings pack (allergies, favorites, coop, Kats, cooling)

## Phase 1 — Pantry you feel

- [ ] **HS-A01** **Harvest festival** — maypole annual ritual (Ideology-aware, works without); mood, trade attraction, seasonal food
- [ ] **HS-A02** **Well-stocked larder mood** — ThoughtWorker tiered buff from distinct preserved foods in cellars/pantries
- [ ] **HS-A03** **Aging** — cheese / ham / cider quality tiers over time in the root cellar

## Phase 2 — Yard & livestock

- [ ] **HS-A05** **Guard geese** — alarm animal / nest theme; map-edge alert at the farmyard
- [ ] **HS-A06** **Prize livestock** — hidden quality across generations; festival + coop synergy
- [ ] **HS-A07** **Dairy shed** — periodic milk (companion to chicken coop); new def, not GoatPen
- [ ] **HS-A08** Bees need bloom in radius
- [ ] **HS-A09** Scarecrow + rare fox-on-coop incident

## Phase 3 — Trade & brand

- [ ] **HS-A04** **Farmstand** — roadside stand selling preserves to visitors + colony brand (Homesteader-local, not Deep Colony goodwill)
- [ ] **HS-A10** County-fair visitor

## Phase 4 — Power & season

- [x] **Water building ladder** — barrel trickle → cistern stockpile+catch → tower capacity; hand-dug → deep well; still = boiled sidegrade; fountain drinks jugs
- [ ] **HS-A11** **Waterwheel** — river water power; interacts with Stormproof droughts
- [ ] **HS-A12** Maple sugaring season
- [ ] **HS-A13** Rain-aware barrels / drought empty

## Phase 5 — Soft-compat consumers (do not move into Living World)

- [ ] **HS-S01** Optional flavor when Living World reports outlander famine / refugees (string hooks only)
- [ ] **HS-S02** Stormproof drought inspect on wells/cisterns
- [ ] **HS-S03** Deep Colony Grand Chef + Homesteader meals (defName list; DC-C09)
- [ ] **HS-S04** Nemesis pantry / smokehouse targeting remains a **Nemesis** soft-compat item (defName list), not a Homesteader world-sim
