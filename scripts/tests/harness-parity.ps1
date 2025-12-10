Param(
    [string]$BaseRef = 'main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Write-Error 'git required for test harness parity'; exit 2 }
if (-not (Test-Path 'scripts\check-csproj-builds-cli.ps1')) { Write-Error 'CLI guard script not found; failing test'; exit 2 }

Write-Host "Running harness parity test against base: $BaseRef"

# Run DryRun
$dryArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\check-csproj-builds-cli.ps1','-ChangedOnly','-DiffRef',$BaseRef,'-DryRun')
if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
$rc = Start-Process -FilePath $exe -ArgumentList $dryArgs -NoNewWindow -Wait -PassThru
if ($rc.ExitCode -ne 0) { Write-Error "DryRun invocation failed with exit $($rc.ExitCode)"; Pop-Location; exit 1 }
if (Test-Path 'changed-csprojs.txt') { Copy-Item -Path changed-csprojs.txt -Destination changed-csprojs-dryrun.txt -Force }

# Run normal detection
$args = @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\check-csproj-builds-cli.ps1','-ChangedOnly','-DiffRef',$BaseRef)
$rc = Start-Process -FilePath $exe -ArgumentList $args -NoNewWindow -Wait -PassThru
if ($rc.ExitCode -ne 0) { Write-Error "Normal invocation failed with exit $($rc.ExitCode)"; Pop-Location; exit 1 }

# Copy artifact
if (Test-Path 'changed-csprojs.txt') { Copy-Item -Path changed-csprojs.txt -Destination changed-csprojs.txt -Force }

Write-Host 'Comparing detection parity...'
$dry = '' ; $n = ''
if (Test-Path 'changed-csprojs-dryrun.txt') { $dry = (Get-Content 'changed-csprojs-dryrun.txt' -Raw).Trim() }
if (Test-Path 'changed-csprojs.txt') { $n = (Get-Content 'changed-csprojs.txt' -Raw).Trim() }
if ($dry -eq '' -and $n -eq '') { Write-Host 'Both empty - nothing to build'; Pop-Location; exit 0 }
if (($dry -eq '' -and $n -ne '') -or ($dry -ne '' -and $n -eq '')) { Write-Error 'DryRun and normal detection differ: one is empty and the other is not'; Pop-Location; exit 1 }
$a = $dry -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$b = $n -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$diff = Compare-Object -ReferenceObject $a -DifferenceObject $b
if ($diff) { Write-Error "DryRun and normal detection results differ: `n$($diff | Out-String)"; Pop-Location; exit 1 }
Write-Host 'DryRun and normal detection are consistent.' -ForegroundColor Green
Pop-Location
exit 0
param(
    [string]$BaseRef = 'main'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

$cli = Join-Path $repoRoot 'scripts\check-csproj-builds-cli.ps1'
if (-not (Test-Path $cli)) { Write-Error "CLI wrapper not present at $cli"; exit 2 }

remove-item -force -erroraction SilentlyContinue changed-csprojs*.txt

Write-Host "Running CLI DryRun parity test (baseRef: $BaseRef)"
pwsh -NoProfile -ExecutionPolicy Bypass -File $cli -ChangedOnly -DiffRef $BaseRef -DryRun
if (Test-Path changed-csprojs.txt) { Copy-Item changed-csprojs.txt changed-csprojs-dryrun.txt -Force }

Write-Host "Running CLI non-DryRun to generate changed-csprojs.txt"
pwsh -NoProfile -ExecutionPolicy Bypass -File $cli -ChangedOnly -DiffRef $BaseRef

if (-not (Test-Path changed-csprojs-dryrun.txt) -and -not (Test-Path changed-csprojs.txt)) { Write-Host 'Both outputs missing: no changes to build'; Pop-Location; exit 0 }

$dry = (Get-Content -Raw changed-csprojs-dryrun.txt -ErrorAction SilentlyContinue).Trim()
$norm = (Get-Content -Raw changed-csprojs.txt -ErrorAction SilentlyContinue).Trim()
if ($dry -eq '' -and $norm -eq '') { Write-Host 'No changed projects in both runs'; Pop-Location; exit 0 }
if (($dry -eq '' -and $norm -ne '') -or ($dry -ne '' -and $norm -eq '')) { Write-Error 'Parity mismatch: dry vs normal detection (one empty)'; exit 1 }
$a = $dry -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$b = $norm -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$diff = Compare-Object -ReferenceObject $a -DifferenceObject $b
if ($diff) { Write-Error "Parity mismatch between dry and regular outputs:`n$($diff | Out-String)"; exit 1 }
Write-Host 'Parity check OK.' -ForegroundColor Green
Pop-Location
exit 0
