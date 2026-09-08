# Stormproof — update ideas

**Status:** playable core shipped; this is the next grid/weather QoL pool.  
**Mod:** [Stormproof](../../Stormproof/). Checklist: [Stormproof/ROADMAP.md](../../Stormproof/ROADMAP.md).  
**Lane rule:** weather, grid defence, atmospheric events. Underground air → Strata. Water storage → Homesteader. Personal antagonists baiting storms → Nemesis (fail-open).

---

## Pitch reminder

Stormproof's buildings are invisible when they work. Updates should make **coverage, forecast, and cost** legible: where the spire actually catches, what the next weather does to batteries, what the last storm cost. Do not turn Stormproof into a second power-overhaul or a Strata atmosphere sim.

---

## Shipped in this pool

| ID | What |
|---|---|
| AZR-143 | Radius / room overlays (spire, pylon, dampener, suppressor, scrubber) |
| AZR-144 | Grid monitor projected trajectory when a forecaster is on the same net |

---

## Next (Linear)

### AZR-142 — Scheduled load profiles

`CompLoadShedder` is a reactive breaker. Give each sub-grid an hour schedule (run / shed), with the existing cutoff as the floor. Forecast override: shed early when the forecaster says a storm lands inside N hours. Manual override must be one click. Existing saves keep current behaviour until a schedule is set.

### AZR-145 — Per-storm damage report

Almanac records weather, not cost. Each hazard event should ledger suffered vs prevented (strikes caught, Zzzt absorbed, fires snuffed, grid-down time). Cap history. Record, not a letter. Needs comps to report saves they already know.

### AZR-109 — Nexus XML parse crash

Reporter `zqhaohao`, 2026-09-04: `unknown parse failure` during `CombineIntoUnifiedXML` on Stormproof game-condition / incident / research defs (and Homesteader a few minutes earlier). Repo XML is well-formed. Likely a bad file earlier in that load order, or a Nexus zip encoding issue. Do not "fix" defs without a log that names a real malformed node.

---

## Soft-compat (do not steal)

| Other mod | Stormproof offers | They consume |
|---|---|---|
| Homesteader | Drought game condition is on | HS-S02 well / cistern inspect |
| Strata | Ion storm / surge is on | Underground ion-immunity; flood unpumped levels |
| Nemesis | Ion storm active | High-aggression baiting |
