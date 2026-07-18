# Batch alpha-fix Homesteader Wellspring PNGs except seamless terrain tiles.
$repo = $PSScriptRoot
$root = Join-Path $repo 'Homesteader\Textures\Wellspring'
$conservative = @(
    'CleanBandages.png',
    'BoiledWater.png'
)
$files = Get-ChildItem $root -Recurse -Filter '*.png' |
    Where-Object { $_.FullName -notmatch '\\Terrain\\' -and $_.Name -notlike '*.tmp.png' }

$full = @()
$gentle = @()
foreach ($f in $files) {
    if ($conservative -contains $f.Name) { $gentle += $f.FullName } else { $full += $f.FullName }
}

Write-Output "Processing $($full.Count) Wellspring textures (full clean), $($gentle.Count) conservative, skipping terrain."
& (Join-Path $repo 'fix_texture_alpha.ps1') -Paths $full
& (Join-Path $repo 'fix_texture_alpha.ps1') -Paths $gentle -Conservative
