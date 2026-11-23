# Strata 4: RimWorld 1.7 compatibility matrix (AZR-486)

Checked 2026-11-23 from Steam Workshop version tags and comments. Re-check before each Strata release.
Strata's compat code is all gated on the other mod being loaded (reflection or soft patches), so a mod that is
missing on 1.7 costs nothing at runtime. Nothing is dropped yet: every row below still has a 1.6 version that
Strata 4 supports, and dropping code would break 1.6 players.

| Mod | Strata code | 1.7 status | Strata action |
| --- | --- | --- | --- |
| Vanilla Expanded Framework pipes | `VefPipeSystemBackend`, `VefPipeNetworkUtil` | Yes (1.7 tag, updated Nov 20) | Keep. Re-test shaft fluid ties in game on 1.7. |
| Vanilla Temperature Expanded (air-con) | `VteAcPipeBackend` | Yes, players report 1.7 bugs in VTE itself | Keep. Re-test duct ties. |
| Dubs Bad Hygiene | `DbhPlumbingBackend`, `Patch_DbhPullWaterFromShaftBuffer`, `GenStep_DbhGroundwater` | Yes (1.7 tag) | Keep. Re-test plumbing and groundwater. |
| Dubs Central Heating | `DchPlumbingBackend` | Yes (1.7 tag) | Keep. Re-test. |
| Rimatomics | `RimatomicsCoolingBackend` | Yes (1.7 tag) | Keep. Re-test cooling junction. |
| Rimefeller | `RimefellerPipelineBackend`, `Patches` (AZR-748) | Unclear: Workshop tags stop at 1.2, but AZR-748 found PipeType in the 1.7 DLL | Keep. Check the Workshop page by hand. |
| Combat Extended | `StrataCombatExtendedSoftCompat` | No (1.6 only, 1.7 asked in comments) | Keep (1.6). Gate is already "CE loaded". |
| Vehicle Framework | `VehicleFrameworkRoofDeckCompat` | Yes (1.7 tag) | Keep. Re-test roof-deck vehicles. |
| Biomes! Caverns | `BiomesCavernsUtility`, `GenStep_BiomesCavernFeatures` | Yes (1.6 and 1.7) | Keep. Re-test a Biomes cavern dig. |
| Darktorch floodlight | `Patches/Exhaust_Strata.xml` | Unclear (Workshop page not found) | Keep (XML patch is conditional). |
| Pick Up And Haul | `StrataPuahSoftCompat` | Unclear: no 1.7 tag, players say it works | Keep. |
| Homesteader, Stormproof (sister mods) | `SisterModBridges`, `StormproofClient` | Ours, ship for 1.6 and 1.7 | Keep. |
| One bed to sleep with all | none (bed assign list only) | No (1.6 only). Polycule Edition: yes | Nothing to change. In-game spot check in AZR-646. |
| Multiplayer | not supported (AZR-488) | No (1.6 only) | Stays "not supported". |
| As Above So Below 1 and 2 | `incompatibleWith` in About.xml | Unclear (pages not found) | Keep the incompatibility. |
