# Changelog

All notable changes to Wellspring are documented here.

## [Unreleased]

### Added
- Deep well: a reinforced work bench well that draws 12 water per bill. Requires new waterworks research.
- Solar still: passively condenses 2 water every half-day to day, even where it never rains. Requires wellcraft research.
- Water tower: 2×2 elevated storage holding 16 stacks of water, protected from deterioration. Requires waterworks research.
- Stone fountain: a 2×2 stuffable stone centerpiece (+30 beauty) that costs 10 water to fill. Requires wellcraft research.
- Irrigated planter: a 1×1 self-watering plant grower (130% fertility) for normal ground crops, works indoors. Requires irrigation research.
- Boiled water: a humble hot drink boiled at any stove or campfire for a little joy.
- Mud bricks: adobe made from water and hay at the crafting spot or stonecutter's table. A cheap stony stuff for walls, floors and furniture - quicker to build with than cut stone, but weaker.
- Clean bandages: cloth boiled in water, giving 70% potency medicine craftable at the crafting spot or tailor benches.
- Waterworks research project (medieval) gating the deep well and water tower.

### Changed
- Regenerated all five textures (hand-dug well, rain barrel, cistern, water jug, irrigated soil) in the painterly style used by Homesteader and Stormproof; irrigated soil tile upscaled to 512×512.
- Batch alpha-cleaned all Wellspring PNGs (buildings and items) to strip baked white/gray backgrounds and edge halos. Seamless terrain tiles left untouched; boiled water and clean bandages used conservative cleanup to preserve intentional white subjects.
- Regenerated boiled water and clean bandages icons using background-only alpha processing after aggressive cleanup punched holes in light stone and cream cloth surfaces.
- Regenerated boiled water jug sprite with an open rope-handle loop so the interior gap stays transparent.

## [1.0.0] — Initial release

### Added
- Water: a heavy jug resource drawn from wells and rain barrels, trenched into cropland to irrigate it.
- Hand-dug well: a work bench where colonists draw 5 water per bill of honest labor. Requires wellcraft research.
- Rain barrel: passively collects 4 water every 1–2 days. Requires wellcraft research.
- Cistern: 2×1 covered storage holding 8 stacks of water (4 per cell), protected from deterioration. Requires wellcraft research.
- Irrigated soil: buildable terrain (+30% fertility, 3 water + work per tile) that can only be laid over ground that could already grow crops. Requires irrigation research.
- Wellcraft and irrigation research projects (both neolithic).

### Roadmap
- Fluid pipe system: pipes, powered pump and sprinkler (industrial tier, will add a C# assembly).
- Drought and flash-flood events that make water infrastructure matter.
- Animal drinking troughs and hand-watering.
