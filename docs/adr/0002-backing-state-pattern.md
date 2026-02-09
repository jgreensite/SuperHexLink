# ADR-0002: Backing State Pattern for Hex

**Status**: Accepted
**Date**: 2025-11-01
**Deciders**: @jgreensite

## Context

`Hex.HexState` contained both runtime data (materials, GameObjects) and persisted data (type, position, rotation). This coupling caused bugs in serialization and made undo/redo unreliable.

## Decision

Introduce `BaseHexState` as an immutable backing store. `Hex.HexState` delegates its persisted properties to `BaseHexState`. Only `BaseHexState` is serialized.

## Consequences

**Positive**:
- Clear separation of runtime vs. persisted data
- Deterministic serialization (no Unity objects in the stream)
- Enables undo/redo via snapshot comparison
- Supports networking (only send `BaseHexState`)

**Negative**:
- Extra indirection for property access
- Must remember to update backing state when modifying hex

**Mitigations**:
- Properties on `HexState` are thin wrappers — no performance cost
- `Initialize()` enforces the binding at construction time
