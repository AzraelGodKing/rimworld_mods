# RimWorld Mods

A collection of RimWorld mods by AzraelGodKing.

## Mods

### Homesteader

Grow it, put it by, live off the land. Tribal survival, farm-to-table crafting, wells and irrigation, cool storage, comforts, and off-grid power. RimWorld 1.6. C# for root cellar cooling, pantry, tastes, and favorite foods (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

| Category | Content |
|---|---|
| **Tribal survival** | Drying rack (jerky, fruit leather, dried mushrooms, pemmican without research); curing rack + smokehouse (Primitive homestead); hayloft, ingredient barrel |
| **Farm-to-table** | Grain mill, butter churn (+ buttermilk), cheese press, pickling crock, homestead hearth (pantry meals, pie, biscuits, flapjacks) |
| **Water & irrigation** | Rain barrel → cistern → water tower; hand-dug well → deep well; solar still; irrigated soil & planter; optional [Dubs Bad Hygiene](https://steamcommunity.com/sharedfiles/filedetails/?id=836308268) plumbing bridge |
| **Pantry & tastes** | Pantry tab (stock + What can I make); spoilage triage; preserve crates (trade lot + pantry nutrition/variety); Tastes tab (favorites + hidden-until-reaction allergies); well-stocked larder mood |
| **Orchard & apiary** | Apple, cherry, and sugar maple (seasonal sap yield); maple sap → syrup → flapjacks; beehives (honey + beeswax candles) |
| **Storage** | Crates, barrels, pallet, root cellar (≤5°C), icehouse (≤0°C), springhouse (≤8°C), preserves shelf |
| **Around the farmhouse** | Nesting box, harvest maypole, chicken coop, compost → composted soil, seed saving / landrace crops |
| **Polyarmory** | Soft multi-weapon carry for colonists who want more than one sidearm (toggleable) |
| **Scenario** | Homesteaders (locks Azrael); Azrael storyteller ships here |
| **Research** | Homestead tree + wellcraft → irrigation / waterworks (dedicated Homesteader tab) |
| **Power** | Compact / bank / advanced / ultratech batteries; portable chemfuel and wood-burning generators |

Odyssey DLC: salted and smoked fish recipes load automatically. Optional refresh art (`Textures/HomesteaderRefresh`) is git-only — not in Workshop / release zips.

Steam paste: [Homesteader/SteamDescription.txt](Homesteader/SteamDescription.txt) · [assets/workshop/homesteader-description.bbcode](assets/workshop/homesteader-description.bbcode).

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

Defend your grid from everything the Rim throws at it. RimWorld 1.6 (**1.4.1**). C# mod (requires the [Harmony](https://github.com/pardeike/HarmonyRimWorld) mod). Steam paste: [SteamDescription.txt](Stormproof/SteamDescription.txt).

| Building | What it does |
|---|---|
| **Solar shield** | Idles at 100W; during a solar flare drains 2,500W continuously to keep all electronics running. If the grid runs dry mid-flare, everything goes dark until it ends. |
| **Storm spire** | Attracts lightning within a wide radius (shown on place/select). Grounded: safe fire protection. Grid-connected: each strike stores up to 1,500 Wd, with a 5% chance of a "Zzzt!" surge (eliminated by perfect grounding research). |
| **Storm capacitor bank** | Lightning-only battery: only spire-caught strikes charge it, it never self-discharges, and "Zzzt!" surges can't touch it. Discharges up to 2,000W to cover grid deficits. |
| **Surge protector** | Absorbs one "Zzzt!" short circuit, then recharges for a day. |
| **Weather forecaster** | How long weather holds, incoming storms, hour warning before break; almanac + ledger (strikes, Zzzt, fires snuffed, wear). |
| **Static discharge pylon** | Runs on bottled lightning from capacitor banks: stuns and burns hostiles in a small radius, 50 Wd per shock. |
| **Fallout scrubber** | Strips toxic buildup from pawns and animals sheltering in its enclosed room (room outline on select). |
| **Storm caller** | Summons a rainy thunderstorm on demand — lightning for your spires, rain for your wildfires. Half-day storm, five-day recharge; queues for the next map tick. |
| **EMP dampener** | Colony buildings in range are immune to EMP stuns. |
| **Load shedder** | Automatic breaker plus optional 24-hour schedule / forecast override / Hold Auto·Run·Shed; battery cutoff still trips first. |
| **Grid monitor console** | Live production/consumption/storage with time-to-empty; 8-hour weather-aware forecast when a forecaster shares the net (brownout through Hard); 25% / 10% alarms. |
| **Armored conduit** | Fireproof, high-durability power conduit; immune to storm wear. |
| **Storm vane** | Decorative copper weather vane. Pure class. |
| **Atmospheric barrier** | Map-wide: counters toxic fallout, toxic surge, volcanic ash, and noxious haze while powered. |
| **Climate stabilizer** | Cancels temperature offsets from heat waves, cold snaps, volcanic winters, heat domes, and polar fronts while powered. |
| **Sky restorer** | Map-wide: usable daylight during eclipses, volcanic winters, ash, and darkened skies. |
| **Fire suppressor** | Extinguishes fires in a wide fixed radius (flashstorm / dry lightning); radius shown on place/select. |
| **Drought condenser** | Cancels drought plant-growth penalties map-wide while powered (Odyssey). |

Grid stress: **graded brownout** below 40% battery; **storm wear** on ordinary conduit and batteries. Soft / Default / Hard settings.

Events: **Ion storm** (batteries bleed, EMP bursts, extra Zzzt), **Heat dome**, **Polar front**, **Toxic surge**, and **Dry lightning**. Hazard hardening covers those plus vanilla fallout / heat / cold / eclipse / volcanic winter / flashstorm / drought.

Research: Stormproof → Storm protection → Flare shielding → Atmospheric control → Perfect grounding → Hazard hardening.

Source lives in `Stormproof/Source`. The compiled `Stormproof.dll` is produced by the **Build mod DLLs** GitHub Action and included in the [release download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Stormproof.zip) (Assemblies are gitignored).

### Nemesis

A named hostile becomes a personal antagonist — taunts, sabotage, captain progression after escapes, vengeance army returns, and a dossier for leads. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)). Foundation by **Dredd (Misakabob)**.

- Site: [nemesis.html](https://azraelgodking.github.io/rimworld_mods/nemesis.html)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3773562126) · [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Nemesis.zip)

### Deep Colony

Perk trees, trauma & therapy, apprenticeship, generational inheritance, and living faction reputation. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

- Site: [deep-colony.html](https://azraelgodking.github.io/rimworld_mods/deep-colony.html)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3773568314) · [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/DeepColony.zip)

### Date Night

Schedule romance: Date and Lovin timetable slots, real date activities (dinner, picnic, walk, gifts…), quality, anniversaries, favourite spots, and double dates. Lovin still uses a shared double; private time when alone. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)).

- Site: [datenight.html](https://azraelgodking.github.io/rimworld_mods/datenight.html)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3774158903) · [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/DateNight.zip)

### Niceties

Small colony comforts: well-kept apparel (Poor = vanilla daily wear), throne-room altars, wear any gendered cut, hide cryptosleep from the colonist bar, melee hunting, shared bedrooms, and leave a way out while building. RimWorld 1.6 (requires [Harmony](https://github.com/pardeike/HarmonyRimWorld)). Soft / Default / Hard presets; each toggle is independent. Incompatible with Share Rooms [LWM] and Smarter Construction.

- Site: [niceties](https://azraelgodking.github.io/rimworld_mods/niceties)
- [Download zip](https://github.com/AzraelGodKing/rimworld_mods/releases/latest/download/Niceties.zip)

## Installation

A raw git clone has **no** compiled DLLs (they are gitignored). Prefer a packaged zip:

1. Download a mod zip from the [latest GitHub Release](https://github.com/AzraelGodKing/rimworld_mods/releases/latest) or the [GitHub Pages site](https://azraelgodking.github.io/rimworld_mods/). To roll back, pick an older tag on the [Releases](https://github.com/AzraelGodKing/rimworld_mods/releases) page (`downloads-…` snapshots, or `{Mod}-v{version}` shipped builds).
2. Extract the mod folder into your RimWorld `Mods` directory:
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`
   - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods`
   - macOS: `~/Library/Application Support/Steam/steamapps/common/RimWorld/Mods`
3. Enable the mod(s) in the in-game mod list.
4. **Homesteader**, **Stormproof**, **Strata**, **Nemesis**, **Deep Colony**, **Date Night**, and **Niceties** require the **Harmony** mod. **Living World** and **Azrael** are in-repo but not release-ready (not in the GitHub Release zips).

Developers: `dotnet build <Mod>/Source/<Mod>.csproj -c Release` writes the DLL under `<Mod>/Assemblies/` locally. After the nine builds, `dotnet test Tests/ModChecks/ModChecks.csproj -c Release` checks Harmony string targets, DefOf/defName literals, Scribe parity, Series hub roster, Keyed literals, and def XML fields against the Krafs ref pack (no RimWorld install).

Safe to add to an existing save. Before removing a mod from a save, deconstruct its buildings and consume/sell its items first (resolve an active Nemesis hunt before removing Nemesis).

## Changelogs

Each mod's `About/changelog.txt` is **latest Steam notes only**. Full history is on the [docs site](https://azraelgodking.github.io/rimworld_mods/) changelog section of each mod page (source: `site/src/data/changelogs/`).

Steam paste: [Homesteader](Homesteader/About/changelog.txt), [Stormproof](Stormproof/About/changelog.txt), [Strata](Strata/About/changelog.txt), [Nemesis](Nemesis/About/changelog.txt), [Deep Colony](Deep%20Colony/About/changelog.txt), [Date Night](DateNight/About/changelog.txt), [Azrael](Azrael/About/changelog.txt), [Living World](LivingWorld/About/changelog.txt), [Niceties](Niceties/About/changelog.txt).

## Website

Browse the rebuilt docs hub at the [GitHub Pages site](https://azraelgodking.github.io/rimworld_mods/).

- **Visual language:** frontier workshop — deep slate / ash / ember-copper / bone, Bricolage Grotesque + Source Sans 3 (`site/src/styles/main.css`). Brand-first full-bleed landing hero; mod data stays in `site/src/data/`.
- **Direct downloads:** every listed mod has Steam Workshop **and** GitHub release zip buttons (hub `#downloads` + each mod page) for players who cannot use the Workshop. The rolling [`latest`](https://github.com/AzraelGodKing/rimworld_mods/releases/latest) tag is rebuilt on each relevant push to `main`. Each of those builds also keeps an immutable snapshot (`downloads-YYYY-MM-DD-<sha7>`). Versioned GitHub Releases (`Homesteader-v1.0.2`, …) and optional Nexus uploads are the **Release & Publish** workflow — see [docs/RELEASE.md](docs/RELEASE.md) and [docs/VERSIONING.md](docs/VERSIONING.md).
- **Steam + Nexus tracker:** the Vue hub pulls on every visit ([`site/src/composables/useStats.js`](site/src/composables/useStats.js)). Steam and Nexus APIs block browser CORS, so the reliable numbers come from [`stats/live.json`](https://raw.githubusercontent.com/AzraelGodKing/rimworld_mods/stats/live.json) — refreshed every 15 minutes by [`.github/workflows/live-stats.yml`](.github/workflows/live-stats.yml) with a force-push to the `stats` branch only (never commits on `main`). [`docs/data/stats-cache.json`](docs/data/stats-cache.json) is last-resort offline fallback (`node scripts/fetch-workshop-stats.js --force`). localStorage paints the last snapshot instantly; it does not skip the live pull.
- **Admin force refresh:** unlisted [`docs/admin-stats.html`](docs/admin-stats.html) (`noindex`, not in nav). Passphrase hash lives in [`docs/data/admin-gate.json`](docs/data/admin-gate.json) — rotate by replacing `passphraseSha256`. Can download or optionally publish the fallback JSON with a PAT you paste for that session only (held in memory, not `sessionStorage`).
- **Legacy HTML keep-list (AZR-135):** `admin-stats.html`, unlisted `living-world.html` (until the mod is on the hub), and `homesteader-catalog.html`. `ledger.html` / `signal.html` / `strata-roadmap.html` redirect to Vue routes. Everything else in `docs/*.html` is a stub for an old mod page.
- **Accessibility:** skip link, landmarks, focus styles, keyboardable mobile nav, `prefers-reduced-motion`, and live regions for stats.

## License

Everything in this repository is released under the [MIT License](LICENSE).
