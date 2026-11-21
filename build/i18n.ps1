<#
.SYNOPSIS
Validates and synchronizes the MediaPilot user interface resource files.

.DESCRIPTION
The neutral resource file (Strings.resx) holds the primary language, Spanish.
Every other language lives in its own satellite file (Strings.<culture>.resx).

This script keeps those files in sync:

  -Check   Compares every satellite file against the neutral one and reports
           missing keys, unexpected keys, empty values and entries that are
           still pending translation. It exits with code 1 when a satellite
           file is incomplete, so it can be used as a gate in CI.

  -Sync    Creates or refreshes a satellite file from the neutral one. Existing
           translations are preserved. Keys that are new or that are still
           pending translation receive the neutral value and a marker comment,
           so the interface is never blank while a language is incomplete.

.PARAMETER Check
Validates the resource files instead of modifying them.

.PARAMETER Sync
Creates or refreshes a satellite resource file.

.PARAMETER Culture
Culture of the satellite file handled by -Sync, for example en or pt-BR.

.EXAMPLE
./build/i18n.ps1 -Check

.EXAMPLE
./build/i18n.ps1 -Sync -Culture pt-BR
#>
[CmdletBinding(DefaultParameterSetName = 'Check')]
param(
    [Parameter(ParameterSetName = 'Check')]
    [switch] $Check,

    [Parameter(ParameterSetName = 'Sync', Mandatory)]
    [switch] $Sync,

    [Parameter(ParameterSetName = 'Sync')]
    [string] $Culture = 'en'
)

$ErrorActionPreference = 'Stop'

$resourcesDirectory = Join-Path $PSScriptRoot '..\src\MediaPilot.Localization\Resources'
$neutralPath = Join-Path $resourcesDirectory 'Strings.resx'

# Marker written on entries that still hold the neutral value and therefore
# need a translation. It doubles as the signal used by -Check.
$pendingMarker = 'Pending translation'

function Get-ResxEntries {
    <#
    .SYNOPSIS
    Reads the data entries of a .resx file.
    #>
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [switch] $TolerateInvalid
    )

    $entries = [ordered]@{}

    if (-not (Test-Path -LiteralPath $Path)) {
        return $entries
    }

    $document = New-Object System.Xml.XmlDocument
    $document.PreserveWhitespace = $true

    try {
        $document.Load($Path)
    }
    catch {
        if (-not $TolerateInvalid) {
            throw
        }

        Write-Warning "The file is not valid XML and will be regenerated: $Path"
        return $entries
    }

    foreach ($node in $document.DocumentElement.SelectNodes('data')) {
        $valueNode = $node.SelectSingleNode('value')
        $commentNode = $node.SelectSingleNode('comment')

        $value = ''
        if ($null -ne $valueNode) {
            $value = $valueNode.InnerText
        }

        $comment = ''
        if ($null -ne $commentNode) {
            $comment = $commentNode.InnerText
        }

        $entries[$node.GetAttribute('name')] = [pscustomobject]@{
            Name    = $node.GetAttribute('name')
            Value   = $value
            Comment = $comment
        }
    }

    return $entries
}

function Get-ResxHeader {
    <#
    .SYNOPSIS
    Returns the schema and resheader block of a .resx file, up to the first
    data entry, so the generated file keeps the exact same layout.
    #>
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $lines = [System.IO.File]::ReadAllLines($Path)
    $header = New-Object System.Collections.Generic.List[string]

    foreach ($line in $lines) {
        $header.Add($line)

        if ($line -match '^\s*</resheader>\s*$') {
            break
        }
    }

    return $header
}

function ConvertTo-XmlText {
    <#
    .SYNOPSIS
    Escapes a value so it can be written as the text of a .resx element.
    #>
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string] $Text
    )

    return $Text.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}

function Test-EntryPending {
    param(
        [Parameter(Mandatory)]
        $Entry
    )

    return $Entry.Comment -like "*$pendingMarker*"
}

function Invoke-Check {
    $neutral = Get-ResxEntries -Path $neutralPath
    $satellites = Get-ChildItem -LiteralPath $resourcesDirectory -Filter 'Strings.*.resx'

    $errors = 0
    $warnings = 0

    Write-Host "Neutral resource file: Strings.resx ($($neutral.Count) entries)"

    foreach ($satellite in $satellites) {
        $culture = $satellite.BaseName -replace '^Strings\.', ''
        $entries = Get-ResxEntries -Path $satellite.FullName

        $missing = @($neutral.Keys | Where-Object { -not $entries.Contains($_) })
        $unexpected = @($entries.Keys | Where-Object { -not $neutral.Contains($_) })
        $empty = @($entries.Keys | Where-Object { [string]::IsNullOrEmpty($entries[$_].Value) })
        $pending = @($entries.Keys | Where-Object { Test-EntryPending $entries[$_] })

        Write-Host ""
        Write-Host "Satellite '$culture': $($entries.Count) entries"

        foreach ($key in $missing) {
            Write-Host "  ERROR   missing key: $key" -ForegroundColor Red
            $errors++
        }

        foreach ($key in $unexpected) {
            Write-Host "  ERROR   unexpected key: $key" -ForegroundColor Red
            $errors++
        }

        foreach ($key in $empty) {
            Write-Host "  ERROR   empty value: $key" -ForegroundColor Red
            $errors++
        }

        foreach ($key in $pending) {
            Write-Host "  WARNING pending translation: $key" -ForegroundColor Yellow
            $warnings++
        }

        if ($missing.Count -eq 0 -and $unexpected.Count -eq 0 -and $empty.Count -eq 0) {
            Write-Host "  Key set matches the neutral file." -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "Result: $errors error(s), $warnings pending translation(s)."

    if ($errors -gt 0) {
        return 1
    }

    return 0
}

function Invoke-Sync {
    $targetPath = Join-Path $resourcesDirectory "Strings.$Culture.resx"

    if (-not (Test-Path -LiteralPath $neutralPath)) {
        throw "The neutral resource file was not found: $neutralPath"
    }

    $neutral = Get-ResxEntries -Path $neutralPath
    $existing = Get-ResxEntries -Path $targetPath -TolerateInvalid

    $header = Get-ResxHeader -Path $neutralPath
    $body = New-Object System.Collections.Generic.List[string]

    $added = 0
    $preserved = 0

    foreach ($key in $neutral.Keys) {
        $neutralValue = $neutral[$key].Value
        $value = $null
        $comment = $null

        if ($existing.Contains($key)) {
            $current = $existing[$key]

            if (-not (Test-EntryPending $current) -and -not [string]::IsNullOrEmpty($current.Value)) {
                $value = $current.Value
                $preserved++
            }
        }

        if ($null -eq $value) {
            $value = $neutralValue
            $comment = $pendingMarker
            $added++
        }

        $body.Add("  <data name=""$key"" xml:space=""preserve"">")
        $body.Add("    <value>$(ConvertTo-XmlText $value)</value>")

        if ($comment) {
            $body.Add("    <comment>$comment</comment>")
        }

        $body.Add("  </data>")
    }

    $content = ($header + $body) -join "`r`n"
    [System.IO.File]::WriteAllText($targetPath, $content + "`r`n</root>`r`n", (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "Satellite file updated: $(Split-Path -Leaf $targetPath)"
    Write-Host "  Entries:       $($neutral.Count)"
    Write-Host "  Translated:    $preserved"
    Write-Host "  Pending:       $added"
    Write-Host ""
    Write-Host "Translate the pending entries marked with the '$pendingMarker' comment."

    return 0
}

if (-not (Test-Path -LiteralPath $neutralPath)) {
    throw "The neutral resource file was not found: $neutralPath"
}

switch ($PSCmdlet.ParameterSetName) {
    'Sync' { exit (Invoke-Sync) }
    default { exit (Invoke-Check) }
}
