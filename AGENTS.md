# Agent notes

## Cursor Cloud specific instructions

### Local Windows deploy

The author split Steam installs. **All live mod deployments from this repo go to the Dev RimWorld Mods folder only:**

`E:\SteamLibrary\steamapps\common\RimWorld - Dev\Mods`

Do **not** copy, junction, or robocopy mods into the gaming install (`E:\SteamLibrary\steamapps\common\RimWorld\Mods` or `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`).

Use [`scripts/deploy-mods.ps1`](scripts/deploy-mods.ps1) on Windows. Override the destination with `$env:RIMWORLD_DEV_MODS` if the Dev library path differs.

### Cloud Agent RimWorld (Linux VM)

Cloud agents still install/link mods under `/opt/rimworld/RimWorld/Mods` via [`.cursor/install-rimworld.sh`](.cursor/install-rimworld.sh) and [`scripts/setup-rimworld.sh`](scripts/setup-rimworld.sh). That path is the cloud VM, not the Windows gaming PC. Do not retarget those scripts at the `E:\...` Dev folder.

### Gravship / Odyssey

Launching a gravship cannot be reproduced in the cloud VM (needs Odyssey + a save on the Dev install). After Strata C# changes, `dotnet build Strata/Source/Strata.csproj -c Release` is the automated check here; in-game launch/land confirmation happens on the Dev RimWorld.

## Versions

Never bump `About.xml` / version fields unless the user asks.

## Changelogs

Update root [`CHANGELOG.md`](CHANGELOG.md) and the edited mod’s `CHANGELOG.md` on every change.
