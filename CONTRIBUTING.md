# Contributing

For **code style, architecture guidelines, testing patterns, and commit conventions**, see the comprehensive [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md).

This page covers the quick-start workflow and build tooling.

---

## Install git hooks

Run this in PowerShell from the repository root:

```powershell
.\scripts\install-git-hooks.ps1
```

This installs a pre-commit hook that runs `scripts/check-csproj-builds.ps1 -ChangedOnly` to perform a `dotnet build` only for the projects affected by files staged for the commit. CI still runs a full build guard.

## Run Tests Locally (Required before PR)

```powershell
# 1. CoreLogic unit tests (fast — <1 sec)
dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj

# 2. Pester script tests
.\scripts\run-local-pester.ps1

# 3. Build guard (all projects)
.\scripts\check-csproj-builds.ps1
```

## Build Guard Variants

```powershell
# Changed-only (fast, for staged files)
.\scripts\check-csproj-builds.ps1 -ChangedOnly

# Against a Git diff reference (PR workflow)
.\scripts\check-csproj-builds.ps1 -ChangedOnly -DiffRef main

# Verbose diagnostics
.\scripts\check-csproj-builds.ps1 -ChangedOnly -Verbose

# Test harness for the changed-only guard
.\scripts\test-check-changedonly.ps1
```

## Git Workflow

We use a **Feature Branch** workflow. Direct commits to `main` are discouraged.

- `main` — Stable, production-ready. Protected by CI.
- `feature/*` — New features (e.g., `feature/hex-state-refactor`)
- `bugfix/*` — Bug fixes
- `hotfix/*` — Urgent production fixes

### Pull Request Process

1. Create a feature branch: `git checkout -b feature/your-feature-name`
2. Make changes following [code style guidelines](docs/CONTRIBUTING.md#code-style)
3. Run all tests (see above)
4. Commit using [Conventional Commits](https://www.conventionalcommits.org/) (e.g., `feat(ui): add new menu`)
5. Push and open PR — CI runs `changed-only-checks` on PRs and `static-checks` on push to `feature/*`

### PR Review Checklist
- [ ] Code follows [style guidelines](docs/CONTRIBUTING.md#code-style)
- [ ] Tests added for new functionality
- [ ] All tests pass (CoreLogic + Unity + Pester)
- [ ] Documentation updated if needed
- [ ] No new compiler warnings
- [ ] Commit messages follow [conventions](docs/CONTRIBUTING.md#commit-messages)
