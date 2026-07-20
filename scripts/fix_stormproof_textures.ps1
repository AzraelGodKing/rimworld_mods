# Batch alpha-fix every Stormproof PNG.
$repo = $PSScriptRoot
$root = Join-Path $repo 'Stormproof\Textures'
$files = Get-ChildItem $root -Recurse -Filter '*.png' | ForEach-Object { $_.FullName }

Write-Output "Processing $($files.Count) Stormproof textures (full clean)."
& (Join-Path $repo 'fix_texture_alpha.ps1') -Paths $files
