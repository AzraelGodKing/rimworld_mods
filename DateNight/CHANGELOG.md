# Changelog

Detailed notes for **Date Night** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **Docs site** — [`docs/datenight.html`](../docs/datenight.html) (how-it-works, settings, install) wired into the hub nav / comparison table; CI packs `DateNight.zip` for the download button.
- **Workshop ID** — `About/PublishedFileId.txt` (`3774158903`) so uploads update the existing item; included in the docs stats roster.
- **Workshop preview** — `About/Preview.png` (~748 KB, schedule-hero option) for Steam upload size limits.
- **Lovin schedule** — `DateNight_Lovin` TimeAssignmentDef on the Schedule tab (rose). While assigned, colonists seek bed like Sleep (including no-sleep / full-rest pawns), work priority matches Sleep, and lovin MTB is shortened to Always-Do-Lovin rates when a partner shares the bed. Default keeps vanilla post-lovin cooldown (pregnancy-safer); optional Eager mode shortens cooldown too.
- **Schedule button** — Harmony draws Lovin next to Sleep/Meditate (vanilla hardcodes the assignment grid, so XML alone never showed a button).
- **Dev tools** — Date Night debug: make selected pair lovers, click-to-pair with selected, paint Lovin all day on selected, force lovin now.

### Fixed
- **Lovin never firing on schedule** — pawns preferred their own single beds (vanilla requires a shared double), hourly chance burned while waiting alone, and MustKeepLyingDown kept them stuck. Now both path to one cached rendezvous double bed; tick forces LayDown + Lovin when both are in it.
- **Bed claim storm** — tick loop called `ClaimBedIfNonMedical` on a new free double whenever reserve checks failed, so couples ate every bed on the map. Ownership claims removed from the hot path; one rendezvous bed is cached per couple and LayDown’s own toil handles claiming.
- **Needs before lovin** — hungry (or wanting food), chemical desire, ingest/flee/player-forced jobs, and exhausted rest block bed-forcing / lovin so colonists eat (etc.) first, then date.
