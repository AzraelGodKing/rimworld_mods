# Strata V2 Roadmap

Features deferred past V1.0. Not committed scope — a backlog for planning.

## Logistics & multilevel AI
- **Cargo lift** — proper vertical item transport (V1 uses haul relay + stockpiles by stairs).
- **Elevator haul priority** — prefer powered elevator over stairs for heavy cross-level hauls.
- **Multi-shaft routing** — relay picks shortest portal path, not first BFS link.
- **Haul + seal race fix** — re-check portal seal mid `JobDriver_HaulToLevel`.

## Combat & threats
- **Deep raid lord tuning** — no flee/kidnap underground (match raid pursuit behavior).
- **Burrower telegraph** — tremor warning before deep raid spawns.
- **Sealed-shaft siege** — raiders attempt to unseal or find alternate entry.
- **Depth-scaled threat table** — richer ore ↔ more bugs/raids tradeoff pass.

## Power & building UX
- **Shaft conduit inspect UI** — show linked partner and flow direction.
- **Dedicated exhaust-fan research** — Strata research gate instead of bare Electricity.
- **Placement helpers** — shaft conduit pairing ring, fan intake arrow, duct outdoor-exit hint.

## Engineering & compatibility
- **Unit tests** — pure logic: `LevelGraph`, `StrataDepth`, smoke math, `RelayClaims`, throttle rules.
- **In-repo README** — full install/compat doc beyond `About.xml`.
- **Mod compatibility settings** — optional patch toggles for popular building/power mods.
- **Strata-specific alerts** — smoke on empty level, colonists below sealed shaft.

## Environment (stretch)
- **Cross-level gas** — if vanilla ever links pocket maps, sealed portals block tox between levels explicitly.
- **Gas pocket room containment** — tox trapped by sealed doors within a single map (beyond stairwell seal).
