# Homesteader — update ideas

**Status:** playable core shipped; this is the next content/QoL pool.  
**Mod:** [Homesteader](../../Homesteader/). Checklist: [Homesteader/ROADMAP.md](../../Homesteader/ROADMAP.md).  
**Lane rule:** player-colony agrarian fantasy — grow it, put it by, sell it, celebrate it. Off-map faction sims → [Living World](living-world.md). Named hunters targeting the pantry → Nemesis (defName list only). Colonist identity → Deep Colony.

**Sources:** existing Homesteader ROADMAP; Steam Workshop comments (Aug 2026); series soft-compat (Stormproof drought, Living World HS1).

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
| HS-Q01 | **Homesteader architect tab** — crates, barrels, cellar, icehouse, springhouse, preserves shelf, hayloft under one tab (keep Production for stations) | S | Fixes Furniture bloat without requiring Adaptive Storage Framework |
| HS-Q02 | **ASF Storage tab patch** — if Adaptive Storage Framework is loaded, also list storage defs on its Storage category | S | Fail-open; comment asked for “the storage tab” |
| HS-Q03 | **Texture audit** — directional drying rack / hayloft / hearth; missing `_north` must never pink-check | S | Player reported `DryingRack_north.png`; confirm pack vs git LFS |
| HS-Q04 | **Settings pack** — today settings are DevMode allergy-reveal only. Add: allergy flare intensity, favorite-food mood, coop egg interval, Kats on/off, cooling tooltip verbosity | S | Match Deep Colony’s “players can tone it down” habit |

Do **not** merge jam cauldron into canning kitchen or hearth into wood stove in Phase 0 — document the split in About/FAQ instead (hearth cooks; stove heats). Revisit only if playtests agree they’re redundant.

---

## Phase 1 — Pantry you feel

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A01 | **Harvest festival** — once per harvest season at a built maypole: gathering job / Ideology ritual if present, mood, extra food, slight visitor/trade pull | M | Maypole building already exists |
| HS-A02 | **Well-stocked larder** — ThoughtWorker: 3 / 6 / 9 distinct preserved foods in cellar / icehouse / springhouse / preserves shelf → tiered mood | S/M | Count *kinds*, not stacks |
| HS-A03 | **Aging** — waxed cheese / ham / cider in a root cellar gain quality (or a hidden “aged” hediff/comp) over days; icehouse can slow it | M | Needs a ticker; cap so it isn’t a 24/7 map scan |

---

## Phase 2 — Yard & livestock

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A05 | **Guard geese** — flock/building that alerts at map-edge hostiles near the farmyard (loud animal + letter, not a turret) | M | Nesting-box synergy |
| HS-A06 | **Prize livestock** — hidden quality on coop/shed animals; festival can show a prize; better eggs/milk from high quality | M | Generations stay light (not Deep Colony bloodlines) |
| HS-A07 | **Dairy shed** — new def (not `GoatPen`): periodic milk if hay/mash is nearby, companion to chicken coop | M | Same Comp_Spawner pattern as the coop |
| HS-A08 | **Bees need bloom** — hive output scales down without flowers/crops in radius; inspect shows “nothing in bloom” | S | Makes the orchard earn its keep |
| HS-A09 | **Scarecrow + fox raid** — cheap yard beauty/scare; rare predator incident on the coop if unattended | S/M | Incident, not a new AI animal |

---

## Phase 3 — Trade & brand

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A04 | **Farmstand** — roadside stand: visitors buy jam/cheese/cider for silver; colony **brand** (local goodwill-like score, **not** Deep Colony faction ledger) | L | Own component; never call `AddFactionDrift` |
| HS-A10 | **County-fair visitor** — rare Misc: a judge or neighbor samples pantry food; prize mood + silver, or a polite shrug | S/M | Can wait until farmstand exists |

Brand stays Homesteader-local (travelers talk about *your jam*). Deep Colony still owns faction goodwill.

---

## Phase 4 — Power, water, season

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-A11 | **Waterwheel** — river/moving-water power; output drops in Stormproof drought if that mod is loaded | M | Series ROADMAP already names this |
| HS-A12 | **Maple sugaring season** — sap harvest strongly boosted in early spring / cold; near-zero in summer | S | Plant already exists; seasonal feel |
| HS-A13 | **Rain-aware barrels** — rain barrel fill rate follows weather (storm / drought). Stormproof drought can empty outdoor barrels slowly | S | Vanilla rain if Stormproof absent |

---

## Phase 5 — Soft-compat (fail-open)

| ID | Idea | Size | Notes |
|----|------|------|-------|
| HS-S01 | **Living World HS1** — keyed letter flavor when LW reports outlander famine / refugees (“we should send jam”) | S | No farmstand inside LW; [living-world.md](living-world.md) |
| HS-S02 | **Stormproof cisterns** — drought condenser / dry spell inspect lines on wells and cisterns | S | Series soft-compat web |
| HS-S03 | **Deep Colony cooking perk** — optional extra taste thought if DC Grand Chef + Homesteader meal (DC owns the perk hook; this is the food list / defNames) | S | Pair with DC-C09 |
| HS-S04 | **Nemesis pantry list** — export smokehouse / cellar / farmstand defNames for Nemesis sabotage targeting | S | Nemesis implements the hunt; Homesteader only keeps names stable |

---

## Explicitly later / probably never

- Restoring `Homesteader_GoatPen`
- Merging drying rack + curing rack (different jobs: dry vs salt)
- Gravship hydroponic homestead (Odyssey) — parked unless gravship players ask
- Outfit Routines / farmer-gear auto-swap → [outfit-routines.md](outfit-routines.md), not this mod
- More orchard species (pear/plum) until aging + festival ship — depth before width

---

## Suggested build order

1. **Phase 0** — Q01 + Q03 immediately (Steam pain); Q04 with the next content drop.
2. **Phase 1** — A02 (ThoughtWorker, small), then A01 festival, then A03 aging.
3. **Phase 2** — A08 bloom, A07 dairy shed, A05 geese; A06/A09 with festival.
4. **HS-S01** anytime (tiny). Farmstand (A04) after pantry mood exists so the stand has something to brag about.
