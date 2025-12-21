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

## Git Workflow

We use a **Feature Branch** workflow. Direct commits to `main` are discouraged.

### Branching Strategy
- `main` - Stable, production-ready code. Protected by CI.
- `feature/*` - New features (e.g., `feature/hex-state-refactor`)
- `bugfix/*` - Bug fixes
- `hotfix/*` - Urgent production fixes

### Pull Request Process

1. **Create a feature branch** from `main`:
   ```powershell
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** following code style guidelines.

3. **Run Tests Locally** (Required):
   ```powershell
   # Run all unit tests (Pester)
   .\scripts\run-local-pester.ps1

   # Run build guard (All projects)
   .\scripts\check-csproj-builds.cmd
   ```

4. **Commit changes**:
   - Follow Conventional Commits (e.g., `feat(ui): add new menu`).

5. **Push and Open PR**:
   ```powershell
   git push origin feature/your-feature-name
   ```
   - CI will run `changed-only-checks` on your PR.
   - CI will run `static-checks` (full guard) on push to `feature/*`.

### PR Review Checklist
- [ ] Code follows style guidelines
- [ ] Tests added for new functionality
- [ ] All tests pass (CoreLogic + Unity + Pester)
- [ ] Documentation updated if needed
- [ ] No new compiler warnings
