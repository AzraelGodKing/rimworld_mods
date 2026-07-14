# Strata — one home, many floors

Dig down and build a true multistory RimWorld base. Every level is a real map:
pathfinding, temperature, rooms, and combat all work exactly like vanilla, and
colonists commute between floors on their own to work, eat, sleep, haul, and
attend rituals.

- **Website & downloads:** https://azraelgodking.github.io/rimworld_mods/strata.html
- **V2 roadmap:** https://azraelgodking.github.io/rimworld_mods/strata-roadmap.html
- **Bug reports:** https://github.com/AzraelGodKing/rimworld_mods/issues

## Requirements

- RimWorld **1.6**
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) — load before Strata

No DLC required. Works with all DLC.

## Installation

1. Install Harmony.
2. Subscribe on the Workshop, or extract the `Strata` folder from the website
   download into your RimWorld `Mods` directory.
3. Enable **Harmony**, then **Strata**. Restart the game.

**Save compatibility:** safe to add mid-save. Before removing the mod: bring
everyone to the surface, haul up anything you care about, and deconstruct all
stairwells (a level with pawns on it will not be deleted).

## What's inside

| System | Short version |
| --- | --- |
| **Levels** | Build an excavated stairwell → a solid-rock level the size of the map above opens below, stacked exactly beneath it. Keep going deeper. A second shaft on the same floor joins the SAME level below. |
| **Fluidity** | Colonists relay themselves between floors for work, food, rest, hauling, construction materials, and rituals. You designate; they commute. |
| **Logistics** | Stockpile priority works across the whole column. Blueprints pull missing materials from other levels. The build menu and (optional) resource readout see the whole colony. |
| **Power** | Stairwells and elevators tie the levels' grids (demand-driven, both directions). The shaft conduit gives a dedicated tie point and extends its own lower junction. |
| **Atmosphere** | A multi-gas room simulation. Burners emit smoke that pools, flows through doors and vents, and chokes unventilated rooms; foul deep gas seeps from breached pockets, pools where it leaks, poisons, and **explodes on open flame**. Any working outlet guarantees a room stays safe — every ventilation tool works on every gas. Tribal-tech smoke hole included. |
| **The living deep** | New levels hide small chambers sealed in the rock, found by mining like ore: steam geysers (vanilla geothermal just works down there) and pressurized deep-gas pockets. Cap a gas vent with a **gas well** and burn the canisters in the smokeless **deep-gas generator** — or feed a Helixien gas pipe network directly. |
| **Threats** | Raiders pursue you through unsealed shafts and besiege sealed ones. Infestations scale with depth. Deep raids erupt insect swarms through the floor. Cave-ins, gas-pocket breaches, tremors. |
| **UI** | Levels tab (resizable) with jump/rename, Page Up/Down level hotkeys (rebindable), mod settings for every major system, placement guides for vents and ducts. |

## Mod settings

Options → Mod settings → Strata: level-view hotkeys, work/food/rest relay
toggles, cross-level rituals, smoke simulation on/off plus a severity slider,
raid pursuit, and the vacant-level performance throttle.

## Compatibility

- **Ancient Urban Ruins** — compatible. AUR's building interiors are invisible
  to Strata's level system by design. The only interaction: Strata's smoke also
  applies inside AUR buildings (disable the smoke simulation in mod settings if
  undesired).
- **Vanilla Helixien Gas Expanded** — soft integration: with VHGE loaded, a
  Strata gas well gains a pipe connector and feeds the helixien gas network
  directly instead of ejecting canisters. Reflection-based; nothing breaks
  when VHGE is absent.
- **Pipe/fluid mods** (Dubs Bad Hygiene, Rimefeller, VE pipes) — no cross-level
  integration yet; cross-level pipe ties are Pillar 2 of the V2 roadmap.
- Mods adding **fuel-burning buildings** are auto-detected and given exhaust
  behavior when they look like burners. Dev mode → Strata → "List smoke
  emitters" shows what got covered on your map.
- Multi-level mods that patch pathfinding or map generation globally are the
  most likely source of conflicts; Strata deliberately patches neither.

## Performance

Each opened level is a full map **the same size as the map above it** — that
is what makes true 1:1 vertical stacking possible, and it means the cost
scales with your chosen map size. On large maps (300×300 and up) every level
adds correspondingly more load; if you plan to dig many levels deep, prefer a
smaller map size at world generation. Levels with nobody on them throttle
their ambient simulation to 1-in-4 ticks (toggleable), so vacant storage
floors cost much less than a live map. All cross-level systems run on slow
periodic checks, not per-tick work, and there is no custom cross-map
pathfinding anywhere in the mod.

Levels opened before this change keep their old fixed 200×200 size; their
landings stay aligned proportionally rather than exactly.

## Dev tools

Development mode adds a **Strata** debug category: fire any Strata incident
(bypassing storyteller pacing), saturate/clear/inspect smoke and deep gas,
list smoke emitters, list hidden chambers (geysers and gas vents, discovered
or not), log level depths, and **Run self-tests** — invariant checks over the
live colony that catch registration, gas-def, and level-graph problems.

## Building from source

`Strata/Source/Strata.csproj` targets .NET Framework 4.7.2 and references
[Krafs.Rimworld.Ref](https://www.nuget.org/packages/Krafs.Rimworld.Ref) 1.6.
`dotnet build -c Release` outputs to `Strata/Assemblies/`.
