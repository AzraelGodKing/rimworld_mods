Homesteader optional texture refresh
===================================

Original sprites stay in Textures/Homesteader and Textures/Wellspring.
This folder is used only when Mod Options → Homesteader → "Use refreshed textures" is on
(default off). Restart RimWorld if sprites look stale after toggling.

Every *def* has its own art (no shared placeholders across jam/cellar/etc.).
Files named `Name_north/south/east/west.png` are Graphic_Multi facings of **one**
building — the same object from four camera angles, not four different buildings.

Orchard trees and composted soil also get their own art instead of vanilla oaks
/ shared irrigated soil.

Diggo (`Homesteader/Buildings/HippoDogPlushie`) is brought art and is never swapped.
The 27 statue (`Homesteader/Buildings/Statue27`) is brought `art/brought/27_2.0.png` and is never swapped.

A later pass redrew the sprites that read as product photos / RPG icons / true-isometric
renders against actual Core sprites (wood-fired generator, cowboy hat, oak/maple trees,
chess table, sculptures, meals): thick dark outlines, muted palette, flattened top-down
3/4 camera. Originals under Textures/Homesteader are unchanged.
