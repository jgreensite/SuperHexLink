# Development Setup

Complete guide to setting up a development environment for SuperHexLink.

## Prerequisites

### Required Software

**Unity 2021.3.18f1**
- Download: [Unity Hub](https://unity.com/download)
- Install Unity 2021.3.18f1 through Unity Hub
- Required modules:
  - Windows Build Support (IL2CPP)
  - iOS Build Support (if building for iOS)
  - Android Build Support (if building for Android)

**.NET SDK 6.0+**
- Download: [.NET SDK](https://dotnet.microsoft.com/download)
- Verify installation:
  ```powershell
  dotnet --version  # Should be 6.0 or higher
  ```

**Visual Studio 2022** or **VS Code**
- Visual Studio 2022 Community (recommended for Unity):
  - Workload: "Game development with Unity"
  - Unity Tools for Visual Studio
- VS Code alternative:
  - Extension: C# for Visual Studio Code
  - Extension: Unity Code Snippets
  - Extension: Unity Tools

**Git**
- Download: [Git for Windows](https://git-scm.com/download/win)
- Configure:
  ```powershell
  git config --global user.name "Your Name"
  git config --global user.email "your.email@example.com"
  ```

### Optional Tools

**PowerShell 7+** (recommended)
- Download: [PowerShell](https://github.com/PowerShell/PowerShell/releases)
- Better scripting experience than Windows PowerShell 5.1

**GitHub CLI**
- Download: [GitHub CLI](https://cli.github.com/)
- Simplifies PR creation and repository management

## Initial Setup

### 1. Clone Repository

```powershell
# Clone via HTTPS
git clone https://github.com/yourusername/SuperHexLink.git
cd SuperHexLink

# Or via SSH (if configured)
git clone git@github.com:yourusername/SuperHexLink.git
cd SuperHexLink
```

### 2. Restore Dependencies

**CoreLogic (automatic)**:
```powershell
cd CoreLogic
dotnet restore
dotnet build
```

**Deploy CoreLogic to Unity**:
```powershell
.\scripts\deploy-corelogic-to-unity.cmd
```

This copies CoreLogic.dll and dependencies to `Assets/Plugins/CoreLogic/` so Unity can reference it.

**Unity packages (automatic on first open)**:
- Unity will automatically restore packages from `Packages/manifest.json`
- This includes:
  - Universal Render Pipeline
  - Input System
  - Test Framework
  - TextMesh Pro

### 2.x Third-party Unity assets

These assets are intentionally excluded from source control because they are distributed under separate licenses. Download each one from its vendor and copy or unzip the folder into the listed path inside the Unity project before opening the editor.

- **Sirenix Odin Inspector** (required for serialized inspectors and drawers)
  - Download: https://odininspector.com/
  - Target path: `Assets/Plugins/Sirenix/`
- **ParadoxNotion frameworks** (behavior tree/flow tools)
  - Download: https://paradoxnotion.com/
  - Target path: `Assets/ParadoxNotion/`
- **SimpleFileBrowser** (cross-platform file picker)
  - Download: https://github.com/yasirkula/UnitySimpleFileBrowser
  - Target path: `Assets/Plugins/SimpleFileBrowser/`
- **NativeFilePicker** (mobile file access)
  - Download: https://github.com/yasirkula/UnityNativeFilePicker
  - Target path: `Assets/Plugins/NativeFilePicker/`

After copying the folders in place, re-open the Unity project (or right-click the new folders in the Project window and choose "Reimport") so the assets import correctly.

### 3. Activate Unity License

**First time setup**:
1. Open Unity Hub
2. Sign in with Unity account
3. Activate personal/professional license
4. Open project from Unity Hub

**Command line activation** (for CI/build machines):
```powershell
# Request license file
Unity.exe -batchmode -quit -nographics -logFile - -createManualActivationFile

# Upload generated Unity_v2021.x.alf to: https://license.unity3d.com/manual
# Download .ulf file and activate:
Unity.exe -batchmode -quit -nographics -logFile - -manualLicenseFile Unity_v2021.x.ulf
```

### 4. Configure IDE

**Visual Studio 2022**:
1. Open `SuperHexLink.sln`
2. Set Unity External Editor:
   - Unity → Edit → Preferences → External Tools
   - External Script Editor: Visual Studio 2022
3. Regenerate project files:
   - Unity → Assets → Open C# Project

**VS Code**:
1. Open workspace folder in VS Code
2. Install recommended extensions (`.vscode/extensions.json`)
3. Set Unity External Editor:
   - Unity → Edit → Preferences → External Tools
   - External Script Editor: Visual Studio Code
4. Generate solution:
   - Unity → Edit → Preferences → External Tools → Regenerate project files

### 5. Verify Setup

**CoreLogic builds**:
```powershell
dotnet build CoreLogic\src\CoreLogic\CoreLogic.csproj
# Expected: Build succeeded. 0 Warning(s). 0 Error(s).
```

**CoreLogic tests pass**:
```powershell
dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj
# Expected: Passed! - Failed: 0, Passed: 10, Skipped: 0
```

**All .csproj files build**:
```powershell
.\scripts\check-csproj-builds.cmd
# Expected: All projects built successfully
```

**Unity opens without errors**:
1. Open project in Unity
2. Wait for import to complete
3. Check Console for errors (should be clean)
4. Try entering Play mode

## Development Workflow

### Git Workflow

**Create feature branch**:
```powershell
git checkout -b feature/your-feature-name
```

**Make changes and commit**:
```powershell
# Stage changes
git add .

# Commit with descriptive message
git commit -m "Add hex spawning system"

# Push to remote
git push origin feature/your-feature-name
```

**Create pull request**:
```powershell
# Using GitHub CLI
gh pr create --title "Add hex spawning system" --body "Description of changes"

# Or visit GitHub web UI
```

### CoreLogic Development

**Edit-build-test cycle**:
```powershell
# Terminal 1: Watch mode (auto-rebuild on changes)
cd CoreLogic
dotnet watch test --project tests\CoreLogic.Tests\CoreLogic.Tests.csproj

# Terminal 2: Make changes in your IDE
code src\CoreLogic\Grid\HexGrid.cs
```

**Debug tests**:
1. Open `CoreLogic.Tests.csproj` in Visual Studio
2. Set breakpoints in test or source code
3. Right-click test → Debug Test(s)

### Unity Development

**Recommended workflow**:
1. Keep Unity Editor open during development
2. Make C# changes in external IDE
3. Unity auto-recompiles on save
4. Test in Play mode immediately

**Check for compilation errors without opening Unity**:
```powershell
.\scripts\check-unity-errors.cmd
```

This parses Unity's Editor.log and shows errors/warnings instantly.

**Common Unity tasks**:
- **Run EditMode tests**: Window → General → Test Runner → EditMode → Run All
- **Profile performance**: Window → Analysis → Profiler → Enter Play mode
- **Inspect scene hierarchy**: Window → General → Hierarchy
- **View Console logs**: Window → General → Console (Ctrl+Shift+C)

## Project Structure

### CoreLogic (Standalone Library)

```
CoreLogic/
├── src/
│   └── CoreLogic/
│       ├── Grid/           # Hex coordinate system
│       ├── State/          # Game state models
│       └── CoreLogic.csproj
└── tests/
    └── CoreLogic.Tests/
        └── CoreLogic.Tests.csproj
```

**Design principle**: CoreLogic has zero Unity dependencies. Test it independently.

### Unity Project

```
Assets/
├── Scripts/
│   ├── Adapters/          # CoreLogic → Unity integration
│   ├── GameManagement/    # Game loop, state management
│   ├── HexSystem/         # Hex rendering, spawning
│   ├── PlayerControl/     # Input handling
│   └── UI/                # UI controllers
├── Prefabs/               # Hex tile prefabs
├── Materials/             # Hex materials
├── Scenes/                # Game scenes
└── Tests/
    └── Editor/            # Unity integration tests
```

**Design principle**: Unity code uses CoreLogic via adapter layer.

### Build Scripts

```
scripts/
├── check-csproj-builds.ps1     # Validate all .csproj files build
├── check-csproj-builds.cmd     # Windows wrapper
├── install-prepush-hook.ps1    # Install pre-push validation
└── run-unity-editmode-tests.cmd # Run Unity tests in batch mode
```

## Configuration

### Action logging

- `Assets/Scripts/ActionLogger.cs` now records key interactions inside `GameSpawner` and `HexSpawner`. Logs are gated by `Assets/Scripts/ActionLogSettings` (a ScriptableObject you can create from the Unity menu via **Create → Logging → Action Log Settings**). Configure which categories/ severities are captured (General, HexLifecycle, HexLand, GameLifecycle) and set the `enabled` toggle to turn the action log on or off without code changes.
- Assign the same `ActionLogSettings` asset to the `actionLogSettings` field on the `GameSpawner` and `HexSpawner` GameObjects in your scene so Unity emits `[ActionLog/...` entries for builds, state loads, land assignments, etc. When enabled, you get reproducible textual traces in the Console or `check-unity-errors.ps1` output to mirror what the UI does while you manually test.

### Server Configuration

The game loads server settings from `Assets/StreamingAssets/server-config.json` with environment variable overrides:

```json
{
  "serverHost": "127.0.0.1",
  "serverPort": 6321,
  "connectionTimeoutMs": 5000,
  "maxRetries": 3,
  "enableLogging": true
}
```

**Environment Variable Overrides** (higher priority):
- `SUPERHEX_SERVER_HOST` - Server hostname/IP
- `SUPERHEX_SERVER_PORT` - Server port number
- `SUPERHEX_CONNECTION_TIMEOUT` - Connection timeout in milliseconds
- `SUPERHEX_MAX_RETRIES` - Maximum retry attempts
- `SUPERHEX_ENABLE_LOGGING` - Enable/disable logging (true/false)

**Usage in Code**:
```csharp
// Recommended: Use runtime configuration
var config = GameConstants.GetServerConfig();
Debug.Log($"Connecting to {config.ServerHost}:{config.ServerPort}");

// Legacy: Deprecated constants (will be removed in future)
Debug.Log(GameConstants.GAMESERVERREMOTEADDRESS); // Shows deprecation warning
```

### Unity Project Settings

**Player Settings**:
- File → Build Settings → Player Settings
- Product Name: SuperHexLink
- Company Name: [Your Company]
- Default Icon: Set project icon

**Quality Settings**:
- Edit → Project Settings → Quality
- Current profile: Ultra (for development)
- Reduce for mobile builds

**Input System**:
- Edit → Project Settings → Player → Active Input Handling: Both
- Uses new Input System + legacy for compatibility

### Editor Config (.editorconfig)

Located at repository root. Defines code style rules:
- Indent: 4 spaces
- Newline: LF (Unix-style)
- StyleCop rules customized per directory

**Applied automatically** by Visual Studio and VS Code.

### OmniSharp Config (omnisharp.json)

Located at repository root. Configures C# language server:
- Excluded projects: Third-party assemblies (Sirenix, ParadoxNotion)
- Enables semantic highlighting
- Configures code analysis

## Troubleshooting

### "Unity not found" errors

**Problem**: Scripts can't find Unity executable

**Solution**: Add Unity to PATH:
```powershell
$env:PATH += ";C:\Program Files\Unity\Hub\Editor\2021.3.18f1\Editor"
```

Or specify path explicitly:
```powershell
$env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\2021.3.18f1\Editor\Unity.exe"
```

### "dotnet: command not found"

**Problem**: .NET SDK not in PATH

**Solution**: Restart terminal after installing .NET SDK, or add manually:
```powershell
$env:PATH += ";C:\Program Files\dotnet"
```

### Unity compilation errors after checkout

**Problem**: Missing third-party packages or corrupted Library/

**Solution**:
```powershell
# Delete Library folder (Unity will regenerate)
Remove-Item -Recurse -Force Library\

# Reopen Unity (will reimport all assets)
# This takes 5-10 minutes
```

### Visual Studio can't find Unity assemblies

**Problem**: IntelliSense shows errors but Unity compiles fine

**Solution**: Regenerate project files from Unity:
1. Edit → Preferences → External Tools
2. Click "Regenerate project files"
3. Restart Visual Studio
4. Rebuild solution

### CoreLogic tests fail with "file not found"

**Problem**: Running tests from wrong directory

**Solution**: Always run from repository root:
```powershell
cd D:\dev\SuperHexLink
dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj
```

### Git LFS files not downloading

**Problem**: Large files (textures, models) show as pointers

**Solution**: Install and initialize Git LFS:
```powershell
git lfs install
git lfs pull
```

### "Pre-push hook failed"

**Problem**: Validation hook blocks push due to failing tests

**Solution**: Fix the failing tests, or bypass (not recommended):
```powershell
git push --no-verify
```

## Advanced Setup

### Self-Hosted CI Runner

For running Unity tests in CI:

**Install GitHub Actions runner**:
1. Download runner from GitHub repository settings
2. Extract to `scripts\actions-runner\` (gitignored)
3. Configure and run as Windows service:
   ```powershell
   .\config.cmd --url https://github.com/yourorg/SuperHexLink --token YOUR_TOKEN
   .\svc.cmd install
   .\svc.cmd start
   ```

**Requirements**:
- Unity installed and activated for service account
- .NET SDK 6.0+
- Runner has network access to GitHub

### Code Profiling

**CoreLogic profiling** (BenchmarkDotNet):
```powershell
dotnet add CoreLogic\tests\CoreLogic.Tests package BenchmarkDotNet
# Add benchmark tests
dotnet run --project CoreLogic\tests\CoreLogic.Tests -c Release -- --filter *Benchmark*
```

**Unity profiling**:
1. Window → Analysis → Profiler
2. Deep Profile: CPU Usage
3. Enter Play mode
4. Analyze hotspots in HexSpawner, HexRenderer

### Custom Editor Extensions

Add custom Unity editor tools:

```csharp
// Assets/Editor/HexGridDebugger.cs
[MenuItem("Tools/Hex Grid Debugger")]
public static void ShowDebugger()
{
    EditorWindow.GetWindow<HexGridDebuggerWindow>();
}
```

### Memory Profiling

**Track memory usage**:
1. Window → Analysis → Memory Profiler
2. Capture snapshot in Play mode
3. Look for:
   - Hex object pooling effectiveness
   - Texture memory usage
   - Managed heap fragmentation

## Learning Resources

### Unity Documentation
- [Scripting API](https://docs.unity3d.com/ScriptReference/)
- [Manual](https://docs.unity3d.com/Manual/)
- [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)

### C# and .NET
- [C# Language Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/)

### Project-Specific
- [Hex Coordinate Systems](https://www.redblobgames.com/grids/hexagons/) - Red Blob Games guide
- [Architecture Documentation](ARCHITECTURE.md) - Project architecture
- [Testing Guide](TESTING.md) - Running tests
- [Contributing Guidelines](CONTRIBUTING.md) - Code style and workflow

## Related Documentation

- [Architecture](ARCHITECTURE.md) - Technical architecture and design decisions
- [Testing Guide](TESTING.md) - How to run tests locally and in CI
- [Contributing](CONTRIBUTING.md) - Contribution guidelines and code standards
- [TODO](TODO.md) - Current tasks and roadmap
