<#
.SYNOPSIS
    Builds the distributable packages of MediaPilot.

.DESCRIPTION
    Publishes the application as a self-contained executable, compiles the
    WiX installer over that output and leaves both in the artifacts folder.

    The installer is a WiX project and is not part of the solution: it needs
    the published output to exist before it can be compiled, so it is built
    here and not by "dotnet build MediaPilot.slnx".

.PARAMETER Runtime
    Target runtime identifier. Defaults to win-x64.

.PARAMETER Configuration
    Build configuration. Defaults to Release.

.EXAMPLE
    ./build/package.ps1

.EXAMPLE
    ./build/package.ps1 -Runtime win-arm64
#>
[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Runtime = 'win-x64',

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'MediaPilot.slnx'
$installerProject = Join-Path $root 'installer\MediaPilot.Installer.wixproj'
$publishDirectory = Join-Path $root 'artifacts\publish'
$packageDirectory = Join-Path $root 'artifacts\packages'

$properties = [xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)
$versionNode = $properties.SelectSingleNode('//VersionPrefix')
$version = if ($null -ne $versionNode) { $versionNode.InnerText } else { $null }

if (-not $version) {
    throw 'The version could not be read from Directory.Build.props.'
}

Write-Host ''
Write-Host '==> MediaPilot packaging' -ForegroundColor Cyan
Write-Host "    Version       : $version"
Write-Host "    Runtime       : $Runtime"
Write-Host "    Configuration : $Configuration"

Write-Host ''
Write-Host '==> Publishing the application (self-contained)' -ForegroundColor Cyan
& dotnet publish (Join-Path $root 'src\MediaPilot.App\MediaPilot.App.csproj') `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "The publish failed with exit code $LASTEXITCODE."
}

Write-Host ''
Write-Host '==> Building the installer' -ForegroundColor Cyan
& dotnet build $installerProject `
    --configuration $Configuration `
    --nologo `
    "-p:ProductVersion=$version" `
    "-p:PublishDirectory=$publishDirectory\"
if ($LASTEXITCODE -ne 0) {
    throw "The installer build failed with exit code $LASTEXITCODE."
}

$package = Get-ChildItem -Path (Join-Path $root "installer\bin\$Configuration") `
    -Filter "MediaPilot-$version-x64.msi" `
    -Recurse `
    -File |
    Select-Object -First 1

if ($null -eq $package) {
    throw "The expected installer was not found under installer\bin\$Configuration."
}

Write-Host "    Installer     : $($package.FullName)"

New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null
$destination = Join-Path $packageDirectory "MediaPilot-$version-x64.msi"
Copy-Item -LiteralPath $package.FullName -Destination $destination -Force

$size = [math]::Round((Get-Item -LiteralPath $destination).Length / 1MB, 1)

Write-Host ''
Write-Host "Package ready: $destination ($size MB)" -ForegroundColor Green
