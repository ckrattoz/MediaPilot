<#
.SYNOPSIS
    Publishes MediaPilot into a ready to distribute directory.

.DESCRIPTION
    Runs "dotnet publish" for MediaPilot.App and leaves the result in
    artifacts/publish.

    By default the publish is runtime dependent: the executable is small (a
    few MB) but the target machine needs the .NET Desktop Runtime 10 installed.
    With -SelfContained the runtime travels with the executable and nothing has
    to be installed, at the cost of about 150 MB.

    -SingleFile produces a single .exe. Trimming is not enabled: WPF is not
    compatible with automatic trimming without a very specific extra
    configuration, and a trimmed executable can fail at runtime.

.PARAMETER Runtime
    Target runtime identifier. Defaults to win-x64.

.PARAMETER SelfContained
    Includes the .NET runtime in the output.

.PARAMETER SingleFile
    Publishes into a single executable.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.EXAMPLE
    ./build/publish.ps1

.EXAMPLE
    ./build/publish.ps1 -SelfContained -Runtime win-arm64
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-x86', 'win-arm64')]
    [string] $Runtime = 'win-x64',

    [switch] $SelfContained,

    [switch] $SingleFile,

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\MediaPilot.App\MediaPilot.App.csproj'
$output = Join-Path $root 'artifacts\publish'

Write-Host ''
Write-Host '==> Publishing MediaPilot' -ForegroundColor Cyan
Write-Host "    Runtime        : $Runtime"
Write-Host "    Self-contained : $(if ($SelfContained) { 'yes' } else { 'no' })"
Write-Host "    Single file    : $(if ($SingleFile) { 'yes' } else { 'no' })"
Write-Host "    Output         : $output"

$arguments = @(
    'publish'
    $project
    '--configuration', $Configuration
    '--runtime', $Runtime
    '--output', $output
    '--nologo'
)

if ($SelfContained) {
    $arguments += '--self-contained', 'true'
}
else {
    $arguments += '--self-contained', 'false'
}

if ($SingleFile) {
    $arguments += '-p:PublishSingleFile=true'
}

# Automatic trimming is not compatible with WPF without extra configuration.
$arguments += '-p:PublishTrimmed=false'

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "The publish failed with exit code $LASTEXITCODE."
}

$executable = Join-Path $output 'MediaPilot.App.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "The expected executable was not found in '$executable'."
}

$size = [math]::Round((Get-ChildItem -LiteralPath $output -Recurse -File | Measure-Object -Property Length -Sum).Sum / 1MB, 1)

Write-Host ''
Write-Host "Publish succeeded: $executable ($size MB in total)." -ForegroundColor Green
