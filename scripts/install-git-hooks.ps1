Param(
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
$gitHooksDir = Join-Path $repoRoot '.git\hooks'
$preCommitPath = Join-Path $gitHooksDir 'pre-commit'

if (-not (Test-Path $gitHooksDir)) {
    Write-Host "Project is not a git repo or .git/hooks doesn't exist: $gitHooksDir" -ForegroundColor Yellow
    exit 1
}

$script = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'

if ($DryRun) { $installFlags = ' -ChangedOnly -DryRun' } else { $installFlags = ' -ChangedOnly' }

$hookContent = @"
#!/bin/sh
# Pre-commit hook installed by scripts/install-git-hooks.ps1
# This guard runs the standard dotnet build guard script and prevents commit if any csproj fails to build.
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
exit 0
"@

Write-Host "Preparing to install pre-commit hook to: $preCommitPath"
if (Test-Path $preCommitPath) {
    $existing = Get-Content -Path $preCommitPath -Raw
    if ($existing -match 'check-csproj-builds.ps1') {
        Write-Host "An existing pre-commit hook already contains a call to check-csproj-builds.ps1" -ForegroundColor Yellow
        # DryRun: explain modifications that would be made
        if ($DryRun) {
            Write-Host "(DryRun) Found existing call(s) to check-csproj-builds.ps1. Showing lines and planned edits:" -ForegroundColor Cyan
            $existing -split "`n" | Where-Object { $_ -match 'check-csproj-builds.ps1' } | ForEach-Object {
                if ($_ -match '-DryRun') { Write-Host " - (Already includes -DryRun) $_" -ForegroundColor Green } else { Write-Host " - (Would inject -DryRun) $_" -ForegroundColor Yellow }
            }
            exit 0
        }
        # If not a DryRun install, inject -DryRun into the existing lines if the user has asked for a DryRun installer
        if ($DryRun -or $installFlags -match '-DryRun') {
            Write-Host "Injecting -DryRun flag into existing pre-commit hook call(s) for check-csproj-builds.ps1" -ForegroundColor Yellow
            $lines = $existing -split "`n"
            $updated = $lines | ForEach-Object {
                $l = $_
                if ($l -match 'check-csproj-builds.ps1' -and -not ($l -match '-DryRun')) {
                    # Insert -DryRun before '|| exit' if present, otherwise at the end of the call
                    if ($l -match '\|\|\s*exit') { $l = $l -replace '(check-csproj-builds\.ps1\b[^\|\n\r]*)(\|\|\s*exit)', '$1 -DryRun $2' }
                    else { $l = $l -replace '(check-csproj-builds\.ps1\b[^\n\r]*)', '$1 -DryRun' }
                }
                $l
            }
            $updated -join "`n" | Set-Content -Path $preCommitPath -Encoding Ascii
            Write-Host "Updated existing pre-commit hook with -DryRun injection." -ForegroundColor Green
            exit 0
        }
        Write-Host "No changes made to existing hook." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "Appending csproj build guard to existing pre-commit hook (backing up original) ..." -ForegroundColor Yellow
        Copy-Item -Path $preCommitPath -Destination "$preCommitPath.bak" -Force
        $appendBlock = @"
# Begin csproj build guard (added by scripts/install-git-hooks.ps1)
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
# End csproj build guard
"@
        Add-Content -Path $preCommitPath -Value $appendBlock -Encoding Ascii
    }
}
else {
    if ($DryRun) {
        Write-Host "(DryRun) Would write the following pre-commit hook file to: $preCommitPath" -ForegroundColor Cyan
        Write-Host $hookContent
        exit 0
    }
    else {
        Set-Content -Path $preCommitPath -Value $hookContent -Force -Encoding Ascii
    }
}
# Ensure it's executable on *nix
try {
    & git update-index --add --chmod=+x $preCommitPath 2> $null
} catch {
    # best-effort
}
Write-Host "Pre-commit hook installed. You can remove it by deleting $preCommitPath" -ForegroundColor Green
Param(
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
$gitHooksDir = Join-Path $repoRoot '.git\hooks'
$preCommitPath = Join-Path $gitHooksDir 'pre-commit'

if (-not (Test-Path $gitHooksDir)) {
    Write-Host "Project is not a git repo or .git/hooks doesn't exist: $gitHooksDir" -ForegroundColor Yellow
    exit 1
}

$script = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'

if ($DryRun) { $installFlags = ' -ChangedOnly -DryRun' } else { $installFlags = ' -ChangedOnly' }

$hookContent = @"
#!/bin/sh
# Pre-commit hook installed by scripts/install-git-hooks.ps1
# This guard runs the standard dotnet build guard script and prevents commit if any csproj fails to build.
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
exit 0
"@

Write-Host "Preparing to install pre-commit hook to: $preCommitPath"
if (Test-Path $preCommitPath) {
    $existing = Get-Content -Path $preCommitPath -Raw
    if ($existing -match 'check-csproj-builds.ps1') {
        Write-Host "An existing pre-commit hook already contains a call to check-csproj-builds.ps1" -ForegroundColor Yellow
        # DryRun: explain modifications that would be made
        if ($DryRun) {
            Write-Host "(DryRun) Found existing call(s) to check-csproj-builds.ps1. Showing lines and planned edits:" -ForegroundColor Cyan
            $existing -split "`n" | Where-Object { $_ -match 'check-csproj-builds.ps1' } | ForEach-Object {
                if ($_ -match '-DryRun') { Write-Host " - (Already includes -DryRun) $_" -ForegroundColor Green } else { Write-Host " - (Would inject -DryRun) $_" -ForegroundColor Yellow }
            }
            exit 0
        }
        # If not a DryRun install, inject -DryRun into the existing lines if the user has asked for a DryRun installer
        if ($DryRun -or $installFlags -match '-DryRun') {
            Write-Host "Injecting -DryRun flag into existing pre-commit hook call(s) for check-csproj-builds.ps1" -ForegroundColor Yellow
            $lines = $existing -split "`n"
            $updated = $lines | ForEach-Object {
                $l = $_
                if ($l -match 'check-csproj-builds.ps1' -and -not ($l -match '-DryRun')) {
                    # Insert -DryRun before '|| exit' if present, otherwise at the end of the call
                    if ($l -match '\|\|\s*exit') { $l = $l -replace '(check-csproj-builds\.ps1\b[^\|\n\r]*)(\|\|\s*exit)', '$1 -DryRun $2' }
                    else { $l = $l -replace '(check-csproj-builds\.ps1\b[^\n\r]*)', '$1 -DryRun' }
                }
                $l
            }
            $updated -join "`n" | Set-Content -Path $preCommitPath -Encoding Ascii
            Write-Host "Updated existing pre-commit hook with -DryRun injection." -ForegroundColor Green
            exit 0
        }
        Write-Host "No changes made to existing hook." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "Appending csproj build guard to existing pre-commit hook (backing up original) ..." -ForegroundColor Yellow
        Copy-Item -Path $preCommitPath -Destination "$preCommitPath.bak" -Force
        $appendBlock = @"
# Begin csproj build guard (added by scripts/install-git-hooks.ps1)
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
# End csproj build guard
"@
        Add-Content -Path $preCommitPath -Value $appendBlock -Encoding Ascii
    }
}
else {
    if ($DryRun) {
        Write-Host "(DryRun) Would write the following pre-commit hook file to: $preCommitPath" -ForegroundColor Cyan
        Write-Host $hookContent
        exit 0
    }
    else {
        Set-Content -Path $preCommitPath -Value $hookContent -Force -Encoding Ascii
    }
}
# Ensure it's executable on *nix
try {
    & git update-index --add --chmod=+x $preCommitPath 2> $null
} catch {
    # best-effort
}
Write-Host "Pre-commit hook installed. You can remove it by deleting $preCommitPath" -ForegroundColor Green
Param(
    [switch]$DryRun
)

$repoRoot = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent | Split-Path -Parent
$gitHooksDir = Join-Path $repoRoot '.git\hooks'
$preCommitPath = Join-Path $gitHooksDir 'pre-commit'

if (-not (Test-Path $gitHooksDir)) {
    Write-Host "Project is not a git repo or .git/hooks doesn't exist: $gitHooksDir" -ForegroundColor Yellow
    exit 1
}

$script = Join-Path $repoRoot 'scripts\check-csproj-builds.ps1'

if ($DryRun) { $installFlags = ' -ChangedOnly -DryRun' } else { $installFlags = ' -ChangedOnly' }

$hookContent = @"
#!/bin/sh
# Pre-commit hook installed by scripts/install-git-hooks.ps1
# This guard runs the standard dotnet build guard script and prevents commit if any csproj fails to build.
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
exit 0
"@

Write-Host "Installing pre-commit hook to: $preCommitPath"
if (Test-Path $preCommitPath) {
    $existing = Get-Content -Path $preCommitPath -Raw
    if ($existing -match 'check-csproj-builds.ps1') {
        Write-Host "A pre-commit hook already contains a call to check-csproj-builds.ps1" -ForegroundColor Yellow
        # DryRun preview: show what we'd change without touching the file
        if ($DryRun) {
            Write-Host "(DryRun) Would inspect and optionally inject -DryRun into existing call(s) where missing:" -ForegroundColor Cyan
            # Show matched lines and what would change
            $lines = $existing -split "`n"
            foreach ($line in $lines) {
                if ($line -match 'check-csproj-builds.ps1') {
                    if ($line -match '-DryRun') { Write-Host " - (Present) $line" -ForegroundColor Green } else { Write-Host " - (Missing) $line`n   => (Would modify to include -DryRun)" -ForegroundColor Yellow }
                }
            }
            exit 0
        }
        # If not DryRun and the existing hook call does not include -DryRun, inject one for idempotency
        if (-not ($existing -match '-DryRun')) {
            Write-Host "Injecting -DryRun flag into existing pre-commit hook call(s) for check-csproj-builds.ps1" -ForegroundColor Yellow
            $lines = $existing -split "`n"
            $updated = @()
            foreach ($line in $lines) {
                if ($line -match 'check-csproj-builds.ps1' -and -not ($line -match '-DryRun')) {
                    # Attempt to insert -DryRun before any trailing '|| exit' or end of line
                    if ($line -match '\|\|\s*exit') {
                        $line = $line -replace '(check-csproj-builds\.ps1\b[^\|\n\r]*)(\|\|\s*exit)', '$1 -DryRun $2'
                    } else {
                        $line = $line -replace '(check-csproj-builds\.ps1\b[^\n\r]*)', '$1 -DryRun'
                    }
                }
                $updated += $line
            }
            $updated -join "`n" | Set-Content -Path $preCommitPath -Encoding Ascii
            Write-Host "Updated existing pre-commit hook with -DryRun injection." -ForegroundColor Green
            exit 0
        } else {
            Write-Host "Existing pre-commit hook already contains -DryRun; no changes made." -ForegroundColor Green
            exit 0
        }
    }
    else {
        # Append our guard to the existing hook so we don't overwrite custom hooks
        Write-Host "Appending csproj build guard to existing pre-commit hook (backing up original)..." -ForegroundColor Yellow
        Copy-Item -Path $preCommitPath -Destination "$preCommitPath.bak" -Force
        $appendBlock = @"
# Begin csproj build guard (added by scripts/install-git-hooks.ps1)
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File \"$script\"$installFlags || exit 1
else
    echo "PowerShell not found. Skipping csproj build guard."
fi
# End csproj build guard
"@
        Add-Content -Path $preCommitPath -Value $appendBlock -Encoding Ascii
    }
}
else {
    if ($DryRun) {
        Write-Host "(DryRun) Would write the following pre-commit hook file to: $preCommitPath" -ForegroundColor Cyan
        Write-Host $hookContent
        exit 0
    }
    else {
        Set-Content -Path $preCommitPath -Value $hookContent -Force -Encoding Ascii
    }
}
# Ensure it's executable on *nix
try {
    & git update-index --add --chmod=+x $preCommitPath 2> $null
} catch {
    # best-effort
}
Write-Host "Pre-commit hook installed. You can remove it by deleting $preCommitPath" -ForegroundColor Green

# Begin csproj build guard (added by scripts/install-git-hooks.ps1)
if [ -x "/usr/bin/pwsh" ]; then
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$script"$installFlags || exit 1
elif [ -x "/usr/bin/powershell" ]; then
    powershell -NoProfile -ExecutionPolicy Bypass -File "$script"$installFlags || exit 1
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
