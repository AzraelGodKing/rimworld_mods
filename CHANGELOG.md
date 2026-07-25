# Changelog

Short repo highlights. Detailed notes live in each mod's own `CHANGELOG.md`.

## Unreleased

- **Strata** — ancient/excavated stair freeze on first open (`dig-stair-freeze-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — rescue downed pawns to beds on another level (`rescue-across-levels-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — shaft/fluid performance pass (`shaft-perf-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — VHGE feed/equalize on PipeSystemTick + silence Rimefeller when absent (`vhge-pipesystem-tick-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — VHGE AASB feed + junction place lag fix (`vhge-aasb-lagfix-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — VHGE cross-floor gas actually flows (Extra* accumulates over PipeSystem interval) (`vhge-crossfloor-flow-v2`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — VHGE shaft junction matches power/DBH demand+bootstrap flow (`vhge-power-shaft-parity-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — VHGE Helixien wakes without a local tank (`vhge-tankless-wake-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — DBH + VHGE shaft fluid: AASB-style 0.5 fill equalize + Helixien DirectFeed (`fluid-aasb-flow-v1`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — VHGE Helixien gas flows across shaft junctions. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — DBH shaft water without local tanks (junction mini-tower + PullWater). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — shaft power charges linked batteries; upward flow + transfer cap. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — shaft power every-tick / battery-free host surplus (incl. dark-floor demand bootstrap); cross-level hostile threat letters (`power-threat-crosslevel-v4`). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Repo** — `art/brought/` convention + Cursor rule: brought sprites may be installed as-is, never used as generation / style references.
- **Repo** — ignore `Homesteader/Textures.rar` (local texture dump).
- **Docs** — Strata V3 roadmap: G1–G7 merged; **More UX M1–M10 ✅** on `V3-M-ux` (`m-ux-finish-v1`). → [docs/strata-roadmap.html](docs/strata-roadmap.html)
- **Strata** — More UX finish: medical pin, reinstall haul, modder API, visitors, VF roof-deck, depth dim. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — hostile lost miners now get a raid-style threat letter. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — Simplified Chinese (简体中文) language pack. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship: clear B1 ghost hull rings after land + force-cull non-destroyable duplicate elevators (no debug-log pop). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship: block mid-flight underdeck view + land MapDrawer regen (RGB neon fix). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — play-settings tips + gas overlay `load`/`Gas:` labels moved to Keyed (EN; RU English placeholders). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — M10: level-switch flicker / `ArgumentOutOfRangeException` on CurrentMap index. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — M9: pilot console / launch ritual reachable from linked A+/B+ floors. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — V3 level depth cap: ±2 default (was ±4), Unlimited levels setting off; large gravship underdeck sync hitch reduced. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — V3 G1: pin takeoff grav engine thingID for land rebind (no PreferBestEngine/CurrentMap). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship land: stop double-snapping underdeck contents (walls/beds drifting into rock). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship land: reclaim packed shafts / prefer engineDelta so furniture stays on the pad. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship takeoff: despawn host stairs on GravAnchor-kept maps (not only on abandon). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship land: stop periodic content snaps (RGB / HiddenConduit spam after bad shaft restore). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship land: shaft-snap to on-ship stairs + exact deck paint (fix Venn offset). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship land: clear orphan ghost pads (no deck restore under off-ship stragglers). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship underdeck: MultiFloors-style pad-only silhouette (engineDelta + void off-pad). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — V3 G2: stable gravship shaft identity (shaftId / stack GUID) for land reconnect. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — V3 G3–G7: land gate, travel entitlement toggle, table-only wiring, shaft grav-extender, A+/B+ deck cargo pack/place. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — gravship land: stop 60k-cell silhouette wipe that RGB-corrupted underdeck after G7 place. → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Strata** — soft-compat with **Vanilla Gravship Expanded - Chapter 1** plus VGE-inspired **gravship life support**; VGE new-game crash fix; `Strata_HaulAcrossLevels` “Collection was modified” (ReachableLevels reentrancy). → [Strata/CHANGELOG.md](Strata/CHANGELOG.md)
- **Nemesis (in progress)** — personal-antagonist mod foundation landed under `Nemesis/` (`AzraelGodKing.Nemesis`). Core hunt loop, multi-type harassment, soft compat stubs with Stormproof / Strata / Homesteader. Original foundation by Dredd (Misakabob); expanded here. Public site teaser remains mysterious at `docs/signal.html`. **No public `Nemesis.zip` download** for now (CI still compiles). → [Nemesis/CHANGELOG.md](Nemesis/CHANGELOG.md)
- **Docs** — mysterious hub teaser + `docs/signal.html` for the personal-antagonist idea (classified / incoming-signal framing).
- **Docs** — Strata V3 target locked (`feature/strata-v3`, includes VGE Chapter 1 compat): New Gravship Linking / Polish / More UX changes, ±2 cap. → [docs/strata-roadmap.html](docs/strata-roadmap.html)
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
