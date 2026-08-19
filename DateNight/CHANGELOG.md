# Changelog

Detailed notes for **Date Night** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

Steam Workshop paste: [`About/changelog.txt`](About/changelog.txt).

## [Unreleased]

### Added
- **Update idea pool** — couple sync, stood-up nuance, anniversary, destination prefs, BPC smoke, Ideology/Royalty/visitor dates. Spec: [docs/ideas/date-night-updates.md](../docs/ideas/date-night-updates.md).
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
