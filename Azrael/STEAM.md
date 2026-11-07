# Azrael — Steam / Workshop notes

**Package ID:** `azraelgodking.Azrael`  
**Requires:** Harmony, RimWorld 1.6 and 1.7  
**Multiplayer:** soft support (safe-removal deconstruct is synced; every player needs the host's Azrael settings)  
**Hard deps:** none beyond Harmony  
**Recommended (soft):** Homesteader (canonical Azrael teller), Strata, Stormproof, Living World, Nemesis, Deep Colony

## What this is

Thin series showcase:

1. **The Deep Homestead** — scenario that locks Azrael and starts a homestead-flavored trio. Pick a mountainous / cave-friendly tile.
2. **Azrael storyteller** — **owned by Homesteader**. This package only injects the teller when Homesteader is absent (`PatchOperationFindMod` nomatch), so Homesteader + Azrael together never double-define `Azrael`.
3. **Series hub** — Mod Options → Azrael. Loaded series mods + versions, live bridges, named conflicts. Copy report when asking for help.

## Release checklist (1.0.0, first release)

Build and package:
- [ ] Rebuild both DLLs right before uploading: `dotnet build Azrael/Source/Azrael.csproj -c Release` (1.6) and take the 1.7 DLL from CI (`Azrael/Source/Azrael.1.7.csproj` needs the .NET 10 SDK). `Assemblies/*.dll` are not in git, so an old local DLL ships if you skip this.
- [ ] Workshop folder has `1.6/Assemblies/Azrael.dll`, `1.7/Assemblies/Azrael.dll` and `LoadFolders.xml`.
- [ ] Player.log on each game version: `[Azrael] v1.0.0 loaded from ...; series-release-v1`.

In game (full list: in-game test list in the project files):
- [ ] Homesteader alone → Azrael appears in the storyteller list.
- [ ] Azrael alone (no Homesteader) → Azrael appears; Deep Homestead scenario works; no red errors.
- [ ] Homesteader + Azrael → one Azrael teller (no duplicate def); Deep Homestead forces Azrael.
- [ ] Mod Options → Azrael: Failed patches says none on 1.6 and 1.7; Copy report pastes; Save report writes the file.
- [ ] + Strata: `Strata` research unlocked at Deep Homestead start.
- [ ] Mid-save add does not brick the save.

Publish:
- [ ] Workshop description and changelog from `About/changelog.txt`; site notes in `site/src/data/changelogs/azrael.md`.
- [ ] After the Workshop upload, add `About/PublishedFileId.txt` and the Workshop id to `mod.json` and the site's `mods.json` entry.
- [ ] Release & Publish workflow with mod `azrael` (dry run first).

## Load order

Harmony → Core → (DLC) → Homesteader / series content → **Azrael** last among them.
