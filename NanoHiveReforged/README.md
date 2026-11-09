# NanoHive Reforged

A faithful-plus rebuild of Nanohive2 for RimWorld.

## What it does

- Repairs nearby friendly buildings and roofed items while powered
- Repairs colonist gear (apparel/equipment) in range
- Optionally repairs colonist inventory items in range
- Uses a Rare ticker like the original mod

## Settings (2.0)

Gameplay is per hive. Each hive has buttons for radius (1–30), repair rate (1–10 HP per target), and what it repairs (buildings, items, gear, inventory, only stored items, buildings first). Those values save with the hive (`HiveConfig`).

Mod Options are only the seed for a **new** save (or a 1.x save the first time it loads). After that the save keeps its own colony default:

- Repair HP per target per cycle (default: 1)
- Toggle repairing colonist apparel/equipment
- Toggle repairing colonist inventory items
- Optional repair radius override (otherwise uses the building's default)
- Repair shimmer (visual only)

## Install

1. Copy the `NanoHiveReforged` folder into your RimWorld `Mods` directory.
2. Build both projects (below) so `1.6/Assemblies/NanoHiveReforged.dll` and `1.7/Assemblies/NanoHiveReforged.dll` exist. `LoadFolders.xml` loads only the folder for the running game version.
3. Enable **NanoHive Reforged** in the mod list.

## Texture

- Building texture file: `Textures/Buildings/RepairingAura.png` (matches `texPath` `Buildings/RepairingAura` in the ThingDef).

## Build notes

```bash
dotnet build NanoHiveReforged/Source/NanoHiveReforged.csproj -c Release
dotnet build NanoHiveReforged/Source/NanoHiveReforged.1.7.csproj -c Release
```

No local RimWorld install is needed. Both projects reference the game through NuGet `Krafs.Rimworld.Ref`:

| Project | Game | Target | Output |
|---|---|---|---|
| `NanoHiveReforged.csproj` | 1.6 (`1.6.4871`) | `net472` | `1.6/Assemblies/` |
| `NanoHiveReforged.1.7.csproj` | 1.7 (`1.7.4998-unstable`) | `net10.0` (needs the .NET 10 SDK) | `1.7/Assemblies/` |

Both link `Common/ModPack.cs` and `Common/ModVersionLog.cs` (no Harmony). CI builds both and runs the headless mod checks. DLLs are not in git, so rebuild both (or take the 1.7 DLL from CI) before a Workshop upload.
