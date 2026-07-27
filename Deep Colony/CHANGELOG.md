# Changelog

Detailed notes for Deep Colony only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **Docs teaser** — mystery hub page (`docs/ledger.html`, nav `···`) — fragmentary ledger, no download, no public name. Stamp `ledger-mystery-v1`.

### Added
- **Counsel trauma job** — doctors (and right-click) can run counseling sessions that advance trauma recovery; random therapy chat remains as a light supplement.
- **Mentoring work type** — dedicated `DC_Mentoring` work type (no longer buried under Warden), with skill-gap and no-mutual-loop rules.
- **Artistic perk tree** — inspired hand / master artisan (levels 5 / 15).
- **Faction reputation hooks** — successful trades, visitor gifts, and shared-enemy kills (fractional drift accumulates).
- **Birth inheritance** — `PregnancyUtility.ApplyBirthOutcome` plus spawn/join paths; richer passion inheritance (minor + rare major).
- **Captivity on rescue** — former player colonists returning from enemy factions get captivity trauma; mentorship links clear on death.
- Inspect readout for perk points, mentor/apprentices, and active trauma.
- Build stamp `systems-complete-v1`.

### Fixed
- **Therapy prolonged trauma** — healing incorrectly decreased thought age; vanilla memories expire when age rises, so therapy now advances age toward expiry.
- **Mentor Social XP explosion** — teaching toil awarded Social XP every tick (~80k/session). Now interval-based, and active sessions clear on job interrupt.
- **Only one mentoring pair colony-wide** — active-session registry now supports concurrent pairs and resets on load.
- **Ally goodwill collapse** — natural drift was −1 goodwill per hour for every friendly faction (~−24/day). Now rare MTB drift only above +40 / below −40.
- **Combat shock on every down** — now 40% chance and only from hostile damage (1.6 private `MakeDowned` hook).
- **Perk points every skill level** — points only at perk gate levels 5 and 15.
- **Crafting perks used WorkSpeedGlobal** — swapped to modest `GeneralLaborSpeed` so they don't accelerate all work.
- **Perk hediffs missing after load** — reapplied from unlocked perk list on spawn/load.

### Added
- **Massacre trauma** — 3+ colonist deaths in one day applies massacre survivor trauma to living colonists on that map.
- **Captivity trauma** — joining the colony after non-player imprisonment applies captivity trauma.
- **Bereavement shock** — a second close violent loss while grieving upgrades to bereavement shock.
- **Birth inheritance** — children born into the colony (no `SetFaction`) now run inheritance on spawn.
- Build stamp `ship-review-v1` logged at startup.

### Changed
- Apprenticeship XP multipliers: passive 1.25× / active 1.40× (was 1.4 / 1.6).
- Softened combat shock / violent loss / massacre mood hits; trimmed top-tier construction, research, and cooking perk magnitudes.
- Therapy social interaction weight scaled down so it doesn't dominate chat.
- Faction raid/trade drift amounts slightly reduced.
