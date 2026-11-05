# Strata modder API

`Strata.StrataModderApi` is the stable surface for other mods. Everything else in Strata is internal and can change in any release. `ApiVersion` (currently **2**) goes up when members are added.

Strata is an optional dependency for most mods, so call it through reflection or from a class that only loads when Strata is active.

## Levels

| Member | Returns |
|---|---|
| `IsLevel(Map)` | true for any Strata level (underground, upper deck, gravship stack level) |
| `IsUnderground(Map)` / `IsUpperLevel(Map)` / `IsGravshipLinkedLevel(Map)` | the level kind |
| `LevelIndex(Map)` | +N for the Nth upper floor, 0 for the surface, -N for the Nth level down |
| `LevelLabel(Map)` | the player's name for the level, or the map label |
| `AnyLinkFrom(Map)` | true when the map has a stair or elevator link |
| `ReachableMaps(Map)` | linked maps, cheapest route first (a fresh list) |
| `TravelCost(Map from, Map to)` | cheapest route in walked cells (pathfinder walk between portals on floors in between, refreshed every 2500 ticks, plus 20 per stair ride and 8 per elevator ride; not the walk to the first portal), 0 for the same map, -1 when not linked |
| `Hops(Map from, Map to)` | stair/elevator rides on that route, 0 for the same map, -1 when not linked |
| `BestFirstStep(from, target, near, pawn)` / `FirstStepCandidates(from, target)` | which portal to walk into |

## Work relay

```csharp
// Send pawns to another floor for your work type.
StrataModderApi.RegisterWorkProbe((pawn, map) =>
    pawn.workSettings.WorkIsActive(MyDefOf.MyWork) && MyTracker.HasWork(map));

// A custom ThinkNode_JobGiver that looks for work: Strata relays after it finds nothing.
StrataModderApi.RegisterWorkSeekingJobGiverMarker("JobGiver_MyWork");
```

Probes run during job search, so keep them cheap. Modded work types with normal `WorkGiver_Scanner`s are already picked up without a probe.

The `PawnRelay.RegisterWorkProbe`, `UnregisterWorkProbe` and `RegisterWorkSeekingJobGiverMarker` entry points still work but are obsolete.

## Events

```csharp
StrataModderApi.RegisterLevelCreatedListener(map => ...);   // after a new level finishes generating
StrataModderApi.RegisterLevelRemovedListener(map => ...);   // just before a level is removed; the map is still valid
StrataModderApi.RegisterVerticalLinkListener((a, b, portal) => ...); // a stair or elevator now links a and b
```

Link events fire when a portal is built or a level is created, not when a save loads. A mod with its own vertical links can call `NotifyVerticalLink(a, b, portal)` to tell the other listeners. Exceptions in listeners are caught and logged.

## Incidents

`WouldBlockUndergroundIncident(IncidentDef)` is true when Strata stops that incident on underground maps.
