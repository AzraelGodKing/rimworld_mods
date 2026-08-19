# Homesteader — update ideas

**Status:** playable core shipped; **HS-Q04 through HS-S04 implemented** (this pool). Animal poop stays draft PR #74.  
**Mod:** [Homesteader](../../Homesteader/). Checklist: [Homesteader/ROADMAP.md](../../Homesteader/ROADMAP.md).  
**Lane rule:** player-colony agrarian fantasy — grow it, put it by, sell it, celebrate it. Off-map faction sims → [Living World](living-world.md). Named hunters targeting the pantry → Nemesis (defName list only). Colonist identity → Deep Colony.

**Sources:** existing Homesteader ROADMAP; Steam Workshop comments (Aug 2026); series soft-compat (Stormproof drought, Living World HS1).  
**In flight:** animal poop → compost (draft PR #74, `cursor/homesteader-animal-poop-dd08`) — yard feature, not this pool. This pool (Q04–S04) is implemented.

---

## Pitch reminder

Updates should make the **yard, pantry, and table** feel lived-in: seasons, livestock personality, preserves that improve, a farm that other people notice. Do not turn Homesteader into a second cooking overhaul or a world-politics mod.

---

## What’s already in (don’t rebuild)

Maypole **aura** (+6 harvest cheer) exists — the missing piece is an **annual festival ritual**, not another pole. Chicken coop, orchard, apiary, cellar/icehouse/springhouse, dairy pipeline, tastes/allergies, 27/Kats, Diggo/Sharkira, Dubs Bad Hygiene water bridge, CN/RU packs are in.

**Goat pen** was removed from the ship (`Homesteader_GoatPen`). Do **not** restore that def. If dairy livestock returns, use a new **dairy shed** id (HS-A07).

---

## Phases (summary)

| Phase | Theme | IDs | Count |
|-------|-------|-----|-------|
| 0 | Workshop QoL | HS-Q01–Q04 | 4 |
| 1 | Pantry you feel | HS-A01, A02, A03 | 3 |
| 2 | Yard & livestock | HS-A05, A06, A07, A08, A09 | 5 |
| 3 | Trade & brand | HS-A04, A10 | 2 |
| 4 | Power & season | HS-A11, A12, A13 | 3 |
| 5 | Soft-compat | HS-S01–S04 | 4 |

---

## Phase 0 — Workshop QoL (do these first)

Steam (Aug 2026): storage on the Furniture tab bloating the architect; a drying-rack `_north` missing texture report; “too many similar stations” (drying vs curing, cellar vs icehouse vs springhouse, hearth vs wood stove).

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-Q01 | **Homesteader architect tab** — crates, barrels, cellar, icehouse, springhouse, preserves shelf, hayloft under one tab (keep Production for stations) | S | **Shipped** (homestead tab). Fixes Furniture bloat without requiring Adaptive Storage Framework |
| HS-Q02 | **ASF Storage tab patch** — if Adaptive Storage Framework is loaded, also list storage defs on its Storage category | S | **Do not.** ASF is a framework with no architect tab; the old patch hid Homesteader storage. Homestead tab is the fix (HS-Q01). |
| HS-Q03 | **Texture audit** — directional drying rack / hayloft / hearth; missing `_north` must never pink-check | S | **Shipped** in repo (`DryingRack_north.png` etc.). Confirm Workshop zip is unpacked, not `Texture.rar`. |
| HS-Q04 | **Settings pack** — allergy flare intensity, favorite-food mood, coop egg interval, Kats on/off, cooling tooltip verbosity, larder/festival/aging/LW flavor | S | **Shipped** |

Do **not** merge jam cauldron into canning kitchen or hearth into wood stove in Phase 0 — document the split in About/FAQ instead (hearth cooks; stove heats). Revisit only if playtests agree they’re redundant.

---

## Phase 1 — Pantry you feel

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A01 | **Harvest festival** — once per harvest season at a built maypole: gathering job / Ideology ritual if present, mood, extra food, slight visitor/trade pull | M | **Shipped** (fall maypole mood + food + idle walk-up; not a full Ideology ritual) |
| HS-A02 | **Well-stocked larder** — ThoughtWorker: 3 / 6 / 9 distinct preserved foods in cellar / icehouse / springhouse / preserves shelf → tiered mood | S/M | **Shipped** (count *kinds*, not stacks) |
| HS-A03 | **Aging** — waxed cheese / ham / cider in a root cellar gain quality (or a hidden “aged” hediff/comp) over days; icehouse can slow it | M | **Shipped** (comp ticker; icehouse slower; ingest mood + market value) |

---

## Phase 2 — Yard & livestock

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A05 | **Guard geese** — flock/building that alerts at map-edge hostiles near the farmyard (loud animal + letter, not a turret) | M | **Shipped** |
| HS-A06 | **Prize livestock** — hidden quality on coop/shed animals; festival can show a prize; better eggs/milk from high quality | M | **Shipped** (building-local quality, not Deep Colony bloodlines) |
| HS-A07 | **Dairy shed** — new def (not `GoatPen`): periodic milk if hay/mash is nearby, companion to chicken coop | M | **Shipped** |
| HS-A08 | **Bees need bloom** — hive output scales down without flowers/crops in radius; inspect shows “nothing in bloom” | S | **Shipped** |
| HS-A09 | **Scarecrow + fox raid** — cheap yard beauty/scare; rare predator incident on the coop if unattended | S/M | **Shipped** |

---

## Phase 3 — Trade & brand

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A04 | **Farmstand** — roadside stand: visitors buy jam/cheese/cider for silver; colony **brand** (local goodwill-like score, **not** Deep Colony faction ledger) | L | **Shipped** (never calls `AddFactionDrift`) |
| HS-A10 | **County-fair visitor** — rare Misc: a judge or neighbor samples pantry food; prize mood + silver, or a polite shrug | S/M | **Shipped** |

Brand stays Homesteader-local (travelers talk about *your jam*). Deep Colony still owns faction goodwill.

---

## Phase 4 — Power, water, season

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A11 | **Waterwheel** — river/moving-water power; output drops in Stormproof drought if that mod is loaded | M | **Shipped** |
| HS-A12 | **Maple sugaring season** — sap harvest strongly boosted in early spring / cold; near-zero in summer | S | **Shipped** |
| HS-A13 | **Rain-aware barrels** — rain barrel fill rate follows weather (storm / drought). Stormproof drought can empty outdoor barrels slowly | S | **Shipped** |

---

## Phase 5 — Soft-compat (fail-open)

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-S01 | **Living World HS1** — keyed letter flavor when LW reports outlander famine / refugees (“we should send jam”) | S | **Shipped** (fail-open) |
| HS-S02 | **Stormproof cisterns** — drought condenser / dry spell inspect lines on wells and cisterns | S | **Shipped** |
| HS-S03 | **Deep Colony cooking perk** — optional extra taste thought if DC Grand Chef + Homesteader meal (DC owns the perk hook; this is the food list / defNames) | S | **Shipped** (meal defName list; DC-C09 still owns the perk) |
| HS-S04 | **Nemesis pantry list** — export smokehouse / cellar / farmstand defNames for Nemesis sabotage targeting | S | **Shipped** (Nemesis `IsOnHomesteaderPantryTarget`) |

---

## Explicitly later / probably never

- Restoring `Homesteader_GoatPen`
- Merging drying rack + curing rack (different jobs: dry vs salt)
- Gravship hydroponic homestead (Odyssey) — parked unless gravship players ask
- More orchard species (pear/plum) until aging + festival ship — depth before width

---

## Suggested build order

This pool is implemented (Q04–S04). Next Homesteader work is playtime feedback, not more IDs from this list. Animal poop remains PR #74.
