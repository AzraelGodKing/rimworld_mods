# Changelog

Detailed notes for **NanoHive Reforged** only.

## [Unreleased]

## [2.0.0]

Player-facing version **2.0.0** (`About.xml` `modVersion`). Stamp `nanohive2-net10`. Ships for RimWorld **1.6** and **1.7** from the same source; 1.6 is not frozen. Saves from 1.x load with the same behaviour.

AZR-583 AZR-591 AZR-596 AZR-597 AZR-598 AZR-604 AZR-606 AZR-612

### Added
- **Per-hive buttons** (AZR-596, AZR-606) — Repair buildings / items / gear / inventory, Only stored items, Buildings first, Radius (1 to 30), Rate (1 to 10 HP per target), Use as colony default, and Copy to all hives. Each hive saves a `HiveConfig` (`nanoHive` node); `GameComponent_NanoHive` saves the colony default that new hives copy.
- **Repair budget** (AZR-606) — per cycle a hive spends at most rate × `budgetTargets` (40) HP, most damaged first (HP%, then `thingIDNumber`), optionally buildings first. With the default rate and 40 or fewer damaged things in range, repair is the same as 1.x.
- **Idle power** (AZR-606) — 200 W while nothing in range is damaged, the full `basePowerConsumption` (1600 W) while repairing. `NanoHiveProps` mod extension: `defaultRange`, `idlePowerConsumption`, `budgetTargets`.
- **Inspect pane** (AZR-604) — radius, rate, budget, things in range, damaged count and HP repaired last cycle, or idle / no power.
- **Repair shimmer** (AZR-612) — pulsing glow on a repairing hive and fading sparks on what it fixed, drawn from `MapComponent_NanoHive`. Vanilla `MoteGlow` shader on both 1.6 and 1.7, draw-only and Rand-free. Setting `repairShimmer`. A custom 1.7 GLSL shader is not done.
- **Soft Multiplayer support** — `SetToggle`, `SetRange`, `SetRepairRate`, `MakeColonyDefault` and `CopyToAllHives` are registered with `Multiplayer.API.MP.RegisterSyncMethod` by reflection. No Multiplayer DLL is shipped.
- **Translations** (AZR-598) — Chinese Simplified and Russian Keyed and DefInjected.
- **Launch assets** (AZR-598) — `About/ModIcon.png`, `<url>`, credit to the original Nanohive2, this page.

### Changed
- **Two builds, one source** (AZR-591) — official 1.6 loads `1.6/Assemblies` (net472, 1.6 refs). 1.7 loads `1.7/Assemblies`, now a .NET 10 build against the 1.7 refs (`Krafs.Rimworld.Ref` 1.7.4998-unstable). Both link `Common/ModPack.cs` and `Common/ModVersionLog.cs`; startup logs `[NanoHive Reforged] v2.0.0 ...; nanohive2-net10`.
- **Settings are starting values** (AZR-596) — `NanoHiveSettings` keeps its class and field names so the 1.x file reads, but only seeds a new save (or a 1.x save on first load). The window no longer calls `Write()` every frame.
- **Radius ring** (AZR-604) — `DrawExtraSelectionOverlays` and `PlaceWorker_NanoHiveRange` draw the real radius; `specialDisplayRadius` is removed from the def.
- **Stuff** (AZR-606) — Metallic only. Hives already built from wood or stone keep their stuff.
- **Repair cycle** (AZR-606) — radial cells cached per hive, static scratch lists, pawns from `SpawnedPawnsInFaction`; no per-cycle allocations. Profiler scope `NanoHiveReforged.TickRare` (AZR-612).
- **CI** (AZR-591) — ModChecks covers NanoHive (DLLs, defs, Keyed keys) and checks that every NanoHive `texPath` has a texture. Release zips include NanoHive.

### Fixed
- **Repair list** (AZR-597) — repaired buildings call `listerBuildingsRepairable.Notify_BuildingRepaired`, so colonists stop queuing repair jobs on walls the hive already fixed.

## [1.1.0]

- New Workshop preview. Declared RimWorld 1.7 in `supportedVersions` (1.6 DLL).
