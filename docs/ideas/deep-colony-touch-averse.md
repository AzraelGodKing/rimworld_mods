# Deep Colony — Touch-need traits (F01 / F02)

**Status:** shipping (`touch-need-v1`).  
**Mod:** [Deep Colony](../../Deep%20Colony/). Do not reuse F01 or F02.  
**Lane:** colonist identity. Date Night still owns Date / Lovin schedules. Homesteader still owns polyarmory bed-sharing.

## Pitch

A **touch-averse** colonist will not start a love relationship with someone until they are actually fine being touched by *that* person. Comfort is now a **named ladder**, not a single yes/no. **Touch-starved** is the inverse: they still will not fall for a stranger, but they *need* trusted contact and attach sooner. **Tactile** and **cuddly** share the same meter for mood only.

## Comfort tiers

Stored per other `thingIDNumber` on `Comp_DeepColony.touchComfortByPawn` (0–1). The "fine with touch" band is the settings threshold (default 0.65); other bands scale from it.

| Tier | Meaning | Typical use |
|------|---------|-------------|
| Distant | Stranger | Too-close mood for averse |
| Familiar | Knows them | Still not romance |
| At ease | Fine standing close | Romance for **reserved** and **touch-starved** |
| Fine with touch | Fine being touched | Romance for **touch-averse** (degree 0); bed share |
| Intimate | Fine with closeness | Romance for **touch-intolerant** |

## Traits (exclusion `DC_TouchNeed` — one per pawn)

| Trait | Degrees | Romance gate | Other |
|-------|---------|--------------|--------|
| `DC_TouchAverse` | reserved (−1), **touch-averse (0)**, touch-intolerant (+1) | At ease / fine with touch / intimate | Crowding and unwanted bedmates; existing saves stay degree 0 |
| `DC_TouchStarved` | — | At ease (faster comfort, +romance weight) | Unhappy after a day without trusted contact; huge mood when someone they trust is adjacent |
| `DC_Tactile` | — | None | Mood from friendly people in touching distance |
| `DC_Cuddly` | — | None | Mood sharing a bed with someone trusted; mild penalty sleeping alone while that person is still in the colony |

Nudist still conflicts with touch-averse degrees only.

## Romance gate

Same hooks as F01 (`RandomSelectionWeight`, `SuccessChance`, `RomanceEligiblePair`, `AddDirectRelation`). Organic attempts also need touching distance. Player romance needs the tier; the job can walk them into range. Float-menu fail text names current tier vs required tier.

Touch-starved romance attempts that already pass the gate get 1.6× selection weight.

Ex-lovers and current partners seed at max (intimate). Blood family seeds at intimate min so kids/parents are not "too close." Incest stays vanilla-blocked.

## Non-goals

- No Date Night courtship-before-lovers.
- No new trauma def.
- Cuddly does **not** override vanilla "won't share a bed with a non-partner" (that is still Homesteader polyarmory's lane).

## Debug

Grant reserved / touch-averse / touch-intolerant / touch-starved / tactile / cuddly (replaces other touch-need traits). Max nearby comfort. Log dump includes tier names.
