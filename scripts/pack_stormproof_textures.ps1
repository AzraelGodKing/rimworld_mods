# Upscale Stormproof building sprites so architect icons / in-world draw aren't tiny padded dots.
$repo = Split-Path $PSScriptRoot -Parent
$root = Join-Path $repo 'Stormproof\Textures\Stormproof\Buildings'
$files = Get-ChildItem $root -Filter '*.png' |
    Where-Object { $_.Name -notlike 'ArmoredConduit*' } |
    ForEach-Object { $_.FullName }

Write-Output "Packing $($files.Count) Stormproof building textures to ~90% canvas fill."
& (Join-Path $PSScriptRoot 'pack_texture_content.ps1') -Paths $files -Fill 0.90
