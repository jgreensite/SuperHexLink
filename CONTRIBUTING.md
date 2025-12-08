# Contributing

Please run the git hook installer to prevent commits that introduce compile errors.

## Install git hooks

Run this in PowerShell from the repository root:

```powershell
.\scripts\install-git-hooks.ps1
```

This installs a pre-commit hook that runs `scripts/check-csproj-builds.ps1` to perform a `dotnet build` on every .csproj. It will block the commit if any project fails to build.

## Local build guard (manual)

If you prefer to run the build guard manually, execute:

```powershell
.\scripts\check-csproj-builds.ps1
```

This is the same script used by CI and will catch compile errors across projects.
