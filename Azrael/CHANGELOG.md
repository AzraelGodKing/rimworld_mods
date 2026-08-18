# Changelog

Detailed notes for **Azrael** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

Steam Workshop paste: [`About/changelog.txt`](About/changelog.txt).

## [Unreleased]

### Added
- **The Deep Homestead** scenario — 3 settlers, farm kit, mountain-foothills opener; forces Azrael; MayRequire Homesteader / Strata start research and rock salt.
- **Standalone storyteller fallback** — injects `StorytellerDef` Azrael only when Homesteader is not loaded (Homesteader owns the canonical copy). `PatchOperationFindMod` matches the Homesteader display name so both mods together do not duplicate the def.
- **CI zip** — `Azrael.zip` with the compiled DLL is packed on `latest` so Deep Homestead's forced-storyteller part loads.
- **Soft series load order** — loadAfter Homesteader / Strata / Stormproof / Nemesis / Deep Colony / Living World / Date Night without hard Workshop deps (Harmony only).

### Fixed
- **RimWorld 1.6 storyteller comps** — Cassandra Classic structure + `CassandraClassic` portraits (standalone patch + Homesteader canonical).
- **Deep Homestead Strata research** — start research → `Strata_DiggingDown` (was incorrectly the research tab defName).
