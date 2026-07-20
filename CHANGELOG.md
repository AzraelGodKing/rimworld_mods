# Changelog

Short repo highlights. Detailed notes live in each mod's own `CHANGELOG.md`.

## Unreleased

- **Repo** — `art/brought/` convention + Cursor rule: brought sprites may be installed as-is, never used as generation / style references.
- **Nemesis (in progress)** — personal-antagonist mod foundation landed under `Nemesis/` (`AzraelGodKing.Nemesis`). Core hunt loop, multi-type harassment, soft compat stubs with Stormproof / Strata / Homesteader. Original foundation by Dredd (Misakabob); expanded here. Public site teaser remains mysterious at `docs/signal.html`. **No public `Nemesis.zip` download** for now (CI still compiles). → [Nemesis/CHANGELOG.md](Nemesis/CHANGELOG.md)
- **Docs** — mysterious hub teaser + `docs/signal.html` for the personal-antagonist idea (classified / incoming-signal framing).
- **Homesteader / Stormproof** — cinematic Workshop preview makeover (Strata-style painted scenes) + docs hub cards use the new banners.
- **Repo** — Removed decompiled RimWorld scratch under `Strata/Tools/`. Download zips no longer committed; CI builds all four mods (incl. Nemesis), publishes zips to the rolling `latest` GitHub Release (PR runs stay read-only; `latest` tag force-moved to the build SHA), and PRs get compile-only checks. Root PowerShell utilities moved to `scripts/`. Dropped leftover `docs/wellspring.html` and `scripts/fix_wellspring_textures.ps1`.
- **Strata / Homesteader / Stormproof** — mod-local performance (FPS+-inspired, no global FPS+ clones): idle raid skips, gas overlay/motes on viewed map only, dirty-flag root cellar cooling, staggered allergy scans, ion-storm LINQ removal.
- **Homesteader** — Polyarmory trait (polycule bed-sharing); Tastes tab + rare allergies; docs site redo; Kats Effect Super Chat / hostile Kats; 1.6 favorite-food Harmony fix. → [Homesteader/CHANGELOG.md](Homesteader/CHANGELOG.md)
- **Strata** — gravship orphan-level adoption + damaged-save repair (rotated land rewire, adopt travelling floors, load-time rebind). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — harden against outdated `TraverseParms.For` (1.6) so mechs/robots stop idling; cross-level haul Refuel + ReachableLevels snapshot; gravship orphan repair. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — cross-level haul: auto-refuel + higher-priority storage pulls (Refuel job + ReachableLevels snapshot fix); Anomaly Capture Harmony startup fix (`__result` first). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — omni inter-floor connector (power + all fluid shafts, wall-placeable); cross-level caravan formation (linked-level pawns walk to surface gathering); mech work/charge across floors; sleep/baby bed ownership harden; cross-level bill ingredient pull + Anomaly platforms; gravship underdeck land; takeoff rescues; float menus + childbirth doctor + capture/arrest + prisoner/infant beds; VTE shaft AC; stockpile haul; sleep/work relays; bedrooms (#31); Russian; gas defaults/perf. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Stormproof** — late-game **hazard hardening**: atmospheric barrier, climate stabilizer, sky restorer, fire suppressor, drought condenser + heat dome / polar front / toxic surge / dry lightning events; research tab XML fix; Languages scaffolding. → [Stormproof/CHANGELOG.md](Stormproof/CHANGELOG.md)

## Released

- **Strata 2.0** (2026-07-16) — Dig / build / breathe; Odyssey gravship stacks. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Homesteader 1.0** — Initial farmstead release. → [Homesteader/CHANGELOG.md](Homesteader/CHANGELOG.md)
- **Stormproof** — Grid defense and storm tools. → [Stormproof/CHANGELOG.md](Stormproof/CHANGELOG.md)
