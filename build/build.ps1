<#
.SYNOPSIS
    Builds MediaPilot and runs the test suite.

.DESCRIPTION
    Restores the dependencies, builds the whole solution in the requested
    configuration and runs the tests. The repository treats compiler warnings
    as errors, so this script fails on any warning.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.PARAMETER SkipTests
    Builds without running the tests.

.EXAMPLE
    ./build/build.ps1

.EXAMPLE
    ./build/build.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'MediaPilot.slnx'

function Invoke-Step {
    param(
        [Parameter(Mandatory)]
        [string] $Description,

        [Parameter(Mandatory)]
        [scriptblock] $Action
    )

    Write-Host ''
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

Invoke-Step 'Restoring dependencies' {
    dotnet restore $solution
}

Invoke-Step "Validating the localization resources" {
    & (Join-Path $PSScriptRoot 'i18n.ps1') -Check
}

Invoke-Step "Building ($Configuration)" {
    dotnet build $solution --configuration $Configuration --no-restore --nologo
}

if (-not $SkipTests) {
    Invoke-Step 'Running the tests' {
        dotnet test $solution --configuration $Configuration --no-build --nologo --verbosity normal
    }
}

Write-Host ''
Write-Host 'Build succeeded.' -ForegroundColor Green
