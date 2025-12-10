# Consolidated Backlog (Short-Term & Long-Term Items)

This backlog consolidates next steps and considerations gathered during recent work on changed-only builds, DryRun support, and harness/CI diagnostics.

Short-term (next sprint)
- Add CI assertion to validate DryRun parity for synthetic PRs (already implemented, validate stability).
- Add an end-to-end harness CI job that runs `scripts/test-check-changedonly-branch.ps1 -DryRun` and uploads artifacts (implemented).
- Add automated tests to the guard script for special cases (e.g., |DiffRef| not found, empty stage, multiple asset patterns).
- Ensure `changed-csprojs.txt` is untracked and generated only in CI/working check (done).
- Create a CLI wrapper script `check-csproj-builds-cli.ps1` with robust input validation and exit codes, and reduce multiple entrypoint duplication.

Medium-term (address and refine)
- Add a CI step or test that asserts `install-git-hooks.ps1 -DryRun` preserves custom hooks and only appends our guard when safe.
- Add unit / integration tests for the minimal harness to ensure `-DryRun` forwarding and `ShouldProcess` behavior is correct across Windows PowerShell and pwsh.
- Add example hook content and a helper to ensure idempotency of the hook (e.g., `scripts/print-hook-content.ps1`).

Long-term (future features)
- Consider converting the guard to a cross-platform CLI (C# dotnet global tool or native executable) to avoid PowerShell differences across platforms.
- Add targeted CI matrices to run harnesses across scalars: `pwsh` vs `powershell.exe`, and across Windows/Linux runners.
- Add telemetry/diagnostics to CI artifact uploads to make it easier to triage false positives (e.g. upload the exact Git diffs used for changed-only detection).
- Consider adding `-WhatIf`/`ShouldProcess` friendly semantics to any real changes performed by these scripts for safe execution in automation.

Notes: prioritize small, actionable tasks in the short-term column to reduce false positives and make tests fast in CI.
