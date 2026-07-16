# Posts the Strata [Unreleased] changelog section to a Discord webhook.
# Usage: .\scripts\post_strata_changelog.ps1 -WebhookUrl 'https://discord.com/api/webhooks/...'

param(
    [Parameter(Mandatory = $true)]
    [string]$WebhookUrl
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$changelogPath = Join-Path $repo 'Strata/CHANGELOG.md'
if (-not (Test-Path $changelogPath)) {
    throw "Strata/CHANGELOG.md not found under $repo"
}

$raw = Get-Content $changelogPath -Raw -Encoding UTF8

if ($raw -notmatch '(?s)## \[Unreleased\]\s*(.*?)(?=\r?\n## \[|\z)') {
    throw 'Could not find [Unreleased] section in Strata/CHANGELOG.md'
}
$body = $Matches[1].Trim()

function Convert-MarkdownLine {
    param([string]$Line)
    $line = $Line.TrimEnd()
    if ($line -match '^### (.+)$') { return "`n**$($Matches[1].ToUpper())**`n" }
    if ($line -match '^- \*\*(.+?)\*\*:\s*(.+)$') {
        return "- **$($Matches[1])** - $($Matches[2])"
    }
    if ($line -match '^- \*\*(.+?)\*\*\.\s*(.+)$') {
        return "- **$($Matches[1])** - $($Matches[2])"
    }
    if ($line -match '^- \*\*(.+?)\*\*\s*(.+)$') {
        return "- **$($Matches[1])** - $($Matches[2])"
    }
    if ($line -match '^- \*\*(.+?)\*\*$') { return "- **$($Matches[1])**" }
    if ($line -match '^- (.+)$') { return "- $($Matches[1])" }
    if ($line -eq '') { return '' }
    return $line
}

$lines = $body -split "`r?`n"
$discordLines = foreach ($line in $lines) { Convert-MarkdownLine $line }
$text = ($discordLines -join "`n").Trim()

$header = @"
**STRATA - changelog (unreleased)**

One home, many floors - polish pass for multistory bases.

Download: https://azraelgodking.github.io/rimworld_mods/downloads/Strata.zip
Mod page: https://azraelgodking.github.io/rimworld_mods/strata.html
Full changelog: https://github.com/AzraelGodKing/rimworld_mods/blob/main/Strata/CHANGELOG.md

"@

function Send-DiscordMessage {
    param([string]$Content)
    $payload = @{ content = $Content } | ConvertTo-Json -Compress -Depth 5
    Invoke-RestMethod -Uri $WebhookUrl -Method Post -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($payload))
}

$maxLen = 1900
$chunks = [System.Collections.Generic.List[string]]::new()
$current = $header
foreach ($line in ($text -split "`n")) {
    $candidate = if ($current.EndsWith("`n")) { $current + $line } else { $current + "`n" + $line }
    if ($candidate.Length -gt $maxLen -and $current.Length -gt $header.Length) {
        $chunks.Add($current.TrimEnd())
        $current = "**STRATA changelog (continued)**`n`n" + $line
    } else {
        $current = $candidate
    }
}
if ($current.Trim().Length -gt 0) { $chunks.Add($current.TrimEnd()) }

$i = 0
foreach ($chunk in $chunks) {
    $i++
    Write-Output "Posting part $i of $($chunks.Count) ($($chunk.Length) chars)..."
    Send-DiscordMessage -Content $chunk
    if ($i -lt $chunks.Count) { Start-Sleep -Milliseconds 750 }
}

Write-Output "Done - posted $($chunks.Count) message(s) to Discord."
