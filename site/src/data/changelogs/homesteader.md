# Changelog

Detailed notes for Homesteader only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

Steam Workshop paste: [`About/changelog.txt`](About/changelog.txt).

## [Unreleased]

Player-facing version **1.0.0** (`About.xml` `modVersion`). Startup writes `[Homesteader] v1.0.0 loaded from ...` in Player.log.

### Added
- **Homestead architect tab** — crates, barrels, pallet, hayloft, cellars, icehouse, springhouse, cistern, and water tower moved off Furniture (Steam Aug 6). Adaptive Storage Framework does not get those buildings (it has no Storage tab); the Homestead tab stays.
- **Update idea pool** — Workshop QoL (architect tab, ASF Storage patch, texture audit, settings) plus pantry/yard/farmstand/waterwheel phases. Goat pen stays removed; dairy shed is the livestock follow-up. Spec: [docs/ideas/homesteader-updates.md](../docs/ideas/homesteader-updates.md).
- **Optional texture refresh** — Mod Options → Homesteader → "Use refreshed textures" (off by default). Original sprites are kept. The new pack gives every building, item, plant, and floor its own unique texture (no more shared jam/cellar/etc. placeholders); orchard trees and composted soil get dedicated art too. `_*_{north,south,east,west}` files are one building from four camera angles, not four different objects. Curing rack refresh redone as one mixed-charcuterie rack (hams, sausages, bacon slab, netted salami) with matching facings. Diggo keeps brought `art/brought/HippoDogPlushie.png`; the 27 statue keeps brought `art/brought/27_2.0.png` (neither is in the refresh swap). Outliers that read as product photos, RPG loot, or true-isometric renders (apparel, power buildings, maypole, monuments, grand 27, shark plushie, chicken coop, beehive, icehouse/root cellar/springhouse, solar still, water tower, orchard trees, crops, jam/mason jar/pie/cans) were redrawn against actual Core sprites (wood-fired generator, cowboy hat, oak/maple, chess table, sculptures): thick dark outlines, muted palette, flattened top-down 3/4 camera. EN/CN/RU settings strings. Restart if sprites look stale after toggling.
- **Azrael storyteller** — Cassandra-style pacing with slightly more Misc / ThreatSmall (series flavor). Canonical package; the optional Azrael showcase mod only injects this teller if Homesteader is not loaded.
- **ForcedStoryteller scen part** — Homesteader.ScenPart_ForcedStoryteller so Homesteaders (and sibling mod scenarios via MayRequire) can lock Azrael.
- **Homesteaders scenario** — locks Azrael storyteller on start.
- **Little guy trait** — flavor-only trait (`Homesteader_LittleGuy`) with no gameplay effects; one-time mail on first load after update announcing it.
- **CN / RU language packs** — Chinese Simplified and Russian Keyed + full DefInjected (buildings, items, plants, recipes, research, thoughts, hediffs, incidents, and related defs).

### Fixed
- **Azrael storyteller (1.6)** — comps rewritten to match Cassandra Classic (valid disease / quest / raid-beacon fields); portraits use `CassandraClassic` art so the teller loads without XML / missing-texture errors.
- **ASF hid Homestead storage** — Adaptive Storage Framework has no `ASF_Architect` tab. The old patch moved crates/cellars/cisterns there and deleted `Homesteader_Storage`, so those buildings vanished whenever ASF was a dependency.
- **Meal allergies** — flares and food AI now inspect `CompIngredients`, so simple/fine meals cooked with milk, wheat, eggs, or fish match.
- **Storage capacity copy** — descriptions use per-cell `maxItemsInCell` totals (hayloft 8, large crate 24, root cellar 24, icehouse 16, springhouse 6).
- **Handheld soap** — wash consumes a bar (`CompUseEffect_DestroySelf`); the tub already used fuel.
- **Allergy catalog** — pumpkin pie, ploughman's lunch, bread, flapjacks, and toast-and-jam match milk and/or wheat.
- **Root cellar description** — UTF-8 dash and `5°C` instead of mojibake.
- **Cistern / water tower** — ShelfBase storage parity so bills/hauling see contents.
- **Dubs Bad Hygiene plumbing** — `PatchOperationFindMod` matches the Workshop display name so pipe comps actually attach.
- **Quilted bed blanks Furniture UI** — added `CompProperties_AffectedByFacilities` (BedWithQualityBase PlaceWorker NRE without it, same class of bug as bedroll).
- **Crates / barrels invisible to cook & cure bills** — Homesteader `Building_Storage` defs now match vanilla ShelfBase parity (`ignoreStoredThingsBeauty`, `Blueprint_Storage`, `storageGroupTag`, `disallowNotEverStorable`), including icehouse / springhouse. Should also unstick curing-rack “only salt works” when meat was parked in ingredient barrels.
- **Allergy flare without eating** — food allergy hediff / discovery no longer run from `ThoughtsFromIngesting` (also used by food AI and ingest menus). Reactions apply only on real `Thing.Ingested`.

### Changed
- **Water building ladder** — each step earns the upgrade: rain barrel is a cheap weathering trickle; cistern stores eight stacks, stops rot, and fills itself; water tower holds twenty-four stacks, catches faster, and hauls as Critical. Hand-dug well stays five jugs on demand; deep well hauls twenty for little extra work. Solar still is the arid sidegrade (boiled water, not a faster barrel). Stone fountain drinks ~2 water jugs/day (auto-refuel); +32 beauty only while flowing.
- **Refresh wood-burning generator** — redrew `HomesteaderRefresh/Buildings/WoodGenerator.png` to match Core wood-fired generator packing (compact 1x1 firebox + cylinder, log hopper, grate glow, chimney) with clean alpha instead of the muddy outlined-eaten blob.
- **Refresh wood stove** — redrew `HomesteaderRefresh/Buildings/WoodStove.png` as a compact Core-style 1x1 cast-iron stove (cook plate, chimney, grate glow, side logs) instead of the photoreal render.
- **Refresh battery bank** — redrew `HomesteaderRefresh/Buildings/BatteryBank.png` as a 2x2 industrial cell rack (wood frame, beige tops, copper posts) instead of a grid of AA-battery icons.
- **Refresh compact battery** — redrew `HomesteaderRefresh/Buildings/CompactBattery.png` as a chunky Core-style 1x1 industrial cell (beige top, copper posts, wood cradle) instead of the tiny UI battery icon.
- **Refresh curing rack** — redrew all four Graphic_Multi facings as one 2x1 mixed-charcuterie rack (ham, sausages, bacon slab, netted salami, salt bowls) from four camera angles, Core outlines instead of the painterly still.
- **Refresh drying rack** — redrew all four Graphic_Multi facings as one 3x1 low slatted table (jerky strips, fruit leather, dried mushrooms) from four camera angles, Core outlines instead of the photoreal lattice.
- **Refresh grain mill** — redrew `HomesteaderRefresh/Buildings/GrainMill.png` as a compact Core-style 1x1 hand mill (millstone, grain hopper, crank, flour spout) instead of the photoreal stone basin.
- **Refresh harvest maypole** — redrew `HomesteaderRefresh/Buildings/HarvestMaypole.png` as a Core-style tall pole with wheat and ribbons; ribbon gaps are true alpha instead of filled white.
- **Refresh hayloft** — redesigned all four Graphic_Multi facings as one 2x1 open elevated wood loft (hay bales, stilts, peaked roof, ladder) from four camera angles; gaps under the loft are true alpha instead of filled white.
- **Refresh icehouse** — redesigned as four Graphic_Multi facings of one 2x2 thick-walled ice cellar (stone, wood frame, packed ice, sawdust, roof vent). Refresh swap uses `Graphic_Multi` and enables rotation; the original def stays `Graphic_Single` / non-rotatable so toggle-off still works.
- **Refresh compost heap** — redrew `HomesteaderRefresh/Buildings/CompostHeap.png` as a compact Core-style 2x2 open-front wooden slat bay (dark compost mound, straw, steam, pitchfork) instead of the photoreal two-bay isometric bin.
- **Refresh beehive + beeswax candle** — redrew as compact Core-style 1x1s: wooden Langstroth hive box (matches the def, not a straw skep) and a fat outlined candle in a dish instead of the photoreal paint.
- **Refresh brewing bench** — redrew all four Graphic_Multi facings as one wood bench (copper kettle, keg, bottles) from four camera angles, Core outlines instead of the painted still.
- **Refresh butter churn** — redrew `HomesteaderRefresh/Buildings/ButterChurn.png` as a compact Core-style 1x1 wooden dash churn (stave barrel, hoops, lid, T-handle dasher) instead of the photoreal render.
- **Refresh canning kitchen** — redrew as four Graphic_Multi facings of one 2x1 wood-fired canning station (water-bath pot on a grate, mason-jar rack). Refresh swap uses `Graphic_Multi`; the original def stays `Graphic_Single` so toggle-off still works.
- **Refresh checkers table** — redrew `HomesteaderRefresh/Buildings/CheckersTable.png` as a compact Core-style 1x1 wooden table with a carved board, red/cream pieces, and short wood legs instead of the white chess-table pedestal.
- **Refresh cheese press + cider press** — redrew as compact Core-style 1x1s with distinct silhouettes: low table + hoop mold + cheese wheels versus fat slatted barrel + juice bowl + apples (not matching gallows-style screw frames).
- **Chicken coop** — replaced hay-bin placeholder sprite with a proper henhouse graphic (`Textures/Homesteader/Buildings/ChickenCoop.png`).
- **27 statue** — replaced in-game sprite with brought `27_2.0` art (installed as-is at `Textures/Homesteader/Buildings/Statue27.png`).
- **Diggo the plushie** — replaced in-game sprite with brought `HippoDogPlushie42` art (installed as-is at `Textures/Homesteader/Buildings/HippoDogPlushie.png`).

### Removed
- **Goat pen** — building and texture removed (`Homesteader_GoatPen` / `GoatPen.png`).

### Fixed
- **Passive cooling performance** — root cellar / icehouse / springhouse cell cache rebuilds only when a cooler spawns or despawns (dirty flag), not every 250 ticks; `AmbientTemperature` early-outs when the map has no cooled cells.
- **Environmental allergy scans** — check one colonist per pulse (rotated) so Mold/PetDander map scans no longer hit everyone on the same tick.
- Favorite-food Harmony patch updated for RimWorld 1.6 (`FoodUtility.ThoughtsFromIngesting` now returns `List<ThoughtFromIngesting>` instead of `List<ThoughtDef>`), stopping the Homesteader static-constructor crash.
- Wellspring research nodes use non-negative `researchViewY` (negative coords are treated as unset by the game).
- Lard no longer has Nutrition without ingestible properties; apple juice sets `socialPropernessMatters` to avoid warden prison-cell food loops.
- Pawns can work homestead production stations again (jam cauldron, hearth, drying rack, mill, brewery, etc.). Stations had bills and recipes but no `WorkGiverDef` with `fixedBillGiverDefs`, so `WorkGiver_DoBill` never considered them — colonists could build and set "do forever" bills but would not interact. Note: wood-fired stations (jam cauldron, hearth, smokehouse) still need fuel before work starts.
- **Sugar, butter, and cream are used in recipes again.** Jam needs sugar; bread and flapjacks need butter; cheese is pressed from cream + rock salt; butter is churned from cream (skim milk first); porridge and cider use sugar. Cider no longer accepts hay or fungus.
- **Rock salt** now trains Cooking (matches the Cooking workgiver) instead of Construction.
- **Research gates match the tree:** curing rack, smokehouse, hayloft, and ingredient barrel need Primitive homestead; mill, churn, pickling crock, and hearth need Farmstead crafting; washer toss, log bench, and 27 statues need Homestead comforts.
- **Root cellar** now passively cools stored food (C#): items on the cellar use ambient temperature capped at 5°C for spoilage, so summer heat no longer cooks the pantry. Requires Harmony.
- **Advanced battery** can be rotated; **washer toss barrel** is no longer rotatable (single graphic).
- About and research blurbs updated for dairy/sugar pipelines, costs, and unlocks.
- 27 monument/statue aura now grants a visible **statue of 27** moodlet (+27) in the Mood tab. The aura hediff includes `HediffCompProperties_Disappears` (required by vanilla `CompCauseHediff_AoE` to refresh while in range), and the mood uses Core `ThoughtWorker_Hediff` like joywire.
- Renamed `Homesteader_Statue27` to `Homesteader_StatueTwentySeven` — RimWorld rejects ThingDef names ending in a digit (blueprint/frame/install defs inherit the suffix and fail validation too).
- Restored shark plushie and 27 monument/statue sprites from the original concept art references (replacing the tiny regenerated placeholders that lost the intended look).
- Overalls now draw on the pawn again. They previously pointed at the vanilla `Pants/Pants` worn texture, which doesn't exist (vanilla pants have never been rendered on pawns), so nothing was drawn and render errors were logged. Overalls now use the TribalA worn graphic - the only stuff-colored torso+legs worn art in the game - so they read as work clothes covering torso and legs in the fabric/leather they're made from.
- Removed invalid `harvestDestroys` tag from apple, cherry and maple orchard plants (not a PlantProperties field in 1.6; regrowth is already handled by `harvestAfterGrowth`).
- Bread, porridge, trail stew and hearty stew now inherit from the correct vanilla meal base (`MealBase` instead of the non-inheritable `MealSimple`/`MealFine` defNames), restoring their thingClass, food type, rot ticking and eat sounds/effects.
- Homestead supplier caravan patch now targets the `OutlanderCivil` def node directly; the old xpath failed because `caravanTraderKinds` only exists on the abstract outlander base def.
- Homesteaders scenario: corrected the starting pawns config page def name (`ConfigPage_ConfigureStartingPawns`) and added the RimWorld 1.6 `surfaceLayer` block. The scenario now ships as version-specific copies (`1.6` and `Legacy` load folders) so 1.4/1.5 remain supported.
- Checkers and washer toss joy givers now declare the joy kind matching their vanilla jobs (Gaming_Cerebral / Gaming_Dexterity) and require manipulation.
- Beehive uses the `Building` thing class so it can be deconstructed and minified without config errors.
- Removed duplicate thing categories inherited from parents (log bench, rocking chair, quilted bed: BuildingsFurniture; cider, mead: Drugs).
- Root cellar no longer lists thing categories, since it is intentionally not minifiable.
- Honey is now flagged with `socialPropernessMatters`, preventing warden food-delivery loops in prison cells.

### Added
- **Polyarmory trait** — pawns with it treat their polycule (lovers and polyarmory metamours) as fine bedmates: no SharedBed jealousy, and `WillingToShareBed` allows multi-person / Polyamory Beds setups.
- **Tastes tab** on humanlike pawns — lists **5 favorite foods** and allergies. Allergies stay hidden as “Unknown sensitivity” until discovery.
- **Rare allergies** — roll heavily favors **None**; otherwise Big-9 style food (milk, eggs, peanuts, tree nuts, wheat, soy, fish, shellfish, sesame) or environmental (pollen/hay fever, dust mites, pet dander, mold). Soft-medium flare (mood −10 + short hediff); food AI avoids discovered food allergens. Never lethal.
- Allergy **None** stays hidden as “Unknown sensitivity” (same as real allergies) so an empty list does not reveal immunity; DevMode reveal still shows None.
- Favorites list removed from the pawn inspect/basic info string; favorites only appear on the **Tastes** tab.
- Mod settings (DevMode): **Reveal allergies** — show names on the Tastes tab before discovery.
- Tastes tab **DEV: Reroll tastes** only appears when God mode is on.
- **Kats Effect** — rare Misc storyteller event that only fires when a finished 27 statue/monument is on the home map. Hybrid anomalous-broadcast letter (with a short Foundation addendum), temporary heat dome (~+10°C), short brain-rot hediff, 27-silver Super Chat (or hostile Kats if unpaid), and a personal Kats directive mood (+12, ~1 day). Min refire 27 days.
- `Languages/English/Keyed/Homesteader.xml` — all C# player strings (wash tub, passive cooling, favorite food inspect, Kats Effect letter body) with `.Translate()` wiring.
- `Languages/README.md` — translator guide (Keyed + DefInjected layout, package id).
- **Homestead livestock:** chicken coop (periodic eggs); animal mash recipe at the hearth (barley/pumpkin/hay).
- **Charcuterie:** render lard or tallow from meat; stuff sausage from salted/smoked meat or jerky + fat + herbs (hearth).
- **Homestead textiles:** flax crop, spinning wheel (flax/wool → homespun cloth), loom (quilt); quilted bed now costs a homestead quilt (comforts + textiles).
- **Soil & cold storage:** compost heap (compost → fertilizer), composted soil terrain (1.45 fertility), icehouse (≤0°C), springhouse (≤8°C) — reuse root-cellar cooling.
- **Pantry craft:** canning kitchen (mason jars, canned stew/jam), cider press (apple juice) + ferment juice at the brewing bench, wash tub (refuel with soap, use for freshly washed mood).
- Research: livestock, textiles, soil and cold storage, pantry craft. Crafting workgiver covers spinning wheel, loom, and compost heap.
- Soap can render with lard or tallow as well as butter. Preserves shelf accepts sausage, canned goods, juice, and rendered fats.
- **Dubs Bad Hygiene soft-compat** — when DBH is loaded, rain barrel / cistern / water tower gain plumbing pipe + water storage (caps 100 / 2000 / 8000); hand-dug well acts as a primitive groundwater source; deep well is a piped deep inlet. Jug water for irrigation is unchanged. Optional only (soft `loadAfter`).
- **Dedicated research tab** — all Homesteader projects live under their own *Homesteader* tab (no longer on Main).
- Preserves shelf now also accepts butter, cream, beeswax, bread, flapjacks, pantry meals, and Odyssey salted/smoked fish.
- Homesteader C# assembly (`Homesteader.dll`) for root cellar cooling; requires the Harmony mod.
- **Pantry meals** at the hearth: toast and jam (bread + jam), ploughman's lunch (bread + cheese + pickles), and honey porridge (grain + honey, joy bonus).
- **Honey and maple syrup** can stand in for sugar in jam, porridge, and cider.
- Dried herbs now season trail stew and hearty stew.
- Homestead supplier also stocks butter, maple syrup, bread, soap, waxed cheese, and cider vinegar.
- **Pumpkin pie** and **buttermilk biscuits** at the hearth; churning butter now also yields **buttermilk**.
- **Cider vinegar** (from cider) and **vinegar pickles**; dedicated **apple cider** brew from orchard apples.
- **Waxed cheese** (cheese + beeswax) and **smoked cheese** (smokehouse).
- **Homestead soap** (beeswax + butter) — use a bar for a freshly washed moodlet.
- **Harvest maypole** grants nearby harvest cheer (+6 mood).
- Taste thoughts for jam, cheese, flapjacks, toast, pie, and biscuits.
- **Favorite food** (C#): every humanlike with a mood (colonists, guests, raiders, etc.) rolls a favorite from Homesteader foods plus vanilla meals/treats. Shown on inspect; eating it gives +8 mood and a food-preference bonus.
- Root cellar cooling now also chills spoilables in adjacent indoor cells.
- **27 monuments (two styles):** build either a golden or a harvest-stone statue of the number 27. Both share the same aura — colonists within range gain +27 mood — so you pick the look that fits your yard. Unlocked with homestead comforts research; golden costs gold and granite, harvest costs granite and hay.
- **27 statue + grand 27 statue:** gilded dripping-27 sculptures on vanilla-style stone plinths (1×1 and 2×2). Quality-scaled beauty via `FurnitureWithQualityBase`; +27 mood aura to pawns in range (same reach as the monuments).
- **Sharkira the plushie:** a chonky stuffed shark sewn from cloth. Same comforts as Diggo — high beauty (scales with quality), sittable, and cuddling for meditative recreation. Buildable from the start; homestead supplier caravans occasionally carry one.

#### The orchard
- **Plants:** apple tree, cherry tree, and sugar maple - slow-growing sowable trees that regrow after each harvest instead of dying (sowing gated behind primitive homestead research).
- **Items:** apples, cherries (raw fruit that feeds the existing cider, jam, and drying recipes), maple sap, maple syrup (never spoils), flapjacks (simple meal with a joy bonus).
- **Recipes (homestead hearth):** boil maple syrup (8 sap → 2 syrup), bake flapjacks (4 flour + 1 syrup).

#### The apiary
- Beehives now also produce **beeswax** on a slow second cycle, alongside honey.
- **Beeswax candle:** a refuelable, chemfuel-free light source built from and fueled by beeswax (homestead brewing research).

#### Around the farmhouse
- **Preserves shelf:** 2×1 display storage (3 stacks per cell, +8 beauty) fixed-filtered to preserved goods - jam, pickles, cheese, honey, syrup, jerky, dried goods, cured meats, sugar, hardtack (homestead storage research).
- **Nesting box:** a straw-lined animal bed for small animals (body size ≤ 0.6, comfort 0.7).
- **Harvest maypole:** a +12 beauty gathering spot - colonists hold parties and celebrations around it (homestead comforts research).

#### Winter pantry (food preservation)
- **Buildings:** curing rack, hayloft (bulk farm storage), ingredient barrel (meat, milk, eggs).
- **Items:** rock salt, fruit leather, dried mushrooms, salted meat, smoked fish, salted fish, pemmican (via drying rack, no research).
- **Recipes:** fruit leather, dried mushrooms, pemmican, rock salt from chunks, salt-cure meat, smoke fish.
- **Odyssey:** salted fish and smoked fish recipes when the Odyssey DLC is active.

#### From the field (farm-to-table)
- **Buildings:** grain mill, butter churn, pickling crock, homestead hearth (wood-fired oven).
- **Items:** flour, butter, pickled vegetables, hardtack, bread, trail stew, hearty stew.
- **Recipes:** mill flour, churn butter, pickle vegetables, bake bread/hardtack, cook trail stew and hearty stew.

#### Growing settlement (research & progression)
- **Research:** primitive homestead, homestead food preservation, farmstead crafting, homestead storage, advanced homestead, homestead brewing, homestead comforts.
- **Buildings:** root cellar (2×2 food storage, 6 stacks), large storage crate (2×2, 6 stacks), jam cauldron (wood-fired), wood-burning generator (300W, burns wood).
- **Items:** jam (180-day shelf life).

#### Crops & beekeeping
- **Crops:** barley, pumpkin, herb bush and sugar beet as growable plants, with raw barley, raw pumpkin, dried herbs and sugar beet harvest items.
- **Items:** sugar (milled from sugar beets), honey, porridge.
- **Buildings:** beehive (slowly produces honey).
- **Recipes:** cook porridge, mill sugar.

#### Brewing & dairy
- **Drinks:** cider, mead and herbal tea brewed at the new brewing bench (cider and mead are social alcohol, tea is a soothing hot drink).
- **Items:** cream, cheese.
- **Buildings:** brewing bench, cheese press.
- **Recipes:** skim cream, press cheese, brew cider/mead/herbal tea.

#### Furniture, floors & recreation
- **Buildings:** log bench, rocking chair, quilted double bed, oil lamp (refuelable light), wood stove (refuelable heater).
- **Diggo the plushie:** a small chonky hippo-dog plushie sewn from cloth, celebrating a dognamedKats reaching 1k followers. High beauty for its size (scales with quality), sittable for a bit of comfort, and colonists can cuddle it for a little meditative recreation. Buildable from the start and occasionally carried by the homestead supplier caravan.
- **Floors:** wooden deck, straw matting, packed gravel.
- **Joy:** checkers table and washer toss barrel recreation buildings.

#### Apparel
- Straw hat, work apron, overalls, wool poncho.

#### Scenario & flavor
- **Scenario:** "Homesteaders" start — three settlers with farm animals, seeds and tools, but no advanced tech.
- **Backstories:** farmhand, homestead cook and beekeeper backstories (RimWorld 1.5+).
- **Trading:** homestead supplier caravan carrying preserved foods, dairy, drinks and farm goods, added to outlander factions.

#### Art
- Hand-painted textures for all new content: 4 plants, 38 item sprites, 31 buildings (with directional variants for the curing rack, homestead hearth, hayloft, brewing bench, log bench, rocking chair and quilted bed), 4 apparel icons and 3 seamless terrain tiles.
- Regenerated the drying rack (with directional variants), hayloft (with directional variants), preserves shelf, jerky, salted meat and smoked meat sprites in the newer painterly style; docs page images refreshed to match.
- Regenerated the compact battery, battery bank, advanced battery, ultratech battery and portable generator sprites in the newer painterly style; docs page images refreshed to match.

### Changed
- **Workshop preview makeover** — cinematic painted `About/Preview.png` in the Strata style (farmstead + root-cellar cutaway; Grow • Preserve • Power), replacing the old sprite-collage banner.
- Favorites expanded from 1 to **5** per pawn (legacy single favorite migrates and fills remaining slots).
- **Docs site redo** — Homesteader overview and item catalog rebuilt with a dedicated orchard-dusk layout (`docs/homesteader.css`): full-bleed hearth hero, lean feature sections, catalog for the full item list. Shared hub `style.css` left alone for other mods. Site `docs/img` assets for the Homesteader page refreshed from current Homesteader/Wellspring textures (south-facing buildings where available).
- **Kats Effect Super Chat** is a flat **27 silver** (was 27–81). Pay in full or the colony gets nothing taken — unpaid manifests hostile **Kats** (SCP-27272727) at the map edge on an assault lord. Prefers Wolfein race/xenotype when that mod is loaded; otherwise any humanlike pawn (villager/colonist fallback) with orange hair named Kats.
- Replaced empty `Languages/English/.gitkeep` scaffolding with real Keyed files.
- Organized Homesteader sources: C# merged into `PassiveCooling.cs`, `WashEffects.cs`, and `FavoriteFood.cs`; expansion XML renamed into domain files (livestock/soil/textiles/pantry/yard-and-pantry); hediffs/thoughts consolidated; composted soil lives under homestead terrain.
- Passive coolers (root cellar / icehouse / springhouse) now apply the **coolest** overlapping ceiling to a cell instead of an arbitrary cooler’s temp.
- Docs: full item catalog moved to its own page (`docs/homesteader-catalog.html`).
- **Merged Wellspring into Homesteader.** Wells, rain barrels, cisterns, solar stills, water towers, irrigated soil/planters, boiled water, mud bricks, and clean bandages now ship with Homesteader. Research (wellcraft / irrigation / waterworks) lives on the Homesteader tab. Hand-dug and deep wells are on the homestead cooking workgiver. `Wellspring_*` defNames are preserved for save continuity — disable any old standalone Wellspring mod.
- Renamed **shark plushie** to **Sharkira the plushie** (in-game label).
- **Diggo the plushie** — updated hippo-dog sprite art; build cost lowered to **20 cloth** (same as Sharkira).
- Regenerated orchard and beeswax sprites (maple sap, maple syrup, raw apples, raw cherries, beeswax, beeswax candle) to match the painterly Homesteader art style with proper transparent backgrounds.
- Regenerated flapjacks icon to match painterly homestead meal style (maple syrup stack with butter).
- Regenerated salted meat, sugar, and sugar beet icons after batch alpha-clean damaged their white salt/crystal surfaces.
- Regenerated smokehouse (all four facings) and storage barrel building sprites in painterly homestead style (stone foundation, wood walls, brick chimney).
- Batch alpha-cleaned all Homesteader PNGs (buildings, items, apparel, plants) to strip baked white/gray backgrounds, interior voids, and edge halos. Seamless terrain tiles left untouched; flour, cream, porridge, rock salt, salted meat, sugar and ultratech battery used conservative cleanup to preserve intentional white subjects.
- Regenerated flour icon after alpha-clean damaged the white powder surface.

## [1.0.0] — Initial release

### Added
- Storage crate, storage barrel, pallet.
- Drying rack with jerky and dried produce.
- Smokehouse with smoked meat.
- Compact battery, battery bank, advanced battery, ultratech battery, portable generator.
