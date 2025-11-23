<#
Git pre-push hook (PowerShell) to run quick sanity checks prior to pushing.
Place this file under `.git/hooks/pre-push` or run the wrapper to wire it up.
This hook runs `scripts/check-csproj-builds.ps1` to catch compilation errors before push.
#>
Param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent | Split-Path -Parent
$checkScript = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'
if (-Not (Test-Path $checkScript)) {
    Write-Host "Check script not found: $checkScript" -ForegroundColor Yellow
    exit 0
}

& $checkScript
if ($LASTEXITCODE -ne 0) {
    Write-Host "Pre-push check failed. Aborting push." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Pre-push checks passed." -ForegroundColor Green
exit 0
