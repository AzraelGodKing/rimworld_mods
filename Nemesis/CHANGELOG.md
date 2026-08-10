# Nemesis — Changelog

Foundation by **Dredd (Misakabob)** — original design, persistent antagonist pawn, escape/capture loop, aggression pacing, assaults, waste drops, fixation/prison-break triggers, resolution dialog, and settings. Credited with gratitude; this monorepo package extends that work.

## Unreleased (monorepo integration)

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
