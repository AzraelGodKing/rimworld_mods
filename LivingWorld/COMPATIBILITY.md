# Living World compatibility

This page lists what Living World 1.0.0 works with, how each integration works, and what happens when the other mod or DLC is missing. Living World 1.0.0 is the first public release and ships one package for both RimWorld 1.6 and 1.7.

"Fail-open" means that if the other mod is missing, out of date, or fails to bind, Living World keeps running as if that mod were not there. Living World has no hard dependency except Harmony.

**Last tested in game:** no row has been tested on 1.0.0 yet. Each status will change from "Untested" to the date it was checked, on 1.6 and on 1.7 separately.

## Sister mods

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Deep Colony | Untested | Untested | Live. Deep Colony registers a goodwill consumer on `LivingWorldSignals` and reacts to decisive victories, betrayals and refugee flights you heard about. Handlers now always get ground truth, so a distorted rumour no longer moves goodwill the wrong way. Planned: move Deep Colony from field reflection to `LivingWorldCompatApi`. | Without Deep Colony, nothing listens. | Not yet |
| Azrael (storyteller and series hub) | Untested | Untested | Live. Azrael's series incident weights boost refugees (×6) and warbands (×5). The hub shows the Living World ↔ Deep Colony bridge; `LivingWorldCompatApi.IsHandlerRegistered` replaces its read of the private `handlers` field (the field keeps its name for older hub builds). | Without Azrael, base incident chances apply. | Not yet |
| Nemesis | Untested | Untested | War sites skip tiles that hold a Nemesis world object (matched by name). Planned (Nemesis N3): listen for `Collapse` and `DecisiveVictory` to end or escalate a hunt. | Without Nemesis, war sites use any free tile. | Not yet |
| Homesteader | Untested | Untested | The Listening Post scenario locks the Azrael storyteller when Homesteader is loaded. Planned (HS-S01): flavor lines on `FamineRumor` and `RefugeeFlight`. | Without Homesteader, the scenario uses your storyteller pick. | Not yet |
| Stormproof | n/a | n/a | No integration yet. | n/a | n/a |
| Strata | n/a | n/a | No integration needed. | n/a | n/a |

## DLCs

| DLC | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Royalty | Untested | Untested | The Empire (any faction with royal titles) is protected: wars can take its settlements but never its last one. | Without Royalty, no faction is protected by titles. | Not yet |
| Odyssey | Untested | Untested | Chronicle entries, moods and fallout remember the planet layer of their tile, so jump-to-tile works on orbit layers and distances are only measured within one layer. Inter-settlement caravans travel between surface settlements only. | Without Odyssey, everything is on the surface. | Not yet |
| Ideology, Biotech, Anomaly | n/a | n/a | No integration needed. | n/a | n/a |

## Other mods

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Harmony (`brrainz.harmony`) | Required | Required | Each patch class is applied on its own (`SafePatchAll`): settlement label and inspect text, the trader-caravan blackout, and the way-camp rest bonus. | A failed patch logs an error and only its feature stops. | Not yet |
| Mods that add factions | Untested | Untested | Any humanlike, non-hidden faction with a settlement joins the simulation. Starting relations come from vanilla: hostile factions start tense, pirates (`naturalEnemy`) and permanent enemies never form pacts. | n/a | Not yet |

## Multiplayer

**Soft support, untested.** Living World does not ship or reference the Multiplayer API DLL. When the Multiplayer mod (`rwmt.Multiplayer`) is active, Living World registers these sync methods by reflection at startup (`LivingWorldMultiplayer`):

- `LivingWorldMultiplayer.ApplySettings`: closing Mod Options in a game copies your settings into the game.
- `LivingWorldDebugCommand.Enqueue`: dev-mode debug actions.
- `LivingWorldRemoval.Run`: the "Prepare this save for removal" button.

**Settings are saved with the game.** The simulation runs on the game's own copy of the settings, not on each player's Mod Options, so all clients run the same sim. A new game (or an older save) starts from the Mod Options of the player who made it. In a session only the host can change them; a client who changes Mod Options gets a message and keeps their values for their own games.

The trade-blackout roll is seeded by faction and hour, so it is the same on every client even when asked from UI code. Tick code never iterates a hash set or dictionary.

**To do:** a two-client desync test over several in-game years with wars, traffic and fallout.

## For modders

`LivingWorld.LivingWorldCompatApi` (`ApiVersion` 1) is the supported surface. Resolve it by name and check `ApiVersion >= 1`.

- `IsActive`: a game is loaded and the sim is on.
- `Register(handler)` / `Unregister(handler)`: listen for news (`LivingWorldSignals.WorldEventHandler`, one `WorldEvent` argument). Handlers run inside the sim tick: keep them cheap and deterministic. Only rely on `StableHookKinds`. Handlers get ground truth; `distorted` says the player heard a wrong version.
- `IsHandlerRegistered(Type)`: for health checks.
- `RelationTone(a, b)`, `AtWar(a, b)`, `Intensity(a, b)`, `UnderTradeBlackout(faction)`, `Prosperity(settlement)`, `Epithet(settlement)`.

`WorldEvent` field names (`kind`, `trueKind`, `distorted`, `seenByPlayer`, `factionAId`, `factionBId`, `tile`, `tileLayer`, `severity`) are frozen. Enum members are saved by name and are never renamed.
