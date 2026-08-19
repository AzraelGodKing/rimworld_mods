# Stormproof — update ideas

**Status:** ideas only. Playable core (grid defense + hazard hardening) is shipped.  
**Mod:** [Stormproof](../../Stormproof/). Checklist: [Stormproof/ROADMAP.md](../../Stormproof/ROADMAP.md).  
**Lane rule:** grid, weather, lightning, EMP, ion, and atmospheric counters. Off-map politics → Living World. Named hunters → Nemesis. Farm cisterns / rain barrels → Homesteader (Stormproof only **emits** drought/ion queries). Column floors → Strata (Stormproof may own a **surface antenna** building).

**Sources:** [Stormproof/ROADMAP.md](../../Stormproof/ROADMAP.md); series soft-compat web; Odyssey DeepFreeze gap vs climate stabilizer.

---

## Pitch reminder

Updates should make the **grid and the sky** feel like something you can prepare for: isolate a surge, harvest a storm, see the next hazard coming. Do not turn Stormproof into a second storyteller, a farm water mod, or a personal-antagonist pack.

---

## What’s already in (don’t rebuild)

Solar shield, storm spire, surge protector, EMP dampener, armored conduit, capacitor bank, weather forecaster, static pylon, fallout scrubber, storm caller, load shedder, grid monitor, storm vane. Hazard hardening: atmospheric barrier, climate stabilizer, sky restorer, fire suppressor, drought condenser. Events: ion storm, heat dome, polar front, toxic surge, dry lightning. Stormfront scenario. CN/RU.

**No Mod Options panel** — Languages README mentions settings that do not exist yet (SP-Q01).

Climate stabilizer **detects** Odyssey DeepFreeze for power draw but does **not** cancel its temperature offset (SP-Q02). Static pylon is auto-defense, not a manned lightning gun.

---

## Phases (summary)

| Phase | Theme | IDs | Count |
|-------|-------|-----|-------|
| 0 | QoL + emit API | Q01–Q03 | 3 |
| 1 | Grid isolation | A01–A03 | 3 |
| 2 | Sky events + turbines | A04–A07 | 4 |
| 3 | Endgame hardware | A08–A09 | 2 |
| 4 | Soft-compat emit | S01–S04 | 4 |

---

## Phase 0 — QoL (do these first)

| ID | Idea | Size | Notes |
|----|------|------|-------|
| SP-Q01 | **Settings pack** — ion frequency, spire Zzzt %, surge cooldown, hazard building draws, event on/off | S | Match Deep Colony / Homesteader “tone it down” |
| SP-Q02 | **DeepFreeze offset** — climate stabilizer cancels Odyssey DeepFreeze temperature like heat wave / polar front | S | Power draw already keyed; Harmony gap |
| SP-Q03 | **Outbound queries** — fail-open helpers: ion storm active, drought protected, last lightning harvest, storm-scarred (boolean) | S | Homesteader / Nemesis / Living World consume; no hard deps |

---

## Phase 1 — Grid isolation

| ID | Idea | Size | Notes |
|----|------|------|-------|
| SP-A01 | **Substations** — zone a net so Zzzt / ion burst stay on one segment | M | Load shedder is a breaker, not isolation |
| SP-A02 | **Faraday cage** — room-scoped EMP + ion bleed immunity (cheaper than map dampeners) | M | Buildings in the room only; pawns still get vanilla EMP |
| SP-A03 | **Lightning divertor** — cheap early spire that only redirects strikes (no harvest, no 5% Zzzt) | S | Killbox roofs / wooden bases; research under storm protection |

---

## Phase 2 — Sky and turbines

| ID | Idea | Size | Notes |
|----|------|------|-------|
| SP-A04 | **Storm overdrive + brake** — thunderstorm / high wind: +50% nearby turbines; damage risk without a brake building | M | Existing ROADMAP |
| SP-A05 | **Rolling brownout** — staged watt cap / flicker; monitor + shedder + substations counter | M | New GameCondition; not a second ion storm |
| SP-A06 | **Aurora** — beauty/mood + intermittent EMP flicker; dampeners / sky restorer interact | S/M | Pretty, then a bite |
| SP-A07 | **Heat lightning** — high dry bolts, little rain; spire + fire suppressor pressure without a full dry-lightning front | S | Distinct from dry lightning front |

---

## Phase 3 — Endgame hardware

| ID | Idea | Size | Notes |
|----|------|------|-------|
| SP-A08 | **Lightning gun** — mannable turret that spends capacitor Wd | M | End of perfect-grounding line; not a second static pylon |
| SP-A09 | **Weather dominator** — pick tomorrow’s weather, long cooldown, vanilla goodwill ding (not Living World politics) | L | Storm caller stays “summon a thunderstorm” |

---

## Phase 4 — Soft-compat (fail-open emit)

| ID | Idea | Size | Notes |
|----|------|------|-------|
| SP-S01 | **Drought / ion query** — Homesteader HS-S02 / HS-A13 inspect and barrel drain; Stormproof only exposes state | S | Homesteader owns cistern copy |
| SP-S02 | **Surface antenna** — building: underground grids stay ion-immune only if a surface antenna is powered (Strata owns floors) | M | Series ROADMAP; skip if Strata absent |
| SP-S03 | **Ion-bait signal** — Nemesis N4 / NM-S03 may lure at high aggression when an ion storm is active | S | Nemesis implements the lure |
| SP-S04 | **Storm-scarred region** — optional boolean Living World can flavor in a letter | S | No chronicle UI in Stormproof |

---

## Explicitly later / probably never

- Capacitor rail artillery (too close to the static pylon)
- Ideology weather precepts as a Stormproof religion
- Owning Homesteader waterwheels or Strata pumps
- A second storyteller
- Storm vane art polish (SP2) unless a Workshop shot needs it

---

## Suggested build order

1. **Phase 0** — Q01 + Q02 immediately; Q03 with the first sibling consumer.
2. **Phase 1** — A03 divertor (small), then A01 substations.
3. **Phase 2** — A05 brownout or A04 overdrive; aurora/heat lightning as flavor.
4. **Phase 3** — only if the Workshop page wants an endgame screenshot.
5. **S01 / S03** anytime after Q03.
