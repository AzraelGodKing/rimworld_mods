# Wave Survival Reforged

A RimWorld 1.6 and 1.7 rebuild of [Wave Survival [Game Mode]](https://steamcommunity.com/sharedfiles/filedetails/?id=732261506) by Justin C, last built for Alpha 14. Storyteller art by [Philbro](http://philbro.deviantart.com/), reused with credit. The original shipped only a compiled DLL, so this is a fresh rewrite of the same design.

## Versions

- **1.0.0**: first release, RimWorld 1.6 (Workshop 3821001515).
- **2.0.0**: ships for RimWorld 1.6 and 1.7 from one source. There is no frozen 1.6 build: both folders get every update (decided 2026-11-07, AZR-592). Saves from 1.0 load into 2.0.

Multiplayer is not supported.

## What it does

Pick the **Wave Survival** scenario (it locks the storyteller), or the **Wave Survival** storyteller with any scenario.

- Random threats are off. Raid waves come on a timer: the first after 5 days, then every 2 days, landing 6 hours after your landing time of day. Special and boss waves are followed by half a day's breather.
- Threat points follow `pointsByWave` (50 at wave 1, +50 a wave to wave 10, steeper later), scaled by the difficulty's threat scale.
- Every 4th wave is **special** and every 10th a **boss** wave. Each wave picks a type from its pool (see `Defs/WaveTypes.xml`) and a hostile faction able to carry it out.
- The next wave is **pre-rolled** when it is scheduled and saved. The alert and a HUD at the top of the screen show its type, faction and points, and that plan is what runs. If its faction can no longer attack, another faction takes it.
- A wave is announced only once it spawns. A failed special wave falls back to a plain raid, then a manhunter pack; if nothing can run, the wave waits `postponeHours` and keeps its number.
- After each wave, a letter reports how many enemies were killed, downed and fled.
- Once a day, pods drop supplies at the orbital trade beacon: resources by chance (each stops at its `maxStockpile`, counting loose items near the beacon), survival meals whenever food runs low, and sometimes a weapon priced to the wave number. After `noBeaconFallbackDays` without a beacon, a half-size drop lands near the colony.
- Daily chances of an orbital trader (only with a working comms console), and of a wanderer joining a small colony.
- Colonists get a "still alive" mood buff (+35 on Standard).
- Run stats (waves, kills, colonists lost, drops, days) and, when the colony falls, a letter and an entry on a local best-runs list.
- On RimWorld 1.7, a screen-edge glow builds before each wave and flashes on arrival.

The vanilla endgame quests and deep-drill infestations still run. Diseases start from day 0, as in the original.

## Settings

- **Preset**: Casual, Standard, Brutal or Custom. Copied into the save when a run starts (`RunSettings`), so changing it later never changes a run in progress. Standard is exactly the XML config.
- **Show the wave HUD**.
- **Wave screen effect** (1.7 only), for photosensitive players.
- The best-runs list, with a button to clear it. It lives in the mod settings file and gameplay never reads it.

## Tuning

Every number lives in `Defs/WaveSurvivalConfig.xml` (`WSR_Config`); `ConfigErrors` flags out-of-range values. The supply list there is `supplyDrops`: each entry has a `thing`, a daily `chance` (0 to 1), a `count` range, and a `maxStockpile` cap.

Wave types are `WaveSurvivalReforged.WaveTypeDef`s in `Defs/WaveTypes.xml`:

| Field | Meaning |
|------|------|
| `pool` | `Normal`, `Special` or `Boss` |
| `kind` | `Raid` (a hostile faction's raid) or `Incident` (any incident, named in `incident`) |
| `raidStrategy`, `arrivalMode` | Raid strategy and arrival defNames |
| `factions` | FactionDef names allowed; empty means any hostile, non-hidden faction |
| `minWave`, `weight` | First wave it can appear in, and pick weight |
| `pointsFactor`, `bonusPoints` | Points multiplier, and flat bonus (scaled by threat scale) |
| `canSplit` | May come as two half-size raids (`doubleRaidChance`) |

Game defs are named by string and resolved quietly: a type whose incident, strategy, arrival mode or factions are not loaded is simply skipped (dev mode logs which). DLC types use `MayRequire`. Add a wave type by adding or patching a def.

## Code layout

| File | Role |
|------|------|
| `Source/WaveSurvivalComponent.cs` | Game component: timer, run settings and stats, beacon cache, save data and upgrade. |
| `Source/WaveSurvivalComponent.Waves.cs` | Planning and running waves, points, fallbacks. |
| `Source/WaveSurvivalComponent.Supplies.cs` | Daily drops, weapons, traders, wanderers. |
| `Source/WaveTypeDef.cs`, `Source/WavePlan.cs` | Wave types and the saved pre-rolled wave. |
| `Source/WaveTracker.cs` | Follows a wave's enemies and sends the wave report. |
| `Source/RunSettings.cs`, `Source/RunStats.cs` | Presets copied into the save; run stats and best-run records. |
| `Source/WaveSurvivalMod.cs` | Mod settings page. |
| `Source/WaveHud.cs`, `Source/Alert_*.cs` | HUD and alerts. |
| `Source/RimWorld17/WaveVignette.cs` | 1.7-only screen effect; GLSL in `1.7/Shaders`, ShaderDef in `1.7/Defs`. |
| `Source/ScenPart_WaveSurvivalStoryteller.cs` | Locks the scenario to the Wave Survival storyteller. |
| `Source/Thought_StillAlive.cs`, `Source/ThoughtWorker_StillAlive.cs` | The mood buff. |
| `Source/WaveSurvivalReforgedBuildInfo.cs` | Build stamp for the Azrael hub. |

Everything only runs while the Wave Survival storyteller is selected, so switching storyteller mid-game pauses the waves. No Harmony.

## Build

```
dotnet build WaveSurvivalReforged/Source/WaveSurvivalReforged.csproj -c Release
dotnet build WaveSurvivalReforged/Source/WaveSurvivalReforged.1.7.csproj -c Release
```

Output goes to `WaveSurvivalReforged/1.6/Assemblies/` (net472, 1.6 refs) and `WaveSurvivalReforged/1.7/Assemblies/` (.NET 10, 1.7 refs). Both are gitignored, so rebuild both before any Workshop upload; the 1.7 build needs the .NET 10 SDK (or use the CI build's DLL).
