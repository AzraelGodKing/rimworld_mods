# Changelog

Detailed notes for **Date Night** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

Steam Workshop paste: [`About/changelog.txt`](About/changelog.txt).

## [Unreleased]

Player-facing version **1.0.1** (`About.xml` `modVersion`). Startup writes `[DateNight] v1.0.1 loaded from ...` in Player.log.

### Changed
- **Workshop preview** (`preview-redraw-v1`) — new `About/Preview.png`: dusk walk + picnic, RimWorld pawn bodies, clean DATE NIGHT type (no fake Schedule UI). Docs hub card synced. ~520 KB, under Steam's 1 MB cap.

### Fixed
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
- **Docs site** — [`docs/datenight.html`](../docs/datenight.html) (how-it-works, settings, install) wired into the hub nav / comparison table; CI packs `DateNight.zip` for the download button.
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
