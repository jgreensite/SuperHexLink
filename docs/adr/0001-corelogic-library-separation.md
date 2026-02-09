# ADR-0001: CoreLogic Library Separation

**Status**: Accepted
**Date**: 2025-11-01
**Deciders**: @jgreensite

## Context

Game logic (grid math, coordinate conversion, serialization) was embedded in Unity MonoBehaviours. This made unit testing slow (requires Unity Editor) and prevented reuse outside Unity.

## Decision

Extract pure game logic into a standalone .NET Standard 2.0 library (`CoreLogic/`) with no Unity dependencies. Use an adapter pattern (`CoreLogicAdapter.cs`) for Unity integration.

## Consequences

**Positive**:
- Unit tests run in < 100 ms (no Unity boot)
- Logic reusable for server, tools, or other clients
- Clear separation of concerns
- Deterministic behavior easy to validate

**Negative**:
- Extra adapter layer for type conversions
- Two build systems (dotnet + Unity)
- Must keep CoreLogic models in sync with Unity models

**Mitigations**:
- Adapter is thin (static extension methods)
- CI validates both builds on every push
- `deploy-corelogic-to-unity.cmd` automates DLL deployment
