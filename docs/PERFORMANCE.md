# Performance Guide

Performance budgets, profiling techniques, and optimization patterns for SuperHexLink.

**Last updated**: February 2026

---

## Performance Budgets

| Metric | Budget | Current | Notes |
|--------|--------|---------|-------|
| **Frame rate** | 60 fps | ~60 fps (7x7) | Scales with hex count |
| **Board generation** | < 500 ms | ~200 ms (7x7) | Includes instantiation + material assignment |
| **Save to disk** | < 100 ms | ~50 ms (7x7) | JSON serialization is synchronous |
| **Load from disk** | < 500 ms | ~300 ms (7x7) | Includes legacy detection + repair |
| **CoreLogic test suite** | < 200 ms | ~50 ms | Must stay fast for TDD workflow |
| **Memory (board)** | < 50 MB | ~20 MB (7x7) | GameObjects + materials + state |
| **GC allocations per frame** | < 1 KB | TBD | Profile with Unity Profiler |

---

## Known Hotspots

### 1. `HexSpawner.BuildMe()` — O(cols × rows)
- Instantiates one `GameObject` per hex
- Assigns materials via `Renderer.material` (creates material instances)
- **Optimization**: Object pooling (`E6-F1-S1`), shared materials where possible

### 2. `HexSpawner.SetLand()` — per-hex material lookup
- Dictionary lookup in `GameConstants.MaterialMap` — O(1) per hex
- `GetComponent<Renderer>()` and `GetComponent<MeshRenderer>()` called per hex
- **Optimization**: Cache component references on `Hex` initialization

### 3. `MapValidator.ValidateMap()` — O(cols × rows × rules)
- Iterates every hex, checks adjacency rules
- **Optimization**: Only validate changed hexes in editor (incremental validation)

### 4. `HexReplacementPipeline` — O(candidates × attempts)
- Harbour placement retries up to `maxAttemptsBeforeFallback` per candidate
- **Optimization**: Pre-compute candidate list, use spatial indexing

### 5. JSON Serialization (Save/Load) — synchronous on main thread
- Blocks rendering during large board saves
- **Optimization**: Async save with `Task.Run` + progress callback (`E3-F3-S2`)

---

## Profiling Guide

### Unity Profiler (in-editor)
```
Window → Analysis → Profiler
```

Key areas to watch:
- **CPU**: Look for spikes in `HexSpawner.BuildMe`, `SetLand`, `Refresh`
- **Memory**: Track `GC.Alloc` per frame — should be < 1 KB in steady state
- **Rendering**: Batching efficiency, draw calls (Window → Analysis → Frame Debugger)

### CoreLogic Benchmarks
```powershell
# Add [Test] benchmarks in CoreLogic.Tests
dotnet test --filter "Benchmark" CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj
```

### Memory Profiling
```
Window → Analysis → Memory Profiler (install via Package Manager)
```
- Take snapshots before and after board generation
- Compare to find leaks (unreleased materials, orphaned GameObjects)

---

## Optimization Patterns

### 1. Object Pooling (planned — `E6-F1`)
```csharp
// Instead of:
var hex = Instantiate(hexPrefab);  // GC allocation
Destroy(hex);                       // GC pressure

// Use:
var hex = hexPool.Get();            // Reuse existing
hexPool.Return(hex);                // Return to pool
```

### 2. Component Caching
```csharp
// Instead of (per-frame):
GetComponent<Renderer>().material = mat;

// Cache on init:
private Renderer _renderer;
void Awake() { _renderer = GetComponent<Renderer>(); }
void SetMaterial(Material mat) { _renderer.material = mat; }
```

### 3. Shared Materials
```csharp
// Instead of (creates instance per hex):
renderer.material = forestMaterial;  // Implicit copy

// Use shared (all hexes share one instance):
renderer.sharedMaterial = forestMaterial;  // No copy
```

### 4. Async Save/Load
```csharp
// Instead of blocking main thread:
public void Save() { File.WriteAllText(path, json); }

// Use async:
public async Task SaveAsync()
{
    var json = await Task.Run(() => JsonConvert.SerializeObject(state));
    await File.WriteAllTextAsync(path, json);
}
```

### 5. Spatial Indexing for Large Boards
```csharp
// For boards > 20x20, use a dictionary lookup instead of linear search
private Dictionary<(int Col, int Row), Hex> _hexLookup;
public Hex GetHex(int col, int row) => _hexLookup.TryGetValue((col, row), out var h) ? h : null;
```

---

## Scaling Considerations

| Board Size | Hex Count | Expected FPS | Memory | Notes |
|------------|-----------|--------------|--------|-------|
| 7×7 | 49 | 60 fps | ~20 MB | Current default |
| 15×15 | 225 | 60 fps | ~60 MB | Should work fine |
| 30×30 | 900 | 45-60 fps | ~150 MB | May need pooling |
| 50×50 | 2,500 | 20-30 fps | ~400 MB | Needs LOD + culling |
| 100×100 | 10,000 | < 10 fps | ~1 GB | Needs ECS migration |

**Recommendation**: Object pooling at 15×15+, LOD at 30×30+, ECS evaluation at 50×50+.

---

## GC Pressure Reduction Checklist

- [ ] No `string.Format` or string concatenation in hot loops — use `StringBuilder` or interpolation with `Span<T>`
- [ ] No `LINQ` in per-frame code paths — use `for` loops
- [ ] No `GetComponent<T>()` in `Update()` — cache in `Awake()`
- [ ] No `new List<T>()` in hot paths — reuse collections
- [ ] No `Debug.Log` in production builds — use `[Conditional("UNITY_EDITOR")]` or `#if UNITY_EDITOR`
