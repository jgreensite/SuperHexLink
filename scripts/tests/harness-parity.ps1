Param(
    [string]$BaseRef = 'main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $d = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
    while ($d -and -not (Test-Path (Join-Path $d '.git')) -and -not (Test-Path (Join-Path $d 'README.md'))) { $d = Split-Path -Path $d -Parent }
    return $d
}
$repoRoot = Get-RepoRoot
if (-not $repoRoot) { $repoRoot = (Get-Location).Path }
Push-Location $repoRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Write-Error 'git required for test harness parity'; exit 2 }
if (-not (Test-Path 'scripts\check-csproj-builds-cli.ps1')) { Write-Error 'CLI guard script not found; failing test'; exit 2 }

Write-Host "Running harness parity test against base: $BaseRef"

# Run DryRun
$dryArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\check-csproj-builds-cli.ps1','-ChangedOnly','-DiffRef',$BaseRef,'-DryRun')
if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
$rc = Start-Process -FilePath $exe -ArgumentList $dryArgs -NoNewWindow -Wait -PassThru
if ($rc.ExitCode -ne 0) { Write-Error "DryRun invocation failed with exit $($rc.ExitCode)"; Pop-Location; exit 1 }
if (Test-Path 'changed-csprojs.txt') {
    $src = Join-Path $repoRoot 'changed-csprojs.txt'
    $dest = Join-Path $repoRoot 'changed-csprojs-dryrun.txt'
    if ($src -ne $dest) { Copy-Item -Path $src -Destination $dest -Force }
}

# Run normal detection
$args = @('-NoProfile','-ExecutionPolicy','Bypass','-File','scripts\check-csproj-builds-cli.ps1','-ChangedOnly','-DiffRef',$BaseRef)
$rc = Start-Process -FilePath $exe -ArgumentList $args -NoNewWindow -Wait -PassThru
if ($rc.ExitCode -ne 0) { Write-Error "Normal invocation failed with exit $($rc.ExitCode)"; Pop-Location; exit 1 }

# Copy artifact
if (Test-Path 'changed-csprojs.txt') {
    $src = Join-Path $repoRoot 'changed-csprojs.txt'
    $dest = Join-Path $repoRoot 'changed-csprojs.txt'
    if ($src -ne $dest) { Copy-Item -Path $src -Destination $dest -Force }
}

Write-Host 'Comparing detection parity...'
$dry = '' ; $n = ''
if (Test-Path 'changed-csprojs-dryrun.txt') { $raw = (Get-Content 'changed-csprojs-dryrun.txt' -Raw); $dry = if ($raw) { $raw.Trim() } else { '' } }
if (Test-Path 'changed-csprojs.txt') { $raw2 = (Get-Content 'changed-csprojs.txt' -Raw); $n = if ($raw2) { $raw2.Trim() } else { '' } }
if ($dry -eq '' -and $n -eq '') { Write-Host 'Both empty - nothing to build'; Pop-Location; exit 0 }
if (($dry -eq '' -and $n -ne '') -or ($dry -ne '' -and $n -eq '')) { Write-Error 'DryRun and normal detection differ: one is empty and the other is not'; Pop-Location; exit 1 }
$a = $dry -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$b = $n -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Sort-Object
$diff = Compare-Object -ReferenceObject $a -DifferenceObject $b
if ($diff) { Write-Error "DryRun and normal detection results differ: `n$($diff | Out-String)"; Pop-Location; exit 1 }
Write-Host 'DryRun and normal detection are consistent.' -ForegroundColor Green
Pop-Location
exit 0
 
