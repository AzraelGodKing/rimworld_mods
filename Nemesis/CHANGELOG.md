# Nemesis — Changelog

Foundation by **Dredd (Misakabob)** — original design, persistent antagonist pawn, escape/capture loop, aggression pacing, assaults, waste drops, fixation/prison-break triggers, resolution dialog, and settings. Credited with gratitude; this monorepo package extends that work.

## Unreleased

<<<<<<< HEAD
### Hunt base / polish pass
- Progressive intel (`CampIntel`): scraps → last-known tile → world camp marker (real stash / empty false lead / trap).
- Caravan route tracking: abandoned caches with calling cards, soft road ambushes; CaravanHarass can force a track beat.
- `LordJob_NemesisHunt`: prioritizes fixation target, flees on heavy losses / critical HP / timeout (finale duel stays no-flee).
- Soft drop-pod arrival (Odyssey-biased) on personal assaults.
- Gear tint by archetype (persists across upgrades); rivalry social thought + social-fight bias with fixation target.
- Comms reply dialog after taunts (taunt back / offer truce / demand surrender) + console float menu.
- Soft compat: Stormproof ion-storm sabotage bait; Strata stairs-aware harassment map; Homesteader pantry/smokehouse food preference.
- Balance: soft-suppress concurrent foreign storyteller raids for ~1 day after a nemesis threat.
- UX: `About/Preview.png`, **Marked** scenario, tip set, `STEAM.md` blurb, DevMode debug actions, `PLAYTEST.md` checklist.
=======
### Review fixes (depth pass)
- Rival cameo: scribe original faction, temporarily SetFaction to a non-hostile faction (prefer Ancients) so player turrets do not auto-kill; restore on EndRivalCameo and EndHunt mid-cameo.
- Escape upgrades: destroy existing primary before AddEquipment; remove no-op HasPartsToWear; Wear replaces apparel by destroy when unspawned.
- Calling card / journal: add CompProperties_Usable (UseItem + keyed Read… labels) so float-menu use reaches CompUseEffect_NemesisNote.
- Calling card texture: switch from unverified Harddrive path to Core `Things/Item/Resource/Cloth`; journal from Textbook (multi-dir) to Core `Things/Item/Resource/ComponentIndustrial`.
>>>>>>> 4b3e3a1 (fix(nemesis): cameo faction, upgrades, usable notes, Core textures)

### Personality & evidence pass
- Archetypes (Stalker / Butcher / Saboteur / Trickster) rolled at hunt create; bias PickAction + distinct taunt voices; surface in settings status.
- Trait leak: Pyromaniac fire branch, Bloodlust assault bias, Greedy steal flavor on food/caravan.
- Escape upgrades (armor / weapon / bionic) + calling-card drops; Obsessed+ rival cameo (“nobody kills you but me”) via dedicated bools.
- ThingDefs: calling card + journal; TrophyTheft; journal/trophy restore on kill/execute.
- Opportunist timing, killbox-aware raid edges, 60-day anniversary, Obsessed+ nightmares.
- KeepPrisoner captivity arc: jailbreak raids, cellmate aura, agency-safe mole (warn → attributed sabotage).
- Kill attribution satisfied-win vs robbed grave path; vendetta thought on spouse/lover/friend; Execute reputation ripples; endgame crasher (ship/grav probe); soft Royalty/Ideology/Biotech seasoning.

### Personal identity
- Scar / missing-part / prosthetic taunts from the nemesis pawn's hediffs (injuries the colony inflicted).
- Escalating intimacy lines (bedroom, highest skill, spouse/lover, bonded animal) gated by hunt age.
- Hunted mood thread: ongoing ThoughtWorker for the fixation target, short spike after actions, relief thoughts when the hunt ends in capture/kill/clear.
- Ignored-action counter: consecutive unanswered harassment escalates aggression and fires a dedicated taunt.

### New harassment
- KidnapAttempt, SniperTerror, GraveDesecration, FoodTampering, InformantReveal, TrophyTheft (append-only enum).
- Raid credit-taking letter (~15% default) when a different hostile faction raids during a hunt.

### Arc / pacing / finale
- Named phases: Watching / Testing / Obsessed / Reckoning (action gates match prior aggression thresholds).
- Deliberate silence windows after major strikes, with a mid-quiet letter.
- Staged finale choice at max escapes: duel or refuse (largest assault).
- Trophy ledger (last ~20 completed hunts) in mod settings.

### Resolution depth
- Recruit (resistance broken), ransom to faction, hand to enemies (when a third faction is hostile to theirs).
- Living truce: odd gift pods and raid-warning letters.
- TargetDied grief letter + optional grave visit (spawn / pause / leave); satisfied-win path when nemesis is the killer.

### Polish
- Immortality telegraph (fleck + message) on kill-intercept.
- PickAction variety guard (no consecutive duplicate actions).
- Waste packs via drop pods near a valid spot.
- Difficulty presets (Subtle / Classic / Relentless) + Advanced sliders; red "Resolve current hunt now" escape hatch.

### Soft compat (fail-open)
- Homesteader: prefer favorite-food stacks; beehive vandalism; well-fouling.
- Strata: rare burrow-attributed deep raid when `Strata_DeepRaid` probe succeeds (underground only).
- Stormproof: EMP sabotage prefers LoadShedder / GridMonitor ("went for the brain"); dampener/surge protections unchanged; ion-storm opportunist bias.
- Royalty titled duel letter; Ideology relic theft letter; Biotech mechs on Reckoning assault; Odyssey/ship countdown endgame probe.

### Monorepo notes
- **No public download zip** — CI still compiles Nemesis, but `Nemesis.zip` is not published on the rolling GitHub Release.
- Package id `AzraelGodKing.Nemesis` (Harmony 1.6).

### Inherited from Dredd 1.4.x (summary)

- Persistent named antagonist; cannot be killed until cornered/captured path; escalating taunts/raids/assaults/waste; settings for triggers, pacing, action mix; truce; rogue on peace treaty; fixation + prison break + killed-ally triggers; resolution Execute / Release / Keep / Truce.
