# Stormproof — ROADMAP

Playable core is in. Next pool: [docs/ideas/stormproof-updates.md](../docs/ideas/stormproof-updates.md).

**Ownership:** weather, grid defence, and atmospheric events stay in **Stormproof**. It does **not** own Strata's underground atmosphere or Homesteader's water storage. Soft-compat is fail-open both ways; see [series ROADMAP](../ROADMAP.md).

---

## Shipped

- [x] **AZR-49** Mod settings (Soft / Default / Hard, incident and suppressor toggles)
- [x] **AZR-58** Storm vane art polish (SP2)
- [x] **AZR-77** Graded brownout
- [x] **AZR-78** Storm wear on unhardened conduit and batteries
- [x] **AZR-79** Weather almanac
- [x] **AZR-80** Fulgurite from well-struck spires
- [x] Natural-hazard family — dry lightning, heat dome, polar front, toxic surge, plus hazard buildings
- [x] **AZR-143** Coverage overlays — radius on select and place (spire, pylon, dampener, suppressor); enclosed-room highlight on the fallout scrubber
- [x] **AZR-144** Power forecast on the grid monitor when a weather forecaster shares the net
- [x] **AZR-142** Scheduled load profiles on the shedder (24-hour run/shed timetable, forecast override, hold)
- [x] **AZR-145** Per-storm damage report in the almanac (strikes, Zzzt, fires, wear per entry)
- [x] **AZR-408** Load schedule hours colored by the supply grid forecast
- [x] **AZR-409** Almanac lifetime totals kept as running counters
- [x] **AZR-414** Grid forecast models the load shedder's schedule and cutoff
- [x] **AZR-415** Longest clean stretch in the almanac
- [x] **AZR-416** Forecast charge sparkline in the almanac

---

## Soft-compat (Stormproof side)

Do not implement the other mod's systems here. Name the hook so each side can fail-open.
Public surface: `Stormproof.StormproofCompatApi` (AZR-323).

- [x] **Strata** — `IsIonStormActive` / `IsGridSurgeActive` for ion-immune underground grid and surge awareness. Surface antenna / flood logic stays in Strata.
- [x] **Homesteader HS-S02** — `IsDroughtActive` for well / cistern inspect. Drought condenser stays Stormproof; Homesteader only reads the drought.
- [x] **Nemesis** — `IsIonStormActiveForBaiting` for high-aggression baiting (Nemesis owns the hunt).

---

## Next

Tracked in the idea pool and Linear:

- [ ] **AZR-109** Nexus `unknown parse failure` on load — not reproduced from these defs; likely packaging / load-order XML poison (same reporter as Homesteader AZR-108)

---

[Strata V3](../Strata/V3_ROADMAP.md) · [Homesteader](../Homesteader/ROADMAP.md) · [Nemesis](../Nemesis/ROADMAP.md)
