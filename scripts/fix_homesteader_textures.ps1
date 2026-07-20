# Batch alpha-fix every Homesteader PNG except seamless terrain tiles.
$repo = Split-Path $PSScriptRoot -Parent
$root = Join-Path $repo 'Homesteader\Textures'
$conservative = @(
    'UltratechBattery.png',
    'RockSalt.png',
    'SaltedMeat.png',
    'Sugar.png',
    'Flour.png',
    'Cream.png',
    'Porridge.png'
)
$files = Get-ChildItem $root -Recurse -Filter '*.png' |
    Where-Object { $_.FullName -notmatch '\\Terrain\\' }

$full = @()
$gentle = @()
foreach ($f in $files) {
    if ($conservative -contains $f.Name) { $gentle += $f.FullName } else { $full += $f.FullName }
}

Write-Output "Processing $($full.Count) textures (full clean), $($gentle.Count) conservative, skipping terrain."
& (Join-Path $PSScriptRoot 'fix_texture_alpha.ps1') -Paths $full
& (Join-Path $PSScriptRoot 'fix_texture_alpha.ps1') -Paths $gentle -Conservative
