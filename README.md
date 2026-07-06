# RimWorld Mods

A collection of RimWorld mods by AzraelGodKing.

## Mods

### Homesteader

Everything a growing homestead needs: better storage, food preservation, and off-grid power. Supports RimWorld 1.4 / 1.5 / 1.6. Fully XML-based — no C# assembly.

| Category | Content |
|---|---|
| **Storage** | Storage crate (3 stacks), storage barrel (3 stacks, food & drink), pallet (2 stacks, cheap outdoor storage — even chunks) |
| **Food preservation** | Drying rack (jerky & dried produce, no power/research), smokehouse (wood-fired; smoked meat keeps ~30 days and still cooks into meals) |
| **Power** | Compact battery (1x1), battery bank (2x2), advanced battery (1x2, 90% efficiency), ultratech battery (2x2, 4000 Wd at 99% efficiency — endgame), portable generator (1x1 chemfuel, 350W) |

### Stormproof

Defend your grid from everything the Rim throws at it. RimWorld 1.6. C# mod (requires the [Harmony](https://github.com/pardeike/HarmonyRimWorld) mod).

| Building | What it does |
|---|---|
| **Solar shield** | Idles at 100W; during a solar flare drains 2,500W continuously to keep all electronics running. If the grid runs dry mid-flare, everything goes dark until it ends. |
| **Storm spire** | Attracts lightning within a wide radius. Grounded: safe fire protection. Grid-connected: each strike stores up to 1,500 Wd in batteries, with a 25% chance of a "Zzzt!" surge. |
| **Surge protector** | Absorbs one "Zzzt!" short circuit, then recharges for a day. |
| **EMP dampener** | Colony buildings in range are immune to EMP stuns. |
| **Armored conduit** | Fireproof, high-durability power conduit. |
| **Storm vane** | Decorative copper weather vane. Pure class. |

Source lives in `Stormproof/Source`; the compiled `Stormproof.dll` ships in `Stormproof/Assemblies` and is rebuilt by CI on every source change.

## Installation

1. Download / clone this repository.
2. Copy the `Homesteader` folder into your RimWorld `Mods` directory:
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`
   - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods`
   - macOS: `~/Library/Application Support/Steam/steamapps/common/RimWorld/Mods`
3. Enable **Homesteader** in the in-game mod list.

Safe to add to an existing save. Before removing it from a save, deconstruct the mod's buildings and consume/sell its items first.
