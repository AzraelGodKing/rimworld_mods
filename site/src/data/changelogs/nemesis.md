# Nemesis — Changelog

Foundation by **Dredd (Misakabob)** — original design, persistent antagonist pawn, escape/capture loop, aggression pacing, assaults, waste drops, fixation/prison-break triggers, resolution dialog, and settings. Credited with gratitude; this monorepo package extends that work.

## [Unreleased]

## [2.0.0]

Player-facing version **2.0.0** (`About.xml` `modVersion`). Stamp `nemesis2-net10`. Ships for RimWorld **1.6** and **1.7** from the same source; 1.6 is not frozen.

- **RimWorld 1.7 build** (AZR-519) — the `1.7` folder gets its own .NET 10 DLL built against the 1.7 game files and Harmony 3. The `1.6` folder keeps its .NET Framework DLL. Both are built from the same source.
- **Several nemeses at once** (AZR-537) — each hostile faction can run its own hunt, up to **Hunts at once** (default 2, 1-3). A new hunt cannot start within **Days between new hunts** (default 3) of the last one, so one bad raid no longer spawns several. Two hunts never raid you within half a day of each other. Letters show the nemesis name in their faction's colour. The dossier gets a button per hunt.
- **Save upgrade** (AZR-552) — a 1.x save's single hunt (active, truce, or waiting on the capture dialog) loads into the new hunt list with its bounty, notes and epitaphs. The last known tile now keeps its planet layer, and the nemesis and target are saved as references next to their IDs. On the first 2.0 load, finished nemeses that 1.x left pinned as world pawns are released. A 2.0 save does not load back on 1.x.
- **Multiplayer** (AZR-553, AZR-658) — the health check runs on a fixed 120-tick cadence instead of faster on the map you are looking at, so clients never fire escapes on different ticks. Dossier buttons and the capture dialog go through `NemesisPlayerCommand`, registered as Multiplayer sync methods by reflection when the Multiplayer mod is on. Comms console replies are now a job: the colonist walks to the console first, like a vanilla call, and the options only show when vanilla's own options are usable (power, path, able to talk). Mod settings are not synced; every player needs the host's settings. Not tested in a two-client session yet.
- **NemesisCompatApi 2** — multi-hunt aware: `EngagedHuntCount`, `EngagedNemesisPawns()`, `LastHuntThreatTick`, `ClearAllHunts()`. `IsNemesisPawn` now means any engaged hunt (active, truce or captured) everywhere.
