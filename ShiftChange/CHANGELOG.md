# Changelog

Detailed notes for **Shift Change** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **Full-sail triggers** (`shift-change-fullsail-v1`) — Cooking / Doctor / Handling work kits (JobGiver_Work notify + tick), Ideology ritual start (`RitualBehaviorWorker.TryExecuteOn` + lord fallback), Sleep→Ritual→Work priority, restore hysteresis, soft apparel claims, inventory-prefer for stripped layers, expanded main-tab rule blocks + settings + Dev stubs.
- **Shift Change MVP** (`shift-change-mvp-v1`) — new mod (formerly Outfit Routines / Wardrobe idea). Per-colonist Sleep-shift rules: walk to a wardrobe stockpile, wear apparel matching an Assign apparel policy, snapshot previous clothes, restore when Sleep ends. Vanilla OptimizeApparel suppressed while managed. Main tab + Mod Options + Dev tools. Package `azraelgodking.ShiftChange`.
