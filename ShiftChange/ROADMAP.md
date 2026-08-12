# Shift Change roadmap

Formerly parked as **Outfit Routines** / Wardrobe (`docs/ideas/outfit-routines.md` → `docs/ideas/shift-change.md`).

## Shipped

- [x] Mod package (`ShiftChange` / `azraelgodking.ShiftChange`): About, csproj, Harmony, settings, CI wiring
- [x] `GameComponent_ShiftChange` — per-pawn rules, apparel ThingID snapshots, managed mode, soft apparel claims
- [x] Sleep timetable trigger → wardrobe stockpile → apparel policy → snapshot restore
- [x] WorkType triggers (Cooking / Doctor / Handling) via `JobGiver_Work` + tick fallback
- [x] Ideology ritual start via `RitualBehaviorWorker.TryExecuteOn` + lord tick fallback
- [x] Priority: Sleep → Ritual → Work; keep civilian snapshot across rule switches
- [x] Hysteresis before restore; inventory-prefer for removed layers; OptimizeApparel skip while managed
- [x] Main tab UI (all rule blocks) + Dev tools + Mod Options

## Next

- [ ] More work types on demand (beyond Cook / Doctor / Animals)
- [ ] Stronger reservation (vanilla Reserve) during the walk-to-wardrobe window
- [ ] Tip/letter when wardrobe empty or policy missing
- [ ] Assign-tab feel polish

## Later

- [ ] Gravship / Odyssey captain kit
- [ ] Anomaly psychic rituals
- [ ] Patient-as-surgery-target gown
- [ ] Dresser furniture / dedicated wardrobe building
- [ ] VE apparel pack smoke tests
- [ ] Docs site page + Workshop preview
