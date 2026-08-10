# Outfit Routines (parked idea)

**Status:** In progress — Wardrobe mod MVP (`Wardrobe/`). Sleep + Cook/Doctor/Animals triggers.

**Source:** Community/QoL brainstorm (job-based apparel assignment). Feasibility notes also in the Cursor plan `QoL Mods Feasibility`.

## Problem

Vanilla Expanded and similar packs add lots of job-specific apparel. Micromanaging swaps is painful, so colonists stay in armor forever. Need automatic dress-for-task without scanning the whole map every tick.

## Pitch

A new policy UI (Assign-adjacent tab) with per-pawn rules:

> If pawn **P** is scheduled / about to do task **T**, go to storage/zone **Y**, change into outfit **Z** (add or replace). When the task ends, return to **Y** and restore previous clothing or wear outfit **E**.

Examples: chef gear while cooking, scrubs for surgery, sleepwear for Sleep, ceremonial outfit for rituals, farmer gear for animals, patient gown when operated on, captain hat for gravship (later).

## Rule shape

`Pawn → Task / WorkType / Schedule / Ritual → Place/Storage → Outfit → Add|Replace → Return: snapshot|Outfit B`

Per-pawn rules avoid two colonists claiming the same specific apparel Thing (reserve during swap).

## Recommended MVP (when built)

1. Triggers: Sleep timetable, WorkType issued (Cook / Doctor / Animals), Ideology ritual start (`RitualBehaviorWorker.TryExecuteOn`).
2. One designated stockpile / wardrobe zone per rule (or global wardrobe).
3. Replace conflicting layers + snapshot restore via forced wear.
4. Suppress `JobGiver_OptimizeApparel` while a managed mode is active.
5. Defer: gravship events, Anomaly psychic rituals, patient-as-surgery-target, dresser furniture.

## Architecture sketch

- `GameComponent`: rules, active mode per pawn, apparel ThingID snapshots, zone refs, cooldowns.
- Harmony: `JobGiver_Work.TryIssueJobPackage`, `JobGiver_GetRest` / timetable, ritual start, OptimizeApparel skip while managed.
- Execution: snapshot → path to zone → Wear/Remove → resume task → on exit restore.

## Effort

| Scope | Size | Time |
|-------|------|------|
| Draft↔civilian + Sleepwear + one zone + restore | M | ~1–2 weeks |
| + Work-types + Add/Replace + hysteresis | L | ~3–5 weeks |
| + Rituals, Anomaly, rich UI, VE apparel compat | L–XL | ~5–8+ weeks |

## Packaging

Separate mod from Date Night (different audience / Workshop page). Suggested working name: **Wardrobe** / **Outfit Routines**. Package id TBD (`azraelgodking.Wardrobe`).

## Prior art

Change Dresser, Outfit Manager / Auto Apparel Pickup (not in this repo).
