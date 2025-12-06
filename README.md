# SuperHexLink

A Unity-based hex-grid strategy game prototype inspired by classic board games.

## Project Structure

```
SuperHexLink/
├── Assets/              # Unity game assets and scripts
├── CoreLogic/           # Standalone .NET library for game logic (see CoreLogic/README.md)
├── Build/               # Build output and platform-specific files
├── docs/                # Project documentation
├── scripts/             # Build and development automation scripts
├── data/                # Game data and map files
└── ProjectSettings/     # Unity project configuration
```

## Quick Start

### Prerequisites
- Unity 2021.3.18f1 or later
- .NET SDK 6.0+ (for CoreLogic development)
- .NET Framework 4.7.1 Developer Pack (for Unity Assembly-CSharp)

### Getting Started
1. Clone the repository
2. Open the project in Unity Hub
3. See [docs/DEV_SETUP.md](docs/DEV_SETUP.md) for detailed setup instructions

### Running Tests
- **CoreLogic tests** (fast): `dotnet test CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj`
- **Unity EditMode tests**: See [docs/TESTING.md](docs/TESTING.md)
- **Pre-push checks**: `.\scripts\check-csproj-builds.cmd`

## Documentation

- **[Development Setup](docs/DEV_SETUP.md)** - Environment setup and build instructions
- **[HexSpawner Setup](docs/HEXSPAWNER_SETUP.md)** - Configure HexSpawner prefabs in Unity
- **[Testing Guide](docs/TESTING.md)** - How to run tests locally and in CI
- **[Architecture](docs/ARCHITECTURE.md)** - Technical architecture and design decisions
- **[TODO List](docs/TODO.md)** - Consolidated project tasks and issues
- **[Contributing](docs/CONTRIBUTING.md)** - How to contribute to the project
- **[Scripts Documentation](scripts/README.md)** - Build automation and CI/CD tooling

## CoreLogic Library

The `CoreLogic/` directory contains a standalone .NET Standard 2.0 library for game logic that can be tested independently of Unity. See [CoreLogic/README.md](CoreLogic/README.md) for details.

## Third-Party Assets

These libraries are not committed (see `docs/DEV_SETUP.md` for download instructions) and should be placed into the matching folders after cloning:
- **Odin Inspector** (Sirenix) → `Assets/Plugins/Sirenix/`
- **ParadoxNotion** (flow graph helper packages) → `Assets/ParadoxNotion/`
- **SimpleFileBrowser** → `Assets/Plugins/SimpleFileBrowser/`
- **NativeFilePicker** → `Assets/Plugins/NativeFilePicker/`

See `packages.config` for the package list that Unity restores automatically.

## License

[Add your license information here]

## Credits

Initially inspired by [this tutorial on 3D hex grids in Unity](https://youtu.be/3ZjjlNqjX8c).
