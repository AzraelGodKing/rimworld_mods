# General fixes — approved set

**Status:** implemented on `cursor/general-fixes-8c68`. Remaining items only; already-shipped work stays out.  
**No About.xml version bumps.** Changelogs updated with whatever landed.

---

## Do these

**1. Deep Colony — envoy submenu**
Pawn right-click lists “Make envoy” once per faction (Steam Aug 15). Collapse to one Envoy submenu.

**2. Deep Colony — envoy on Reputation tab**
Add set/clear envoy on the Reputation screen.

**3. Deep Colony — mentor submenu**
Mentor options dump one row per skill. Nest those too.

**4. Nemesis — show up in raids**
Inject the nemesis on DirectRaid whenever the hunt is active (Steam Aug 15).

**5. Nemesis — keep their faction**
Parking as a world pawn can mismatch faction so inject silently fails. Keep a hostile faction.

**6. Nemesis — no phantom escapes**
Only `FireEscape` if they are spawned, hostile, and losing a fight. No off-map escape letters.

**7. Nemesis — Ideology execution**
Skip the cinematic-escape prefix for public execution / ritual kills (Steam Aug 11).

**8. Strata — flood / sump**
Store original terrain and restore on pump so flood seep actually drains (Steam Jul 26).

**9. Strata — ore hoist**
`DeSpawn` / `SplitOff` before place so single stacks transfer.

**11. Homesteader — architect tab**
Move crates, cellars, hayloft, cistern, water tower off Furniture onto a Homesteader/Storage tab. Stations stay on Production.

**12. Homesteader — Adaptive Storage tab**
If ASF is loaded, also list those buildings on its Storage tab. Fail-open if absent.

**14. Homesteader — soap consumed**
Handheld soap bar must consume a stack on wash (tub already consumes fuel).

**15. Homesteader — allergy catalog**
Add pie, ploughman’s, bread, flapjacks, toast-and-jam to milk and/or wheat.

**16. Homesteader — cellar text**
Fix root cellar `â€”` / `5Â°C`.

**17. Homesteader — cistern storage**
Copy ShelfBase parity onto cistern and water tower.

**18. Azrael — no duplicate storyteller**
`PatchOperationFindMod` must match display name `Homesteader`, not packageId.

**19. Azrael — CI zip**
Build `Azrael.csproj` and publish `Azrael.zip` on `latest`.

**20. Dubs / VE pipe patches**
Switch Homesteader DBH + Strata shaft-fluid FindMod lists to Workshop display names.

**21. Date Night — pregnancy-safe cooldown**
Honor `pregnancySafeCooldown` in `TryStartLovinNow`.

---

## Out (already elsewhere)

- **10** foreign portals — PR #76
- **13** drying rack north — PR #73 + Steam reply
- **22** Lovin vs Joy — DN1
- Gravship stairs — draft `cursor/strata-gravship-ladders-dd08`
- Animal poop — draft `cursor/homesteader-animal-poop-dd08`
