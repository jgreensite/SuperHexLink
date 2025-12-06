# CoreLogic Library

A standalone .NET Standard 2.0 library containing the core game logic for SuperHexLink. This library is designed to be testable independently of Unity, enabling fast iteration and reliable automated testing.

## Project Structure

```
CoreLogic/
├── src/
│   └── CoreLogic/
│       ├── Grid/              # Hex grid math and coordinate systems
│       ├── Models/            # Game state POCOs
│       └── Serialization/     # JSON serialization helpers
└── tests/
    └── CoreLogic.Tests/       # NUnit tests
```

## Design Principles

1. **Unity-Independent**: No Unity dependencies; runs on any .NET platform
2. **Pure Logic**: Deterministic algorithms with no side effects
3. **Fast Tests**: Unit tests run in milliseconds via `dotnet test`
4. **Serialization-First**: All models designed for JSON roundtrip

## Key Components

### Grid Math (`CoreLogic.Grid`)
- `HexGrid` - Coordinate conversions (axial ↔ cube ↔ world)
- `AxialCoord` - Immutable hex coordinate representation
- Neighbor calculations and distance functions

### Models (`CoreLogic.Models`)
- `BaseHexState` - Complete hex state (type, rotation, position, metadata)
- `HexState` - Minimal serialized hex state
- Designed for deterministic serialization/deserialization

### Serialization (`CoreLogic.Serialization`)
- `BaseHexStateSerializer` - JSON serialization for BaseHexState
- `HexSerializer` - List serialization utilities

## Building and Testing

### Build
```powershell
dotnet build CoreLogic/src/CoreLogic/CoreLogic.csproj
```

### Run Tests
```powershell
dotnet test CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj
```

### Watch Mode (for TDD)
```powershell
dotnet watch test --project CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj
```

## Integration with Unity

CoreLogic is deployed to Unity as a compiled DLL in `Assets/Plugins/CoreLogic/`.

**Deploy to Unity**:
```powershell
.\scripts\deploy-corelogic-to-unity.cmd
```

This builds CoreLogic and copies the DLL (plus Newtonsoft.Json dependency) to Unity's Plugins folder.

**Using in Unity**:

The Unity project uses an adapter pattern to bridge between Unity's `Hex.BaseHexState` and `CoreLogic.Models.BaseHexState`:

```csharp
using CoreLogic.Models;

// Convert Unity → CoreLogic
var coreState = unityHexState.ToCore();

// Convert CoreLogic → Unity
var unityState = coreState.FromCore();
```

See `Assets/Scripts/CoreLogicAdapter.cs` for implementation details.

**When to redeploy**:
- After modifying CoreLogic source code
- When CoreLogic tests reveal bugs that need fixing
- After pulling changes that affect CoreLogic

## Code Quality

- **Zero warnings**: All StyleCop and compiler warnings suppressed or fixed
- **100% deterministic**: All operations are pure functions
- **Fast builds**: < 1 second for full rebuild
- **Fast tests**: All tests run in < 100ms

## Adding New Features

1. Add model/logic to `CoreLogic/src/CoreLogic/`
2. Add tests to `CoreLogic/tests/CoreLogic.Tests/`
3. Verify tests pass: `dotnet test`
4. Add Unity adapter methods if needed in `Assets/Scripts/CoreLogicAdapter.cs`
5. CI will automatically validate on push

## Dependencies

- **Newtonsoft.Json** (13.0.3) - JSON serialization
- **NUnit** (test only) - Unit testing framework

## Contributing

When adding new logic:
1. Keep Unity dependencies out of CoreLogic
2. Write tests first (TDD encouraged)
3. Ensure deterministic behavior (no DateTime.Now, no Random without seed)
4. Document public APIs with XML comments
