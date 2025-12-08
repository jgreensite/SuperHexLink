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
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$script" -ChangedOnly || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File "$script" -ChangedOnly || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
exit 0
"@

Write-Host "Installing pre-commit hook to: $preCommitPath"
if (Test-Path $preCommitPath) {
    $existing = Get-Content -Path $preCommitPath -Raw
    if ($existing -match 'check-csproj-builds.ps1') {
        Write-Host "A pre-commit hook already contains a call to check-csproj-builds.ps1 - skipping modification" -ForegroundColor Yellow
    }
    else {
        # Append our guard to the existing hook so we don't overwrite custom hooks
        Write-Host "Appending csproj build guard to existing pre-commit hook (backing up original)..." -ForegroundColor Yellow
        Copy-Item -Path $preCommitPath -Destination "$preCommitPath.bak" -Force
        $appendBlock = @"
# Begin csproj build guard (added by scripts/install-git-hooks.ps1)
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$script" -ChangedOnly || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File "$script" -ChangedOnly || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
# End csproj build guard
"@
        Add-Content -Path $preCommitPath -Value $appendBlock -Encoding Ascii
    }
}
else {
    Set-Content -Path $preCommitPath -Value $hookContent -Force -Encoding Ascii
}
# Ensure it's executable on *nix
try {
    & git update-index --add --chmod=+x $preCommitPath 2> $null
} catch {
    # best-effort
}
Write-Host "Pre-commit hook installed. You can remove it by deleting $preCommitPath" -ForegroundColor Green
