# Shift Change (from Outfit Routines)

**Status:** MVP in-repo (`ShiftChange/` package) — Sleep timetable → wardrobe stockpile → apparel policy + snapshot restore.  
**Package:** `ShiftChange` / `azraelgodking.ShiftChange`.  
**Mod roadmap:** [ShiftChange/ROADMAP.md](../../ShiftChange/ROADMAP.md).

**Source:** Community/QoL brainstorm (job-based apparel assignment). Feasibility notes also in the Cursor plan `QoL Mods Feasibility`. Earlier working names: Outfit Routines / Wardrobe.

## Problem

Vanilla Expanded and similar packs add lots of job-specific apparel. Micromanaging swaps is painful, so colonists stay in armor forever. Need automatic dress-for-task without scanning the whole map every tick.

## Pitch

A policy UI (Shift Change main tab) with per-pawn rules:

> If pawn **P** is scheduled / about to do task **T**, go to storage/zone **Y**, change into outfit **Z** (add or replace). When the task ends, return to **Y** and restore previous clothing or wear outfit **E**.

Examples: chef gear while cooking, scrubs for surgery, sleepwear for Sleep, ceremonial outfit for rituals, farmer gear for animals, patient gown when operated on, captain hat for gravship (later).

## Rule shape

`Pawn → Task / WorkType / Schedule / Ritual → Place/Storage → Outfit → Add|Replace → Return: snapshot|Outfit B`

Per-pawn rules avoid two colonists claiming the same specific apparel Thing (reserve during swap — next).

## MVP (shipped)

1. Trigger: Sleep timetable.
2. One designated stockpile / wardrobe zone per rule (or auto: label contains “Wardrobe”, else first apparel stockpile).
3. Replace conflicting layers + snapshot restore via forced wear jobs.
4. Suppress `JobGiver_OptimizeApparel` while a managed mode is active.

## Next / deferred

- WorkType issued (Cook / Doctor / Animals), Ideology ritual start.
- Gravship events, Anomaly psychic rituals, patient-as-surgery-target, dresser furniture.
- Stronger apparel reservation / hysteresis.

## Architecture

- `GameComponent_ShiftChange`: rules, active mode per pawn, apparel ThingID snapshots, zone refs, cooldowns.
- Harmony: OptimizeApparel skip while managed; later Work / ritual hooks.
- Execution: snapshot → path to zone → Wear/Remove → on exit restore.

## Prior art

Change Dresser, Outfit Manager / Auto Apparel Pickup (not in this repo).
