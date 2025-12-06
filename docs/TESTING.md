# Testing Guide

This document describes how to run tests locally and in CI.

## Quick Start

### CoreLogic Tests (No Unity Required)

**Fastest way to validate changes:**

```powershell
# Build and test CoreLogic
dotnet build CoreLogic\src\CoreLogic\CoreLogic.csproj
dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj

# Or combined
dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj
```

**Expected output:**
```
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10
```

### Repository Build Guard

Validate all .csproj files build:

```powershell
.\scripts\check-csproj-builds.cmd
```

## Local Testing

### Installing Pre-Push Hook

Automate validation before pushing:

```powershell
.\scripts\install-prepush-hook.ps1
```

**What it checks:**
- CoreLogic builds
- CoreLogic tests pass
- All .csproj files build

**Bypass** (use sparingly):
```powershell
git push --no-verify
```

### Unity EditMode Tests

**Requirements:**
- Unity 2021.3.18f1 installed
- Unity license activated

**In Unity Editor:**
1. Window → General → Test Runner
2. Select "EditMode" tab
3. Click "Run All"

**Command Line:**
```powershell
.\scripts\run-unity-editmode-tests.cmd
```

**Output:**
- Results: `TestResults/editmode-results.xml`
- Logs: `TestResults/unity-batch-log.txt`

### Unity PlayMode Tests

**In Unity Editor:**
1. Window → General → Test Runner
2. Select "PlayMode" tab
3. Click "Run All"

**Note**: PlayMode tests require Unity to enter Play mode.

## Test Organization

### CoreLogic Tests

**Location**: `CoreLogic/tests/CoreLogic.Tests/`

**Framework**: NUnit 3.x

**Test Files**:
- `HexGridTests.cs` - Coordinate conversion tests
- `HexGridEdgeTests.cs` - Neighbor and distance tests  
- `BaseHexStateSerializerTests.cs` - Serialization tests
- `HexSerializerTests.cs` - List serialization tests
- `HexStateTests.cs` - State model tests

**Running specific tests:**
```powershell
# Run single test class
dotnet test --filter ClassName=HexGridTests

# Run single test
dotnet test --filter FullyQualifiedName~HexGrid_AxialToCube
```

### Unity Tests

**Location**: `Assets/Tests/Editor/`

**Framework**: Unity Test Runner (NUnit-based)

**Test Files**:
- `HexStateTests.cs` - Unity/CoreLogic integration tests

**Adding new Unity tests:**
1. Create test file in `Assets/Tests/Editor/`
2. Add `[TestFixture]` attribute to class
3. Add `[Test]` attributes to test methods
4. Refresh Test Runner window

## Continuous Integration

### CI Workflows

**Fast Checks** (`.github/workflows/ci.yml`):
- Runs on: GitHub-hosted runners
- Duration: ~1 minute
- Checks:
  1. CoreLogic build
  2. CoreLogic tests
  3. All .csproj builds
  4. Test result artifacts uploaded

**Unity Tests** (`.github/workflows/unity-selfhost-tests.yml`):
- Runs on: Self-hosted Windows runner
- Duration: ~5 minutes
- Checks:
  1. CoreLogic checks (fast-fail)
  2. Unity compilation
  3. Unity EditMode tests
  4. Test results and logs uploaded

### CI Triggers

**On Push:**
- Branches: `main`, `feature/*`
- Both workflows run

**On Pull Request:**
- Target: `main`
- Both workflows run
- Must pass before merge

### Viewing CI Results

1. Navigate to GitHub Actions tab
2. Select workflow run
3. View job logs and test results
4. Download artifacts for detailed output

## Test-Driven Development

### CoreLogic TDD Workflow

```powershell
# Terminal 1: Watch mode (auto-run tests on save)
dotnet watch test --project CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj

# Terminal 2: Edit code
code CoreLogic\src\CoreLogic\Grid\HexGrid.cs
```

### Writing Good Tests

**Naming Convention:**
```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = new AxialCoord(1, 2);
    
    // Act
    var result = HexGrid.AxialToCube(input);
    
    // Assert
    Assert.AreEqual(1, result.X);
}
```

**Test Coverage Goals:**
- Core logic: 90%+ coverage
- Edge cases explicitly tested
- Error conditions validated
- Serialization round-trips verified

## Debugging Tests

### CoreLogic Tests in Visual Studio

1. Open `SuperHexLink.sln`
2. Set breakpoints in test or source
3. Right-click test → Debug Test(s)

### Unity Tests in Unity Editor

1. Open Test Runner
2. Right-click test → Run in Player (with debugger)
3. Attach Visual Studio debugger to Unity

### Test Fixtures and Data

Test data location: `CoreLogic/tests/CoreLogic.Tests/TestData/` (if needed)

Embedded resources for test data:
```csharp
[Test]
public void LoadTestData()
{
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream("TestData.json");
    // ...
}
```

## Troubleshooting

### "Test project not found"

**Problem**: `dotnet test` can't find project

**Solution**: Run from repository root:
```powershell
cd D:\dev\SuperHexLink
dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj
```

### "Unity tests fail immediately"

**Problem**: Unity license not activated

**Solution**:
1. Open Unity Editor manually
2. Sign in and activate license
3. Close Unity
4. Retry batch mode tests

### "Pre-push hook doesn't run"

**Problem**: Hook not installed or disabled

**Solution**:
```powershell
.\scripts\install-prepush-hook.ps1 -Force
git config core.hooksPath .git\hooks
```

### "Tests pass locally but fail in CI"

**Possible causes:**
1. Platform differences (paths, line endings)
2. Missing dependencies or files
3. Timing/async issues
4. Environment-specific configuration

**Debug steps:**
1. Check CI logs and artifacts
2. Run tests in clean checkout
3. Verify file paths are relative
4. Check test isolation

## Performance Testing

### Benchmarking CoreLogic

For performance-critical code, add benchmarks:

```csharp
[Test]
public void Benchmark_WorldToAxial_1000Iterations()
{
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
    {
        HexGrid.WorldToAxial(i * 10.0, i * 10.0, 1.0);
    }
    sw.Stop();
    Assert.Less(sw.ElapsedMilliseconds, 10); // Should be < 10ms
}
```

### Unity Profiling

For Unity performance testing:
1. Window → Analysis → Profiler
2. Run game with profiling enabled
3. Look for hot paths in spawning/rendering

## Test Maintenance

### Updating Tests After Refactoring

1. Run all tests before refactoring
2. Make refactoring changes
3. Fix failing tests
4. Verify no tests were removed accidentally
5. Add tests for new behavior

### Test Code Quality

Tests should be:
- **Fast**: CoreLogic suite runs in < 100ms
- **Isolated**: No shared state between tests
- **Repeatable**: Same input → same output
- **Self-validating**: Clear pass/fail
- **Timely**: Written with or before production code

## Related Documentation

- [Development Setup](DEV_SETUP.md) - Environment configuration
- [Architecture](ARCHITECTURE.md) - System design
- [Contributing](CONTRIBUTING.md) - Contribution guidelines
