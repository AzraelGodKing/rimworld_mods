# Nemesis — Changelog

Foundation by **Dredd (Misakabob)** — original design, persistent antagonist pawn, escape/capture loop, aggression pacing, assaults, waste drops, fixation/prison-break triggers, resolution dialog, and settings. Credited with gratitude; this monorepo package extends that work.

## [Unreleased]

AZR-382 AZR-383 AZR-384 AZR-385 AZR-386 AZR-387 AZR-388 AZR-389 AZR-390

Build stamp (Player.log): `custody-sync-v1`.

### Fixed
- **Capture soft-lock** (AZR-383) — if a captured nemesis dies, vanishes, is traded away, or leaves your cells before you pick their fate, the hunt no longer stays stuck forever. A nemesis who got loose and is still hostile resumes the hunt; otherwise the hunt ends with a letter and an epitaph ("died in custody" / "lost from custody") so a new nemesis can rise. The dossier's *Open resolution dialog* button now explains when there is nothing to decide instead of doing nothing.
- **Resolution choice sync** (AZR-382) — Execute / Release / Keep / Truce now apply on the next game tick through the same command queue as informants and comms replies, instead of straight from the button click (Multiplayer-safer). A second click cannot apply a second outcome.
- **Strata tick cost** (AZR-384) — with Strata loaded, the harassment-map lookup (reflection per map) no longer runs every tick during a hunt; it only runs on the health-check ticks that need it.
- **Clear diagnostic if a RimWorld update breaks Kill hooks** (AZR-385) — all `Pawn.Kill` patches share one lookup. If the signature changes, Player.log gets one `[Nemesis]` error naming exactly what is disabled, and those patches skip cleanly instead of failing as a group.
- **Escort spawn failures logged** (AZR-386) — mech and mount escort spawn errors now write a `[Nemesis]` warning with the PawnKind instead of failing silently.

### Added
- **Kidnap attempt** (AZR-389) — personal (fixation) hunts at aggression 5+ can roll a new action while the target is on the map: the nemesis leads a kidnap-enabled assault with its own letter. If the fixation target is carried off, the hunt ends as *they got what they came for*. New action-mix slider (default 0.05) and dev action. Kidnapping uses vanilla raid kidnap rules (any downed colonist), not a target-only AI.
- **Escape mood fallout** (AZR-388) — from the nemesis's second escape on, colonists get a 4-day "nemesis escaped again" memory (-3); the fixation target takes it harder (-8).
- **Dossier tile jump** (AZR-387) — click the last-known tile to show it on the world map.
- **Epitaph archive** (AZR-390) — *View all* opens a scrollable list of every recorded epitaph (the dossier still previews the latest three).
- CN / RU strings for everything above.

## [1.3.1]

Player-facing version **1.3.1** (`About.xml` `modVersion`).

See `About/changelog.txt` for the full player-facing notes for this ship.
## [1.3.0]

### Changed
- **Version baseline** — minor bump to start the next ship cycle above current Steam / Nexus / GitHub releases. No Workshop/Nexus upload in this change.

## [1.1.1]

Player-facing version **1.1.1** (`About.xml` `modVersion`). Startup writes `[Nemesis] v1.1.1 loaded from ...` in Player.log (`return-heal-v1`).

AZR-200

### Fixed
- **Headless nemesis bleedout loop** (`return-heal-v1`, AZR-200) — `RecoverForReturn` healed `Hediff_Injury` and cleared blood loss, but not `Hediff_MissingPart`. A raider who lost their head and became the hunt via wounded-escape stayed alive (Kill is cancelled), returned still headless, bled ~4h, and immediately fled. Park / inject / spawn now restore missing parts (then injuries / blood loss / anesthetic) so they can fight. Player.log: `Restored missing parts on {name} before return.`

## [1.1.0]

Player-facing version **1.1.0** (`About.xml` `modVersion`). Startup wrote `[Nemesis] v1.1.0 loaded from ...` in Player.log (`dossier-tells-v1`).

- **Tells** (`dossier-tells-v1`, AZR-146) — voice register, weapon family, mark, and habit roll at hunt create and never change. Progression upgrades quality, not weapon type. Mid-hunt saves without tells roll once on load.
- **Dossier** (AZR-74) — main-tab page for the active hunt: identity, tells, sightings, taunts, last-known tile, gear seen, aggression.
- **Informants and bounty** (AZR-75) — pay silver for a lead (tile / gear / next-raid warning / false lead). Standing bounty is recorded on the dossier. Comms console float menu + dossier button.
- **Epitaph** (AZR-76) — hunt end writes a permanent record on the dossier. Fail-open copy onto Deep Colony's Legacy letters when that mod is loaded.
- **Compat IDs** (AZR-59, partial) — extra Rimesis / Back for Vengeance packageId candidates; Player.log notes when a candidate is active. Live Font smoke-test still outstanding. Leader-raid → Rimesis inject (AZR-60) stays blocked on Font's public hook.
- **Calling-card graphic** — `Nemesis_CallingCard` used Steel as `Graphic_Single`; that path is a stack folder and failed to load. Now uses the vanilla component stack sprite.

## [1.0.2]

Player-facing version **1.0.2**. Startup wrote `[Nemesis] v1.0.2 loaded from ...` in Player.log (`update-news-v1`).

- **Update letter** (`update-news-v1`) — loading a colony sends a PositiveEvent letter with the current `About/changelog.txt` block and a Full notes link.
- **Guarded Harmony** — each patch class is applied on its own; one missing target logs and skips instead of aborting the rest of Nemesis.
- **CN / RU Keyed** — filled captain-progression settings, letters, and combat-focus names that English already had.
- **Fixation after a colonist dies** — uses `MapHeld` (corpse / killer map) so the hunt can still pick a surviving colonist.
- **Wounded-escape cheat-death** — if `CreateNemesis` no-ops (Rimesis/BFV claim, failed generate), vanilla `Kill` proceeds. Anesthetic is no longer applied during the lethal prefix.
- **Hunt raids omit the nemesis** — Direct Raid injects the named pawn whenever the hunt is active (not only after the first flee); hunt faction is restored after parking as a world pawn; if the raid group never generated them they spawn at the map edge (Steam Aug 15).
- **Phantom escape letters** — flee only when the nemesis is spawned, hostile, and on a player home map (not a world pawn).
- **Colony executions start a hunt** — executing a prisoner (including Ideology public execution / ExecutionCut, slaves, and colony-bed kills) no longer starts wounded-escape or "killed ally" hunts, so the victim is not parked on the world map sedated with a mount (Steam Aug 11 / 15 / 17). Wounded-escape still requires a hostile in the field.
- **Ideology public execution** — killing a colony prisoner / ExecutionCut no longer intercepts as cinematic wounded-escape (Steam Aug 11).
- **GitHub zip restored** — `Nemesis.zip` published again on the rolling `latest` release for non-Steam installs (alongside Workshop).
- **Marked scenario** — personal-antagonist showcase start (flak + revolvers); locks Azrael when Homesteader is loaded.
- **Rimesis / BFV soft-compat** — public `NemesisCompatApi` (`HasActiveHunt`, `ActiveNemesisPawn`, `IsNemesisPawn`, `WouldClaim`, `ShouldReportMissingToRimesis`) for Font’s Rimesis; skip hunt create / raid inject when a pawn already has Rimesis/BFV hediff markers. Solo behavior unchanged. Spec: `docs/ideas/nemesis-rimesis-compat.md`.
- **Rimesis Availability / Missing** — Font Availability states documented; Nemesis stub `ShouldReportMissingToRimesis` (= `IsNemesisPawn`) for Font to mark pawns Missing. Soft-read of Font Availability still design-only (fail-open reflection once API names land). Leader-raid → Rimesis inject remains later / coexistence bar unchanged.
- **Compat notes** — Deep Colony capture/truce goodwill reviewed (no double-buffer gap). Font later-idea recorded: Nemesis “leader raid” could call Rimesis raid injection for full combat style (beyond coexistence).
- **Hybrid captain progression** — after each escape the nemesis gains a captain level (skills, focus-appropriate gear quality, `Nemesis_BattleHardened` armor; Biotech bionics/genes at thresholds). Combat focus rolled at create. Post-escape action mix favors army raids/assaults and downweights petty sabotage. Soft animal escorts (Giddy-Up aware) + Mechanitor Biotech mech retinue. Settings under Captain progression. Still endable (no cheat-death). Stamp: hunt keeps personal capture/kill/they-win ends.
- **CN / RU localization** — full Chinese Simplified and Russian keyed packs from English (`Languages/ChineseSimplified|Russian/Keyed/Nemesis.xml`).
- **Public release** — docs site declassified (`docs/nemesis.html`); Steam Workshop + `Nemesis.zip` on the rolling GitHub `latest` release; `PublishedFileId.txt` checked in.
- **Workshop preview** — added `About/Preview.png` selling the personal-antagonist fantasy; compressed ~1.39 MB → ~0.36 MB so Steam Workshop accepts it (Preview must be under 1 MB).
- **Post-escape heal** — park / inject / assault recover the nemesis above the flee threshold so army-return and personal assaults no longer vanish within seconds.
- **Vengeance army return** — after escapes, Direct Raid prefers a heavier points raid that injects the same nemesis pawn into the assault (BFV-style: don't come back alone). Raid letters land the line: *Why return alone when you can return with an army?* Dev action: *Nemesis/Actions → Vengeance army raid*.
- **Duplicate emerge / escape letters** — stacked Kill hits no longer re-open a hunt or re-send "Escapes" (claim hunt immediately; 180-tick escape latch; ignore Kill when already off-map). Wounded-escape create counts as the escape beat so queued Kills do not double-letter.
- **Lord owns free world pawn** — escape / create / assault now detach the nemesis from any `Lord` and clear WorldPawns before `PassToWorld` or map spawn (fixes spam after Nemesis assault).
- **Dev debug actions** — Development Mode menu under *Nemesis* / *Nemesis/Actions*: log state, start/clear hunt, fire next action, bump aggression, force escape, resolution dialog, and each harassment action.
- **SocialFightChance Harmony startup crash** — RimWorld 1.6 renamed the second parameter to `initiator`; postfix updated so Nemesis loads again.
- **No public download zip** — ~~CI still compiles Nemesis, but `Nemesis.zip` is not published on the rolling GitHub Release.~~ **Superseded:** zip is published with the public release.
- **Brought into** `rimworld_mods/Nemesis` as `AzraelGodKing.Nemesis` (Harmony 1.6, sibling csproj pattern).
- **Credit** — Dredd / Misakabob named in About + this changelog as original author of the foundation.
- **New harassment** — fake signal → delayed ambush; caravan harassment; EMP / grid sabotage; food-store raids; Anomaly bait (DLC, fail-open).
- **New triggers** — wounded-and-escaped cinematic survival; Ideology slave rebellion (when present).
- **End conditions** — hunt also ends if a fixation target dies or is handed over (nemesis “wins”).
- **Flee-when-losing** — on-map assaults use flee-capable lords; low-HP escape retained from foundation.
- **Personal taunts** — keyed English strings; Homesteader favorite-food / cellar lines and Stormproof ion flavor when those mods are active.
- **Soft compat (fail-open)** — Stormproof EMP dampeners / surge protectors; Strata surface-map preference; Homesteader cellar / favorites via packageId + defName / reflection.
- **Mod-local performance** — nemesis/target pawn registry cache; staggered health checks (faster on viewed map); defer actions during large raids; skip action fire while nemesis is on-map; no LINQ on subdue hot path; dirty flags for resolution / end checks.
- **Safe mid-save add.** Removal: resolve active hunt first so WorldPawn keep-forever pins are released via capture outcomes.

### Inherited from Dredd 1.4.x (summary)

- Persistent named antagonist; cannot be killed until cornered/captured path; escalating taunts/raids/assaults/waste; settings for triggers, pacing, action mix; truce; rogue on peace treaty; fixation + prison break + killed-ally triggers; resolution Execute / Release / Keep / Truce.
