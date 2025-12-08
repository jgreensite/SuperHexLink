Param()

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
$gitHooksDir = Join-Path $repoRoot '.git\hooks'
$preCommitPath = Join-Path $gitHooksDir 'pre-commit'

if (-not (Test-Path $gitHooksDir)) {
    Write-Host "Project is not a git repo or .git/hooks doesn't exist: $gitHooksDir" -ForegroundColor Yellow
    exit 1
}

$script = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'

$hookContent = @"
#!/bin/sh
# Pre-commit hook installed by scripts/install-git-hooks.ps1
# This guard runs the standard dotnet build guard script and prevents commit if any csproj fails to build.
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$script" || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File "$script" || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
exit 0
"@

Write-Host "Installing pre-commit hook to: $preCommitPath"
Set-Content -Path $preCommitPath -Value $hookContent -Force -Encoding Ascii
# Ensure it's executable on *nix
try {
    & git update-index --add --chmod=+x $preCommitPath 2> $null
} catch {
    # best-effort
}
Write-Host "Pre-commit hook installed. You can remove it by deleting $preCommitPath" -ForegroundColor Green
