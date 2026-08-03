# Deep Colony — ROADMAP

Playable core is in (perks, trauma, mentoring, inheritance, living faction reputation).

Series vision: [../ROADMAP.md](../ROADMAP.md).

## Ownership vs Living World

Deep Colony owns **player↔faction goodwill memory and drift** (raids, trades, gifts, shared-enemy kills, idle ally decay tuning). It does **not** own:

- Off-map chronicles / world news letters → [Living World](../docs/ideas/living-world.md)
- Settlement morph / NPC faction wars → Living World
- Named antagonists / hunt sites → [Nemesis](../Nemesis/ROADMAP.md)
- Farmstand / harvest festival → [Homesteader](../Homesteader/ROADMAP.md)

### When Living World ships (consumer only)

- [ ] Register fail-open for Living World world-event signals
- [ ] Map **visible** wars / ally disasters to existing `AddFactionDrift` / `FactionRepUtility` paths
- [ ] Do **not** duplicate a second goodwill buffer inside Living World
- [ ] Keep current idle ally / enemy drift behavior unless playtests say otherwise

## Later Deep Colony fantasy (colony identity — not world sim)

Parked here only if they stay colonist/colony-facing; redirect world-layer ideas to Living World.

- [ ] Further perk / trauma / mentoring polish as needed from playtests
- [ ] Soft-compat notes with Nemesis capture / truce goodwill (if gaps appear)
