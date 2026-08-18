# Changelog

Detailed notes for Deep Colony only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **Post-2.0 idea pool (Batch C)** — 20 ideas, Phase 6–9 (QoL alerts/filters, kin memory, DLC/sibling hooks, gated envoy/tribute). Spec: [docs/ideas/deep-colony-batch-c.md](../docs/ideas/deep-colony-batch-c.md).
- **GitHub zip restored** — `DeepColony.zip` published again on the rolling `latest` release for non-Steam installs (alongside Workshop).
- **Load order** — `loadAfter` Living World so DC’s soft LW goodwill consumer sees LW signals when both are active (`repo-hygiene-no-debate`).
- **Deep Roots scenario** — generational-colony showcase start; locks Azrael when Homesteader is loaded.
- **CN/RU language spot-check (2.0)** — filled missing Chinese Simplified + Russian Keyed strings for settings/presets, Perks/Legacy/Reputation tabs, Phase 3–5 trauma/rep/power UI; DefInjected for main tabs, Phase 5 perks/hediffs/archetypes, specialty traumas, group counsel, confidant/rival.
- **Workshop / docs polish** — About.xml description rewritten per system for 2.0 (Perks/Legacy/Reputation tabs, capstones/branches/respec/archetypes, specialty trauma + group counsel + confidant, elders/rivalry/blackboard, heirlooms + traditions, ledger + envoys + epithets); docs site rebuilt with per-system 2.0 cards (“New in 2.0” / “Hard preset” tags, what’s-new grid, 2.0 badge) instead of a single blurb; Soft/Default keep power systems & attitude consequences off, Hard enables the heavier set. → [docs/deep-colony.html](../docs/deep-colony.html)
- **Nemesis soft-compat note** — capture/truce goodwill reviewed vs DC ledger: no conflict (Execute/Release = vanilla goodwill; Truce = timer only; DC does not double-buffer). Font’s later Rimesis leader-raid injection idea recorded. → [docs/ideas/nemesis-rimesis-compat.md](../docs/ideas/nemesis-rimesis-compat.md)
- **Phase 5 power systems** (`phase5-power-v1`) — settings-gated skill-20 capstones; branching L15 picks; perk forget/respec (cooldown + mood); cross-skill archetypes; conservative recruit pre-perks; heirlooms on colonist death; chronic stress hediff from untreated trauma expiry (counseling eases it). Most power toggles default off; Hard preset enables the combat/power set.
- **Phase 4 reputation transparency** (`phase4-reputation-v1`) — Reputation main tab with per-faction ledger + pending drift; personal envoy assignment (periodic goodwill / death penalty); attitude states with settings-gated trade/caravan/raid consequences (default off; Hard preset on). All events still flow through `AddFactionDrift`.
- **Phase 3 trauma depth** (`phase3-trauma-depth-v1`) — therapy quality scales with Social/opinion/room; group counsel job; fire/toxic/insect + betrayal traumas; scars + resilience/seasoned growth after recovery; contextual flashbacks; combat habits hediff; optional draft/warden penalties (default off); faction grudges; days of remembrance.
- **Phase 2 mentoring & generations** (`phase2-mentor-gen-v1`) — skill-focus mentor float menu; family mentors need −1 skill gap; Biotech blackboard in room (+15% active mentoring XP); Legacy main tab; dead parents + grandparents inheritance + Biotech gene backoff; family skill traditions; adoptive caregiver passion echo; elders (60+) labor slowdown hediff + mentoring boost + late perk point; tier-1 perk apprenticeship after 3 sessions; professional rivalry (+10% competitive XP).
- **Phase 1 quick wins** (`phase1-quickwins-v1`) — perk numeric tooltips; colony Perks main tab + unspent-points alert (1 day); skill rust slowed/stopped by perk tier + double XP reclaiming lost levels; faction settlement epithets from goodwill; founder/parent surnames on colony-born; apprentice graduation letter + optional passion; confidant relation after 3 counsel sessions (+25% therapy); teaching lineage on inspect.
- **Phase 0 foundation** (`phase0-foundation-v1`) — mod settings (per-system on/off, soft/default/hard presets, combat shock / mentor XP / drift MTB / massacre / inheritance / therapy sliders); expanded Dev tools (clear trauma, force mentor, inject drift, backfill perk gates, settings snapshot); retroactive perk-gate points for recruits who joined past skill 5/15. → [docs/ideas/deep-colony-updates.md](../docs/ideas/deep-colony-updates.md)
- **Phased roadmap triage** — 42 ideas tagged Phase 0–5 on ROADMAP + ideas doc.
- **Living World soft consumer (DC1)** — fail-open register on `LivingWorldSignals`; visible decisive victories / betrayals / refugee flights nudge existing `AddFactionDrift` (shared-enemy boost / ally sympathy). No LW project reference.
- **Chinese Simplified & Russian language packs** — full Keyed + DefInjected translations (perks, trauma, mentoring, inheritance, jobs, thoughts).

### Fixed
- **ISEKAI RPG Leveling traits** — inheritance skips Rank (F–SSS) and destiny traits (Protagonist, Antagonist, Reincarnator, Regressor, Summoned Hero, Sealed Power). Growth / combat / utility Isekai traits (Natural Talent, Prodigy, Mighty, Lucky, …) still inherit like vanilla (`isekai-trait-inherit-v1`).
- **Envoy right-click flood** — pawn float menu is one Envoy submenu instead of a row per faction; Reputation tab can assign/clear envoy (Steam Aug 15).
- **Mentor float menu** — multiple teachable skills nest under one Mentor submenu.
- **Warden dread ThoughtDef** — removed invalid `validWhileMinified` XML field (1.6 load error).
- **Reputation tab layout** — left list is name/goodwill only; attitude, standing, envoy, and ledger show on the right after selecting a faction.
- **Fix lineage check** — `IsLineagePair` no longer calls `DirectRelationExists` on Sibling/Grandparent/Grandchild (implied relations); stops log spam on mentor float menu.

### Changed
- **Workshop preview (2.0)** — new `About/Preview.png`: glowing perk-tree constellation behind the title, same dark ember look (picked from 5 generated candidates); 900×600 at ~0.84 MB, under Steam's 1 MB limit. Docs hub card (`docs/img/DeepColonyPreview.png`) synced to match.
- **Workshop description (2.0)** — Steam BBCode description rewritten per system for 2.0 (what's-new tabs/presets section, Hard callouts); source of truth at [assets/workshop/deep-colony-description.bbcode](../assets/workshop/deep-colony-description.bbcode).
- **A12 chalkboard** — mentoring room bonus uses Biotech **blackboard** instead of custom teaching-notes furniture (removed `DC_TeachingNotes`).
- **Public release** — docs site declassified (`docs/deep-colony.html`); Steam Workshop + `DeepColony.zip` on the rolling GitHub `latest` release; `PublishedFileId.txt` checked in. Mystery `ledger.html` redirects here.
- **Workshop preview** — compressed `About/Preview.png` (~1.63 MB → ~0.38 MB) so Steam Workshop accepts it (Preview must be under 1 MB).
- Deep Colony C# project uses `Krafs.Rimworld.Ref` (same CI pattern as sibling mods).

### Fixed
- **Bereavement stacks with Violent Loss** — second close violent death upgrades by removing Violent Loss before applying BereavementShock.
- **Massacre trauma latch** — massacre flag clears when the death window drops below the cluster threshold (trickle deaths no longer block future massacres forever).

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
