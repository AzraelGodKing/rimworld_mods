# RimWorld Mods

A collection of RimWorld mods by AzraelGodKing.

## Mods

### Homesteader

Everything a growing homestead needs: tribal survival, farm-to-table crafting, wells and irrigation, storage, food preservation, and off-grid power. Supports RimWorld 1.4 / 1.5 / 1.6. C# assembly for root cellar cooling and favorite foods (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

| Category | Content |
|---|---|
| **Tribal survival** | Curing rack (rock salt, salted meat), drying rack (+ fruit leather, dried mushrooms, pemmican without research), smokehouse, hayloft, ingredient barrel |
| **Farm-to-table** | Grain mill, butter churn (+ buttermilk), pickling crock, homestead hearth (bread, pantry meals, pumpkin pie, biscuits, stews, flapjacks) |
| **Water & irrigation** | Hand-dug / deep wells, rain barrel, cistern, solar still, water tower, irrigated soil & planter, boiled water, mud bricks, clean bandages; optional [Dubs Bad Hygiene](https://steamcommunity.com/sharedfiles/filedetails/?id=836308268) plumbing bridge |
| **Favorites** | Every humanlike rolls a favorite from Homesteader + vanilla foods (+mood when eaten) |
| **Orchard & apiary** | Apple, cherry, and sugar maple trees (regrow after harvest); maple sap → syrup → flapjacks; beehives produce honey + beeswax; beeswax candle |
| **Storage** | Storage crate, storage barrel, pallet, hayloft, ingredient barrel, root cellar (passive cool ≤5°C), large storage crate, preserves shelf |
| **Food preservation** | Jerky, dried produce, fruit leather, salted/smoked meat, pickled vegetables, jam |
| **Around the farmhouse** | Nesting box (small-animal bed), harvest maypole (gathering spot), beeswax candle |
| **Research** | Homestead tree + wellcraft → irrigation / waterworks |
| **Power** | Compact battery (1×1), battery bank (2×2), advanced battery (1×2), ultratech battery (2×2), portable generator (chemfuel), wood-burning generator (wood) |

Odyssey DLC: salted fish and smoked fish recipes load automatically.

Source lives in `Homesteader/Source`. The compiled `Homesteader.dll` is produced by the **Build mod DLLs** GitHub Action and included in the [release download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Homesteader.zip) (Assemblies are gitignored).

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

Source lives in `Strata/Source`. The compiled `Strata.dll` is produced by the **Build mod DLLs** GitHub Action and included in the [release download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Strata.zip) (Assemblies are gitignored; also available under Actions → workflow_dispatch).

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

Source lives in `Stormproof/Source`. The compiled `Stormproof.dll` is produced by the **Build mod DLLs** GitHub Action and included in the [release download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Stormproof.zip) (Assemblies are gitignored).

### Nemesis

A named hostile becomes a personal antagonist — taunts, false leads, sabotage, and targeted assaults that flee when losing. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)). Foundation by **Dredd (Misakabob)**.

- Site: [nemesis.html](https://azraelgodking.github.io/rimworld_mods/nemesis.html)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3773562126) · [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Nemesis.zip)

### Deep Colony

Perk trees, trauma & therapy, apprenticeship, generational inheritance, and living faction reputation. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

- Site: [deep-colony.html](https://azraelgodking.github.io/rimworld_mods/deep-colony.html)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3773568314) · [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/DeepColony.zip)

### Date Night

Adds a Lovin timetable slot to the Schedule tab. Paint the same hours on both partners; they meet in a double bed. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

- Site: [datenight.html](https://azraelgodking.github.io/rimworld_mods/datenight.html)
- [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/DateNight.zip)

### Shift Change

Automatic dress-for-task: Sleep (MVP) walks colonists to a wardrobe stockpile, changes into an apparel policy, and restores previous clothes when the shift ends. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

- Design: [docs/ideas/shift-change.md](docs/ideas/shift-change.md)
- [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/ShiftChange.zip) (after CI publish)

## Installation

A raw git clone has **no** compiled DLLs (they are gitignored). Prefer a packaged zip:

1. Download a mod zip from the [latest GitHub Release](https://github.com/AzraelGodKing/rimworld_mods/releases/latest) or the [GitHub Pages site](https://azraelgodking.github.io/rimworld_mods/).
2. Extract the mod folder into your RimWorld `Mods` directory:
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`
   - Linux: `~/.steam/steam/steamapps/common\RimWorld/Mods`
   - macOS: `~/Library/Application Support/Steam/steamapps/common/RimWorld/Mods`
3. Enable the mod(s) in the in-game mod list.
4. **Homesteader**, **Stormproof**, **Strata**, **Nemesis**, **Deep Colony**, **Date Night**, **Living World**, and **Shift Change** require the **Harmony** mod.

Developers: `dotnet build <Mod>/Source/<Mod>.csproj -c Release` writes the DLL under `<Mod>/Assemblies/` locally.

### Cloud Agents

Cloud Agent environments that set `RIMWORLD_ARCHIVE_URL` can install the game with [`scripts/setup-rimworld.sh`](scripts/setup-rimworld.sh) (Harmony + workspace mod symlinks under `/opt/rimworld/RimWorld/Mods`). Pass `--build-mods` to compile all mod DLLs, or run `dotnet build` yourself after checkout.

Safe to add to an existing save. Before removing a mod from a save, deconstruct its buildings and consume/sell its items first (resolve an active Nemesis hunt before removing Nemesis).

## Changelogs

Repo highlights: [CHANGELOG.md](CHANGELOG.md). Per-mod detail: [Homesteader](Homesteader/CHANGELOG.md), [Stormproof](Stormproof/CHANGELOG.md), [Strata](Strata/CHANGELOG.md), [Nemesis](Nemesis/CHANGELOG.md), [Deep Colony](Deep%20Colony/CHANGELOG.md), [Date Night](DateNight/CHANGELOG.md), [Living World](LivingWorld/CHANGELOG.md), [Shift Change](ShiftChange/CHANGELOG.md).

## Website

Browse the rebuilt docs hub at the [GitHub Pages site](https://azraelgodking.github.io/rimworld_mods/).

- **Direct downloads:** every listed mod has Steam Workshop **and** GitHub release zip buttons (hub `#downloads` + each mod page) for players who cannot use the Workshop.
- **Steam subscriber tracker:** live in-browser via [`docs/scripts/stats-display.js`](docs/scripts/stats-display.js) (roster: [`docs/data/workshop-mods.json`](docs/data/workshop-mods.json); 1h `localStorage` cache). **Not** driven by GitHub Actions — no hourly bot commits to rebase around. [`docs/data/stats-cache.json`](docs/data/stats-cache.json) is a manual offline fallback only (`node scripts/fetch-workshop-stats.js --force`).
- **Admin force refresh:** unlisted [`docs/admin-stats.html`](docs/admin-stats.html) (`noindex`, not in nav). Passphrase hash lives in [`docs/data/admin-gate.json`](docs/data/admin-gate.json) — rotate by replacing `passphraseSha256`. Can download or optionally publish the fallback JSON with a PAT you paste for that session only.
- **Accessibility:** skip link, landmarks, focus styles, keyboardable mobile nav, `prefers-reduced-motion`, and live regions for stats.

## License

Everything in this repository is released under the [MIT License](LICENSE).
