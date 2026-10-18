# Wave Survival Reforged

A RimWorld 1.6 rebuild of [Wave Survival [Game Mode]](https://steamcommunity.com/sharedfiles/filedetails/?id=732261506) by Justin C, last built for Alpha 14. Storyteller art by [Philbro](http://philbro.deviantart.com/), reused with credit. The original shipped only a compiled DLL, so this is a fresh rewrite of the same design against the 1.6 API.

## What it does

Pick the **Wave Survival** storyteller (the **Wave Survival** scenario is the intended start).

- Random threats are off. Raid waves come on a timer: the first after 5 days, then every 2 days, landing 6 hours after your landing time of day.
- Each wave has 50 more threat points than the last (the increase is scaled by difficulty). 25% of normal waves split into two half-size raids.
- Every 4th wave is special: a siege, sappers, a manhunter pack, or mass animal insanity. If no hostile faction can do a siege or sappers, it falls back to a normal raid.
- Once a day, pods drop supplies at the orbital trade beacon: resources by chance (each stops once you hold enough), survival meals whenever food runs low, and sometimes a weapon priced to the wave number.
- Daily chances of an orbital trader, and of a wanderer joining a small colony.
- Colonists get a +35 "still alive" mood buff.
- Alerts count down to the next wave (red in the last few hours) and remind you to build a beacon.

The vanilla endgame quests and deep-drill infestations still run. Diseases start from day 0, as in the original.

## Tuning

Every number lives in `Defs/WaveSurvivalConfig.xml` (`WSR_Config`). The supply list there is `supplyDrops`: each entry has a `thing`, a daily `chance` (0 to 1), a `count` range, and a `maxStockpile` cap.

## Code layout

| File | Role |
|------|------|
| `Source/WaveSurvivalComponent.cs` | Game component: wave timer, special waves, daily drops, traders and wanderers. Saves its state. |
| `Source/WaveSurvivalConfigDef.cs` | The `WSR_Config` def type. |
| `Source/Alert_WaveTimer.cs`, `Source/Alert_NeedTradeBeacon.cs` | The two alerts. |
| `Source/ThoughtWorker_StillAlive.cs` | The mood buff. |

Everything only runs while the Wave Survival storyteller is selected, so switching storyteller mid-game pauses the waves. No Harmony.

## Build

```
dotnet build WaveSurvivalReforged/Source/WaveSurvivalReforged.csproj -c Release
```

Output goes to `WaveSurvivalReforged/1.6/Assemblies/` (gitignored).
