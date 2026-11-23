# LLM Storyteller (prototype, unreleased)

A RimWorld storyteller, **the Oracle**, driven by a local language model through Ollama or LM Studio (OpenAI-compatible API on localhost).

Setup guides: [Ollama](Docs/SETUP-OLLAMA.md) and [LM Studio](Docs/SETUP-LMSTUDIO.md).

## How it works

- `StorytellerComp_LLM` runs on each home map. When a decision is due (every 24 in-game hours by default) it builds a short colony report and a list of incidents (ThreatBig, ThreatSmall, Misc) that can fire right now, sampled by their normal chance.
- `LlmClient` sends the prompt on a background thread. The comp checks the task on later storyteller intervals; nothing blocks the game.
- The model answers `{"incident": "<defName or none>", "intensity": <number>, "reason": "..."}`. `StoryDecision` only accepts a defName or label from the list, and clamps intensity to 0.5..max (Mod Options, default 1.5).
- The mod re-checks the incident, scales its points, checks `CanFireNow`, and returns a normal `FiringIncident`. The model never runs code.
- Bad answer or no answer: one retry, then Cassandra Classic's home-map comps (`Fallback*` in `FallbackComps.cs`) run until the next decision. Everything off the home maps (caravans, quests, traders, disease) is Cassandra Classic all the time.
- Optional letter with the storyteller's reason. The last six choices are saved with the game and sent back as memory.

Multiplayer is not supported (background requests are not deterministic).

## Not released

`LLMStoryteller/** export-ignore` in `.gitattributes` keeps the whole folder out of every `git archive` package (CI zips and Modwright's Launch Pad), and `scripts/pack_mod.sh` refuses it. It is not in `scripts/matrix/mod-matrix.json`, the download lists in `build-mods.yml`, or the website. CI only builds it and runs `Tests/LLMStoryteller.Tests`.

To release: remove the `.gitattributes` line, add a mod-matrix row and the pack/release entries, add translations (or keep it in `ENGLISH_ONLY_MODS`), and give it its own portraits (it borrows Cassandra Classic's).

## Build

```
dotnet build LLMStoryteller/Source/LLMStoryteller.csproj -c Release       # 1.6
dotnet build LLMStoryteller/Source/LLMStoryteller.1.7.csproj -c Release   # 1.7, needs the .NET 10 SDK
dotnet test Tests/LLMStoryteller.Tests/LLMStoryteller.Tests.csproj
```
