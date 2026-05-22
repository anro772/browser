#requires -Version 5.1
<#
.SYNOPSIS
    Publishes the Slate browser UI into a distributable folder.
.DESCRIPTION
    Produces a self-contained, ReadyToRun-precompiled, NON-single-file
    win-x64 build at publish\win-x64. NOT single-file because that adds a
    one-time temp-extract penalty on first launch which we don't want.
    See plans\we-were-writing-this-declarative-perlis.md for rationale.
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $Runtime       = 'win-x64',
    [string] $OutputDir     = (Join-Path $PSScriptRoot 'publish\win-x64')
)

$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "Publishing Slate ($Configuration, $Runtime)" -ForegroundColor Cyan
Write-Host "  Output -> $OutputDir"
Write-Host ""

# Wipe stale output so removed files don't linger.
if (Test-Path $OutputDir) {
    Write-Host "Cleaning previous output..." -ForegroundColor DarkGray
    Remove-Item -Recurse -Force $OutputDir
}

$project = Join-Path $PSScriptRoot 'BrowserApp.UI\BrowserApp.UI.csproj'

& dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -p:PublishSingleFile=false `
    -p:DebugType=embedded `
    -p:DebugSymbols=true `
    -p:SatelliteResourceLanguages=en `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Sanity-check stamp so we can confirm what's inside a built artifact.
$stamp = @"
Slate publish stamp
Configuration : $Configuration
Runtime       : $Runtime
Built         : $(Get-Date -Format 'u')
Machine       : $env:COMPUTERNAME
"@
Set-Content -Path (Join-Path $OutputDir 'version.txt') -Value $stamp -Encoding UTF8

$exe = Join-Path $OutputDir 'BrowserApp.UI.exe'
if (-not (Test-Path $exe)) {
    Write-Error "Expected $exe to exist after publish but it does not."
    exit 1
}

$size = [math]::Round((Get-ChildItem $OutputDir -Recurse | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host ""
Write-Host "Publish OK." -ForegroundColor Green
Write-Host "  Entry:  $exe"
Write-Host "  Size:   $size MB"
Write-Host ""
Write-Host "Next:" -ForegroundColor Cyan
Write-Host "  1. Smoke-test directly:  & '$exe'"
Write-Host "  2. Build installer:      iscc installer\BrowserApp.iss"
