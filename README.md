# RimWorld Mods

A collection of RimWorld mods by AzraelGodKing.

## Mods

### Homesteader

Everything a growing homestead needs: tribal survival, farm-to-table crafting, storage, food preservation, and off-grid power. Supports RimWorld 1.4 / 1.5 / 1.6. Fully XML-based — no C# assembly.

| Category | Content |
|---|---|
| **Tribal survival** | Curing rack (rock salt, salted meat), drying rack (+ fruit leather, dried mushrooms, pemmican without research), smokehouse, hayloft, ingredient barrel |
| **Farm-to-table** | Grain mill, butter churn, pickling crock, homestead hearth (bread, hardtack, trail stew, hearty stew) |
| **Storage** | Storage crate, storage barrel, pallet, hayloft, ingredient barrel, root cellar, large storage crate |
| **Food preservation** | Jerky, dried produce, fruit leather, salted/smoked meat, pickled vegetables, jam |
| **Research** | Primitive homestead → food preservation / farmstead crafting / homestead storage → advanced homestead |
| **Power** | Compact battery (1×1), battery bank (2×2), advanced battery (1×2), ultratech battery (2×2), portable generator (chemfuel), wood-burning generator (wood) |

Odyssey DLC: salted fish and smoked fish recipes load automatically.

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
2. Copy the mod folder(s) into your RimWorld `Mods` directory:
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`
   - Linux: `~/.steam/steam/steamapps/common/RimWorld/Mods`
   - macOS: `~/Library/Application Support/Steam/steamapps/common/RimWorld/Mods`
3. Enable the mod(s) in the in-game mod list.
4. **Stormproof** also requires the **Harmony** mod.

Safe to add to an existing save. Before removing a mod from a save, deconstruct its buildings and consume/sell its items first.

## Website

Browse the full mod catalog at the [GitHub Pages site](https://azraelgodking.github.io/rimworld_mods/).
