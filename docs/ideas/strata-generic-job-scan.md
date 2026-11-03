# Strata: generic cross-floor job scan (prototype)

Setting: **Generic cross-floor job scan (prototype)**, `genericCrossFloorScan`, default **off**.
Code: `Strata/Source/CrossFloorJobScan.cs`. Nothing was deleted in this pass.

## How it works

When a pawn's own level gives a job giver nothing, Strata walks `LevelGraph.ReachableLevels` (nearest first, at most 3 probes per call) and for each linked level:

1. Skips it if the first portal is not enterable (sealed stairwell, generating level), the level is on the pawn's `WorkRelayAntiLoop` blacklist, or `RelayClaims.CanClaim` is full.
2. Sets the pawn's `mapIndexOrState` and `positionInt` to that level and the link's arrival stair cell, runs the vanilla job giver (`JobGiver_Work.TryIssueJobPackage` or `ThinkNode_JobGiver.TryGiveJob`), and restores both fields in a `finally` block. The swap happens inside one call on the main thread, so no tick, render, save, or other pawn sees it. A result produced on a different tick is thrown away.
3. If vanilla found a job, it relays through `PawnRelay.TryClaimAndRelay`, which registers the `RelayClaims` claim, marks `PortalRelayChain` for multi-hop, and (for work) calls `JobRelay.Remember` so the work hold and anti-loop rules still apply. The found job is queued behind the `EnterPortal`. `Patch_EnterPortalKeepJobs` and `CrossLevelOrderedJobs.KeepQueued` carry it over each stair and start it on arrival.

Guards:
- While a scan runs, `PawnRelay.CanRelayBasics`, `CanLeaveWorkForNeed` and `JobRelay.CanConsider` return false, so no Strata relay can fire from the parked position.
- Jobs that are themselves cross-level (`EnterPortal`, any `Strata_*` job) are discarded, so relays never chain.
- Reservations use the existing `Patch_CrossFloorReserve` path. Nothing is reserved during the scan. The job reserves when it starts on the destination floor.
- Exceptions are reported to `StrataFaultGuard.System.CrossFloorScan`. After 8 errors the scan trips off and the old relays take over again.
- `StrataLevelCap` only limits opening new levels, so it does not affect scanning existing ones. Links come from `LevelGraph`, which already follows only Strata level portals (and foreign portals when that option is on).

## What it replaces when on

| Relay | File | Replaced? | Why |
|---|---|---|---|
| Colonist work (no local job) | `JobRelay.TryAssignBest` | Yes | Vanilla work scan on the other floor replaces the `WorkRelaySignals` probes. `TryHold`, `TryLeaveForBetterWork` (priority trips) and material/bill fetches are kept. |
| Doctor commute to patients | `Patch_MedicalRelay_Doctor` | Yes | Tending is a work giver, so the work scan finds it. |
| Patient to a medical bed | `Patch_MedicalRelay_Patient` | Yes | `JobGiver_PatientGoToBed` returns null when no bed, so the scan is a clean fit. |
| Food | `Patch_FoodRelay` | Yes | `JobGiver_GetFood` returns null when nothing edible is reachable. The `LevelRole` freezer/farm ordering is lost (BFS order instead). |
| Joy | `Patch_JoyRelay` | Yes | `JobGiver_GetJoy` returns null when nothing qualifies. |

Per-need toggles (food, joy, medical) still switch their need off entirely.

## What must stay (do not delete)

| Relay | Why the generic scan cannot do it |
|---|---|
| Rest / owned bed (`Patch_RestRelay`, `SleepRelay`) | `GetRest` always returns a job (sleep on the floor), so "found nothing" never triggers. Owned-bed preference and exhaustion collapse need the dedicated logic. |
| Meditation (`Patch_MeditationRelay`) | `JobGiver_Meditate` falls back to meditating anywhere. Assigned spots on other floors need the explicit lookup. |
| Mental breaks (`Patch_MentalBreakRelay`) | Targets are chosen by mental-state utilities, not job givers, and the pawn is in a mental state (relays blocked). |
| Carried pawns: babies, prisoners, rescue, capture, entities (`WorkGiver_Bring*`, `WorkGiver_RescueToBedAcrossLevels`, `Patch_Capture*`, `Patch_EnterPortalKeepCarriedPawn`) | The job carries another pawn through the portal. The carried pawn is not on the scanned map, so vanilla cannot target it there. |
| Rituals (`RitualTravel`) | Lords are strictly one map. The escort logic is a lord-level problem, not a job scan. |
| Pets (`Patch_PetHomeRelay`) | Animal think trees wander, and the relay keys on area and bed, not on a job giver result. |
| Misc. Robots (`Patch_Robot*`) | Their own JobGivers and recharge logic. Could be added later via the same `TryScanJobGiver` call. |
| Visitors (`Patch_VisitorPortalRelay`) | Not colonists. They must not take colony jobs. |
| Cross-level hauling, install, construction (`WorkGiver_HaulAcrossLevels`, `WorkGiver_InstallAcrossLevels`, `Patch_ConstructAcrossLevels`) | These move items between floors. The generic scan only moves the pawn. |
| External job givers (`Patch_ExternalJobGiverRelay`) | Mod givers registered by marker. They can opt in later through `TryScanJobGiver`. |

## Risks to watch in play-testing

- Mods that cache per-pawn map state during a job scan (some pathing or region caches) could see the parked position. Report the mod if you see odd results.
- Job givers that have side effects (rare) would run them on the other map.
- With many floors, each failed local scan costs up to 3 extra vanilla scans. The existing work-scan cooldown (`PawnRelay.IsColonistWorkScanCooldown`) still applies to work.

If play-testing is clean, the next pass can delete `WorkRelaySignals` probes used only by `TryAssignBest`, plus the food/joy/patient relay bodies.
