# Nemesis compatibility

This page lists what Nemesis 2.0.0 works with, how each integration works, and what happens when the other mod or DLC is missing. Nemesis 2.0.0 ships one package for both RimWorld 1.6 and 1.7, with no 1.6 freeze.

"Fail-open" means that if the other mod is missing, out of date, or fails to bind, Nemesis keeps running as if that mod were not there. Nemesis has no hard dependency except Harmony.

**Last tested in game:** no row has been tested on 2.0.0 yet. Each status will change from "Untested" to the date it was checked, on 1.6 and on 1.7 separately.

## Sister mods

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Stormproof (`AzraelGodKing.Stormproof`) | Untested | Untested | Through `StormproofCompatApi` (shared `StormproofClient`): EMP sabotage skips buildings covered by a powered dampener, a ready surge protector blocks the extra short circuit, and Casual-voice taunts mention ion storms. **Ion-storm baiting:** while an ion storm runs on the home map, an aggressive nemesis (3+) moves within a few hours, once per storm, and weights raids and assaults over sabotage. Setting: **Strike during ion storms**. | Without Stormproof, sabotage hits every building and no storm baiting happens. | Not yet |
| Strata (`AzraelGodKing.Strata`) | Untested | Untested | Typed delegates bound once to `StrataModderApi.IsUnderground`, `IsUpperLevel` and `ReachableMaps`. Harassment targets the surface home map, not a cellar or upper floor. "Too many hostiles here, wait" counts hostiles on every linked level, so a deep raid in the cellars also holds the nemesis back. | Without Strata, only the home map counts. | Not yet |
| Homesteader (`AzraelGodKing.Homesteader`) | Untested | Untested | `FavoriteFoodUtility.GetFavorites` (typed delegate) for food-raid and taunt flavour; Homesteader pantry, cellar and smokehouse buildings count as food storage for food raids. | Without Homesteader, food raids hit any stored meals. | Not yet |
| Deep Colony (`azraelgodking.DeepColony`) | Untested | Untested | Hunt epitaphs are copied to Deep Colony's Legacy letters through its public `FamilyLetterUtility.RecordNote` (no letter popup), so Deep Colony applies its own trim and save rules. Nemesis never writes Deep Colony state directly. | Without Deep Colony, epitaphs stay in the Nemesis dossier only. | Not yet |
| Living World (`azraelgodking.livingworld`) | Untested | Untested | Registers a handler on Living World's soft event bus (`LivingWorldCompatApi.Register`, else `LivingWorldSignals.Register`) and reads ground truth (`trueKind`). **Collapse** of a hunt's faction: aggression +2 and a move within a day, or with the setting off, the hunt goes quiet for 15 days. **DecisiveVictory:** a winning hunt faction adds +0.5 aggression; a losing one delays the next move by a day. Hunt camps use the `Nemesis_HuntSite` world object def, and Living World keeps its war sites off any tile with a "Nemesis" world object. | Without Living World, nothing registers. | Not yet |
| Azrael (`azraelgodking.Azrael`) | Untested | Untested | The hub reads `GameComponent_Nemesis.Instance`, `IsEngaged`, `FindNemesisPawn()` and `NemesisBuildInfo.BuildStamp`. For new code use `NemesisCompatApi` (below), including `LastHuntThreatTick` to space big threats around nemesis raids. | Without Azrael, nothing changes. | Not yet |
| Rimesis, Back For Vengeance | Untested | Untested | A pawn carrying a Rimesis or BFV hediff is never made a nemesis, so two antagonist mods never claim the same pawn. `NemesisCompatApi.WouldClaim` and `ShouldReportMissingToRimesis` are there for them. The Rimesis leader-raid handoff (AZR-60) waits on a public Rimesis hook. | No markers found means no exclusion. | Not yet |

## DLCs

| DLC | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Biotech | Untested | Untested | Mechanitor-focus captains bring 1-3 mechs (setting **Soft mech and warcasket escorts**); waste pack drops. | Without Biotech, no mech escorts or waste drops, and the Mechanitor focus is never rolled. | Not yet |
| Ideology | Untested | Untested | Slave rebellions can start a hunt. | Without Ideology, that trigger never fires. | Not yet |
| Anomaly | Untested | Untested | Anomaly bait action at aggression 4+. | Without Anomaly, its weight is 0. | Not yet |
| Odyssey | Untested | Untested | **Shuttle drop and extract:** a captain from an industrial or higher faction lands by shuttle 40% of the time on an assault, the shuttle waits at the landing spot, and the captain flies out on it when the raid collapses. Setting: **Shuttle drops**. | Without Odyssey, or if the shuttle cannot land, the captain walks in from the map edge. | Not yet |
| Royalty | n/a | n/a | No integration needed. | n/a | n/a |

## Other mods

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Harmony (`brrainz.harmony`) | Required | Required | Each patch class is applied on its own (`SafePatchAll`). All `Pawn.Kill` hooks are one prefix and one postfix. Patches bind game arguments by position, checked against the 1.6.4871 and 1.7.4998 reference assemblies. | A failed patch logs an error and only its feature stops. | Not yet |
| Vanilla Factions Expanded - Pirates (`OskarPotocki.VFE.Pirates`) | Untested | Untested | A level 5+ Destroyer or Survivor captain brings one warcasket (any humanlike pawn kind with "Warcasket" in its defName, preferring their own faction's). | Without VFE Pirates, no warcasket. | Not yet |
| Giddy-Up | Untested | Untested | Mount escorts are plain faction animals that fight beside the captain; with Giddy-Up they are better mounts. | Without it, the animal still fights. | Not yet |

## Multiplayer

**Soft support, untested.** Nemesis does not ship or reference the Multiplayer API DLL. When the Multiplayer mod (`rwmt.Multiplayer`) is active, Nemesis registers these sync methods by reflection at startup (`NemesisMultiplayer`):

- `NemesisPlayerCommand.BuyLead` and `PostBounty`: the dossier's informant buttons.
- `NemesisPlayerCommand.Reply`: comms console replies (taunt back, offer truce, demand surrender). The colonist walks to the console as a vanilla job first; jobs are synced by Multiplayer itself.
- `NemesisPlayerCommand.Resolve`: the capture dialog's choice.

The hunt health check runs on a fixed 120-tick cadence, never on the map a client is looking at. The Living World handler has no random rolls. Dev-mode debug actions are not synced.

**Host config sync is required.** Every player must use the host's Nemesis settings. Nemesis does not copy them for you.

**To do:** a two-client desync test covering a hunt start, an assault with retreat, a comms reply, a bought lead and a capture.

## Balance

Presets in the settings: **Relentless**, **Default** and **Slow-burn**. A new hunt cannot start within **Days between new hunts** of the last one, and at most **Hunts at once** run (one per faction), which also caps how many triggers one bad raid can turn into hunts. Setting an action's weight to 0 turns that action off.

**To do:** a trigger-rate playtest over 3+ in-game years per preset, with the numbers recorded in AZR-572.

## For modders

`Nemesis.NemesisCompatApi` (`ApiVersion` 2) is the supported surface. Resolve it by name, check `ApiVersion >= 2`, and bind delegates once. Members: `HasActiveHunt`, `EngagedHuntCount`, `ActiveNemesisPawn`, `EngagedNemesisPawns()`, `IsNemesisPawn(Pawn)`, `WouldClaim(Pawn)`, `ShouldReportMissingToRimesis(Pawn)`, `LastHuntThreatTick`, `ClearAllHunts()`.
