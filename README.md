# RimWorld Mods

A collection of RimWorld mods by AzraelGodKing.

## Mods

### Homesteader

Everything a growing homestead needs: tribal survival, farm-to-table crafting, storage, food preservation, and off-grid power. Supports RimWorld 1.4 / 1.5 / 1.6. Fully XML-based — no C# assembly.

| Category | Content |
|---|---|
| **Tribal survival** | Curing rack (rock salt, salted meat), drying rack (+ fruit leather, dried mushrooms, pemmican without research), smokehouse, hayloft, ingredient barrel |
| **Farm-to-table** | Grain mill, butter churn, pickling crock, homestead hearth (bread, hardtack, trail stew, hearty stew, maple syrup, flapjacks) |
| **Orchard & apiary** | Apple, cherry, and sugar maple trees (regrow after harvest); maple sap → syrup → flapjacks; beehives produce honey + beeswax; beeswax candle |
| **Storage** | Storage crate, storage barrel, pallet, hayloft, ingredient barrel, root cellar, large storage crate, preserves shelf |
| **Food preservation** | Jerky, dried produce, fruit leather, salted/smoked meat, pickled vegetables, jam |
| **Around the farmhouse** | Nesting box (small-animal bed), harvest maypole (gathering spot), beeswax candle |
| **Research** | Primitive homestead → food preservation / farmstead crafting / homestead storage → advanced homestead |
| **Power** | Compact battery (1×1), battery bank (2×2), advanced battery (1×2), ultratech battery (2×2), portable generator (chemfuel), wood-burning generator (wood) |

Odyssey DLC: salted fish and smoked fish recipes load automatically.

### Strata (experimental)

Dig down and build a true multistory base — one home, many floors. RimWorld 1.6. C# mod (requires the [Harmony](https://github.com/pardeike/HarmonyRimWorld) mod).

Build an **excavated stairwell** and a new underground level opens beneath your base: a solid stratum of mineable rock under a thick roof, ready to be carved into bedrooms, workshops, and freezers. Build another stairwell down there and keep going deeper.

What makes it different from older multi-level mods is the **fluidity engine**: instead of treating each floor as an island, Strata relays colonists between levels on their own —

| Relay | Behavior |
|---|---|
| **Work** | Idle colonists notice mining/plant designations, blueprints, hauling, and bills on other levels and commute down (or up) the stairs to do the work |
| **Food** | Hungry pawns go find a meal on another level instead of starving next to a staircase |
| **Rest** | Sleepy pawns walk home to their own bed — or to any level with a free bed to claim |
| **Haul** | Items with no storage on their level get carried through the stairwell to a level whose stockpiles accept them |

And a touch of real physics: **heat rises, cold falls**. Stairwells exchange temperature between the rooms at their top and bottom — a warm level below convects heat upward quickly, while a warmer level above only bleeds down slowly. Put your freezer downstairs and your generators' heat will drift up, not in.

**Events know which way is up.** Underground levels are sealed rock, so the sky can't touch them — solar flares, eclipses, toxic fallout, weather, and drop-pod raids are all suppressed down there. But hiding doesn't end a fight: **raiders with nobody left to shoot at will find your stairwells and pursue** your colonists down (or up) — a **sealed** stairwell stops them cold. What else can reach you is what lives in the dark: infestations become the signature threat of the deep, alongside **cave-ins**, **gas pockets**, the occasional lucky **deep vein** of ore, and rare **burrowing raiders** who tunnel up through the stone — because nowhere is *completely* safe. On the surface, a wandering **prospector** may tip you off to a rich seam, and **ground tremors** remind you the rock below is never quite still.

**The deeper you dig, the stranger it gets.** Levels sit on a geothermal gradient — deeper is warmer — and deeper levels crawl with more bugs and hide richer ore. **Seal a stairwell** to wall a gas pocket or an infestation onto one level. Prefer a tidier shaft? Research the powered **elevator**: compact 1×1 vertical transport that needs power to descend, but always lets colonists ride back up if the power fails, so no one is ever stranded below. Empty levels quietly throttle their background simulation so a tall base doesn't tank your framerate.

To move goods between levels, just put a stockpile near the stairs on each floor — your colonists haul up and down through the stairwell on their own.

**Power runs up and down the shafts too.** An **elevator** automatically ties the two levels' power grids together, and a researched **shaft power conduit** does the same beside any stairwell — so your surface generators can light the deep (and a level's spare power can flow back up). It shares surplus to whichever level runs short, up to a cap; keep a battery on each level for it to pool across.

**Burners breathe.** Wood-fired and chemfuel generators, **campfires, torches, and open fires** all give off combustion fumes that pool in enclosed rooms as a visible **black smog** that thickens the longer they burn. Run one in a sealed room — or anywhere underground — and colonists start choking (a worsening *smoke inhalation* hediff that's fatal if ignored). The fix is airflow: an open roof or a door to the outdoors vents it for free on the surface; a powered **exhaust fan** or **duct run** clears a room underground; and **smoke rises through open stairwells and elevators** — fumes in the landing room convect upward to the level above (sealing the shaft stops it). Build an **updraft filter** in the stairwell room for a powered chimney boost. Solar, wind, geothermal, and batteries burn nothing, so they stay clean — the safe way to power a deep base. Toggle **"show smoke levels"** in the bottom-right play settings to read the exact smoke percentage under your cursor.

Design: each level is a real map linked by stairwell pairs (built on the vanilla 1.6 pocket-map/portal system), and the AI patches move the *pawn* to the level where it's needed rather than building fragile cross-map jobs — vanilla AI takes over the moment they arrive, so every failure mode degrades safely.

Source lives in `Strata/Source`; the compiled `Strata.dll` ships in `Strata/Assemblies` and is rebuilt by CI on every source change.

### Stormproof

Defend your grid from everything the Rim throws at it. RimWorld 1.6. C# mod (requires the [Harmony](https://github.com/pardeike/HarmonyRimWorld) mod).

| Building | What it does |
|---|---|
| **Solar shield** | Idles at 100W; during a solar flare drains 2,500W continuously to keep all electronics running. If the grid runs dry mid-flare, everything goes dark until it ends. |
| **Storm spire** | Attracts lightning within a wide radius. Grounded: safe fire protection. Grid-connected: each strike stores up to 1,500 Wd, with a 5% chance of a "Zzzt!" surge (eliminated by perfect grounding research). |
| **Storm capacitor bank** | Lightning-only battery: only spire-caught strikes charge it, it never self-discharges, and "Zzzt!" surges can't touch it. Discharges up to 2,000W to cover grid deficits. |
| **Surge protector** | Absorbs one "Zzzt!" short circuit, then recharges for a day. |
| **Weather forecaster** | Shows how long the current weather will hold, announces incoming thunderstorms, and warns an hour before the weather breaks. |
| **Static discharge pylon** | Runs on bottled lightning from capacitor banks: stuns and burns hostiles in a small radius, 50 Wd per shock. |
| **Fallout scrubber** | Strips toxic buildup from pawns and animals sheltering in its enclosed room. |
| **Storm caller** | Summons a rainy thunderstorm on demand — lightning for your spires, rain for your wildfires. Five-day recharge. |
| **EMP dampener** | Colony buildings in range are immune to EMP stuns. |
| **Load shedder** | Automatic breaker: sheds a low-priority sub-grid when supply batteries drop below an adjustable cutoff, reconnects when they recover. |
| **Grid monitor console** | Live production/consumption/storage readout with time-to-empty estimates; warns at 25% battery, alarms at 10%. |
| **Armored conduit** | Fireproof, high-durability power conduit. |
| **Storm vane** | Decorative copper weather vane. Pure class. |

New event — **Ion storm**: batteries bleed charge, random EMP bursts stun powered buildings, and extra "Zzzt!" surges fire. EMP dampeners, surge protectors, and storm capacitor banks counter it.

Source lives in `Stormproof/Source`; the compiled `Stormproof.dll` ships in `Stormproof/Assemblies` and is rebuilt by CI on every source change.

### Wellspring

Water for the Rim: dig wells, catch the rain, and irrigate your fields. Supports RimWorld 1.4 / 1.5 / 1.6. Fully XML-based — no C# assembly.

| Content | What it does |
|---|---|
| **Hand-dug well** | Work bench where colonists draw 5 water per bill. |
| **Rain barrel** | Passively collects 4 water every day or two. |
| **Cistern** | 2×1 covered tank holding 8 stacks of water, protected from deterioration. |
| **Irrigated soil** | Buildable terrain (+30% fertility, 3 water + labor per tile); only lays over ground that could already grow crops. |
| **Research** | Wellcraft (well, barrel, cistern) → irrigation (irrigated soil) — both neolithic. |

Pairs naturally with Homesteader and Stormproof, requires neither. Roadmap: powered pumps, sprinklers, drought and flash-flood events, drinking troughs.

## Installation

1. Download / clone this repository.
2. Copy the mod folder(s) into your RimWorld `Mods` directory:
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`
   - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods`
   - macOS: `~/Library/Application Support/Steam/steamapps/common/RimWorld/Mods`
3. Enable the mod(s) in the in-game mod list.
4. **Stormproof** and **Strata** also require the **Harmony** mod.

Safe to add to an existing save. Before removing a mod from a save, deconstruct its buildings and consume/sell its items first.

## Website

Browse the full mod catalog at the [GitHub Pages site](https://azraelgodking.github.io/rimworld_mods/).
