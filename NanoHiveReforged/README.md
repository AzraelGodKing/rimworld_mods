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

The `.csproj` uses references like:
- `..\\..\\..\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll`
- `..\\..\\..\\RimWorldWin64_Data\\Managed\\UnityEngine.dll`

If your RimWorld install path differs, update the HintPath entries in `Source/NanoHiveReforged.csproj`.

### Requirement

Building this project currently targets ` .NET Framework 4.8`. On machines without the 3.5 targeting pack, build fails with `MSB3644`. Install the  .NET Framework 4.8 Developer Pack or retarget the project to match your RimWorld modding toolchain.
