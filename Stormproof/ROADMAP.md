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

---

## Soft-compat (Stormproof side)

Do not implement the other mod's systems here. Name the hook so each side can fail-open.

- [ ] **Strata** — ion-immune underground grid; surface antenna for comms. Storm surges flooding unpumped levels is a Strata consumer of Stormproof weather, not a Stormproof map.
- [ ] **Homesteader HS-S02** — drought inspect on wells / cisterns. Drought condenser stays Stormproof; Homesteader only reads the drought.
- [ ] **Nemesis** — optional ion-storm baiting at high hunt aggression (Nemesis owns the hunt; Stormproof only exposes that an ion storm is active).

---

## Next

Tracked in the idea pool and Linear:

- [ ] **AZR-142** Scheduled load profiles on the shedder
- [ ] **AZR-145** Per-storm damage report in the almanac
- [ ] **AZR-109** Nexus `unknown parse failure` on load — not reproduced from these defs; likely packaging / load-order XML poison (same reporter as Homesteader AZR-108)

---

[Strata V3](../Strata/V3_ROADMAP.md) · [Homesteader](../Homesteader/ROADMAP.md) · [Nemesis](../Nemesis/ROADMAP.md)
