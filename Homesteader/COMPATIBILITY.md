# Homesteader compatibility

Homesteader 2.0.0 runs on RimWorld 1.6 and 1.7 from one def set. Every row below is
soft: nothing is required except Harmony, and each link turns itself off when the other
mod is missing. "Untested" means the code is in place but nobody has played it on that
version yet.

## Matrix

| Mod | What happens | 1.6 | 1.7 |
| --- | --- | --- | --- |
| Harmony | Required. | Untested | Untested |
| Dubs Bad Hygiene | Wells, cisterns, rain barrels and the water tower join DBH plumbing (`Patches/Patch_DubsBadHygiene.xml`). Loads after DBH. | Untested | Untested |
| Odyssey | Fish curing and smoking recipes (`Odyssey/`, loaded by `LoadFolders.xml`). Rain barrels and cisterns stop during Odyssey droughts. | Untested | Untested |
| Ideology | The sleep accelerator counts as furniture for the bedroom set (`MayRequire`). | Untested | Untested |
| Royalty | The Azrael storyteller lists Royalty content only when it is active. No other link. | Untested | Untested |
| Biotech | The Kats Effect picks an orange Wolfein xenotype when Biotech and Wolfein are both present. | Untested | Untested |
| Wolfein | The Kats Effect visitor is a Wolfein when the race is found (detected by name). | Untested | Untested |
| Multiplayer | Soft support: every button that changes the game is a synced method, and gameplay settings live in the save. Needs a two-client test. | Untested | Untested |
| Strata | Strata adds exhaust to the wood and portable generators, slows rot in root cellars, counts crates and packed gravel like its own haul aids, and floods wells under a flooded level. | Untested | Untested |
| Stormproof | No direct link. Rain barrels and cisterns read the map's rain rate, whatever weather sets it. | Untested | Untested |
| Deep Colony | Grand Chef mood on Homesteader foods, family letters for Homesteader crops (by the `Homesteader_` prefix). The Deep Roots scenario uses Homesteader's forced storyteller. | Untested | Untested |
| Nemesis | Food raids count Homesteader stores and cold rooms (by defName). The Marked scenario uses Homesteader's forced storyteller. | Untested | Untested |
| Living World | Famine rumors and war refugees bring a short message about the colony's own pantry (HS-S01). Messages only. | Untested | Untested |
| Azrael | The series hub shows the Homesteader and Strata bridges and reads `HomesteaderBuildInfo.BuildStamp`. The Azrael storyteller def is the same in both mods. | Untested | Untested |
| Date Night | Couples can dance at the harvest maypole. | Untested | Untested |
| Niceties | No interaction. | Untested | Untested |
| Wellspring (standalone) | Incompatible. Its buildings are part of Homesteader now, under the same defNames. | n/a | n/a |

## Frozen names

Other mods and old saves find Homesteader by these names. Do not rename them.
`Tests/ModChecks/HomesteaderContract.cs` fails the build if one of them changes.

| Name | Kind | Used by |
| --- | --- | --- |
| `Homesteader_WoodGenerator`, `Homesteader_PortableGenerator` | ThingDef | Strata exhaust patch |
| `Homesteader_RootCellar` | ThingDef | Strata rot bridge, Azrael hub |
| `Homesteader_Icehouse`, `Homesteader_Springhouse`, `Homesteader_Smokehouse` | ThingDef | Nemesis |
| `Homesteader_PreservesShelf`, `Homesteader_IngredientBarrel`, `Homesteader_StorageBarrel`, `Homesteader_StorageCrate`, `Homesteader_LargeStorageCrate` | ThingDef | Nemesis, Strata (crates) |
| `Homesteader_HarvestMaypole` | ThingDef | Date Night |
| `Wellspring_HandDugWell` | ThingDef | Strata, Azrael hub |
| `Homesteader_PackedGravel`, `Wellspring_IrrigatedSoil` | TerrainDef | Strata |
| `Wellspring_DrawWater`, `Wellspring_DrawWaterDeep` | RecipeDef | Strata flooded levels |
| `Homesteader_ForcedStoryteller` | ScenPartDef | Deep Colony, Nemesis and Stormproof scenarios |
| `Homesteader.ScenPart_ForcedStoryteller` | class | the same scenarios, by `Class=` |
| `Azrael` | StorytellerDef | Azrael (standalone copy) |
| `Homesteader_Homesteaders` | ScenarioDef | player saves |
| Any `Homesteader_` defName | prefix | Deep Colony |

Spawner progress is saved under `saveKeysPrefix`, so these stay fixed too:
`eggs` (chicken coop), `honey` and `beeswax` (beehive), `rainwater` (rain barrel),
`cistern`, `solarstill`, `watertower`.

## Forced storyteller

`Homesteader.ScenPart_ForcedStoryteller` with the `Homesteader_ForcedStoryteller` def is
the canonical way for a series scenario to lock the Azrael storyteller. Sibling scenarios
reference it with `MayRequire="AzraelGodKing.Homesteader"`. Azrael keeps its own copy
(`Azrael_ForcedStoryteller`) only for Deep Homestead, which must work without Homesteader.
