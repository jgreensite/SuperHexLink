<#
Installs the git pre-push hook that calls `scripts/git-hooks/pre-push.ps1`.
Usage: powershell -NoProfile -ExecutionPolicy Bypass -File scripts\install-prepush-hook.ps1
#>
Param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
$gitHooksDir = Join-Path $repoRoot '.git\hooks'
if (-not (Test-Path $gitHooksDir)) {
    Write-Error "Cannot find .git/hooks directory. Are you in the repository root and is it a git repo?"
    exit 2
}

$targetHook = Join-Path $gitHooksDir 'pre-push'
$scriptRel = 'scripts/git-hooks/pre-push.ps1'

$hookContent = @'
#!/bin/sh
SCRIPT_DIR="$(cd "$(dirname "$0")"/.. && pwd -P)"
if command -v pwsh >/dev/null 2>&1; then
  pwsh -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/%SCRIPT%" "$@"
else
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$SCRIPT_DIR/%SCRIPT%" "$@"
fi
exit $?
'@

$hookContent = $hookContent -replace '%SCRIPT%', $scriptRel.Replace('\','/')

Write-Host "Writing hook to: $targetHook"
[System.IO.File]::WriteAllText($targetHook, $hookContent)

try {
    & git update-index --add --chmod=+x ".git/hooks/pre-push" | Out-Null
} catch {
    Write-Host "Unable to set executable flag via git; attempting chmod (may work on non-Windows shells)." -ForegroundColor Yellow
    try { icacls $targetHook /grant "Users:(RX)" /T | Out-Null } catch { }
}

Write-Host "Pre-push hook installed. Please verify .git/hooks/pre-push exists and is executable." -ForegroundColor Green
exit 0
