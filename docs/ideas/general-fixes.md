# General fixes — approved set

**Status:** this batch is on `fix/general-fixes`. Earlier Steam-first set shipped on `cursor/general-fixes-8c68`.  
**No About.xml version bumps.** Changelogs updated with whatever landed.

---

## This branch (`fix/general-fixes`)

**Homesteader**
- Keep Homestead architect tab. Do **not** retarget storage to `ASF_Architect` (Adaptive Storage Framework has no tab).
- Allergy flare / food AI inspect `CompIngredients`.
- Storage descriptions match `maxItemsInCell` × footprint.

**Nemesis**
- Skip interned / execution hunts (`NemesisTriggers`).
- Fixation uses `MapHeld`.
- Wounded-escape only cancels `Kill` if a hunt actually started.

**Strata**
- Never call vanilla `PocketMapExit.OnEntered` when climbing up.
- Gravship host shafts pack / restore through launch (`gravship-stairs-launch-v1`).
- Second stairwell with no landing cell refuses instead of opening a parallel pocket.
- Orphan upstairs dump only uses standable cells.

**Deep Colony**
- Isekai aptitude allow-list; Rank / destiny stay unique.
- Birth inheritance only for player-side pawns; do not stamp NPC babies.
- Biotech backoff for non-Baseliner xenotypes + xenogenes.
- Mentor mutual-loop gate; envoy uniqueness includes caravans.

**Date Night**
- Lovin hours fall back to vanilla rest / medical rest when there is no double.
- Either partner in the bed may start lovin.
- Private time (self-lovin) for adults on Lovin hours when a partner is not sharing a double. Off in settings. Children never qualify.

---

## Out (already elsewhere)

- **10** foreign portals — PR #76
- **13** drying rack north — textures on disk; packing is a Workshop zip check
- **22** Lovin vs Joy — DN1
- Animal poop — draft `cursor/homesteader-animal-poop-dd08` (feature, not this ship)
