Param(
    [string]$BaseRef = 'main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent | Split-Path -Parent)
Push-Location $repoRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Write-Error 'git required for harness branch flow test'; exit 2 }
if (-not (Test-Path 'scripts\test-check-changedonly-branch.ps1')) { Write-Error 'Branch harness script not found; failing test'; exit 2 }

Write-Host "Running harness branch flow test against base: $BaseRef"

# Run the branch harness in DryRun (use direct invocation so output surfaces in logs)
if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
$argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\test-check-changedonly-branch.ps1','-DryRun')
if ($BaseRef -and $BaseRef.Trim() -ne '') { $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\test-check-changedonly-branch.ps1','-BaseRef',$BaseRef,'-DryRun') }
$rawOut = & $exe @argList 2>&1
$rc = $LASTEXITCODE
if ($rc -ne 0) { Write-Error "Branch harness failed with exit $rc"; Write-Host "Branch harness output:`n$rawOut"; Pop-Location; exit 1 }

if (-not (Test-Path 'changed-csprojs.txt')) { Write-Error 'Branch harness did not produce changed-csprojs.txt'; Pop-Location; exit 1 }
Copy-Item -Path changed-csprojs.txt -Destination changed-csprojs-branch-harness.txt -Force
Write-Host 'Branch harness flow produced expected artifact: changed-csprojs-branch-harness.txt' -ForegroundColor Green
Pop-Location
exit 0
 
