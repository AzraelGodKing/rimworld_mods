# Nemesis — Changelog

Foundation by **Dredd (Misakabob)** — original design, persistent antagonist pawn, escape/capture loop, aggression pacing, assaults, waste drops, fixation/prison-break triggers, resolution dialog, and settings. Credited with gratitude; this monorepo package extends that work.

## Unreleased

### Personal identity
- Scar / missing-part / prosthetic taunts from the nemesis pawn's hediffs (injuries the colony inflicted).
- Escalating intimacy lines (bedroom, highest skill, spouse/lover, bonded animal) gated by hunt age.
- Hunted mood thread: ongoing ThoughtWorker for the fixation target, short spike after actions, relief thoughts when the hunt ends in capture/kill/clear.
- Ignored-action counter: consecutive unanswered harassment escalates aggression and fires a dedicated taunt.

### New harassment
- KidnapAttempt, SniperTerror, GraveDesecration, FoodTampering, InformantReveal (append-only enum).
- Raid credit-taking letter (~15% default) when a different hostile faction raids during a hunt.

### Arc / pacing / finale
- Named phases: Watching / Testing / Obsessed / Reckoning (action gates match prior aggression thresholds).
- Deliberate silence windows after major strikes, with a mid-quiet letter.
- Staged finale choice at max escapes: duel or refuse (largest assault).
- Trophy ledger (last ~20 completed hunts) in mod settings.

### Resolution depth
- Recruit (resistance broken), ransom to faction, hand to enemies (when a third faction is hostile to theirs).
- Living truce: odd gift pods and raid-warning letters.
- TargetDied grief letter + optional grave visit (spawn / pause / leave).

### Polish
- Immortality telegraph (fleck + message) on kill-intercept.
- PickAction variety guard (no consecutive duplicate actions).
- Waste packs via drop pods near a valid spot.
- Difficulty presets (Subtle / Classic / Relentless) + Advanced sliders; red "Resolve current hunt now" escape hatch.

### Soft compat (fail-open)
- Homesteader: prefer favorite-food stacks; beehive vandalism; well-fouling.
- Strata: rare burrow-attributed deep raid when `Strata_DeepRaid` probe succeeds (underground only).
- Stormproof: EMP sabotage prefers LoadShedder / GridMonitor ("went for the brain"); dampener/surge protections unchanged.

### Monorepo notes
- **No public download zip** — CI still compiles Nemesis, but `Nemesis.zip` is not published on the rolling GitHub Release.
- Package id `AzraelGodKing.Nemesis` (Harmony 1.6).

### Inherited from Dredd 1.4.x (summary)

- Persistent named antagonist; cannot be killed until cornered/captured path; escalating taunts/raids/assaults/waste; settings for triggers, pacing, action mix; truce; rogue on peace treaty; fixation + prison break + killed-ally triggers; resolution Execute / Release / Keep / Truce.
