# Date Night compatibility

This page lists what Date Night 2.0.0 works with, how each integration works, and what happens when the other mod or DLC is missing. Date Night 2.0.0 ships one package for both RimWorld 1.6 and 1.7, with no 1.6 freeze.

"Fail-open" means that if the other mod is missing, out of date, or fails to bind, Date Night keeps running as if that mod were not there. Date Night has no hard dependency except Harmony.

**Last tested in game:** no row has been tested on 2.0.0 yet. Each status will change from "Untested" to the date it was checked, on 1.6 and on 1.7 separately.

## Sister mods

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Deep Colony | Untested | Untested | Deep Colony detects Date Night by its package ID and gives lovers a faster confidant bond. Date Night 2.0.0 adds the public `DateNight.DateNightApi` (see below), so Deep Colony can report breakups as they happen instead of waiting for Date Night's 2,500-tick check. | Without Deep Colony, breakups are still found by the 2,500-tick check. | Not yet |
| Homesteader | Untested | Untested | Matched by soft defName. The harvest maypole (`Homesteader_HarvestMaypole`) is the first choice for dance dates. The checkers table counts as a recreation venue because it is a joy building. | Without Homesteader, the dance venue is the lowest-id gather spot. | Not yet |
| Niceties | Untested | Untested | Niceties assigns the beds in rooms it has marked as shared (`Niceties.CompSharedRoom`, found by reflection). Date Night's Lovin-window bed claim never reassigns a bed in those rooms, so each bed has only one owner. | Without Niceties, the bed claim works as before. | Not yet |

## DLCs

| DLC | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Ideology | Untested | Untested | Lovin is checked against the vanilla `InitiatedLovin` history event, so an ideoligion's own lovin precepts decide. If the ideoligion disapproves of lovin, there is no forced Lovin, no "made it" mood for a Lovin window, and no private-time mood. Date moods are unchanged. | Without Ideology, lovin is always allowed. | Not yet |
| Biotech | Untested | Untested | **Trying for a baby** toggle on a lover pair that can conceive (opposite sexes, neither pregnant, vanilla pregnancy chance above 0). While trying, Lovin comes twice as often and uses the short cooldown on Lovin hours. The toggle turns off by itself once she is pregnant. A pregnant partner never goes on dance or walk dates, including double dates. Sterile pawns are not forced into Lovin, as before. | Without Biotech, the toggle is hidden and the pregnancy checks are false. | Not yet |
| Royalty | Untested | Untested | Dinner and picnic meals respect title food requirements. If no meal meets the title, the date is not a dinner. The Meditate column is counted when placing the Date button. | Without Royalty, there are no title checks. | Not yet |
| Odyssey | Untested | Untested | **Fishing date:** the couple stands by one of the player's fishing zones (`Zone_Fishing`) when the weather is nice. A passion for Animals makes this date more likely. | Without Odyssey, there is no fishing date. Saves keep working because the activity is stored as number 9, appended to the list. | Not yet |
| Anomaly | n/a | n/a | No integration needed. | n/a | n/a |

## Other mods (from `loadAfter` and name matching)

| Mod | 1.6 | 1.7 | Integration point | Fail-open behaviour | Last tested |
|---|---|---|---|---|---|
| Harmony (`brrainz.harmony`) | Required | Required | Each patch class is applied on its own (`SafePatchAll`). One startup line reports whether all 12 patches applied (13 with RimJobWorld), or names the ones that failed. | A failed patch logs an error and only its feature stops. | Not yet |
| Rimbody (`Maux36.Rimbody`), Exosuit / Mechsuit Framework | Untested | Untested | Their Schedule buttons are part of the known-mod table, so the Date button sits after them. | Without them, the Date button moves left by their cells. | Not yet |
| Schedule Everything (`Mazo.Schedules`) | Untested | Untested | Same as above: known-mod table. | Same. | Not yet |
| Any other mod that adds Schedule buttons | Untested | Untested | Not detected. Use the **Date button column offset** setting (from −4 to +8). | The button may overlap until the offset is set. | n/a |
| RimJobWorld (`rim.job.world`) | Untested | Unknown (no 1.7 RJW build known yet) | Lovin hours start RJW's bed job and respect `override_lovin`. The JobDriver cleanup patch is applied only when RJW is active. | Without RJW, vanilla Lovin is used. | Not yet |
| Animation and breeding packs (Rimworld Animations, other `*Sex*`, `*Breeding*`, `*Quickie*` drivers) | Untested | Untested | Matched by job and driver name (`DateNightUtility.LovinTokens`), so their Lovin counts as Lovin. | No match means the job is treated as a normal job. | Not yet |
| Romance on the Rim (`telardo.RomanceOnTheRim`) | Untested | Untested | Date Night waits while a RotR romance job is running, and a finished date fills RotR's romance need. | Without RotR, nothing happens. | Not yet |
| Better Pawn Control | Untested | Untested | No integration yet. Its saved schedule presets should include the Date and Lovin slots, because they are ordinary `TimeAssignmentDef`s. | n/a | Not yet |

## Multiplayer

**Soft support, untested.** Date Night does not ship or reference the Multiplayer API DLL. When the Multiplayer mod (`rwmt.Multiplayer`) is active, Date Night registers these sync methods by reflection at startup (`DateNightMultiplayer`):

- `DateNightAskOut.Plan` and `DateNightAskOut.Cancel`: the Ask out tonight and Cancel planned date buttons.
- `DateNightBaby.SetTrying`: the Trying for a baby toggle.
- `DateNightUtility.CopyHours`: the schedule mismatch alert's "copy hours" option.

Painting Date and Lovin hours on the Schedule tab goes through vanilla `Pawn_TimetableTracker.SetAssignment`. Multiplayer syncs that method itself, but the two-client test still needs to confirm it with the custom slots. The Romance panel only reads data. The dev-mode debug actions are not synced.

All random picks are deterministic. Activity, venue, date length, quality roll and gift giver are seeded by the couple and the day, and candidates are ordered by thing id.

**Host config sync is required.** Mod settings change the simulation (cooldown modes, gifts, double dates, gradual romance, and so on). Every player must use the host's Date Night settings. Date Night does not copy them for you.

**To do:** a two-client desync test that covers a gift date, a double date, a Lovin window, Ask out tonight and Trying for a baby.

## For modders

`DateNight.DateNightApi` (`ApiVersion` 1) is the supported surface. Resolve it by name, check `ApiVersion >= 1`, and bind delegates once. Every method is safe to call with null or dead pawns.

- `IsOnDate(Pawn)`: the pawn is on a date job now.
- `HadRecentGoodDate(Pawn, int withinTicks = 60000)`: the pawn's last finished date was within the window and was not awkward or ruined.
- `YearsTogether(Pawn, Pawn)`: years since the lover, fiancé or spouse relation began, or 0.
- `NotifyBreakup(Pawn, Pawn)`: Date Night reacts to the breakup now (resets the favourite spot and gives the "dates are over" mood) instead of at its next check. It is safe to call twice.
