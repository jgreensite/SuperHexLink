<#
Robust harness: Use a temporary branch or stash+temp branch to commit a harmless change,
run the changed-only guard (in DryRun), then cleanup and restore the original workspace state.

Usage:
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-check-changedonly-branch.ps1 [-TargetFile <path>] [-BaseRef <branch>] [-DryRun]

Options:
  -TargetFile <path>   : file to edit (default CoreLogic\src\CoreLogic\README.md)
  -BaseRef <branch>    : base ref to diff against for PR simulation (default main)
  -DryRun              : run the guard in DryRun mode (default true)
#>

Param(
    [string]$TargetFile = 'CoreLogic\src\CoreLogic\README.md',
    [string]$BaseRef = 'main',
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { Write-Host 'git required for harness' -ForegroundColor Red; Pop-Location; exit 2 }
if (-not $PSBoundParameters.ContainsKey('DryRun')) { $DryRun = $true }

$guid = [Guid]::NewGuid().ToString('N')
$tempBranch = "harness/temp-$guid"
$origBranch = (& git rev-parse --abbrev-ref HEAD).Trim()
$scriptPath = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'

function Get-GuardInvoker {
    if (Test-Path (Join-Path $repoRoot 'scripts\check-csproj-builds-cli.ps1')) { return @{Path = Join-Path $repoRoot 'scripts\check-csproj-builds-cli.ps1'; IsCli = $true} }
    if (Test-Path (Join-Path $repoRoot 'scripts\check-csproj-builds.ps1')) { return @{Path = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'; IsCli = $false} }
    return $null
}

function ScriptSupportsDryRun {
    param([string]$p)
    try { return (Select-String -Path $p -Pattern 'DryRun' -SimpleMatch -Quiet) } catch { return $false }
}

function Run-Guard {
    param([string]$GuardPath, [bool]$IsCli, [switch]$Dry)
    $argList = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$GuardPath,'-ChangedOnly','-DiffRef',$BaseRef)
    $supportsDry = ScriptSupportsDryRun -p $GuardPath
    if ($env:CHECK_CS_PROJ_DEBUG) {
        Write-Host "[HARNESS-DEBUG] Guard path: $GuardPath" -ForegroundColor Cyan
        Write-Host "[HARNESS-DEBUG] Guard path supports DryRun: $supportsDry" -ForegroundColor Cyan
        Write-Host "[HARNESS-DEBUG] Harness passed DryRun: $Dry" -ForegroundColor Cyan
    }
    if ($Dry -and $supportsDry) { $argList += '-DryRun' }
    if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
    if ($env:CHECK_CS_PROJ_DEBUG) { Write-Host "[HARNESS-DEBUG] Invoking guard: $exe with args: $($argList -join ' ')" -ForegroundColor Cyan }
    # Use direct invocation to surface stdout/stderr in harness logs (Start-Process hides them)
    $oldDebug = $env:CHECK_CS_PROJ_DEBUG
    try {
        $output = & $exe @argList 2>&1
        $rc = $LASTEXITCODE
        if ($env:CHECK_CS_PROJ_DEBUG) { Write-Host "[HARNESS-DEBUG] Guard output:`n$output" -ForegroundColor Cyan }
    } catch {
        Write-Host "[HARNESS-DEBUG] Guard invocation threw: $($_.Exception.Message)" -ForegroundColor Red
        $rc = 100
    }
    try { if ($oldDebug) { $env:CHECK_CS_PROJ_DEBUG = $oldDebug } else { Remove-Item Env:CHECK_CS_PROJ_DEBUG -ErrorAction SilentlyContinue } } catch { }
    return $rc
}

try {
    $guardInvoker = Get-GuardInvoker
    if (-not $guardInvoker) { Write-Host 'No guard script available' -ForegroundColor Yellow; exit 2 }

    # create test file if not exists
    if (-not (Test-Path $TargetFile)) { New-Item -Path $TargetFile -ItemType File -Force | Out-Null }
    $orig = Get-Content $TargetFile -Raw
    $comment = "# Temp harness commit: $guid"
    Add-Content -Path $TargetFile -Value ("`n$comment`n")
    git add $TargetFile

    $status = (& git status --porcelain) -join "`n"
    if (-not $status) {
        Write-Host 'Working tree clean: using temp branch' -ForegroundColor Green
        git checkout -b $tempBranch
        git commit -m "Harness temp commit $guid" --no-verify > $null 2>&1
        $rc = Run-Guard -GuardPath $guardInvoker.Path -IsCli:$guardInvoker.IsCli -Dry:$DryRun
        if ($rc -ne 0) { Write-Host "Guard returned non-zero ($rc)" -ForegroundColor Red }
        git checkout $origBranch
        git branch -D $tempBranch > $null 2>&1
    } else {
        Write-Host 'Working tree dirty: stash + temp branch' -ForegroundColor Yellow
        $stashRes = & git stash push -u -m "harness-stash-$guid" 2>$null
        if (-not $stashRes) { Write-Warning 'Could not stash; aborting'; exit 2 }
        git checkout -b $tempBranch
        # Apply the stash to temp branch so changes are present in the commit
        try { & git stash pop 2>$null } catch { try { & git stash apply 2>$null } catch { Write-Warning 'Failed to apply stash to temp branch' } }
        git commit -m "Harness temp commit $guid" --no-verify > $null 2>&1
        $rc = Run-Guard -GuardPath $guardInvoker.Path -IsCli:$guardInvoker.IsCli -Dry:$DryRun
        if ($rc -ne 0) { Write-Host "Guard returned non-zero ($rc)" -ForegroundColor Red }
        git checkout $origBranch
        git branch -D $tempBranch > $null 2>&1
        try { & git stash pop 2>$null } catch { try { & git stash apply 2>$null } catch { } }
    }
} finally {
    try { Set-Content -Path $TargetFile -Value $orig } catch { }
    Pop-Location
}
exit 0
