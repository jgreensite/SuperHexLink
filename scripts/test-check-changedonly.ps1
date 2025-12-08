<#
A small test harness script to validate the changed-only build script.
This script will (safely) stage a small change and run the changed-only check
against it, then restore the index and working tree.

Usage:
  pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\test-check-changedonly.ps1
#>

Param(
    [string]$TargetFile = 'CoreLogic\src\CoreLogic\README.md'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
Push-Location $repoRoot

# Pick a file to edit within CoreLogic (a harmless small tweak). If you prefer a different project, update this path.
if ($TargetFile) { $testFile = $TargetFile } else { $testFile = 'CoreLogic\src\CoreLogic\README.md' }
if (-not (Test-Path $testFile)) {
    # Ensure parent exists
    New-Item -ItemType File -Path $testFile -Force | Out-Null
}

# Make a small change
$orig = Get-Content $testFile -Raw
$guid = [Guid]::NewGuid()
$ext = [System.IO.Path]::GetExtension($testFile).ToLowerInvariant()
switch ($ext) {
    '.cs' { $comment = "// Temp change for check-csproj-builds testing: $guid"; break }
    '.csproj' { $comment = "<!-- Temp change for check-csproj-builds testing: $guid -->"; break }
    '.md' { $comment = "# Temp change for check-csproj-builds testing: $guid"; break }
    default { $comment = "# Temp change for check-csproj-builds testing: $guid"; break }
}
Add-Content -Path $testFile -Value ("{0}{1}{0}" -f [Environment]::NewLine, $comment)

# Stage the file
git add $testFile
Write-Host "Staged files:"
git diff --cached --name-only | ForEach-Object { Write-Host " - $_" }

# Run the changed-only build check
$script = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'
$proc = Start-Process -FilePath 'powershell.exe' -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',$script,'-ChangedOnly' -NoNewWindow -Wait -PassThru
if ($proc.ExitCode -ne 0) {
    Write-Host "Changed-only build guard found errors (expected if code doesn't compile): exit $($proc.ExitCode)" -ForegroundColor Red
} else {
    Write-Host "Changed-only build guard passed (exit $($proc.ExitCode))" -ForegroundColor Green
}

# Clean up staged change (unstage + restore)
git reset HEAD $testFile > $null
Set-Content -Path $testFile -Value $orig

Pop-Location
exit 0
