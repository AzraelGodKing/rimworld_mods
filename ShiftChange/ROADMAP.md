# Shift Change roadmap

Formerly parked as **Outfit Routines** / Wardrobe (`docs/ideas/outfit-routines.md` → `docs/ideas/shift-change.md`).

## Shipped (MVP)

- [x] Mod package (`ShiftChange` / `azraelgodking.ShiftChange`): About, csproj, Harmony, settings, CI wiring
- [x] `GameComponent_ShiftChange` — per-pawn Sleep rules, apparel ThingID snapshots, managed mode
- [x] Sleep timetable trigger → walk to wardrobe stockpile → wear apparel matching assigned policy → restore on Sleep end
- [x] Suppress `JobGiver_OptimizeApparel` while managed
- [x] Main tab UI + Dev tools + Mod Options (master enable, cooldown, default wardrobe label)

## Next

- [ ] WorkType triggers (Cook / Doctor / Animals) when a work job is issued
- [ ] Ideology ritual start (`RitualBehaviorWorker.TryExecuteOn`)
- [ ] Hysteresis / reservation so two pawns never claim the same apparel Thing
- [ ] Add-mode (layer on top) vs Replace polish; drop-to-inventory option
- [ ] Assign-tab feel polish; tip when wardrobe empty or policy missing

## Later

- [ ] Gravship / Odyssey captain kit
- [ ] Anomaly psychic rituals
- [ ] Patient-as-surgery-target gown
- [ ] Dresser furniture / dedicated wardrobe building
- [ ] VE apparel pack smoke tests
- [ ] Docs site page + Workshop preview
