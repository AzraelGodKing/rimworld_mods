# Stormproof compatibility

What Stormproof works with, how, and what happens when the other side is missing. Stormproof 1.7.x is the last 1.6-only line; Stormproof 2.0.0 ships one package for both RimWorld 1.6 and 1.7 (no 1.6 freeze).

"Fail-open" means: if the other mod is missing, older, or something fails to bind, both mods keep running as if the other were not there.

**Last tested in game:** no row has been tested on 2.0.0 yet. The dates fill in as each row is checked on 1.6 and 1.7.

## Sister mods

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Strata | Yes | Yes | Reads `StormproofCompatApi` through `Common/Sibling/StormproofClient.cs`: brownout slows the freight lift; storm surge (rain, lightning weather, ion storm) grows seep floods on levels without a running sump pump; the seismograph shows the surface weather, how long it holds, and the storm surge. Fallout scrubbers treat enclosed underground rooms as indoor. Strata soft-depends on Stormproof (`loadAfter`). | No Stormproof: full lift speed, vanilla seep sizes, no surface lines on the seismograph. | Not yet |
| Nemesis | Yes | Yes | EMP sabotage skips buildings a powered EMP dampener covers (live radius and power, via the compat API). A ready surge protector blocks the sabotage's extra Zzzt. Nemesis loads after Stormproof. | No Stormproof: sabotage hits every powered building, extra Zzzt can fire. | Not yet |
| Deep Colony | Yes | Yes | A colonist downed by EMP, flame or lightning during an ion storm or dry lightning front gets the ion-storm trauma (via the compat API). | No Stormproof: no ion-storm trauma reason. | Not yet |
| Azrael | Yes | Yes | The Azrael storyteller weights Stormproof incident picks: ion storm and dry lightning ×1.5, heat dome, polar front and toxic surge ×1.25 (`Azrael_SeriesIncidentWeights`). This stacks with Stormproof's incident frequency slider; the settings show the combined rate. The series hub shows the Strata and Stormproof storm-rooms bridge. Azrael loads after Stormproof. | No Azrael: only Stormproof's own slider applies. | Not yet |
| Homesteader | Yes | Yes | No code link. Its batteries and generators work on Stormproof grids like vanilla ones. The Stormfront scenario locks the Azrael storyteller only when Homesteader is loaded (Homesteader provides the forced-storyteller scenario part). | No Homesteader: Stormfront lets you pick any storyteller. | Not yet |

## DLCs

| DLC | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Biotech | Yes | Yes | The atmospheric barrier also guards against Noxious Haze. Optional setting: the EMP dampener covers player mechanoids. | No Biotech: those parts are skipped; the mech setting is hidden. | Not yet |
| Odyssey | Yes | Yes | The atmospheric barrier covers Volcanic Ash, the sky restorer Volcanic Ash and Darkened Skies, the drought condenser Drought, and the climate stabilizer Deep Freeze. | No Odyssey: those conditions are skipped. A missing target with Odyssey active logs one warning. | Not yet |
| Royalty, Ideology, Anomaly | Yes | Yes | No integration needed. | n/a | Not yet |

## Libraries and others

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Harmony | Required | Required | Every patch class is applied on its own (`SafePatchAll`), so one broken patch does not take the others down. The count of applied patches is in the Player.log version line. | A failed patch logs an error and its feature stops; the rest keep working. | Not yet |
| Multiplayer | Not supported | Not supported | No sync markers. | Do not use in a Multiplayer game. | n/a |

## For modders

`Stormproof.StormproofCompatApi` (API version 2) is the supported surface. Resolve it by name, check `ApiVersion >= 2`, and bind delegates once. `Common/Sibling/StormproofClient.cs` in this repository is a working client. Members:

- `BrownoutFor(Thing)`: brownout on the thing's grid, 0 to 1.
- `IsEmpProtected(Thing)`: a powered EMP dampener covers it now.
- `MapHasReadySurgeProtector(Map)`: the next Zzzt would be absorbed.
- `DampenerRadius`: the dampener's cover radius from its def.
- `StormSurgeIntensity(Map)`: 0 to 1 from rain, lightning weather and ion storms. Pass the surface map.
- `ForecastRemainingTicks(Map)`: ticks until the weather may change, or -1.
- `WeatherBringsLightning(Map)`: the current weather makes lightning. RimWorld picks the next weather only when this one ends, so nothing further ahead is known.
- `IonStormActive(Map)`: ion storm or dry lightning front on the map.
- `IonStormStarted` / `IonStormEnded`: static `Action<Map>` events, once per affected map.
