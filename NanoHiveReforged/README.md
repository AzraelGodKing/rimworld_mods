# NanoHive Reforged

A faithful-plus rebuild of Nanohive2 for RimWorld.

## What it does

- Repairs nearby friendly buildings and roofed items while powered
- Repairs colonist gear (apparel/equipment) in range
- Optionally repairs colonist inventory items in range
- Uses a Rare ticker like the original mod

## Faithful+ settings

- Repair points per TickRare (default: 1)
- Toggle repairing colonist apparel/equipment
- Toggle repairing colonist inventory items
- Optional repair radius override (otherwise uses ThingDef radius)

## Install

1. Copy the `NanoHiveReforged` folder into your RimWorld `Mods` directory.
2. Build `Source/NanoHiveReforged.csproj` so `Assemblies/NanoHiveReforged.dll` exists.
3. Enable **NanoHive Reforged** in mod list.

## Texture

- Building texture file: `Textures/Buildings/RepairingAura.png` (matches `texPath` `Buildings/RepairingAura` in the ThingDef).

## Build notes

```bash
dotnet build NanoHiveReforged/Source/NanoHiveReforged.csproj -c Release
```

The project references RimWorld through `Krafs.Rimworld.Ref` (1.6.4871), same as the other mods. The DLL is written to `Assemblies/NanoHiveReforged.dll`.
