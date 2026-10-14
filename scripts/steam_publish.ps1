<#
.SYNOPSIS
  Upload RimWorld mod updates to the Steam Workshop with SteamCMD, with release notes from About\changelog.txt.

.DESCRIPTION
  UPDATE mode (default) - for each mod that has About\PublishedFileId.txt it:
    1. checks the changelog exists and its first line (the version) matches modVersion in About.xml,
    2. stages a clean copy (git archive honours .gitattributes export-ignore) and adds the built DLL,
    3. writes a SteamCMD .vdf that sets ONLY the content and the change note
       (title, description and preview image on Steam are left alone),
    4. runs SteamCMD, but only when -Upload is given. Without it, this is a dry run.

  CREATE mode (-Create) - for mods with NO PublishedFileId.txt yet. It makes a NEW Workshop item,
  PRIVATE, using the title from About.xml, About\Preview.png, and the description from
  About\steam_description.txt (falls back to the About.xml description). On success it saves the new
  id to About\PublishedFileId.txt. Every real run creates a new item, so do not re-run after a failure
  without checking your Steam profile first. Also needs -Upload to do anything.

.EXAMPLE
  .\scripts\steam_publish.ps1 -Mod Niceties                       # dry run, one mod
  .\scripts\steam_publish.ps1 -All                                # dry run, every published mod
  .\scripts\steam_publish.ps1 -Mod Niceties -Upload -Account you  # really upload an update
  .\scripts\steam_publish.ps1 -Create -Mod LivingWorld            # dry run of a create
  .\scripts\steam_publish.ps1 -Create -Mod LivingWorld -Upload -Account you

  First login (Steam Guard code, once):  _tools\steamcmd\steamcmd.exe +login <account> +quit
#>
[CmdletBinding()]
param(
    [string[]]$Mod,
    [switch]$All,
    [switch]$Create,
    [switch]$Upload,
    [string]$Account = $env:STEAM_ACCOUNT,
    [string]$SteamCmd,
    [string]$AppId = '294100'   # RimWorld
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = (Resolve-Path (Join-Path $here '..')).Path
Set-Location $repo
if (-not $SteamCmd) { $SteamCmd = Join-Path $repo '_tools\steamcmd\steamcmd.exe' }

if (-not $All -and -not $Mod) { throw 'Give -Mod <folder name> (one or more) or -All.' }
if ($Upload -and -not $Account) { throw 'Give -Account <steam login name> (or set STEAM_ACCOUNT) to upload.' }
if ($Upload -and -not (Test-Path $SteamCmd)) { throw "SteamCMD not found at $SteamCmd" }

$modDirs = Get-ChildItem -Directory | Where-Object { Test-Path (Join-Path $_.FullName 'About\About.xml') }
$hasId = { param($d) Test-Path (Join-Path $d.FullName 'About\PublishedFileId.txt') }

if ($Create) {
    $pool = $modDirs | Where-Object { -not (& $hasId $_) }
    if ($All) { $targets = $pool } else {
        $targets = foreach ($m in $Mod) {
            $d = $modDirs | Where-Object { $_.Name -ieq $m }
            if (-not $d) { throw "No mod folder named '$m' with About\About.xml." }
            if (& $hasId $d) { throw "'$m' already has About\PublishedFileId.txt. Use update mode, or creating would make a duplicate." }
            $d
        }
    }
} else {
    $pool = $modDirs | Where-Object { & $hasId $_ }
    if ($All) { $targets = $pool } else {
        $targets = foreach ($m in $Mod) {
            $d = $pool | Where-Object { $_.Name -ieq $m }
            if (-not $d) { throw "'$m' has no About\PublishedFileId.txt. For a mod that is not on Steam yet use -Create; if it IS on Steam, put its number in that file." }
            $d
        }
    }
}

$stageRoot = Join-Path $env:TEMP ('steam_publish_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $stageRoot | Out-Null
$problems = @()
$utf8 = New-Object Text.UTF8Encoding($false)

foreach ($d in $targets) {
    $name = $d.Name
    $tag = ''; if ($Create) { $tag = '  (CREATE, private)' }
    Write-Host "`n=== $name$tag ===" -ForegroundColor Cyan
    try {
        $id = '0'
        if (-not $Create) {
            $id = (Get-Content (Join-Path $d.FullName 'About\PublishedFileId.txt') -Raw).Trim()
            if ($id -notmatch '^\d+$') { throw "PublishedFileId.txt is not a number: '$id'" }
        }

        $clPath = Join-Path $d.FullName 'About\changelog.txt'
        if (-not (Test-Path $clPath)) { throw 'About\changelog.txt is missing (no release notes).' }
        $clLines = Get-Content $clPath -Encoding UTF8
        $version = ($clLines | Select-Object -First 1).Trim()
        $notes = (($clLines | Select-Object -Skip 1) -join "`n").Trim()
        if ($version -notmatch '^\d+\.\d+\.\d+') { throw "First line of changelog.txt must be the version, found '$version'." }
        if ($notes.Length -lt 10) { throw 'Changelog has no notes under the version line.' }

        [xml]$about = Get-Content (Join-Path $d.FullName 'About\About.xml') -Raw -Encoding UTF8
        $modVersion = $about.ModMetaData.modVersion
        if ($modVersion -is [System.Xml.XmlElement]) { $modVersion = $modVersion.InnerText }
        if ($modVersion -and ($modVersion.Trim() -ne $version)) {
            throw "Version mismatch: changelog says $version, About.xml modVersion is $modVersion."
        }
        if ($notes.Length -gt 7900) { $notes = $notes.Substring(0, 7900) + "`n[i]...see the full changelog on GitHub.[/i]" }

        # Stage a clean copy (export-ignore rules drop Source, Tools, *.md, ...)
        $stage = Join-Path $stageRoot $name
        New-Item -ItemType Directory -Force $stage | Out-Null
        $zip = Join-Path $stageRoot "$name.zip"
        & git archive --format=zip -o $zip HEAD $name
        if ($LASTEXITCODE -ne 0) { throw 'git archive failed.' }
        Expand-Archive $zip $stageRoot -Force
        Remove-Item $zip

        $dlls = @(Get-ChildItem (Join-Path $d.FullName 'Assemblies') -Filter *.dll -ErrorAction SilentlyContinue)
        if ($dlls.Count -eq 0) { throw 'No built DLL in Assemblies\. Build the mod first.' }
        $dst = Join-Path $stage 'Assemblies'
        New-Item -ItemType Directory -Force $dst | Out-Null
        Copy-Item $dlls.FullName $dst -Force

        $esc = { param($s) $s.Replace('\', '\\').Replace('"', '\"') -replace "`r?`n", '\n' }
        $extra = ''
        if ($Create) {
            $title = $about.ModMetaData.name
            if ($title -is [System.Xml.XmlElement]) { $title = $title.InnerText }
            if (-not $title) { throw 'About.xml has no <name> to use as the Workshop title.' }
            $preview = Join-Path $d.FullName 'About\Preview.png'
            if (-not (Test-Path $preview)) { throw 'About\Preview.png is missing (Steam needs a preview image for a new item).' }
            $descFile = Join-Path $d.FullName 'About\steam_description.txt'
            if (Test-Path $descFile) { $desc = (Get-Content $descFile -Raw -Encoding UTF8).Trim(); $descSrc = 'About\steam_description.txt' }
            else {
                $desc = $about.ModMetaData.description
                if ($desc -is [System.Xml.XmlElement]) { $desc = $desc.InnerText }
                $desc = "$desc".Trim(); $descSrc = 'About.xml description (add About\steam_description.txt for proper Steam BBCode)'
            }
            if ($desc.Length -lt 20) { throw 'No description found. Add About\steam_description.txt.' }
            $extra = @"

    "title"           "$(& $esc $title)"
    "description"     "$(& $esc $desc)"
    "previewfile"     "$preview"
    "visibility"      "2"
"@
            Write-Host "Title: $title | description from $descSrc ($($desc.Length) chars) | preview: $preview | visibility: private"
        }

        $vdf = @"
"workshopitem"
{
    "appid"           "$AppId"
    "publishedfileid" "$id"
    "contentfolder"   "$stage"$extra
    "changenote"      "$(& $esc $notes)"
}
"@
        $vdfPath = Join-Path $stageRoot "$name.vdf"
        [IO.File]::WriteAllText($vdfPath, $vdf, $utf8)

        $files = (Get-ChildItem $stage -Recurse -File | Measure-Object).Count
        $mb = [math]::Round(((Get-ChildItem $stage -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 2)
        $shownId = $id; if ($Create) { $shownId = '(new)' }
        Write-Host ("Workshop id {0} | version {1} | {2} files, {3} MB | notes {4} chars" -f $shownId, $version, $files, $mb, $notes.Length)
        Write-Host "VDF: $vdfPath"

        if ($Upload) {
            & $SteamCmd +login $Account +workshop_build_item $vdfPath +quit 2>&1 | Tee-Object -Variable steamOut | ForEach-Object { Write-Host $_ }
            if ($LASTEXITCODE -ne 0) { throw "SteamCMD exited with code $LASTEXITCODE. If this was -Create, check your Steam profile before re-running." }
            if ($Create) {
                $newId = $null
                $m1 = [regex]::Match((Get-Content $vdfPath -Raw), '"publishedfileid"\s+"(\d+)"')
                if ($m1.Success -and $m1.Groups[1].Value -ne '0') { $newId = $m1.Groups[1].Value }
                if (-not $newId) {
                    $m2 = [regex]::Match(($steamOut -join "`n"), '(?i)publish(?:ed)?\s*file\s*id\D{0,10}(\d{6,})')
                    if ($m2.Success) { $newId = $m2.Groups[1].Value }
                }
                if (-not $newId) { throw 'Upload finished but I could not find the new Workshop id. Check your Steam profile (it is private), put the number in About\PublishedFileId.txt, and do NOT run -Create again.' }
                [IO.File]::WriteAllText((Join-Path $d.FullName 'About\PublishedFileId.txt'), $newId, $utf8)
                Write-Host "Created $name $version as PRIVATE item $newId. Saved to About\PublishedFileId.txt (commit it)." -ForegroundColor Green
            } else {
                Write-Host "Uploaded $name $version" -ForegroundColor Green
            }
        } else {
            Write-Host 'Dry run: nothing uploaded. Add -Upload -Account <name> to send it.' -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "SKIPPED $name : $($_.Exception.Message)" -ForegroundColor Red
        $problems += "$name : $($_.Exception.Message)"
    }
}

Write-Host ''
if ($problems.Count) { Write-Host "$($problems.Count) problem(s):" -ForegroundColor Red; $problems | ForEach-Object { Write-Host "  $_" } }
if (-not $Upload) { Write-Host "Staged files are in $stageRoot" }
