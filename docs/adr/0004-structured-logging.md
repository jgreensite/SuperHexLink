# ADR-0004: Structured Logging via ActionLogger

**Status**: Accepted
**Date**: 2026-02-01
**Deciders**: @jgreensite

## Context

The codebase had ~100+ `Debug.Log` calls scattered through spawners and state management code. This made logs noisy, hard to filter, and impossible to disable per-category.

## Decision

Replace `Debug.Log` with `ActionLogger.Log(category, severity, message, args)`. Categories and severity levels are controlled via a `ActionLogSettings` ScriptableObject in the Inspector.

## Consequences

**Positive**:
- Logs are filterable by category (HexLifecycle, GameLifecycle, HexLand, General)
- Severity levels (Debug, Info, Warning, Error) route to appropriate Unity log methods
- Can disable noisy categories in production without code changes
- Consistent format across the codebase

**Negative**:
- Slightly more verbose call site (`Log(cat, sev, msg)` vs `Debug.Log(msg)`)
- Requires ScriptableObject to be assigned in scene

**Mitigations**:
- Helper method `Log()` on spawners reduces boilerplate
- Falls back to `Debug.Log` if settings asset is missing

## References
- Implementation: `Assets/Scripts/ActionLogger.cs`, `Assets/Scripts/ActionLogSettings.cs`
