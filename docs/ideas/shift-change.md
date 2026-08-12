# Shift Change (from Outfit Routines)

**Status:** Full-sail in-repo (`ShiftChange/` package) — Sleep + Cook/Doctor/Animals work + Ideology rituals → wardrobe → apparel policy + snapshot restore.  
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

Priority while several could apply: **Sleep → Ritual → WorkType**. Soft apparel claims avoid two colonists grabbing the same Thing.

## Shipped

1. Triggers: Sleep timetable; WorkType (Cooking / Doctor / Handling) on job issue + tick; Ideology ritual start.
2. Wardrobe stockpile per rule (or auto: label contains “Wardrobe”, else first apparel stockpile).
3. Replace / add modes + snapshot restore; optional inventory stash for stripped layers.
4. Suppress `JobGiver_OptimizeApparel` while managed; restore hysteresis.

## Deferred

- Gravship events, Anomaly psychic rituals, patient-as-surgery-target, dresser furniture.
- Vanilla `Reserve` during the walk window; more work types; docs/Workshop page.

## Architecture

- `GameComponent_ShiftChange`: rules, active mode per pawn, apparel ThingID snapshots, claims, cooldowns/hysteresis.
- Harmony: OptimizeApparel skip; `JobGiver_Work.TryIssueJobPackage`; `RitualBehaviorWorker.TryExecuteOn`.
- Execution: snapshot → path to zone → Wear/Remove → on exit restore.

## Prior art

Change Dresser, Outfit Manager / Auto Apparel Pickup (not in this repo).
