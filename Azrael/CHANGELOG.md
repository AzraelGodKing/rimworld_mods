# Changelog

Detailed notes for **Azrael** only. Repo-wide highlights: [../CHANGELOG.md](../CHANGELOG.md).

## [Unreleased]

### Added
- **The Deep Homestead** scenario — 3 settlers, farm kit, mountain-foothills opener; forces Azrael; MayRequire Homesteader / Strata start research and rock salt.
- **Standalone storyteller fallback** — injects `StorytellerDef` Azrael only when Homesteader is not loaded (Homesteader owns the canonical copy).
- **Fixed** Deep Homestead Strata start research → `Strata_DiggingDown` (was incorrectly the research tab defName).
- **Soft series load order** — loadAfter Homesteader / Strata / Stormproof / Nemesis / Deep Colony / Living World / Date Night without hard Workshop deps (Harmony only).
