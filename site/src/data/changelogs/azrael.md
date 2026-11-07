# Changelog


## [1.0.0]

First public release. One source builds two DLLs: `1.6/Assemblies` for RimWorld 1.6 (.NET Framework 4.7.2) and `1.7/Assemblies` for RimWorld 1.7 (.NET 10). `LoadFolders.xml` picks the right one, so there is no frozen 1.6 build (AZR-586, AZR-595).

Player-facing version **1.0.0** (`About.xml` `modVersion`). Stamp `series-release-v1`. Startup writes `[Azrael] v1.0.0 loaded from ...; series-release-v1` in Player.log.

### Added
- **Series event weights toggle** (AZR-727) - Mod Options → Azrael has a checkbox to turn off the series incident weights for plain Azrael pacing. On by default.
- **Save report to file** (AZR-689) - writes the hub report as `Azrael hub report.txt` in the RimWorld save-data folder, for setups where pasting from the clipboard is awkward (Linux, Steam Deck). Copy report falls back to the file if the clipboard throws (AZR-603).
- **Runtime and load folder** (AZR-602) - the hub and the report show the CLR and engine (e.g. .NET 10 / Waylith on 1.7, Mono / Unity on 1.6) and which version folder each series mod loaded (`1.6`, `1.7`, or `/`).
- **Version mismatch warning** (AZR-688) - a series mod that does not list the running RimWorld version, or is older than the version this Azrael build was tested with (Strata 4.0.0, Stormproof 2.0.0, Date Night 2.0.0 and the current versions of the rest), gets a yellow note under its row and a `!` line in the report.
- **Bridge summary** (AZR-726) - the Soft-compat section and the report open with "5 of 7 bridges live" (plus how many are broken).
- **Safe removal sees the whole save** (AZR-587) - Prepare to remove now also lists packed (minified) buildings, blueprints and frames, worn apparel and carried or stored items (inside pawns, graves, shuttles and transporters), plants, creatures, floors, active conditions (Stormproof weather), hediffs and memories from any series mod (Date Night, Niceties, Deep Colony), world-map objects (Living World sites, Nemesis hunt sites), factions, and the mod's saved Game/World/Map components.
- **More build stamps** (AZR-588) - Stormproof and Date Night now expose `XBuildInfo.BuildStamp` (the slug each already logged), so their hub rows show a stamp. Homesteader, Nemesis, Living World, Niceties, Wave Survival Reforged and NanoHive Reforged add theirs in their own release branches.
- **Living World traffic weights** (AZR-589) - `Azrael.StorytellerIncidentWeightsDef` gains a `traffic` table (XML-patchable) and Azrael exposes `StorytellerIncidentWeights.TrafficFactor(key)` for Living World to read under the Azrael storyteller: trade caravans set out ×1.5 as often and detour toward colonies ×1.25. Other storytellers get 1. Takes effect once Living World reads it.
- **Deconstruct confirmation** (AZR-690) - designating a mod's buildings for deconstruction asks first, with the building count.

### Fixed
- **Hub refresh** (AZR-602, AZR-725) - the hub rescans every time Mod Options → Azrael is opened. Before, it scanned once per session (only Copy report refreshed it), so a hub first opened at the main menu stayed stale after loading a save.
- **Removal blocker on non-Strata pocket maps** (AZR-587) - colonists on an Anomaly or Odyssey pocket map no longer block removing Strata. The blocker now means "colonists on a map this mod generated" and works for any series mod.
- **Hub page height** (AZR-687) - the scroll area sizes to what the page actually drew last frame, so new rows and sections never clip.
- **Safe-removal deconstruct lost on save** (AZR-684) - "Designate buildings for deconstruction" waited for the next game tick, so pausing, saving and quitting dropped it while the message said it worked. Designations now apply on click, paused or not.
- **Chinese and Russian start text for The Deep Homestead** (AZR-724) - the translated start dialog targeted the wrong scenario part and never showed. It is now keyed by the part's name, so it works with and without Odyssey.
- **Hub with a changed Nemesis** (AZR-685) - the Nemesis checks in Prepare to remove no longer throw if Nemesis renames something; they read as "nothing there" like the other probes.
- **Forced storyteller keeps its state** (AZR-603) - The Deep Homestead swaps the storyteller def in place (as the in-game storyteller change does) instead of building a new storyteller, so difficulty and incident history carry over.
- **Mountain start with other start-site mods** (AZR-603) - the start-tile patch runs last and keeps a pick that is already large hills or mountains.
- **Copy report RimWorld version** - the report always said "(version unread)" because it looked up the version type in the wrong namespace. It now prints the real version.
- **Player.log path on 1.7** - the startup line said "loaded from  (modified unknown)" on RimWorld 1.7, which loads mod DLLs from memory. It now names the mod folder (all series mods share this line).

### Changed
- **Storyteller description** (AZR-611) - the standalone Azrael storyteller now says the first raid is not forced until day 30 (Cassandra: 20). EN/CN/RU.
- **Release metadata** (AZR-611) - About.xml links the mod's GitHub folder, the build stamp is `series-release-v1`, and STEAM.md's playtest list is now the release checklist.
- **Nemesis 2 aware** - the removal blocker counts every engaged Nemesis hunt through `NemesisCompatApi.EngagedHuntCount` when Nemesis 2 is loaded, and falls back to `IsEngaged` on older builds.
- **Living World incident weights retuned** (AZR-589) - refugees ×6 → ×4 and warbands ×5 → ×3 under Azrael, since traffic now brings news too and the warband respects its points cap.
- **Multiplayer: soft support** - safe-removal deconstruct is registered as a Multiplayer sync method (by reflection, no Multiplayer DLL shipped), so it applies on every client. The Conflicts note now says it is synced and that every player needs the host's Azrael settings.
- **Public contracts only** (AZR-585, AZR-686) - the hub reads sister mods through public members: Nemesis `IsEngaged` and `FindNemesisPawn()` (so a pending resolution also blocks removal), Deep Colony's public family-letter list, and Living World's `LivingWorldCompatApi.IsHandlerRegistered(Type)` (until that ships, the goodwill bridge falls back to type presence). The private Nemesis field fallbacks are gone, and opening Mod Options no longer forces Deep Colony's static constructor.
- **Patch binding** (AZR-603) - the series-weights patch pins `IncidentChanceFinal(IncidentDef, IIncidentTarget)` and binds the incident by position, so a 1.7 overload or rename shows up as a failed patch instead of binding wrong.
- **Separate 1.6 and 1.7 assemblies.** RimWorld 1.6 loads `1.6/Assemblies`, RimWorld 1.7 loads `1.7/Assemblies` (.NET 10, built against the 1.7 refs).
- **Released builds** - CI now packs `Azrael.zip` for the site downloads, and Release & Publish accepts Azrael.
- **Build stamps** - the hub no longer scrapes Player.log for a stamp. Only mods that expose `XBuildInfo.BuildStamp` show one.

### Internal
- **Frozen save surface** (AZR-605) - ModChecks now fails if `Azrael.ScenPart_ForcedStoryteller`, `Azrael.ScenPart_PreferMountainStart`, `Azrael.GameComponent_UpdateNews` or their scribe labels change, since saves reference them.
- .NET 10 pass (AZR-601): nullable reference types on every Azrael file with no warnings, a typed series roster with a case-insensitive packageId index, `System.Threading.Lock` in the patch-failure sink on .NET 10, series incident weights keyed by `StorytellerDef` instead of defName strings, switch expressions.
- Shared `ModPack.ForExecutingAssembly` for the UpdateNews and ModVersionLog pack lookup. No gameplay change.
- Removed unread `RemovalLine.CanDeconstruct` and the hub probe for nonexistent `Homesteader_HandDugWell` (the well is `Wellspring_HandDugWell`). No gameplay change.

### Development history (formerly 1.0.0 pre-release)

- **Workshop preview** — `About/Preview.png` is Azrael's own image (the overlook), no longer a copy of Homesteader's.

The 1.1.0–1.4.0 numbers below were internal pre-release bumps that never shipped; their notes are kept as development history.

### Development history (formerly 1.4.0)

AZR-343 AZR-344 AZR-427 AZR-428 AZR-429 AZR-430 AZR-431 AZR-432 AZR-433 AZR-434 AZR-435 AZR-436

#### Added
- **Mountain start for The Deep Homestead** - the default and "Random" starting site now favor large hills or mountains. You can still pick any tile, and the part can be removed in the scenario editor. EN/CN/RU.
- **Safe removal summary** - the removal report opens with one line: how many things to deconstruct, consume or sell, need attention, and are safe.
- **Multiplayer notice** - with the Multiplayer mod active, Conflicts warns that safe-removal deconstruct only acts on your client.
- **New series mods show up on their own** - any other active AzraelGodKing mod is listed in the hub (with its own "Prepare to remove" button) without waiting for an Azrael update.
- **Series events under the Azrael storyteller** (AZR-343) - while Azrael is your storyteller, series incidents win the pick inside their category more often: Living World refugees ×6 and warbands ×5 (their base chances of 0.05 and 0.02 almost never won before), Homesteader's Kats event ×2, Stormproof ion storms and dry lightning ×1.5, Stormproof heat dome, polar front, and toxic surge ×1.25, and Strata's deep vein, prospector, cave breakthrough, and lost miners discoveries ×1.25. Event timing (how often a Misc or threat event fires) is unchanged. Weights live in `Defs/StorytellerIncidentWeightsDefs/Azrael_SeriesIncidentWeights.xml` (`Azrael.StorytellerIncidentWeightsDef`), so other mods can patch them. Other storytellers are untouched.

#### Fixed
- **Living World ↔ Deep Colony goodwill** - the hub only says "live" when Deep Colony's goodwill listener actually registered with Living World, not just when both mods are loaded.
- **Hub scrolling** - the settings page no longer cuts off the last "Prepare to remove" buttons when the failed-patches section is shown.
- **Safe removal off-map pawns** - Deep Colony perks and trauma on kidnapped colonists, quest guests, and other off-map pawns now count in the report.

#### Changed
- **Safe removal deconstruct** - the button now queues the designations and applies them on the next game tick (series multiplayer-safer pattern) instead of editing the map straight from Mod Options. If the game is paused, they appear when you unpause.
- **Build stamps** - the hub reads `BuildStamp` from any series mod that follows the `XBuildInfo` convention (Azrael now does too).
- **Version reset** - `modVersion` and `mod.json` go back to 1.0.0 (download `Azrael-1.0.0.zip`) until the first release; `About/changelog.txt` is now a first-release summary.

#### Internal
- Hub Harmony failures come only from `PatchFailureSink` (`SafePatchAll` already records every series patch).

### Development history (formerly 1.1.0)

AZR-137 AZR-136

#### Added
- **Safe removal wizard** (`removal-wizard-v1`, AZR-137) — hub action lists orphan buildings/items/blockers and can queue deconstruction. Not a save scrubber. CN/RU keys for the hub.
- **Hub health** (`hub-health-v1`, AZR-136) — Mod Options → Azrael now shows DLC, build stamps, Dubs Bad Hygiene bridges (not loaded vs type missing), and failed Harmony patch classes with the exception summary. Copy report includes RimWorld version, all nine series mods, DLC, bridges, conflicts, and patch failures. Works from the main menu. SafePatchAll logs the full exception and reports into Azrael after startup.

### Initial build

#### Added
- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Series hub** — Mod Options → Azrael lists loaded series mods and About versions, live soft-compat bridges (root cellars, wells, Stormproof rooms, Living World goodwill), named conflicts (AASB / MultiFloors with Strata), and failed Harmony patch classes from this session's log. Copy report for support. Read-only; missing mods show as not loaded. EN/CN/RU.
- **The Deep Homestead** scenario — 3 settlers, farm kit, mountain-foothills opener; forces Azrael; MayRequire Homesteader / Strata start research and rock salt.
- **Standalone storyteller fallback** — injects `StorytellerDef` Azrael only when Homesteader is not loaded (Homesteader owns the canonical copy). `PatchOperationFindMod` matches the Homesteader display name so both mods together do not duplicate the def.
- **CI zip** — ~~`Azrael.zip` with the compiled DLL is packed on `latest` so Deep Homestead's forced-storyteller part loads.~~ **Superseded:** Azrael is not release-ready; CI no longer builds or publishes `Azrael.zip`.
- **Soft series load order** — loadAfter Homesteader / Strata / Stormproof / Nemesis / Deep Colony / Living World / Date Night without hard Workshop deps (Harmony only).

#### Fixed
- **RimWorld 1.6 storyteller comps** — Cassandra Classic structure + `CassandraClassic` portraits (standalone patch + Homesteader canonical).
- **Deep Homestead Strata research** — start research → `Strata_DiggingDown` (was incorrectly the research tab defName).
