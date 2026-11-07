# Deep Colony compatibility

Deep Colony 2.0.0 ships one source for RimWorld 1.6 and 1.7. Every bridge below is optional and fails open: if the other mod is missing, renamed or changes its API, Deep Colony logs one warning and carries on without that feature.

"Untested" means the code builds against that game version and the target members were checked to exist, but nobody has played it in game yet.

## Mods

| Mod | How Deep Colony connects | 1.6 | 1.7 |
|---|---|---|---|
| Harmony (`brrainz.harmony`) | Required dependency. | Required | Required |
| Living World | Reflection on `LivingWorldSignals.Register` and the `WorldEvent` fields `kind`, `seenByPlayer`, `factionAId`, `factionBId`. Living World's own compat API will replace this once it is released. | Untested | Untested |
| Nemesis (`AzraelGodKing.Nemesis`) | `Nemesis.NemesisCompatApi.IsNemesisPawn`, bound once to a delegate. When the nemesis kills a colonist, close colonists gain the "hunted" trauma naming the nemesis and a grudge against its faction. | Untested | Untested |
| Stormproof (`AzraelGodKing.Stormproof`) | Stormproof's compat API (`IonStormActive`) for ion-storm and dry-lightning downs. | Untested | Untested |
| Strata (`AzraelGodKing.Strata`) | Crush damage under a roof counts as a cave-in. The `Strata_GasInhalation` hediff marks deep-gas downs, and flame damage on a gas-poisoned pawn marks a gas firestorm. | Untested | Untested |
| Homesteader (`AzraelGodKing.Homesteader`) | `Homesteader_` defName prefix for food and letters. Deep Roots uses `Homesteader.ScenPart_ForcedStoryteller` behind `MayRequire`, so the scenario loads with or without Homesteader. | Untested | Untested |
| Date Night (`azraelgodking.DateNight`) | Loaded check only (confidant counseling bonus). | Untested | Untested |
| Despicable 2 (`DCSzar.Despicable2.Core`) | `Despicable.HeroKarma.HeroKarmaBridge.ApplyOutcome` (8 or more parameters), parameters cached at startup. | Untested | Untested |
| RimPacts (`wowgag.RimPacts`) | `RimPacts.WorldComponent_RimPacts.OffsetTrust`, with an assembly scan fallback; parameters cached on bind. | Untested | Untested |
| Humanoid Alien Races | No direct calls. The birth safety net (Options → Safety) guards against alien kind defs that break vanilla births. | Untested | Untested |
| Azrael hub (`Azrael.PatchFailureSink`) | Reflection; patch failures are reported there when the hub is loaded. | Untested | Untested |
| Multiplayer | Soft support: player commands are registered as sync methods by reflection. All players must use the same Deep Colony settings. | Untested | Untested |

## DLC

| DLC | What Deep Colony uses | 1.6 | 1.7 |
|---|---|---|---|
| Royalty | Royal title seniority. | Untested | Untested |
| Ideology | Funeral rituals (`LordJob_Ritual` whose ritual def name contains "Funeral") and precepts. | Untested | Untested |
| Biotech | `Blackboard` classroom bonus, growth vat history, births. | Untested | Untested |
| Anomaly | Entity kinds matched by name (Revenant, Sightstealer, Nociosphere, Metalhorror, Shambler, Ghoul, Noctol, Gorehulk), built once into a set. | Untested | Untested |
| Odyssey | Orbit and space biomes or map generators count as isolation. | Untested | Untested |

## For other mods

`DeepColony.DeepColonyApi` (ApiVersion 1) offers read-only signals for storytellers and sister mods:

- `ColonyTraumaLoad()`: the share of free colonists on player home maps who carry a trauma, from 0 to 1.
- `IsColonyMourning`: true for 5 days after a colonist dies.
- `DaysSinceLastColonistDeath()`: whole days, or -1 if no death is remembered.

Call it through reflection if Deep Colony may be absent.
