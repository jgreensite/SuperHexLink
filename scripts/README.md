DryRun / WhatIf
----------------
You can run the guard script in "dry-run" mode to list which projects would be built without running any builds. This is useful for quickly validating changed-only detection locally or in CI:

    pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\check-csproj-builds.ps1 -ChangedOnly -DryRun

If you install the git hook using `scripts/install-git-hooks.ps1 -DryRun`, the installer will only preview the hook content and not write to `.git/hooks`.

Harnesses
---------
Two harness scripts are provided:

- `scripts/test-check-changedonly.ps1` (minimal) — quick DryRun that stages a single file change and prints the detected changed projects.
- `scripts/test-check-changedonly-branch.ps1` (robust) — creates a temp branch (or stash+branch), commits a small change, runs the guard in DryRun by default and cleans up. This harness is suitable for more realistic end-to-end testing.

If you find `install-git-hooks.ps1` behaves unexpectedly, rerun the installer with `-DryRun` to preview modifications before writing changes.

For debugging harness behavior (forwarding `-DryRun` to the guard), set the environment variable `CHECK_CS_PROJ_DEBUG=1` to get more output from the scripts.

## DryRun & WhatIf

The `check-csproj-builds.ps1` wrapper now supports a `-DryRun` switch which prints the detected changed projects and skips running `dotnet build`.

Use the CLI `check-csproj-builds-cli.ps1` if available for `-WhatIf` behavior via PowerShell `ShouldProcess` support; otherwise the wrapper will forward arguments.
CLI usage example:
```powershell
# DryRun: list changed projects that would be built
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\check-csproj-builds-cli.ps1 -ChangedOnly -DiffRef main -DryRun

# Non-dry run: will perform the build(s)
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts\check-csproj-builds-cli.ps1 -ChangedOnly -DiffRef main
```

You can install a pre-commit hook in DryRun mode for testing by running:
```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/install-git-hooks.ps1 -DryRun
```

# Build and Development Scripts

This directory contains automation scripts for building, testing, and managing the SuperHexLink project.

## Directory Structure

```
scripts/
├── git-hooks/              # Git hook scripts
│   └── pre-push.ps1        # Pre-push validation
├── check-csproj-builds.*   # Validate all .csproj files build
├── run-unity-editmode-tests.* # Run Unity tests in batch mode
├── install-prepush-hook.ps1 # Install pre-push hook
└── README.md               # This file
```

## Build Scripts

### deploy-corelogic-to-unity.cmd / .ps1
Builds CoreLogic library and deploys it to Unity's Assets/Plugins folder.

**Usage**:
```powershell
.\scripts\deploy-corelogic-to-unity.cmd
```

**Purpose**:
- Builds CoreLogic.dll in Release configuration
- Copies DLL and dependencies (Newtonsoft.Json) to Assets/Plugins/CoreLogic/
- Enables Unity scripts to reference CoreLogic library

**When to run**:
- After modifying CoreLogic source code
- Before testing Unity integration with CoreLogic
- When setting up a fresh clone

### check-csproj-builds.cmd / .ps1
Validates that all .csproj files in the repository build successfully.

**Usage**:
```powershell
.\scripts\check-csproj-builds.cmd
```

**Purpose**: 
- Fast-fail check before pushing
- Used in CI/CD pipeline
- Catches compilation errors early

**Excludes**:
- Third-party Sirenix projects (known issues with Unity package structure)
- GitHub Actions runner artifacts

### run-unity-editmode-tests.cmd / .ps1
Runs Unity EditMode tests in batch mode (headless).

**Usage**:
```powershell
.\scripts\run-unity-editmode-tests.cmd
```

**Requirements**:
- Unity Editor installed
- Unity license activated
- Environment variable `UNITY_EXECUTABLE` (optional, will auto-detect)

**Output**:
- Test results in `TestResults/editmode-results.xml`
- Unity log in `TestResults/unity-batch-log.txt`

## Git Hooks

### install-prepush-hook.ps1
Installs the pre-push Git hook that runs validation before pushing.

**Usage**:
```powershell
.\scripts\install-prepush-hook.ps1
```

**What it does**:
1. Copies `git-hooks/pre-push.ps1` to `.git/hooks/pre-push`
2. Configures Git to run PowerShell scripts
3. Validates installation

**Pre-push checks**:
- CoreLogic builds successfully
- CoreLogic tests pass
- All .csproj files build

### git-hooks/pre-push.ps1
The actual pre-push validation script (do not run directly).

**Checks performed**:
1. CoreLogic library builds
2. CoreLogic unit tests pass (10 tests)
3. Repository-wide .csproj build validation

**Bypass** (use sparingly):
```powershell
git push --no-verify
```

## Development Utilities

### check-unity-errors.cmd / .ps1
Displays Unity Editor compilation errors and warnings from the log file.

**Usage**:
```powershell
.\scripts\check-unity-errors.cmd
```

**Options**:
```powershell
# Show only errors (skip warnings)
.\scripts\check-unity-errors.ps1 -ErrorsOnly

# Follow log in real-time (like tail -f)
.\scripts\check-unity-errors.ps1 -Follow

# Show last 200 lines
.\scripts\check-unity-errors.ps1 -Lines 200
```

**Purpose**:
- Debug Unity compilation without opening Editor
- Quick validation after code changes
- CI/CD integration for Unity compilation checks

**Output**: Parses Editor.log for CS#### errors and warnings, displays summary

### capture-unity-logs.ps1
Tails Unity Editor.log file for debugging.

**Usage**:
```powershell
.\scripts\capture-unity-logs.ps1
```

**Output**: Streams Unity log to console and `scripts/unity-editor-log-tail.txt`

## CI/CD Scripts

These scripts are called by GitHub Actions workflows:

- `.github/workflows/ci.yml` - Main CI workflow (Linux runners)
- `.github/workflows/unity-selfhost-tests.yml` - Unity tests (Windows self-hosted)

**CI Flow**:
```
Push to GitHub
    ↓
1. Fast Checks (< 1 min)
   ├─ CoreLogic build
   ├─ CoreLogic tests
   └─ csproj build guard
    ↓
2. Unity Checks (self-hosted, ~5 min)
   ├─ Unity compilation
   └─ Unity EditMode tests
```

## Script Conventions

### PowerShell vs CMD
- `.ps1` - PowerShell scripts (core logic)
- `.cmd` - Windows Command Prompt wrappers (calls PowerShell)

**Why both?**: Ensures compatibility with systems that don't have `pwsh` in PATH.

### Parameters and Options
All scripts support `-WhatIf` for dry-run mode:
```powershell
.\scripts\check-csproj-builds.ps1 -WhatIf
```

### Error Handling
- Scripts use `Set-StrictMode -Version Latest`
- Exit codes: 0 (success), non-zero (failure)
- Errors written to stderr with color coding

### Logging
- Info messages: `Write-Host` with color
- Warnings: `Write-Host -ForegroundColor Yellow`
- Errors: `Write-Error` or `Write-Host -ForegroundColor Red`

## Adding New Scripts

When adding a new script:

1. **Create both .ps1 and .cmd versions**:
   ```powershell
   # script-name.ps1 - Main logic
   # script-name.cmd - Wrapper
   ```

2. **Add documentation**:
   ```powershell
   <#
   .SYNOPSIS
       Brief description
   .DESCRIPTION
       Detailed description
   .EXAMPLE
       .\script-name.ps1
   #>
   ```

3. **Follow conventions**:
   - Use `Set-StrictMode -Version Latest`
   - Use `$ErrorActionPreference = 'Stop'`
   - Return proper exit codes
   - Validate parameters

4. **Update this README** with usage instructions

5. **Add to CI if appropriate**

## Troubleshooting

### "Execution of scripts is disabled on this system"
Run PowerShell as Administrator:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Unity executable not found"
Set environment variable:
```powershell
$env:UNITY_EXECUTABLE = "C:\Program Files\Unity\Hub\Editor\2021.3.18f1\Editor\Unity.exe"
```

Or update the script with your Unity path.

### Pre-push hook not running
Reinstall the hook:
```powershell
.\scripts\install-prepush-hook.ps1 -Force
```

### Script hangs or times out
- Check Unity is not already running
- Verify Unity license is activated
- Look for modal dialogs blocking Unity

## Best Practices

### Local Development
1. Install pre-push hook immediately after cloning
2. Run `check-csproj-builds.cmd` before committing
3. Run CoreLogic tests frequently: `dotnet test CoreLogic/tests/...`

### CI/CD
1. Keep fast checks fast (< 1 minute)
2. Run expensive Unity checks on self-hosted runners
3. Upload artifacts for failed builds
4. Use caching for NuGet packages

### Script Maintenance
1. Test scripts on clean checkout
2. Verify both .ps1 and .cmd versions work
3. Update documentation when changing behavior
4. Version control all scripts (no local-only scripts)

## CI/CD Integration

These scripts are used by GitHub Actions workflows:
- `.github/workflows/ci.yml` - Fast checks (CoreLogic build/test, csproj validation)
- `.github/workflows/unity-selfhost-tests.yml` - Unity EditMode tests

See the workflow files in `.github/workflows/` for complete CI configuration.
