# Steam Workshop upload (runs on this PC only)

Script: `scripts\steam_publish.ps1`. SteamCMD lives in `_tools\steamcmd\` (git-ignored; its login cache
is stored there, so it must never be committed). This repo is public, so uploads are not run in CI.

## One-time login (Steam Guard code)

    _tools\steamcmd\steamcmd.exe +login YOUR_STEAM_NAME +quit

## Everyday use (run from the repo root, in PowerShell)

    # 1. Build the mods you are releasing (the script uploads whatever is in <Mod>\Assemblies\)
    # 2. Bump modVersion in About.xml and put the same version on line 1 of About\changelog.txt
    # 3. Commit (the script packages from git HEAD, plus the built DLL)
    # 4. Dry run, nothing is sent:
    .\scripts\steam_publish.ps1 -Mod Niceties
    .\scripts\steam_publish.ps1 -All
    # 5. Upload for real:
    .\scripts\steam_publish.ps1 -Mod Niceties -Upload -Account YOUR_STEAM_NAME

It refuses to upload when the changelog is missing or empty, or when its version differs from About.xml.
Only the content and the change note are sent; the Workshop title, description and preview are left alone.

## New mod not on Steam yet (creates a PRIVATE item, then saves its id)

    .\scripts\steam_publish.ps1 -Create -Mod Azrael                       # dry run
    .\scripts\steam_publish.ps1 -Create -Mod Azrael -Upload -Account YOUR_STEAM_NAME

Needs About\Preview.png. Optional About\steam_description.txt (Steam BBCode) for the description.
Commit the new About\PublishedFileId.txt afterwards. Never re-run -Create after a failure without
checking your Steam profile first, or you may get a duplicate item.

A mod that already has a Workshop page: just put its number in About\PublishedFileId.txt.
