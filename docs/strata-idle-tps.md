# Strata idle TPS (no levels built)

**Stamp:** `idle-tps-v1`  
**Branch:** `fix/startup-dupes-harmony`  
**Date:** 2026-07-27

## Report

A tester on a large modlist (~425 mods), medium map, 3 pawns, barely started:

| Setup | Approx TPS (dev speed+) |
|-------|-------------------------|
| Same save **without** Strata | ~1800 |
| Same save **with** Strata, **no levels dug**, Performance switch on | ~1200 |

So Strata was costing a large chunk of TPS even with zero pocket maps / stairwells.

## Was that “normal”?

Partially expected that *installing* Strata has *some* cost (Harmony patches, MapComponents registered on surface).  
**Not** expected that the Performance switch would leave most of that idle tax in place, or that atmosphere would keep doing heavy room walks with both gas systems off.

## Root cause

### 1. Atmosphere on every map (main sink)

`Patch_Map_FinalizeInit` always adds `AtmosphereMapComponent` to **every** map so gases / vents work when needed.

Hibernation early-out (`ShouldSkipHeavyAtmosphereCycle` → `StrataLevelPerfUtility.IsHibernating`) only applies to **Strata pocket levels**. The **surface is never hibernating**, so the ~60-tick atmosphere cycle kept running.

With **natural + pollutant gases both off** (defaults after settings migration), the cycle still:

- Called `RefreshOutdoorVentCache` → walked `map.regionGrid.AllRooms` for outdoor vent clusters  
- Ran transport / emitter / disperse phases that were mostly no-ops but still scheduled  
- Called `AffectPawns` (flesh pawns × gas defs) for outdoor hediff cleanup

**Performance mode** only multiplied cycle interval / forced “lite” atmosphere — it did **not** idle-skip the surface when gases were off. So A/B tests with the Performance checkbox still looked bad.

### 2. Cross-level relays with no stairs

Work / food / joy / medical (and robot / external work) postfixes still called `LevelGraph.ReachableLevels` when colonists found no local job. With no portals that BFS is cheap-but-not-free and ran often on a busy think tree. Work relay defaults **on** (settings v4); Performance mode disables work relay, but **not** food/joy/medical.

### 3. Always-on game tick postfix

`TickManager.DoSingleTick` postfix always called robot diagnostics, drafted portal pathing, haul-delivery, and portal-relay ticks. Empty queues return quickly, but the postfix itself ran **every tick**.

### 4. Smaller always-on MapComponents

`MapComponent_RaidPursuit` and `MapComponent_CrossLevelThreatWatch` tick on player homes even with no links (mostly early-out after portal scans).

## What we changed (`idle-tps-v1`)

1. **Atmosphere idle gate** — if both gas systems are off, clouds are empty, and there are no pending seeds / loaded clouds → skip the heavy cycle (and clear any mid-cycle phase).  
2. **Vent cache** — only rebuild outdoor vent clusters when there are clouds or vent/updraft/exchanger hardware.  
3. **AffectPawns** — no-op when both gas systems are off.  
4. **Relays** — `LevelGraph.AnyLinkFrom(map)` before ReachableLevels / scan cooldowns (work, food, joy, medical, external, robot).  
5. **Raid pursuit / threat watch** — return when the map has no stair links.  
6. **Game-tick postfix** — if haul + portal-relay + drafted-route queues are empty, only do cheap diagnostics + rare caravan pull.

## What Performance mode still does / does not

| Does | Does not |
|------|----------|
| Disables colonist work relay | Unregister AtmosphereMapComponent |
| Disables Misc. Robots soft-compat | Disable food / joy / medical relays |
| Slows atmosphere cycle (×4) | Fully idle surface with gases off *(fixed separately in idle-tps-v1)* |
| Lite atmosphere / fewer pawn gas affects off-view | Remove Harmony patches |

After `idle-tps-v1`, Performance mode is less critical for the “never dug” case; gases-off + no stairs should be near baseline.

## How to verify

1. Confirm Player.log: `[Strata] Soft-compat build idle-tps-v1 loaded …`  
2. Repeat the A/B (same save, same modlist, no stairs): TPS with Strata should be much closer to without.  
3. Dig a level / enable natural or pollutant gases — cost should rise again (expected).  
4. Optional: Analyzer on `AtmosphereMapComponent.MapComponentTick` before/after.

## Code touchpoints

- `Strata/Source/AtmosphereMapComponent.cs` — `ShouldSkipHeavyAtmosphereCycle`, transport vent gate, `MaybeAffectPawns`  
- `Strata/Source/Patch_WorkRelay.cs`, `Patch_FoodRelay.cs`, `Patch_JoyRelay.cs`, `Patch_MedicalRelay.cs`, `Patch_ExternalJobGiverRelay.cs`, `Patch_RobotWorkRelay.cs`  
- `Strata/Source/RaidPursuit.cs`, `MapComponent_CrossLevelThreatWatch.cs`  
- `Strata/Source/Patch_WorldComponents.cs` + pending flags on haul / portal / drafted pathing  
- `Strata/Source/StrataBuildInfo.cs` — stamp `idle-tps-v1`
