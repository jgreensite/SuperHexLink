<#
Minimal harness to test changed-only behavior in a safe, non-invasive way.
This harness appends a small comment to a file, stages it, runs the CLI guard (preferred) in DryRun,
and restores the file and index afterwards. It avoids leaving the repo dirty.

Usage:
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-check-changedonly.ps1 [-TargetFile <path>] [-DryRun]

Options:
  -TargetFile <path>   : file to edit (default CoreLogic\src\CoreLogic\README.md)
  -DryRun              : run in DryRun mode (default true)
#>

Param(
    [string]$TargetFile = 'CoreLogic\src\CoreLogic\README.md',
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "git not found - test harness requires git" -ForegroundColor Red
    Pop-Location
    exit 2
}

# Default DryRun to true unless explicitly disabled
if (-not $PSBoundParameters.ContainsKey('DryRun')) { $DryRun = $true }

Write-Host "Test harness running (DryRun: $DryRun)" -ForegroundColor Cyan

# Ensure a test file exists
if ($TargetFile) { $testFile = $TargetFile } else { $testFile = 'CoreLogic\src\CoreLogic\README.md' }
if (-not (Test-Path $testFile)) { New-Item -ItemType File -Path $testFile -Force | Out-Null }

$orig = Get-Content $testFile -Raw
$guid = [Guid]::NewGuid().ToString('N')
$ext = [System.IO.Path]::GetExtension($testFile).ToLowerInvariant()
switch ($ext) {
    '.cs'     { $comment = "// Temp change for check-csproj-builds testing: $guid" }
    '.csproj' { $comment = "<!-- Temp change for check-csproj-builds testing: $guid -->" }
    default   { $comment = "# Temp change for check-csproj-builds testing: $guid" }
}

Add-Content -Path $testFile -Value ("`n$comment`n")

# Stage the file
git add $testFile
Write-Host "Staged files:" -NoNewline; git diff --cached --name-only | ForEach-Object { Write-Host "`n - $_" }

# Prefer the CLI script which supports DryRun/WhatIf
$scriptCli = Join-Path $repoRoot 'scripts\check-csproj-builds-cli.ps1'
$scriptWrapper = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'

function Get-ScriptInvoker {
    if (Test-Path $scriptCli) { return @{Path = $scriptCli; IsCli = $true} }
    if (Test-Path $scriptWrapper) { return @{Path = $scriptWrapper; IsCli = $false} }
    return $null
}

function Get-ChangedCsprojsLocal {
    # Map staged files to likely csproj(s) without invoking the guard script.
    $staged = & git diff --cached --name-only | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
    $projSet = [System.Collections.Generic.HashSet[string]]::new()
    foreach ($f in $staged) {
        $lf = $f -replace '/', '\\'
        if ($lf -match '\\Editor\\') { $projSet.Add('Assembly-CSharp-Editor.csproj') | Out-Null }
        elseif ($lf -match '^Assets\\') { $projSet.Add('Assembly-CSharp.csproj') | Out-Null }
        elseif ($lf -match '\.csproj$') { $projSet.Add(([System.IO.Path]::GetFileName($lf))) | Out-Null }
    }
    return $projSet | Sort-Object
}

function Run-Guard {
    param(
        [string]$ScriptPath,
        [bool]$IsCli,
        [switch]$Dry
    )
    function ScriptSupportsDryRun {
        param([string]$p)
        try { return (Select-String -Path $p -Pattern 'DryRun' -SimpleMatch -Quiet) } catch { return $false }
    }
    if ($IsCli) {
        $guardArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$ScriptPath,'-ChangedOnly')
        if ($Dry) { $guardArgs += '-DryRun' }
        # Prefer pwsh when available; fallback to powershell.exe
        if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
        $proc = Start-Process -FilePath $exe -ArgumentList $guardArgs -NoNewWindow -Wait -PassThru
        return $proc.ExitCode
    } else {
        $supportsDry = ScriptSupportsDryRun -p $ScriptPath
        if ($Dry -and $supportsDry) {
            Write-Host "Wrapper detected and supports DryRun; invoking wrapper with -DryRun" -ForegroundColor Yellow
            if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
            $args = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$ScriptPath,'-ChangedOnly')
            if ($Dry) { $args += '-DryRun' }
            $proc = Start-Process -FilePath $exe -ArgumentList $args -NoNewWindow -Wait -PassThru
            return $proc.ExitCode
        }
        elseif ($Dry) {
            Write-Host "Wrapper detected but DryRun enabled; computing changed csproj list locally instead of running wrapper." -ForegroundColor Yellow
            $detected = Get-ChangedCsprojsLocal
            Write-Host "DryRun detected changed projects:" -ForegroundColor Cyan
            foreach ($d in $detected) { Write-Host " - $d" }
            return 0
        } else {
            Write-Host "Running wrapper script (may perform builds)" -ForegroundColor Yellow
            if (Get-Command pwsh -ErrorAction SilentlyContinue) { $exe = 'pwsh' } else { $exe = 'powershell.exe' }
            $proc = Start-Process -FilePath $exe -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',$ScriptPath,'-ChangedOnly' -NoNewWindow -Wait -PassThru
            return $proc.ExitCode
        }
    }
}

$inv = Get-ScriptInvoker
if (-not $inv) { Write-Host "No changed-only guard script present; computing detected changed csproj list locally" -ForegroundColor Yellow; $detected = Get-ChangedCsprojsLocal ; Write-Host "DryRun detected changed projects:" -ForegroundColor Cyan; foreach ($d in $detected) { Write-Host " - $d" } }
else { $exit = Run-Guard -ScriptPath $inv.Path -IsCli:$inv.IsCli -Dry:$DryRun; if ($exit -ne 0) { Write-Host "Guard returned non-zero exit code: $exit" -ForegroundColor Red } }

# Clean up staged change (unstage + restore)
git reset HEAD $testFile > $null 2>&1
Set-Content -Path $testFile -Value $orig

Pop-Location
exit 0
