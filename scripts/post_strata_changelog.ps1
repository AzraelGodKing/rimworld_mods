# Posts the Strata [Unreleased] changelog as Discord embeds (patch-notes style).
# Usage: .\scripts\post_strata_changelog.ps1 -WebhookUrl 'https://discord.com/api/webhooks/...'
#        .\scripts\post_strata_changelog.ps1 -WebhookUrl '...' -DryRun

param(
    [Parameter(Mandatory = $true)]
    [string]$WebhookUrl,

    [switch]$DryRun
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

function Convert-ToDiscordMarkdown {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return '' }
    $t = $Text
    $t = [regex]::Replace($t, '(?s)<!--.*?-->', '')
    $t = [regex]::Replace($t, '\[([^\]]+)\]\(([^)]+)\)', '$1 (<$2>)')
    # Normalize unicode arrows/dashes (char codes so the file stays ASCII-safe)
    $t = $t.Replace("$([char]0x2192)", '->')   # right arrow
    $t = $t.Replace("$([char]0x2014)", '-')    # em dash
    $t = $t.Replace("$([char]0x2013)", '-')    # en dash
    $t = $t.Replace("$([char]0x00B1)", '+/-')  # plus-minus
    $t = $t.Replace("$([char]0x00D7)", 'x')    # multiply
    $t = $t.Replace("$([char]0x2194)", '<->')  # left-right arrow
    $t = $t.Replace("$([char]0x2022)", '-')    # bullet
    $t = [regex]::Replace($t, "[ \t]+\r?\n", "`n")
    $t = [regex]::Replace($t, "(\r?\n){3,}", "`n`n")
    return $t.Trim()
}

function Get-EntryTitle {
    param([string]$Line)
    if ($Line -match '^- \*\*(.+?)\*\*') { return $Matches[1].Trim() }
    if ($Line -match '^- (.+)$') {
        $plain = $Matches[1].Trim()
        if ($plain.Length -gt 80) { return $plain.Substring(0, 77) + '...' }
        return $plain
    }
    return $null
}

function Get-EntryBody {
    param([string]$Line)
    # Title sep: em/en dash, hyphen, or colon
    if ($Line -match ('^- \*\*(.+?)\*\*\s*[' + [char]0x2014 + [char]0x2013 + '\-:]\s*(.+)$')) {
        return $Matches[2].Trim()
    }
    if ($Line -match '^- \*\*(.+?)\*\*\s+(.+)$') { return $Matches[2].Trim() }
    return $null
}

$sections = [System.Collections.Generic.List[object]]::new()
$currentSection = $null
$currentEntry = $null

foreach ($rawLine in ($body -split "`r?`n")) {
    $line = $rawLine.TrimEnd()
    if ($line -match '^\s*<!--') { continue }
    if ($line -match '^### (.+)$') {
        if ($null -ne $currentEntry -and $null -ne $currentSection) {
            $currentSection.Entries.Add($currentEntry)
            $currentEntry = $null
        }
        $currentSection = [pscustomobject]@{
            Name    = $Matches[1].Trim()
            Entries = [System.Collections.Generic.List[object]]::new()
        }
        $sections.Add($currentSection)
        continue
    }
    if ($null -eq $currentSection) { continue }

    if ($line -match '^\s{2,}-\s+') {
        if ($null -eq $currentEntry) { continue }
        $subTitle = $null
        $subBody = $null
        if ($line -match '^\s+-\s+\*\*(.+?)\*\*\.?\s*(.*)$') {
            $subTitle = $Matches[1].Trim()
            $subBody = $Matches[2].Trim()
        }
        else {
            $subBody = ($line -replace '^\s+-\s+', '').Trim()
        }
        $bullet = if ($subTitle) {
            if ($subBody) { "**${subTitle}** - $subBody" } else { "**${subTitle}**" }
        }
        else { $subBody }
        $currentEntry.Bullets.Add((Convert-ToDiscordMarkdown $bullet))
        continue
    }

    if ($line -match '^- ') {
        if ($null -ne $currentEntry) {
            $currentSection.Entries.Add($currentEntry)
        }
        $title = Get-EntryTitle $line
        $entryBody = Get-EntryBody $line
        $currentEntry = [pscustomobject]@{
            Title   = Convert-ToDiscordMarkdown $title
            Body    = if ($entryBody) { Convert-ToDiscordMarkdown $entryBody } else { '' }
            Bullets = [System.Collections.Generic.List[string]]::new()
        }
        continue
    }
}

if ($null -ne $currentEntry -and $null -ne $currentSection) {
    $currentSection.Entries.Add($currentEntry)
}

function Format-EntryBlock {
    param($Entry)
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.Append("**$($Entry.Title)**")
    if ($Entry.Body) { [void]$sb.Append(" - $($Entry.Body)") }
    foreach ($b in $Entry.Bullets) { [void]$sb.Append("`n- $b") }
    return $sb.ToString()
}

function Split-SectionFields {
    param(
        [string]$SectionName,
        [System.Collections.IEnumerable]$Entries
    )
    $fields = [System.Collections.Generic.List[hashtable]]::new()
    $part = 1
    $buf = ''
    foreach ($entry in $Entries) {
        $block = Format-EntryBlock $entry
        if ($block.Length -gt 1024) {
            $block = $block.Substring(0, 1000).TrimEnd() + '...'
        }
        $candidate = if ($buf) { "$buf`n`n$block" } else { $block }
        if ($candidate.Length -gt 1024 -and $buf) {
            $name = if ($part -eq 1) { $SectionName } else { "$SectionName (cont.)" }
            $fields.Add(@{ name = $name; value = $buf; inline = $false })
            $part++
            $buf = $block
        }
        else {
            $buf = $candidate
        }
    }
    if ($buf) {
        $name = if ($part -eq 1) { $SectionName } else { "$SectionName (cont.)" }
        $fields.Add(@{ name = $name; value = $buf; inline = $false })
    }
    return , $fields
}

$allFields = [System.Collections.Generic.List[hashtable]]::new()
foreach ($sec in $sections) {
    if ($sec.Entries.Count -eq 0) { continue }
    $split = Split-SectionFields -SectionName $sec.Name -Entries $sec.Entries
    foreach ($f in $split) { $allFields.Add($f) }
}

$color = 5941866  # 0x5B8C8A
$modPage = 'https://azraelgodking.github.io/rimworld_mods/strata.html'
$download = 'https://azraelgodking.github.io/rimworld_mods/downloads/Strata.zip'
$fullCl = 'https://github.com/AzraelGodKing/rimworld_mods/blob/main/Strata/CHANGELOG.md'
$timestamp = (Get-Date).ToUniversalTime().ToString('o')

# Discord hard limits per message: 6000 chars across all embeds, 10 embeds.
function Get-EmbedCharCount {
    param([hashtable]$Embed)
    $n = 0
    if ($Embed.title) { $n += $Embed.title.Length }
    if ($Embed.description) { $n += $Embed.description.Length }
    if ($Embed.footer -and $Embed.footer.text) { $n += $Embed.footer.text.Length }
    if ($Embed.fields) {
        foreach ($f in $Embed.fields) {
            $n += $f.name.Length + $f.value.Length
        }
    }
    return $n
}

$messages = [System.Collections.Generic.List[hashtable]]::new()
$embeds = [System.Collections.Generic.List[hashtable]]::new()
$msgChars = 0
$maxMsgChars = 5500

function Flush-Message {
    if ($embeds.Count -eq 0) { return }
    $script:messages.Add(@{
            username = 'Strata'
            embeds   = @($script:embeds.ToArray())
        })
    $script:embeds.Clear()
    $script:msgChars = 0
}

function Add-Embed {
    param([hashtable]$Embed)
    $cost = Get-EmbedCharCount $Embed
    if (($embeds.Count -ge 10) -or ($embeds.Count -gt 0 -and ($msgChars + $cost) -gt $maxMsgChars)) {
        Flush-Message
        if (-not $Embed.title) {
            $Embed.title = 'Strata - Changelog (continued)'
        }
        # recount after optional title add
        $cost = Get-EmbedCharCount $Embed
    }
    $embeds.Add($Embed)
    $script:msgChars += $cost
}

Add-Embed @{
    title       = 'Strata - Changelog (Unreleased)'
    description = "One home, many floors.`n`n[Mod page]($modPage) | [Download]($download) | [Full changelog]($fullCl)"
    color       = $color
    url         = $modPage
    timestamp   = $timestamp
    footer      = @{ text = 'Strata | RimWorld 1.6' }
}

$fieldIndex = 0
while ($fieldIndex -lt $allFields.Count) {
    $batch = [System.Collections.Generic.List[hashtable]]::new()
    $chars = 0
    while ($fieldIndex -lt $allFields.Count -and $batch.Count -lt 4) {
        $f = $allFields[$fieldIndex]
        $add = $f.name.Length + $f.value.Length
        if ($batch.Count -gt 0 -and ($chars + $add) -gt 1800) { break }
        $batch.Add($f)
        $chars += $add
        $fieldIndex++
    }

    Add-Embed @{
        color     = $color
        fields    = @($batch.ToArray())
        timestamp = $timestamp
    }
}

Flush-Message

function Send-DiscordPayload {
    param([hashtable]$Payload)
    $json = $Payload | ConvertTo-Json -Compress -Depth 10
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    if ($DryRun) {
        Write-Output $json
        return
    }
    Invoke-RestMethod -Uri $WebhookUrl -Method Post -ContentType 'application/json; charset=utf-8' -Body $bytes
}

$i = 0
foreach ($msg in $messages) {
    $i++
    $embedCount = @($msg.embeds).Count
    $fieldCount = 0
    foreach ($e in $msg.embeds) {
        if ($e.ContainsKey('fields')) { $fieldCount += @($e.fields).Count }
    }
    Write-Output "Posting embed message $i of $($messages.Count) ($embedCount embed(s), $fieldCount field(s))..."
    Send-DiscordPayload -Payload $msg
    if (-not $DryRun -and $i -lt $messages.Count) { Start-Sleep -Milliseconds 900 }
}

Write-Output "Done - posted $($messages.Count) embed message(s) to Discord."
