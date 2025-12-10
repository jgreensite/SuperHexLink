Param(
    [string]$BaseRef = 'main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Write-Error 'git required for harness branch flow test'; exit 2 }
if (-not (Test-Path 'scripts\test-check-changedonly-branch.ps1')) { Write-Error 'Branch harness script not found; failing test'; exit 2 }

Write-Host "Running harness branch flow test against base: $BaseRef"

# Run the branch harness in DryRun
if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
$rc = Start-Process -FilePath $exe -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\test-check-changedonly-branch.ps1','-BaseRef',$BaseRef,'-DryRun') -NoNewWindow -Wait -PassThru
if ($rc.ExitCode -ne 0) { Write-Error "Branch harness failed with exit $($rc.ExitCode)"; Pop-Location; exit 1 }

if (-not (Test-Path 'changed-csprojs.txt')) { Write-Error 'Branch harness did not produce changed-csprojs.txt'; Pop-Location; exit 1 }
Copy-Item -Path changed-csprojs.txt -Destination changed-csprojs-branch-harness.txt -Force
Write-Host 'Branch harness flow produced expected artifact: changed-csprojs-branch-harness.txt' -ForegroundColor Green
Pop-Location
exit 0
param(
    [string]$BaseRef = 'main'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

$harness = Join-Path $repoRoot 'scripts\test-check-changedonly-branch.ps1'
if (-not (Test-Path $harness)) { Write-Error "Branch harness not found at $harness"; exit 2 }

remove-item -force -erroraction SilentlyContinue changed-csprojs*.txt

Write-Host "Running branch harness in DryRun (baseRef: $BaseRef)"
pwsh -NoProfile -ExecutionPolicy Bypass -File $harness -BaseRef $BaseRef -DryRun
if (Test-Path changed-csprojs.txt) { Copy-Item -Path changed-csprojs.txt -Destination changed-csprojs-branch-harness.txt -Force }

if (-not (Test-Path changed-csprojs-branch-harness.txt)) { Write-Error 'Branch harness did not write changed-csprojs.txt or failed to report detection' ; exit 1 }
Write-Host 'Branch harness produced output; check file changed-csprojs-branch-harness.txt for content' -ForegroundColor Green

Pop-Location
exit 0
