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

foreach ($mod in @('Homesteader', 'Stormproof', 'Strata', 'Nemesis')) {
    $zip = Join-Path $out "$mod.zip"
    git -C $repo archive --format=zip -o $zip HEAD $mod
    if ($LASTEXITCODE -ne 0) { throw "git archive failed for $mod" }

    $dll = Join-Path $repo "$mod\Assemblies\$mod.dll"
    if (Test-Path -LiteralPath $dll) {
        $entry = "$mod/Assemblies/$mod.dll"
        Set-ZipEntryFile -ZipPath $zip -EntryName $entry -FilePath $dll
        Write-Output ("{0}: injected {1}" -f $mod, $entry)
    }
    else {
        Write-Warning ("{0}: no {1} — zip has no assembly (build the mod first)" -f $mod, $dll)
    }

    Write-Output ("{0}: {1:N0} KB" -f $mod, ((Get-Item $zip).Length / 1KB))
}
