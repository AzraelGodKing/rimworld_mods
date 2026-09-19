# Manual QA checklist

Branch: `fix/azr-216-214-217-215-59-60-220-222-221-228-227-229-206-209-207-231`

Interactive canvas (optional): open `manual-qa-checklist.canvas.tsx` from the Cursor canvases folder (beside chat). This markdown file is the reliable fallback — check boxes in the editor preview or edit `- [ ]` → `- [x]`.

Mods in this build: Niceties 1.2.1 · Nemesis 1.1.2 · Date Night 1.1.2 · Stormproof 1.3.1 · Homesteader 1.0.5 · Strata 3.5.0 (`edge-rock-fog-v2`)

**QA: passed** — Steam notes for each mod include `[i]QA: passed[/i]`.

---

## Start here (high-signal)

Crash / softlock / wrong-save paths first.

- [x] **AZR-216** Leave a way out — Do: Builders seal someone in a room/enclosure. Pass: Sealed pawn steps aside next tick; no crash.
- [x] **AZR-215** Concurrent builders — Do: Put 2+ builders on the same frames/enclosure. Pass: Enclose checks stay correct; no false seal/crash.
- [x] **AZR-59** BFV soft-detect (partial) — Do: Load with BFV soft-compat path exercised. Pass: Soft-compat works; no crash. (Rimesis still open — skip that half.)
- [x] **AZR-220** Cache leak — Do: Start a new colony without restarting RimWorld. Pass: No inherited bed/window from prior colony.
- [x] **AZR-228** Hard forecast — Do: Run Hard forecast while grid draw changes. Pass: Forecast tracks live drain.
- [x] **AZR-206** Tastes tab — Do: Open and redraw the Tastes tab under load. Pass: No invent-during-draw / UI exception.
- [x] **AZR-231** Wrong-floor bunk — Do: Assign bed; leave free bunk on another floor. Pass: Pawn goes to assigned bed, not free bunk on wrong floor.
- [x] **AZR-232 / AZR-57** Underground fog — Do: Dig clear, walk edges, leave deep rock unexplored. Pass: Cleared visible, edges visible, deep fogged; no full reveal.

---

## Optional build stamps

Confirm Player.log / ModVersionLog extras if you care about build identity.

- [x] `openbuilds-hard-v1`
- [x] `bfv-ferny-id-v1`
- [x] `date-memory-v1`
- [x] `forecast-caller-v1`
- [x] `pantry-tastes-v1`
- [x] `edge-rock-fog-v2`

---

## By mod

### Niceties · 1.2.1

- [x] **AZR-216** Leave a way out — Do: Builders seal someone in a room/enclosure. Pass: Sealed pawn steps aside next tick; no crash.
- [x] **AZR-215** Concurrent builders — Do: Put 2+ builders on the same frames/enclosure. Pass: Enclose checks stay correct; no false seal/crash.
- [x] **AZR-217** Apparel care — Do: Wear Poor-quality apparel; check settings tip. Pass: Wear rate matches vanilla; settings tip is clear.
- [x] **AZR-214** Share Rooms warning — Do: Enable with LWM.ShareRooms present. Pass: Incompatible warning shows for Share Rooms.

### Nemesis · 1.1.2

- [x] **AZR-59** BFV soft-detect (partial) — Do: Load with BFV soft-compat path exercised. Pass: Soft-compat works; no crash. (Rimesis still open — skip that half.)

### Date Night · 1.1.2

- [x] **AZR-220** Cache leak — Do: Start a new colony without restarting RimWorld. Pass: No inherited bed/window from prior colony.
- [x] **AZR-222** Gift fail — Do: Fail a gift with full inventory. Pass: Gift drops at feet + thought applied.
- [x] **AZR-221** Dead prune — Do: Kill a date partner mid-targeting. Pass: No leftover targeting of the dead pawn.

### Stormproof · 1.3.1

- [x] **AZR-228** Hard forecast — Do: Run Hard forecast while grid draw changes. Pass: Forecast tracks live drain.
- [x] **AZR-229** Storm caller (partial) — Do: Trigger storm caller (singleplayer). Pass: Weather changes next tick. (No MP SyncMethod — skip multiplayer.)
- [x] **AZR-227** README vs About — Do: Skim README against About.xml / Workshop notes. Pass: Docs match About for this build.

### Homesteader · 1.0.5

- [x] **AZR-206** Tastes tab — Do: Open and redraw the Tastes tab under load. Pass: No invent-during-draw / UI exception.
- [x] **AZR-209** Preserve crates — Do: Stock pantry with preserve crates. Pass: Nutrition + variety count toward pantry.
- [x] **AZR-207** Homesteaders scenario — Do: Start Homesteaders scenario. Pass: Scenario is playable end-to-end.

### Strata · 3.5.0 · edge-rock-fog-v2

- [x] **AZR-231** Wrong-floor bunk — Do: Assign bed; leave free bunk on another floor. Pass: Pawn goes to assigned bed, not free bunk on wrong floor.
- [x] **AZR-232 / AZR-57** Underground fog — Do: Dig clear, walk edges, leave deep rock unexplored. Pass: Cleared visible, edges visible, deep fogged; no full reveal.
- [x] **AZR-140** Purpose tags — Do: Set level purpose tags; assign conflicting jobs. Pass: Tags tilt jobs; wrong tags never hard-block.

---

## Skip / not in this build

Do not treat these as QA failures.

- [ ] ~~**AZR-59**~~ Skip (partial) — Rimesis half still open; only BFV soft-detect is in this build.
- [ ] ~~**AZR-60**~~ Skip (blocked) — not in this build.
- [ ] ~~**AZR-229**~~ Skip (partial) — no multiplayer SyncMethod; SP next-tick weather only.
- [ ] ~~**AZR-233**~~ Skip — Not fixed — Hospitality floors.
- [ ] ~~**AZR-227**~~ Skip (docs-only) — verify README/About, no gameplay regression expected.
