# Changelog

All notable changes to Homesteader are documented here.

## [Unreleased]

### Fixed
- Bread, porridge, trail stew and hearty stew now inherit from the correct vanilla meal base (`MealBase` instead of the non-inheritable `MealSimple`/`MealFine` defNames), restoring their thingClass, food type, rot ticking and eat sounds/effects.
- Homestead supplier caravan patch now targets the `OutlanderCivil` def node directly; the old xpath failed because `caravanTraderKinds` only exists on the abstract outlander base def.
- Homesteaders scenario: corrected the starting pawns config page def name (`ConfigPage_ConfigureStartingPawns`) and added the RimWorld 1.6 `surfaceLayer` block. The scenario now ships as version-specific copies (`1.6` and `Legacy` load folders) so 1.4/1.5 remain supported.
- Checkers and washer toss joy givers now declare the joy kind matching their vanilla jobs (Gaming_Cerebral / Gaming_Dexterity) and require manipulation.
- Beehive uses the `Building` thing class so it can be deconstructed and minified without config errors.
- Removed duplicate thing categories inherited from parents (log bench, rocking chair, quilted bed: BuildingsFurniture; cider, mead: Drugs).
- Root cellar no longer lists thing categories, since it is intentionally not minifiable.
- Honey is now flagged with `socialPropernessMatters`, preventing warden food-delivery loops in prison cells.

### Added

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

## [1.0.0] — Initial release

### Added
- Storage crate, storage barrel, pallet.
- Drying rack with jerky and dried produce.
- Smokehouse with smoked meat.
- Compact battery, battery bank, advanced battery, ultratech battery, portable generator.
