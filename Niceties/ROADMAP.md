# Niceties

Small colony comforts for RimWorld 1.6. Harmony QoL companion for the Azrael series — not a content pack.

Every nicety ships with a master Mod Options toggle. Nested knobs must no-op while that toggle is off. Do not add a nicety that cannot be turned off without disabling the whole mod.

| Toggle | What it does |
|---|---|
| Well-kept apparel | Worn clothes skip or scale daily deterioration by quality and Crafting. Combat still damages HP. |
| Throne and altar | Royalty throne rooms may contain an Ideology altar; a throne keeps the room a throne room. |
| Wear any outfit | Apparel gender tags cleared so mixed royal cuts do not apply the wrong-gender thought. |
| Hidden cryptosleep | Pawns already in a casket drop off the colonist bar. Carrying in does not hide them early. |
| Melee hunting | Hunters may hunt with a melee weapon, with a body-size cap. Unarmed optional, off by default. |
| Shared bedrooms | Bed gizmo marks the room as shared so it stays a bedroom. Roommates skip disturbed sleep. |
| Leave a way out | Pawns skip finishing a wall that would trap someone or block leftover frames. They step aside before closing themselves in. Replace Stuff frames included. |

Requires [Harmony](https://github.com/pardeike/HarmonyRimWorld). Royalty and Ideology optional. Safe to add or remove mid-save.

Inspired by ideas from Jecrell's Everlasting Apparel, Allow Altars in Throneroom, Wear What You Want, Hide Cryptosleep Pawn, Melee Hunting, Share Rooms, and Smarter Construction. Original implementations — not ports of those Workshop zips.

## Compat notes

- **Melee Hunting (AZR-295)** — the credited Workshop inspiration (`melee.hunting` / Steam “Melee Hunting”) is discontinued and removed from the community Workshop. The maintained counterpart that overlaps Niceties melee hunting is [Hunters Use Melee! (Continued)](https://steamcommunity.com/sharedfiles/filedetails/?id=2900108163) (`Mlie.HuntersUseMelee`), listed in `About.xml` `incompatibleWith`.
- **Multiplayer (AZR-292)** — sim-gating settings bake into `GameComponent_NicetiesSim` for join-time host authority. Live mid-session Mod Options sync would need Multiplayer.API; Niceties does not depend on it.

Linear: [AZR-105](https://linear.app/azraelgodking/issue/AZR-105/niceties-16-qol-pack-from-leftover-workshop-ideas) · [AZR-106](https://linear.app/azraelgodking/issue/AZR-106/niceties-shared-bedrooms-and-no-disturbed-sleep-for-roommates) · [AZR-201](https://linear.app/azraelgodking/issue/AZR-201/niceties-smarter-construction-replace-stuff-enclose-bridge).
