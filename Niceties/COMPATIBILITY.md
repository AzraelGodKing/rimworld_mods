# Niceties compatibility

This page lists what Niceties 2.0.0 works with, what it conflicts with, and what happens when the other mod or DLC is missing. Niceties 2.0.0 ships one package for both RimWorld 1.6 and 1.7, built from the same source.

Niceties has no hard dependency except Harmony. Every patch class is applied on its own (`SafePatchAll`), so one failed patch only switches off its own nicety, and Mod Options says which.

**Last tested in game:** no row has been tested on 2.0.0 yet. Each status changes from "Untested" to the date it was checked, on 1.6 and on 1.7 separately.

## Sister mods

| Mod | 1.6 | 1.7 | Integration point | Without it | Last tested |
|---|---|---|---|---|---|
| Strata | Untested | Untested | Leave a way out treats any `MapPortal` as a way out, which covers Strata's stairs up, down and gravship stairs (no Strata reference). Walls around a stairwell are never held back. Strata's stairwell postfix on `Frame.CompleteConstruction` respects `__runOriginal` when Niceties holds a wall back. | Nothing changes. | Not yet |
| Date Night | Untested | Untested | Date Night reads `Niceties.CompSharedRoom.Shared` by reflection and never reassigns a bed in a room Niceties marked shared, so each bed keeps one owner. Lovers already never disturb each other's sleep in vanilla; Niceties only adds roommates. | Date Night's Lovin-window bed claim works as before. | Not yet |
| Azrael (series hub) | Untested | Untested | The hub lists Niceties with its build stamp (`Niceties.NicetiesBuildInfo.BuildStamp`) and shows Niceties patch failures through `Azrael.PatchFailureSink`. | Patch failures still go to Player.log and to Niceties' Mod Options status lines. | Not yet |

## DLCs

| DLC | 1.6 | 1.7 | What Niceties does with it | Without it | Last tested |
|---|---|---|---|---|---|
| Royalty + Ideology | Untested | Untested | Throne rooms may hold an altar, and a throne keeps a room a throne room. The noble shared-room alert needs Royalty only. | Both throne patches are skipped (`Prepare`), and Mod Options says why. | Not yet |
| Biotech | Untested | Untested | Optional: hide growth-vat children and pawns in a subcore scanner from the colonist bar. | The options are hidden. | Not yet |
| Ideology | Untested | Untested | Optional: hide pawns in a biosculpter pod from the colonist bar. | The option is hidden unless Biotech is active (subcore scanner). | Not yet |
| Odyssey, Anomaly | Untested | Untested | Their map portals count as a way out for leave a way out. | n/a | Not yet |

## Declared conflicts (`incompatibleWith`)

These mods do the same job as one of the niceties. Loading both would apply the rule twice.

| Mod | Overlaps |
|---|---|
| Everlasting Apparel (`Jecrell166.JecsHEverlastingHApparel`) | Well-kept apparel |
| Allow Altars in Throneroom (`z4k97b.altarsinthroneroom`) | Throne and altar |
| Wear What You Want (`RelaxedOtter.WearWhatYouWant`) | Wear any outfit |
| Hide Cryptosleep Pawn (`louze.hiddencryptosleep`) | Hidden cryptosleep |
| Hunters Use Melee! (Continued) (`Mlie.HuntersUseMelee`) | Melee hunting |
| Smarter Construction (`dhultgren.smarterconstruction`) | Leave a way out. Niceties also turns leave a way out off when it sees this mod, and Mod Options says so. |

## Soft compatibility and known risks

| Mod or kind of mod | 1.6 | 1.7 | Notes | Last tested |
|---|---|---|---|---|
| Replace Stuff (`Memegoddess.ReplaceStuff`, `loadAfter`) | Untested | Untested | Its replacement frames count as unfinished walls, so an in-place wall swap keeps the room as it was. | Not yet |
| Mods that rewrite or regroup the colonist bar (Colony Groups style) | Untested | Untested | Niceties filters vanilla's colonist bar entries just before the layout. A mod that draws its own bar never sees the filter, so casket pawns may still show there. Not declared as a conflict. | Not yet |
| Other mods that prefix `Thing.TakeDamage` for deterioration | Untested | Untested | Niceties' prefix runs at high priority and only handles worn apparel. If another mod also rolls against the same damage, both rolls apply, so worn apparel wears less than with either mod alone. | Not yet |
| Room-role mods (Room Sense, Hospitality guest beds) | Untested | Untested | Niceties only raises the bedroom score of a marked room and zeroes its barracks score. Guest beds in a marked room are treated like any other bed; not checked yet. | Not yet |
| Mods that read `apparel.gender` straight from the def | Untested | Untested | 2.0 no longer edits the defs, so such a mod sees vanilla tags again. Turn on "Also let generated pawns wear any cut" only if you want raiders in any cut; it still does not edit defs. | Not yet |

## Multiplayer

Soft support, not tested with two clients yet.

- Settings that change the simulation are baked into the save (`GameComponent_NicetiesSim`). Inside a Multiplayer session every client uses the host's values; Mod Options says "this game uses the host's setting".
- The Share room gizmo is a registered sync method (found by reflection; no Multiplayer API DLL).
- Wear any outfit no longer edits defs at startup, so a joining player's own Mod Options cannot change how pawns are generated.
- UI-only options (the "+N" note, the roster, vat and pod hiding, the enclose outline, the noble alert) are local to each player.

## Adding or removing Niceties

Safe to add mid-save. Removing it mid-save logs a few harmless "could not find type" errors once on the next load, for its two game components; the save is not damaged. Marked shared rooms simply stop being shared.
