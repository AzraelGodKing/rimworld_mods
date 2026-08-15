# Copy or junction repo mods into the Dev RimWorld Mods folder.
# Never writes to the gaming install (...\common\RimWorld\Mods).
#
# Default destination:
#   E:\SteamLibrary\steamapps\common\RimWorld - Dev\Mods
# Override:
#   $env:RIMWORLD_DEV_MODS  or  -ModsDir <path>
#
# Usage:
#   .\scripts\deploy-mods.ps1
#   .\scripts\deploy-mods.ps1 -Junction
param(
    [string]$ModsDir = $env:RIMWORLD_DEV_MODS,
    [switch]$Junction
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ModsDir)) {
    $ModsDir = 'E:\SteamLibrary\steamapps\common\RimWorld - Dev\Mods'
}

$normalized = $ModsDir.TrimEnd('\', '/')
if ($normalized -match '(?i)[\\/]RimWorld[\\/]Mods$' -and $normalized -notmatch '(?i)RimWorld - Dev') {
    throw @"
Refusing to deploy to the gaming RimWorld Mods folder:
  $ModsDir

Use the Dev install:
  E:\SteamLibrary\steamapps\common\RimWorld - Dev\Mods
"@
}

if (-not (Test-Path -LiteralPath $ModsDir)) {
    throw "Dev Mods folder does not exist: $ModsDir"
}

$repo = Split-Path $PSScriptRoot -Parent
$mods = @(
    'Homesteader',
    'Stormproof',
    'Strata',
    'Nemesis',
    'Deep Colony',
    'DateNight',
    'LivingWorld'
)

foreach ($name in $mods) {
    $src = Join-Path $repo $name
    if (-not (Test-Path -LiteralPath $src)) {
        Write-Warning "Skip missing $name"
        continue
    }

    $dest = Join-Path $ModsDir $name
    if ($Junction) {
        if (Test-Path -LiteralPath $dest) {
            $item = Get-Item -LiteralPath $dest
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                cmd /c rmdir "$dest" | Out-Null
            }
            else {
                throw "Destination exists and is not a junction: $dest (remove it or omit -Junction)"
            }
        }
        New-Item -ItemType Junction -Path $dest -Target $src | Out-Null
        Write-Output "Junction $name -> $dest"
        continue
    }

    robocopy $src $dest /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    $code = $LASTEXITCODE
    # robocopy: 0-7 are success/partial; 8+ are failures
    if ($code -ge 8) {
        throw "robocopy failed for $name (exit $code)"
    }
    Write-Output "Copied $name -> $dest"
}

Write-Output "Deployed to $ModsDir"
