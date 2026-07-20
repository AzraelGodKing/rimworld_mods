# Nemesis — playtest checklist

Dev mode: **Debug actions → Nemesis** (and **Nemesis/Actions**). Prefer a mid-game colony with a hostile humanlike faction, powered comms console, and (optional) a caravan out.

## Setup
- [ ] Enable Development mode
- [ ] Load/create a colony with at least one free colonist + hostile faction
- [ ] Soft-compat optional: Stormproof / Strata / Homesteader active for their branches

## Hunt create / end
- [ ] `Force create hunt (fixation)` — intro letter, settings status shows name / archetype / phase
- [ ] Fixation target gets **hunted** mood
- [ ] `Force end hunt` clears status; WorldPawn pin released
- [ ] Settings **Resolve current hunt now** also clears

## Identity / social
- [ ] Nemesis apparel/weapon shows distinctive tint on spawn/assault
- [ ] Escape upgrade (`Execute: NemesisAssault` → damage them → escape) keeps tint + new gear
- [ ] With both on map: social fight chance feels elevated (or rivalry thought present)

## Comms
- [ ] `Execute: CommsTaunt` opens reply dialog when console powered
- [ ] Comms console float menu **Reply to …**
- [ ] Taunt back raises aggression; Offer truce pauses hunt; Demand surrender shortens next action

## Intel / camp / caravan
- [ ] `Advance intel / place camp` three times → scrap → tile letter → world marker
- [ ] Caravan **Investigate** real camp → loot + note + aggression bump
- [ ] Repeat with debug until empty / trap outcomes seen (or new hunt)
- [ ] `Execute: CaravanHarass` with a caravan out → hit / cache / track ambush
- [ ] Passive caravan tracking while Testing+ (wait or advance time)

## Assault polish
- [ ] `Execute: NemesisAssault` — lord prioritizes fixation; flees when badly hurt / time out
- [ ] Finale refuse / `Execute finale assault` — no flee, heavy raid
- [ ] With Odyssey (optional): occasional drop-pod arrival letter line

## Soft compat
- [ ] **Stormproof**: ion condition → sabotage bias / ion-bait letter; dampener still blocks EMP
- [ ] **Strata**: harassment prefers surface with stairs; burrow only underground when available
- [ ] **Homesteader**: food raid prefers favorites / smoked / cellar-adjacent stacks

## Balance / storyteller
- [ ] After assault/raid, vanilla foreign raid soft-suppressed ~1 day (unless forced/same faction)
- [ ] Classic vs Relentless preset pacing feels distinct mid-game

## Resolution / captivity (smoke)
- [ ] Capture → resolution dialog options still work
- [ ] Keep prisoner → jailbreak / cellmate aura still fire
- [ ] Kill nemesis → journal drop; execute path reputation ripples

## UX content
- [ ] Scenario **Marked** appears and start dialog reads cleanly
- [ ] Loading tips can show Nemesis tip lines
- [ ] `About/Preview.png` shows in mod list

## Log hygiene
- [ ] No red errors on hunt create, assault, camp place/visit, end hunt
- [ ] Soft-compat probes fail open when sibling mods absent
