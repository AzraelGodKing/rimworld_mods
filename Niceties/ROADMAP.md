# Niceties

Small colony comforts for RimWorld 1.6 and 1.7, both built from the same source. Harmony QoL companion for the Azrael series — not a content pack.

Every nicety ships with a master Mod Options toggle. Nested knobs must no-op while that toggle is off. Do not add a nicety that cannot be turned off without disabling the whole mod.

| Toggle | What it does |
|---|---|
| Well-kept apparel | Worn clothes skip or scale daily deterioration by quality and Crafting. Combat still damages HP. |
| Throne and altar | Royalty throne rooms may contain an Ideology altar; a throne keeps the room a throne room. |
| Wear any outfit | Colonists may wear any gendered cut with no wrong-gender thought. Defs are not edited; generated pawns dress as vanilla unless the nested "also generated pawns" toggle is on. |
| Hidden cryptosleep | Pawns already in a casket drop off the colonist bar. Carrying in does not hide them early. A small "+N in cryptosleep" note shows how many are hidden; clicking it lists them and jumps to one. Optional (local, off by default): also hide growth-vat children and pawns in biosculpter pods or subcore scanners. |
| Melee hunting | Hunters may hunt with a melee weapon, with a body-size cap (the animal's real size) and an optional revenge-chance cap. Unarmed optional, off by default. The cap slider names a reference animal and has one-click animal buttons. Skipped hunt targets say why in their inspect pane. |
| Shared bedrooms | Bed gizmo marks the room as shared so it stays a bedroom (works on a multi-selection). Roommates skip disturbed sleep. Inspect lists the room's bed owners and the room role reads "Bedroom (shared)". Optional alert when a noble's bed is in a shared room. |
| Leave a way out | Pawns skip finishing a wall that would trap someone or block leftover frames. They step aside before closing themselves in. Replace Stuff frames included. A selected wall frame outlines the area it would close off; the fail reason names who would be trapped; the closed-area size limit is a setting (80 cells by default). |

**Mod Options** — one collapsible section per nicety, each with its own Reset button and a status line when it cannot run (missing DLC, Smarter Construction loaded, patch target missing, Multiplayer host-baked).

**Presets** — Soft / Default / Hard buttons set the nested knobs only; which niceties are on is never touched. "Reset to defaults" is the one full reset (toggles, knobs and the UI note). The options page shows which preset is active, or Custom.

Requires [Harmony](https://github.com/pardeike/HarmonyRimWorld). Royalty and Ideology optional. Safe to add mid-save; removing it logs a few harmless errors once on the next load.

Inspired by ideas from Jecrell's Everlasting Apparel, Allow Altars in Throneroom, Wear What You Want, Hide Cryptosleep Pawn, Melee Hunting, Share Rooms, and Smarter Construction. Original implementations — not ports of those Workshop zips.

## Compat notes

- **Melee Hunting (AZR-295)** — the credited Workshop inspiration (`melee.hunting` / Steam “Melee Hunting”) is discontinued and removed from the community Workshop. The maintained counterpart that overlaps Niceties melee hunting is [Hunters Use Melee! (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2900108163) (`Mlie.HuntersUseMelee`), listed in `About.xml` `incompatibleWith`.
- **Multiplayer (AZR-292, AZR-371, AZR-372, AZR-373, AZR-653)** — sim-gating settings bake into `GameComponent_NicetiesSim`. Outside a Multiplayer session the snapshot follows Mod Options on load and on edit; inside one it stays host-baked and local edits do not touch the save. The share-room gizmo registers a sync method by reflection when Multiplayer is loaded. Niceties still has no hard Multiplayer.API dependency.
- **Private engine targets (AZR-374)** — `Pawn.CheckForDisturbedSleep` and `ColonistBarDrawLocsFinder.CalculateDrawLocs` are patched by name. A `Prepare` guard skips the one patch and logs which feature dropped if a RimWorld update renames them; grep both after each RimWorld patch.

Linear: [AZR-105](https://linear.app/azraelgodking/issue/AZR-105/niceties-16-qol-pack-from-leftover-workshop-ideas) · [AZR-106](https://linear.app/azraelgodking/issue/AZR-106/niceties-shared-bedrooms-and-no-disturbed-sleep-for-roommates) · [AZR-201](https://linear.app/azraelgodking/issue/AZR-201/niceties-smarter-construction-replace-stuff-enclose-bridge).
