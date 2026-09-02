# Rebuilds the per-mod download zips served by the docs site / GitHub Release.
# Packs committed mod trees from HEAD, then injects locally built Assemblies/*.dll
# (DLLs are gitignored; build them first with dotnet, or run CI on main).
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$out = Join-Path $repo 'docs\downloads'
New-Item -ItemType Directory -Force -Path $out | Out-Null

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Set-ZipEntryFile {
    param(
        [string]$ZipPath,
        [string]$EntryName,
        [string]$FilePath
    )
    $zip = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $existing = $zip.GetEntry($EntryName)
        if ($null -ne $existing) {
            $existing.Delete()
        }
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            $FilePath,
            $EntryName,
            [System.IO.Compression.CompressionLevel]::Optimal)
    }
    finally {
        $zip.Dispose()
    }
}

# folder = git path / zip root; zipName = download asset; dllName = Assemblies/*.dll
# Keep in sync with .github/workflows/build-mods.yml pack list.
$mods = @(
    @{ Folder = 'Homesteader'; ZipName = 'Homesteader'; DllName = 'Homesteader' },
    @{ Folder = 'Stormproof'; ZipName = 'Stormproof'; DllName = 'Stormproof' },
    @{ Folder = 'Strata'; ZipName = 'Strata'; DllName = 'Strata' },
    @{ Folder = 'Nemesis'; ZipName = 'Nemesis'; DllName = 'Nemesis' },
    @{ Folder = 'Deep Colony'; ZipName = 'DeepColony'; DllName = 'DeepColony' },
    @{ Folder = 'DateNight'; ZipName = 'DateNight'; DllName = 'DateNight' }
    # LivingWorld, Azrael, and Niceties are not release-ready — omit from public zips.
)

foreach ($mod in $mods) {
    $zip = Join-Path $out "$($mod.ZipName).zip"
    git -C $repo archive --format=zip -o $zip HEAD $mod.Folder
    if ($LASTEXITCODE -ne 0) { throw "git archive failed for $($mod.Folder)" }

    $dll = Join-Path $repo "$($mod.Folder)\Assemblies\$($mod.DllName).dll"
    if (Test-Path -LiteralPath $dll) {
        $entry = "$($mod.Folder)/Assemblies/$($mod.DllName).dll"
        Set-ZipEntryFile -ZipPath $zip -EntryName $entry -FilePath $dll
        Write-Output ("{0}: injected {1}" -f $mod.ZipName, $entry)
    }
    else {
        Write-Warning ("{0}: no {1} — zip has no assembly (build the mod first)" -f $mod.ZipName, $dll)
    }

    Write-Output ("{0}: {1:N0} KB" -f $mod.ZipName, ((Get-Item $zip).Length / 1KB))
}
