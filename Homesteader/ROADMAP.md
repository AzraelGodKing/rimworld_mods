# Homesteader — ROADMAP

Playable core is in. Idea pool (IDs, Steam QoL, build order):
[docs/ideas/homesteader-updates.md](../docs/ideas/homesteader-updates.md).
Series slate: [docs/ideas/next-content.md](../docs/ideas/next-content.md).

Series-wide soft-compat, Azrael storyteller, and showcase scenario:
[../ROADMAP.md](../ROADMAP.md).

**Living World:** Homesteader does **not** own off-map faction sims. Farmstand, harvest festival, aging preserves, Diggo supplier, and livestock yard features stay here. If [Living World](../docs/ideas/living-world.md) is loaded, Homesteader may only **consume** famine/refugee chronicle flavor (optional keyed lines) — fail-open, no hard dependency.

**Goat pen:** `Homesteader_GoatPen` was removed. Do not restore it. Dairy livestock = **dairy shed** (HS-A07).

---

## Phase 0 — Workshop QoL

- [x] **HS-Q01** Homesteader architect tab (storage/cellars off Furniture)
- [x] **HS-Q02** Keep Homestead tab (do not hijack Adaptive Storage Framework — it has no Storage category)
- [x] **HS-Q03** Texture audit (directional drying rack / missing `_north` files are in `Textures/Homesteader/Buildings`)
- [x] **HS-Q04** Player settings pack (allergies, favorites, coop, Kats, cooling, larder/festival/aging/LW flavor)

## Phase 1 — Pantry you feel

- [x] **HS-A01** **Harvest festival** — fall maypole gathering (mood, extra food, idle colonists walk to the pole); not a full Ideology ritual
- [x] **HS-A02** **Well-stocked larder mood** — ThoughtWorker tiered buff from distinct preserved foods in cellars/pantries
- [x] **HS-A03** **Aging** — waxed cheese / smoked meat / cider quality-feel over time in the root cellar (inspect, market value, ingest mood)

## Phase 2 — Yard & livestock

- [x] **HS-A05** **Guard geese** — map-edge alert at the farmyard (letter, not a turret)
- [x] **HS-A06** **Prize livestock** — hidden quality on coop/shed; faster eggs/milk at high quality
- [x] **HS-A07** **Dairy shed** — periodic milk if hay/mash is nearby; new def, not GoatPen
- [x] **HS-A08** Bees need bloom in radius
- [x] **HS-A09** Scarecrow + rare fox-on-coop incident

## Phase 3 — Trade & brand

- [x] **HS-A04** **Farmstand** — roadside stand selling preserves to visitors + colony brand (Homesteader-local, not Deep Colony goodwill)
- [x] **HS-A10** County-fair visitor

## Phase 4 — Power & season

- [x] **Water building ladder** — barrel trickle → cistern stockpile+catch → tower capacity; hand-dug → deep well; still = boiled sidegrade; fountain drinks jugs
- [x] **HS-A11** **Waterwheel** — river water power; interacts with Stormproof droughts
- [x] **HS-A12** Maple sugaring season
- [x] **HS-A13** Rain-aware barrels / drought empty

## Phase 5 — Soft-compat consumers (do not move into Living World)

- [x] **HS-S01** Optional flavor when Living World reports outlander famine / refugees (string hooks only)
- [x] **HS-S02** Stormproof drought inspect on wells/cisterns
- [x] **HS-S03** Homesteader meal defName list for Deep Colony Grand Chef (DC-C09 still owns the perk hook)
- [x] **HS-S04** Nemesis pantry / smokehouse targeting remains a **Nemesis** soft-compat item (defName list), not a Homesteader world-sim
