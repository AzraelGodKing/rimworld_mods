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
    @{ Folder = 'DateNight'; ZipName = 'DateNight'; DllName = 'DateNight' },
    @{ Folder = 'Niceties'; ZipName = 'Niceties'; DllName = 'Niceties' }
    # LivingWorld, Azrael, and Wave Survival Reforged are not release-ready — omit from public zips.
)

foreach ($mod in $mods) {
    $zip = Join-Path $out "$($mod.ZipName).zip"
    git -C $repo archive --format=zip -o $zip HEAD $mod.Folder
    if ($LASTEXITCODE -ne 0) { throw "git archive failed for $($mod.Folder)" }

    $dllCandidates = @(
        @{ Rel = "1.6\Assemblies\$($mod.DllName).dll"; Entry = "$($mod.Folder)/1.6/Assemblies/$($mod.DllName).dll" },
        @{ Rel = "1.7\Assemblies\$($mod.DllName).dll"; Entry = "$($mod.Folder)/1.7/Assemblies/$($mod.DllName).dll" }
    )
    $injected = $false
    foreach ($cand in $dllCandidates) {
        $dll = Join-Path $repo "$($mod.Folder)\$($cand.Rel)"
        if (Test-Path -LiteralPath $dll) {
            Set-ZipEntryFile -ZipPath $zip -EntryName $cand.Entry -FilePath $dll
            Write-Output ("{0}: injected {1}" -f $mod.ZipName, $cand.Entry)
            $injected = $true
        }
    }
    if (-not $injected) {
        Write-Warning ("{0}: no {1}.dll — zip has no assembly (build the mod first)" -f $mod.ZipName, $mod.DllName)
    }

    Write-Output ("{0}: {1:N0} KB" -f $mod.ZipName, ((Get-Item $zip).Length / 1KB))
}
