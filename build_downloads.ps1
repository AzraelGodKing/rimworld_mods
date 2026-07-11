# Rebuilds the per-mod download zips served by the docs site.
# Run from anywhere; zips are built from committed (HEAD) files only.
$repo = $PSScriptRoot
$out = Join-Path $repo 'docs\downloads'
New-Item -ItemType Directory -Force -Path $out | Out-Null

foreach ($mod in @('Homesteader', 'Stormproof', 'Wellspring', 'Strata')) {
    $zip = Join-Path $out "$mod.zip"
    git -C $repo archive --format=zip -o $zip HEAD $mod
    if ($LASTEXITCODE -ne 0) { throw "git archive failed for $mod" }
    Write-Output ("{0}: {1:N0} KB" -f $mod, ((Get-Item $zip).Length / 1KB))
}
