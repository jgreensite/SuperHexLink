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
