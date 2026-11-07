# Changelog

Detailed notes for **Date Night** only.

## [Unreleased]

## [2.0.0]

Player-facing version **2.0.0** (`About.xml` `modVersion`). Stamp `datenight2-net10`. Ships for RimWorld **1.6** and **1.7** from the same source; 1.6 is not frozen.

AZR-511 AZR-521 AZR-524 AZR-538 AZR-539 AZR-542 AZR-557 AZR-559 AZR-560 AZR-574

### Added
- **Ask out tonight** (AZR-524) — pawn gizmo on an adult colonist with a lover or ladder partner. Plans one date at the first hour from 18:00 to 23:00 that neither partner painted Sleep or Work (falls back to 18:00), saved as `dateNightPlannedDates`. Starts both pawns when the hour comes unless drafted, in a ritual or on a need job; after a 2-hour grace a "missed" message clears it. Cancel gizmo while planned. Schedules are not touched.
- **Romance panel** (AZR-524) — "Romance" gizmo opens `Dialog_DateNightRomance`: stage (or ladder progress), time together and anniversary day, last date (how long ago, quality, date count), favourite venue and score, double-date rapport, planned date. Per-couple history is new, saved as `dateNightCoupleHistory` (missing on a 1.x save = no dates yet). Toggle `showRomanceGizmos`.
- **Smarter quality** (AZR-559) — `QualityScore(DateQualityInputs)` is now a pure function. New inputs: mutual opinion (the lower of the two; 60+ is +1, below 0 is −1), either partner Kind (+1) or Abrasive (−1), a fine or lavish meal eaten on the date (+1, saved on the job as `ateFineMeal`), and room impressiveness 65+ (+1, indoors). The roll is still seeded per couple and day.
- **Weighted activities** (AZR-559) — the pick is weighted by `CoupleTaste` (either partner): Gourmand toward dinner (+1.5) and picnic, Ascetic toward walk and stargaze (+1) and away from dinner, Night Owl toward stargaze (+1.5), an Artistic passion toward picnic, an Animals passion toward fishing. It stays deterministic per couple and day. Traits are looked up by defName.
- **Alternating gift giver** (AZR-559) — `IsGiftGiver` flips by `DaysPassed` parity. It used to be always the lower thing id. Walk lead and stand order still use the stable `IsInitiator`.
- **Fishing date** (AZR-574) — `DateActivity.Fishing = 9` (appended). Odyssey only: a standable cell beside a `Zone_Fishing` that both can reach (zones in id order). Meditative joy.
- **Trying for a baby** (AZR-574) — Biotech gizmo on lovers who can conceive (opposite sexes, not pregnant, `PregnancyChanceForPartners > 0`). It halves the Lovin MTB and gives the short cooldown on Lovin hours, even in pregnancy-safe mode. It is cleared in the 2,500-tick sweep once they can no longer conceive. Saved on `CoupleHistory.tryingForBaby`. Pregnant partners never get dance or walk dates (double-date guests included).
- **Proposal weight** (AZR-574) — postfix on `InteractionWorker_MarriageProposal.RandomSelectionWeight`: ×(1 + 0.25 per wonderful date in a row, max 4). `CoupleHistory.wonderfulStreak`.
- **`DateNightApi`** (AZR-574) — `ApiVersion` 1 with `IsOnDate`, `HadRecentGoodDate`, `YearsTogether` and `NotifyBreakup` (runs the breakup reaction now; the 2,500-tick poll stays as the fallback).
- **Soft Multiplayer** (AZR-560) — `DateNightMultiplayer` registers `DateNightAskOut.Plan` / `Cancel`, `DateNightBaby.SetTrying` and `DateNightUtility.CopyHours` through `Multiplayer.API.MP.RegisterSyncMethod`, found by reflection (no DLL). Host config sync is required. The two-client test is still to do.
- **`COMPATIBILITY.md`** (AZR-560) — matrix of sister mods, DLCs, `loadAfter` mods, name-matched packs and Multiplayer, with 1.6 and 1.7 columns. Every row is "Untested" until checked in game.
- **Gift picker** (AZR-524) — `Dialog_DateNightGiftPicker` lists ingestible items with search. The settings gift list is rows with a remove button; names with no `ThingDef` show in red.

### Changed
- **Settings** (AZR-524) — scroll view with Lovin, Dates, Gifts, Romance and Schedule tab sections (fits Russian at 1280×720).
- **Nobles** (AZR-559) — date meals use `WillEat(..., careIfNotAcceptableForTitle: true)`. With no allowed meal, the date is not a dinner or picnic.
- **Ideology** (AZR-574) — the Lovin-window "made it" mood and the private-time mood need `InitiatedLovin` to be allowed (the ideoligion's own precepts decide). This is done in code rather than with guessed precept defNames.
- **Homesteader / Niceties** (AZR-574) — the gather-spot pick puts `Homesteader_HarvestMaypole` first. The Lovin-window bed claim skips rooms that Niceties marked shared (`Niceties.CompSharedRoom.Shared`, by reflection). The startup patch check now covers 12 patches (13 with RJW).
- **Date button column** (AZR-524) — keeps the known-mod table (Royalty, Rimbody, Schedule Everything) but no longer counts every other Harmony owner of `DrawTimeAssignmentSelectorGrid` as a button. New `scheduleColumnOffset` setting (−4 to +8) for unknown patchers. `Pawn.GetGizmos` added to the startup patch check (now 11, 12 with RJW).
- **Two builds, one source** (AZR-521) — official 1.6 loads `1.6/Assemblies` (net472, 1.6 refs). 1.7 loads `1.7/Assemblies`, now a .NET 10 build against the 1.7 refs (`Krafs.Rimworld.Ref` 1.7.4998-unstable, Harmony 3.0.0-rc.4). Both report 2.0.0, so the shared `About.xml` version and the update letter are right on both.
- **Code cleanup** (AZR-557) — nullable reference types on every Date Night source file with no warnings; mod settings are never null, so the 23 null checks are gone; RimJobWorld `can_fuck` / `can_be_fucked` / `can_masturbate` bind once to `Func<Pawn,bool>` delegates and `RJWSettings.override_lovin` to a static field ref (no per-call `object[]`, no boxing); the RJW JobDefs are looked up once; the three pair-selection debug actions share one helper.
- **1.7 safety** (AZR-538) — one startup line says whether all 10 patches applied (11 with RimJobWorld), or names the ones that did not; the Schedule-tab combo draws with `Widgets.DrawBoxSolid` instead of a raw `GUI.DrawTexture`; the RJW `JobDriver.Cleanup` postfix is only applied when RJW is active (`Prepare()`); dinner dates add the `NutritionEaten` record like vanilla `FinalizeIngest`.
- **Save upgrade** (AZR-539) — new `dateNightSaveVersion` (2; a missing key means a 1.x save, logged once on load). `dateNightWasOnLovin` / `dateNightWasOnDate` and the open Date/Lovin windows (`dateNightSocialWindows`) are saved, so a transition across a save is still seen and an open window still awards made-it / stood-up. `FavoriteVenue` gains `wereLovers` (default true, so 1.x entries keep working). Every 1.x type name and Scribe key is unchanged and `DateActivity` stays append-only, so no `BackCompatibilityConverter` is needed. Nothing removed from the save.
- **Performance pass** (AZR-542) — new `DateNightPawns`: the colonist list and an id map are built once per tick (own copy, since `PawnsFinder` refills one shared list), the full world-pawn map only when an id is not a colonist, and `ExistingMostLikedLovePartner` once per pawn per tick (was 4–6 calls per Lovin pawn per second across 19 sites). The 60-tick schedule pass returns early for pawns on neither slot with no open window. The rendezvous cache holds the `Building_Bed` and re-checks reach every 250 ticks instead of scanning every bed by id. Lovin / RJW job detection and ritual duties are classified once per `JobDef`, driver `Type` and `DutyDef` instead of 10+ `IndexOf` calls per check. The date job's ritual and `CanDate` FailOn runs once a second, not every tick. Gather spot, joy building and dinner table are memoised per couple per tick and checked for reach in id order until one passes; meals and gifts are scored first and reach-checked best-first (ties break on thing id). Scratch sets are reused. Profiler numbers on a 30-colonist 1.7 save still need Azrael's game.

### Fixed
- **Gradual romance demotion** — `TickBreakups` treated any couple without a love relation as broken up. Ladder couples (friends, sweethearts, boyfriend/girlfriend) were never lovers, so after every date the pair was "broken up": boyfriend/girlfriend dropped to sweethearts and progress reset, and lovers only happened on a lucky wonderful date. A breakup now needs `wereLovers`.
- **Joy-giver dates** — dates started by `JobGiver_GetJoy` skipped `EnsureBondStarted` and `BindIfPossible`. Both paths now use `DateNightDateUtility.MakeDateJob`.
- **Mismatch alert** — used `FindDatePartner`, which also returns a speculative new friend, so it flagged pairs who were not dating. It now uses `FindCommittedPartner` (lover or ladder bond), as does the inspect line. New-friend search only runs for a pawn who painted Date or Lovin.

## [1.8.0]

Player-facing version **1.8.0** (`About.xml` `modVersion`). Stamp `gradual-romance-v1`.

AZR-651 AZR-652 AZR-521 AZR-538 AZR-539 AZR-542 AZR-575

### Added
- **Gradual romance** (AZR-651) — adults without a lover can date as friends, then sweethearts, then boyfriend/girlfriend. Two good dates plus opinion (15 / 25 / 40 / 50) climb each rung; a wonderful date counts double and an awkward date does not count. The last step adds a vanilla Lover relation and a letter. Lovin stays locked until they are lovers or spouses. Existing couples skip the ladder. Opinion collapse steps the pair down. Toggle in settings.
- **Romance on the Rim soft-compat** (AZR-652) — fail-open. Date Night does not start a date while RotR is already running a kiss, stroll, proposal, or other `RomanceOnTheRim_*` / `RotR_Job_*` job. A finished date calls `RomanceUtility.TryAffectRomanceNeedLevelForPawn` (stroll / lovin / kiss Tuning amounts; +0.10 on an anniversary date). Pawns without RotR's romance need (usually not lovers yet) are a no-op. `loadAfter` when present; no hard dependency.

### Changed
- **1.6 / 1.7 dual package** (AZR-521 AZR-538) — confirmed: official 1.6 loads `1.6/Assemblies`, unstable 1.7 loads `1.7/Assemblies`.
- **Save upgrade** (AZR-539) — new romance bonds scribe as `dateNightRomanceBonds` with PostLoadInit null-guards. Older 1.6.1+ saves load; missing keys start empty.

### Fixed
- **Pawn id lookups** (AZR-542) — `FindPawnById` builds a per-tick dictionary instead of scanning the world list per key.

### Internal
- English, Chinese Simplified, and Russian keys plus four new stage thoughts (AZR-575).

## [1.7.2]

Player-facing version **1.7.2** (`About.xml` `modVersion`). Stamp `date-state-gifts-v1`.

AZR-522 AZR-523 AZR-540

### Changed
- **Separate 1.6 and 1.7 assemblies.** Official 1.6 loads `1.6/Assemblies`. Unstable 1.7 loads `1.7/Assemblies`.

### Fixed
- **New-game state leak** (AZR-522) — GameComponent ctor resets date ticks, anniversary years, venues, double-date sessions, and bed claims (a new game never runs ExposeData).
- **Deterministic gift selection** (AZR-540) — gifts resolve to a `ThingDef[]` sorted by `defName`; equal-distance tie-break uses `thingIDNumber`.
- **Date robustness** (AZR-523) — FailOn when the partner is drafted or not on `DateNight_GoOnDate` (250-tick start grace); partner death skips ruined thought/venue penalty; schedule flags seed in FinalizeInit without enter transitions.

## [1.7.1]

Player-facing version **1.7.1** (`About.xml` `modVersion`). Stamp `thought-partner-v1`.

### Changed
- **Workshop preview** — new `About/Preview.png` (evening table, walk, and stars). Docs card synced.
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6. Existing 1.6 DLL; no gameplay change.

## [1.6.2]

Player-facing version **1.6.2** (`About.xml` `modVersion`).

### Changed
- Declared RimWorld **1.7** in `supportedVersions` alongside 1.6. Existing 1.6 DLL; no gameplay change.

## [1.6.1]

Player-facing version **1.6.1** (`About.xml` `modVersion`). Stamp `thought-partner-v1`.

### Fixed
- **Partner's name in thought descriptions** (`thought-partner-v1`, forum report from Victorraze): mood thoughts like "our spot" read "That place with *[own name]* is ours" instead of naming the partner. Vanilla fills `{0}` in a thought description with the pawn who has the thought. The 17 social Date Night thoughts now use `DateNight.Thought_DateNightSocial`, which fills `{0}` with the other pawn (partner, or the other couple on a double date). English, Chinese, and Russian text is unchanged. Memories already in a save keep the old wording until they expire.

## [1.6.0]

Player-facing version **1.6.0** (`About.xml` `modVersion`). Stamp `couple-lifecycle-v1`.

### Added
- **Milestone anniversaries** — every fifth year together gets its own letter and a bigger, longer-lasting anniversary mood.
- **Double-date rapport** — couples remember how their double dates went. Pairs who get along pick each other again (and stay friendly even if one of them is a bit prickly); pairs who keep clashing stop being paired up.
- **Reunions** — when partners have been apart for two days or more (caravan, kidnapping, another map) and end up together again, both get a "reunited" mood.
- **Breakups** — when a couple who went on dates breaks up or divorces, both get a short "no more date nights" mood and their favourite date spot is forgotten.

### Changed
- **Thoughtful gifts** — a partner no longer gifts something the receiver would refuse: no drinks or drugs for teetotalers, nothing their drug policy or ideoligion forbids, and no food they wouldn't eat.

### Fixed
- **Double date no-show** — the "double date fell through" mood now uses the same adults-only check as every other Date Night mood.
- **Dev tools** — the "Force private time" debug action no longer errors on a pawn without an age tracker.

### Internal
- One shared `TryGainThought` helper replaces five identical copies (windows, double dates, venues, anniversaries, reunions). `CoupleKey` and `BothCanReach` copies are merged, and the unused `FindDinnerSpot` is removed. No behavior change.

## [1.5.1]

Player-facing version **1.5.1** (`About.xml` `modVersion`). Stamp `schedule-sync-v1`.

### Added
- **Schedule sync** (`schedule-sync-v1`) — click the schedule-mismatch alert to copy painted Date/Lovin hours from one partner to the other.
- **Configurable gifts** — settings list of gift ThingDef names plus optional joy-luxury scan for modded items.
- **Favourite venue destroyed** — one-shot `DateNight_VenueDestroyed` thought when a remembered high-score venue building/cell is gone.
- **Anniversary gift bias** — activity picker prefers Gift on anniversary days.

### Fixed
- **Gift handoff** — failed inventory transfer drops the gift at the partner's feet and still grants the gift thought (no silent "delivered").
- **Ephemeral caches** — rendezvous-bed and social-window caches clear on new/load game so thing IDs cannot collide across colonies.
- **Self-lovin ageTracker** — null-guard before Adult check in `NotifySelfLovinFinished`.

## [1.5.0]

Player-facing version **1.5.0** (`About.xml` `modVersion`). Startup writes `[DateNight] v1.5.0 loaded from ...` in Player.log (`rjw-soft-v1`).

### Added
- **RimJobWorld** (`rjw-soft-v1`) — fail-open soft-compat (`rim.job.world`). When RJW's `override_lovin` is on (the default), scheduled couple lovin starts `JoinInBed` instead of vanilla `Lovin`, so RJW's Harmony prefix cannot cancel the job and leave a gap Date Night would refill. Private time starts `RJW_Masturbate` (`JobMaker` targets match RJW's own job giver: pawn, bed, cell) so the sex need is satisfied; Date Night's mood thought and lovin cooldown apply when that job succeeds during Lovin hours. Couple eligibility also fail-opens through `xxx.can_fuck` / `can_be_fucked`. No RJW project reference; missing RJW is a no-op.

## [1.4.0]

Player-facing version **1.4.0**.

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

## [1.1.1]

Player-facing version **1.1.1**. Startup writes `[DateNight] v1.1.1 loaded from ...` in Player.log (`update-news-v1`).

### Added
- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Anniversaries** (`azr-81`) — on the anniversary of the current love bond (oldest lover / fiancé / spouse start tick): a letter, a small “anniversary” thought for both (+4 / +6), then one of two specials — **anniversary date** (+10 / +15) if they finish a date that day, or **missed anniversary** (−6 / −8) at the next midnight if they never went out. Dating replaces the small thought. Date quality also gets a +2. Off in settings.
- **Favourite spots** (`azr-82`) — couples remember the venue of finished dates (table / gather spot / outdoor cell). Wonderful dates pull them back; a ruined date can sour the place (`our spot` / `that place is ruined` thoughts). Off in settings.
- **Double dates** (`azr-83`) — two couples with overlapping Date hours may share a venue. Quality rolls once for the group; finishing spills opinion onto the other pair (friends vs rivals). Walk/gift activities coerce to hangout so four pawns can sit still. If the other couple never starts within ~1 hour, the hosts date alone (`double date fell through`). Off in settings. Ideology / Biotech reroutes still go through `CanDate`.

### Fixed
- **Harmony JobDriver.Cleanup** — guarded PatchAll was feeding `JobDriver_Date` / `JobDriver_SelfLovin` to Harmony because they inherit vanilla `Cleanup(JobCondition)`. Harmony treated that as a patch auxiliary and failed both classes at load. Only types with `[HarmonyPatch]` are processed now.

## [1.0.2]

Player-facing version **1.0.2**.

### Fixed
- **Guarded Harmony** — each patch class is applied on its own; one missing target logs and skips instead of aborting the rest of Date Night.
- **Schedule tab warning** (`schedule-imgui-v1`) — opening the Schedule tab no longer warns. Date still paints last on the extra row (after Clean / Workout / …) so it is not nested in another dropdown, but it is a postfix after `Priority.Last` instead of a Harmony finalizer (finalizers wrap this OnGUI method in try/catch). Extra-column detection is cached instead of scanning Harmony/`TypeByName` every frame.
- **Rituals vs Date hours** (`ritual-date-v1`) — scheduled Date / Lovin no longer InterruptForced pawns out of Ideology rituals (or other lords that forbid long-need jobs). The ceremony can finish; the date waits until it does.
- **Date hours idle apart** (`azr-16-date-together-v2`) — every date activity (walk, picnic, stargaze, dinner, dance, hangout, gift, recreation) could send each partner to a different cell, so they stood rooms apart throwing hearts. Both now share one venue (adjacent seats at a table / campfire; one outdoor cell for walks). The walk leader waits if the partner lags; parked activities wait at the spot. Dates are no longer yanked by casual jobs (hauling floors, etc.). The follower no longer freezes facing one way (the date toil owns facing and now looks at the path while walking and at the partner while standing). (`azr-14-schedule-combo-v3`) — Date is one extra Schedule cell immediately after the last extra button (Clean / Workout / …), drawn last so it is not inside another dropdown. Face is Date; Lovin is in the ▾ menu. The cell is clamped so it cannot cover Manage areas.
- **Lovin animation loop** (`azr-15-lovin-anim-v1`) — with Rimworld Animations / RJW / breeding-ritual packs, Date Night re-issued vanilla `Lovin` every second because those mods swap in their own job. The animation never finished, so pregnancy never rolled. Scheduled lovin now waits until the current lovin/sex/animation job ends, then starts a new one only if the Lovin hours are still on.

### Added
- **Date cooldown** (`date-cooldown-v1`) — after a finished date the couple waits the same span as post-lovin (~4–8 hours; Eager ~100 ticks) before another scheduled date. Dev *Force date* still starts immediately. Persists in the save.
- **Date activities** — dates resolve a real activity from what's available on the map instead of always standing at a gather spot: **dinner** (fetch a meal, eat by a table), **picnic** (meal at a pretty outdoor cell, fair weather), **walk** (amble between scenic waypoints, initiator leads), **stargazing** (lie face-up outdoors at night), **dancing** (spin at a gather spot), **gift** (initiator fetches beer / chocolate / ambrosia / psychite tea / insect jelly and hands it over — receiver gets a +6 mood / +10 opinion thought), **recreation** (hang near a joy building, double joy trickle), and the classic **hangout** fallback. Both partners resolve the same activity from a shared couple+day seed. All dates trickle a little joy (Social; Meditative for stargazing). Off in settings (`Date activities`).
- **Date quality** — outcomes vary with the activity, venue beauty, weather, and a shared seeded roll: **wonderful date** (+8 mood / +12 opinion), the usual nice date, or **awkward date** (+1 / +1). Off in settings (flat nice-date thought).
- **Ruined date** — a date cut short after it properly started (draft, mental break, or an active hostile threat) leaves a −4 *date ruined* mood for 0.75 days.
- **Post-date lovin spark** — a finished date multiplies the couple's lovin MTB by 0.25 for ~1 in-game day (stacks under the schedule boost, persists through saves). Off in settings.
- **Per-activity job reports** — "having a dinner date with X", "stargazing with X", etc., in EN/RU/CN.
- **Dev tools** — *Force date with activity* (float menu; override window ~4 in-game hours so the partner resolves the same activity) and *Clear forced date activity*.
- **Schedule mismatch alert** — if one lover has Date or Lovin painted and the other does not (or they painted different hours), the alerts bar shows Schedule mismatch.
- **Date hours** — teal Schedule slot. Partners walk / table / hang out together. Mood + opinion, no bed, no pregnancy.
- **Missed date** — if a partner is drafted, in a medical bed, or off-map for the window, the one who showed up gets a short stood-up thought. Both making it grants a small boost.
- **Window bed claim** — rendezvous double is assigned for Lovin hours, then previous beds restore. Off in settings.
- **Ideology / Biotech** — no-lovin precepts and sterile genes skip forced lovin and reroute to a date. Missing DLC fails open.
- **CN / RU** — keyed settings plus DefInjected TimeAssignment, Job, and Thought labels.
- **Private time (self-lovin)** — adults on Lovin hours can use any bed (including a single) when a partner is not sharing a double. Mood thought only; no pregnancy. Off in settings. Children never qualify.
- **Docs site** — [`docs/datenight.html`](https://github.com/AzraelGodKing/rimworld_mods/blob/main/docs/datenight.html) (how-it-works, settings, install) wired into the hub nav / comparison table; CI packs `DateNight.zip` for the download button.
- **Workshop ID** — `About/PublishedFileId.txt` (`3774158903`) so uploads update the existing item; included in the docs stats roster.
- **Workshop preview** — `About/Preview.png` (~748 KB, schedule-hero option) for Steam upload size limits.
- **Lovin schedule** — `DateNight_Lovin` TimeAssignmentDef on the Schedule tab (rose). While assigned, colonists seek bed like Sleep (including no-sleep / full-rest pawns), work priority matches Sleep, and lovin MTB is shortened to Always-Do-Lovin rates when a partner shares the bed. Default keeps vanilla post-lovin cooldown (pregnancy-safer); optional Eager mode shortens cooldown too.
- **Schedule button** — Harmony draws Lovin next to Sleep/Meditate (vanilla hardcodes the assignment grid, so XML alone never showed a button).
- **Dev tools** — Date Night debug: make selected pair lovers, click-to-pair with selected, paint Lovin or Date all day, force lovin / private time / date now.

### Fixed
- **Lovin hours with no double** — GetRest falls back to vanilla sleep (and medical rest) instead of leaving the pawn idle. Either partner in the bed may start lovin.
- **Rimbody / workout schedule overlap** — Lovin sat on Rimbody’s Workout cell, so the rose button showed Lovin but clicks opened the workout / Joy dropdown. Lovin now takes the next free extra column (also Exosuit Piloting and Schedule Everything). Fail-open if those mods are absent.
- **Exosuit Framework schedule overlap (DN1)** — Piloting also takes an extra column; Lovin sits one slot further right (no longer covers Joy).
- **Pregnancy-safe cooldown** — default setting no longer zeros `canLovinTick` on the Lovin schedule; Eager mode still shortens cooldown.
- **Exosuit Framework schedule overlap (DN1)** — when Exosuit / Piloting occupies the same Schedule-tab slot as Lovin, Lovin draws on the second row so both stay clickable (fail-open if Exosuit absent).
- **Startup TimeAssignmentDefOf warning** — Harmony `PatchAll` no longer runs in the Mod ctor (compiling the Schedule-tab patch touched `TimeAssignmentSelector` / `TimeAssignmentDefOf` before DefOfs init). Patches apply in `StaticConstructorOnStartup` after defs load.
- **Lovin never firing on schedule** — pawns preferred their own single beds (vanilla requires a shared double), hourly chance burned while waiting alone, and MustKeepLyingDown kept them stuck. Now both path to one cached rendezvous double bed; tick forces LayDown + Lovin when both are in it.
- **Bed claim storm** — tick loop called `ClaimBedIfNonMedical` on a new free double whenever reserve checks failed, so couples ate every bed on the map. Ownership claims removed from the hot path; one rendezvous bed is cached per couple and LayDown’s own toil handles claiming.
- **Needs before lovin** — hungry (or wanting food), chemical desire, ingest/flee/player-forced jobs, and exhausted rest block bed-forcing / lovin so colonists eat (etc.) first, then date.
