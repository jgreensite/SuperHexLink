# Contributing

Please run the git hook installer to prevent commits that introduce compile errors.

## Install git hooks

Run this in PowerShell from the repository root:

```powershell
.\scripts\install-git-hooks.ps1
```

This installs a pre-commit hook that runs `scripts/check-csproj-builds.ps1 -ChangedOnly` to perform a `dotnet build` only for the projects affected by files staged for the commit. This keeps the pre-commit fast while still catching compile errors before a commit. CI still runs a full build guard.

## Local build guard (manual)

If you prefer to run the build guard manually, execute:

```powershell
.\scripts\check-csproj-builds.ps1
```

This is the same script used by CI and will catch compile errors across projects.

## Changed-only build guard and test harness

The repository includes a changed-only build guard to speed up local checks and CI for pull requests. By default it runs quietly; add `-Verbose` if you want to see debug output.

- Run the changed-only guard against staged changes (local):

```powershell
.\scripts\check-csproj-builds.ps1 -ChangedOnly
```

- If you want to run it against a Git diff reference (useful in PR checks):

```powershell
.\scripts\check-csproj-builds.ps1 -ChangedOnly -DiffRef main
```

- Enable verbose output to see path mappings and diagnostics:

```powershell
.\scripts\check-csproj-builds.ps1 -ChangedOnly -Verbose
```

- Test harness for the changed-only guard:

```powershell
.\scripts\test-check-changedonly.ps1
# Or target a specific file to verify the mapping & build:
.\scripts\test-check-changedonly.ps1 -TargetFile Assets/Editor/MapValidationWindow.cs
```

- Install the pre-commit hook to use changed-only checks automatically on commit:

```powershell
.\scripts\install-git-hooks.ps1
```

If you want to install the pre-commit hook in DryRun mode (so it only prints detected projects and doesn't run builds), do:

```powershell
.\scripts\install-git-hooks.ps1 -DryRun
```

If you need the hook to run in verbose mode for debugging, edit the pre-commit hook or call the script manually with `-Verbose`.
