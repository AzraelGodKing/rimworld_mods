# Pillar 1 integration merge plan

Branch: **`feature/atmosphere-o2-co2`** / **`feature/pillar1-fluid-adapters`** (integrated with Claude [PR #21](https://github.com/AzraelGodKing/rimworld_mods/pull/21) and [PR #20](https://github.com/AzraelGodKing/rimworld_mods/pull/20))

Combines:

- **Claude Fable 5** — [PR #21](https://github.com/AzraelGodKing/rimworld_mods/pull/21) (`claude/pillar-one-implementation-2ad2nd`): atmosphere, hidden chambers, deep gas economy
- **Claude Fable 5** — [PR #20](https://github.com/AzraelGodKing/rimworld_mods/pull/20) (`Feature/multi-level-quest-sites`): sunken ruin exploration sites, abandon warning, pocket-map despawn safety
- **Cursor** — fluid shaft adapters (DBH, DCH, Rimatomics, VHGE, **Rimefeller**), DBH groundwater, Atmosphere v2, dig-down, cages

## Strategy

1. Commit Cursor work on `feature/pillar1-fluid-adapters`
2. Branch `feature/pillar1-integrated` and **merge PR #21** (not rebase — preserves both histories)
3. Resolve conflicts manually where Git could not choose semantics
4. **Drop Cursor Pillar 1 prototypes** superseded by Claude's implementation
5. **Keep Cursor-only** fluid shaft + DBH groundwater code
6. Rebuild `Strata.dll` and verify compile

## What we kept from each side

| Area | Source | Notes |
|------|--------|-------|
| `AtmosphereMapComponent`, `StrataGasDef`, gas defs/buildings | Claude | Replaces in-place `SmokeMapComponent` refactor |
| `GenStep_HiddenChambers`, `GenStep_StrataFog` | Claude | Replaces `GenStep_DeepPockets` |
| `CompGasWell`, `GasNetAdapter`, gas textures | Claude | Replaces `DeepGas/` + `Buildings_DeepGas.xml` |
| `Strata_GasExtraction` research | Claude | Dropped duplicate `Strata_DeepGasExtraction` |
| `ShaftFluid/*`, junction defs/patches | Cursor | DBH, DCH, Rimatomics, VHGE, **Rimefeller** |
| Sunken ruin site + warren gensteps | PR #20 | Gated by *Underground gas* + *deep excavation* |
| `Patch_AbandonWarning`, `Patch_PocketMapRemoval` | PR #20 | Multi-level safety |
| `GenStep_DbhGroundwater` | Cursor | Added to merged map generator |
| `Strata_FluidShafts` research | Cursor | Alongside Claude research |

## What we removed (Cursor duplicates)

- `Strata/Source/DeepPockets/`
- `Strata/Source/DeepGas/`
- `Strata/Source/Atmosphere/`
- `Strata/Defs/ThingDefs_Buildings/Buildings_DeepGas.xml`
- `Strata/Patches/DeepGas_VHGE.xml` (Claude's `GasNetAdapter` covers well→VHGE)
- `SmokeMapComponent.cs` (Claude: deleted; save compat via subclass in `AtmosphereMapComponent.cs`)

## Manual conflict resolutions

| File | Resolution |
|------|------------|
| `MapGenerators_Strata.xml` | HiddenChambers + **DbhGroundwater** + Fog |
| `Research_Strata.xml` | `Strata_GasExtraction` + `Strata_FluidShafts` |
| `Patch_MapComponents.cs` | `AtmosphereMapComponent` only (no DeepPockets) |
| `Strata.cs` | `GasNetAdapter.Inject()` + Harmony |
| `IncidentWorker_GasPocket.cs` | Claude (AtmosphereMapComponent breach) |
| `CHANGELOG.md` / `V2_ROADMAP.md` | Merged sections, attributed by author |
| `Strata.dll` | Rebuilt after source merge |

## Post-merge checklist

- [x] `dotnet build Strata/Source/Strata.csproj -c Release`
- [ ] Fresh game: hidden chambers + fog on new level
- [ ] Deep gas vent / well / generator (Claude economy)
- [ ] VHGE gas well pipe feed (`GasNetAdapter`)
- [ ] DBH / DCH / VHGE / **Rimefeller** shaft junctions
- [ ] Sunken ruin incident → warren descent → hoard; abandon warning with pawns underground
- [ ] Rimatomics coolant junction (community playtest)
- [ ] Dev-mode self-tests pass
- [ ] Open PR: integration branch → `main`

## Branch naming (for humans)

- **Cursor** = fluid adapters, DBH groundwater, playtest fixes
- **Claude Fable 5** = Pillar 1 atmosphere + chambers + gas economy ([PR #21](https://github.com/AzraelGodKing/rimworld_mods/pull/21))
