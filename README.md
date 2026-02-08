# SuperHexLink

![CI](https://github.com/jgreensite/SuperHexLink/actions/workflows/ci.yml/badge.svg)
![Unity](https://img.shields.io/badge/Unity-2021.3.18f1-blue)
![.NET](https://img.shields.io/badge/.NET_Standard-2.0-purple)

A **Unity-based hex-grid strategy game** inspired by classic board games like Catan. Features a custom map editor, save/load system with legacy format conversion, harbour placement pipeline, and a standalone CoreLogic library for fast unit testing.

---

## Architecture at a Glance

```
┌─────────────────────────────────────────┐
│     Unity Presentation Layer            │  MonoBehaviours, UI, Rendering
├─────────────────────────────────────────┤
│     Unity Game Logic Layer              │  Spawners, State, Map Editor
├─────────────────────────────────────────┤
│     CoreLogic Adapter                   │  Unity ↔ CoreLogic bridge
├─────────────────────────────────────────┤
│     CoreLogic Library (.NET Std 2.0)    │  Grid math, serialization — no Unity deps
└─────────────────────────────────────────┘
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for full details.

## Quick Start

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| Unity | 2021.3.18f1+ | Game engine |
| .NET SDK | 6.0+ | CoreLogic library development |
| .NET Framework | 4.7.1 Developer Pack | Unity Assembly-CSharp builds |
| PowerShell | 7+ (pwsh) | Build scripts and CI tooling |

### Clone & Run

```bash
git clone https://github.com/jgreensite/SuperHexLink.git
cd SuperHexLink
```

Then open the project in **Unity Hub** and load the main scene from `Assets/Scenes/`.

For detailed environment setup, see [docs/DEV_SETUP.md](docs/DEV_SETUP.md).

### Running Tests

```powershell
# CoreLogic unit tests (fast, no Unity required — run these first)
dotnet test CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj

# Build guard (validates all .csproj files compile)
.\scripts\check-csproj-builds.cmd

# Pester tests for build scripts
.\scripts\run-local-pester.ps1

# Unity EditMode tests (requires Unity installed)
.\scripts\run-unity-editmode-tests.cmd
```

See [docs/TESTING.md](docs/TESTING.md) for the full testing guide.

## Project Structure

```
SuperHexLink/
├── .github/             # CI workflows, issue/PR templates, CODEOWNERS
├── Assets/
│   ├── Scripts/         # Core game scripts (spawners, state, utilities)
│   ├── Editor/          # Unity Editor extensions (Map Editor window)
│   ├── Tests/Editor/    # Unity EditMode tests
│   ├── UI/              # UI scripts and UXML layouts
│   └── Prefabs/         # Hex, edge, corner prefabs
├── CoreLogic/           # Standalone .NET library (grid math, serialization)
│   ├── src/CoreLogic/   # Library source
│   └── tests/           # NUnit tests
├── docs/                # Architecture, testing, setup guides
├── scripts/             # Build automation, CI tooling, git hooks
├── data/maps/           # Saved map files (JSON)
└── BACKLOG.md           # Epics, Features, Stories for the product roadmap
```

## Documentation

| Document | Description |
|----------|-------------|
| [BACKLOG.md](BACKLOG.md) | Product roadmap — Epics, Features, Stories |
| [CHANGELOG.md](CHANGELOG.md) | Release notes and version history |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | System design and key decisions |
| [docs/adr/](docs/adr/README.md) | Architecture Decision Records (ADRs) |
| [docs/SECURITY.md](docs/SECURITY.md) | Threat model, input validation, secrets |
| [docs/PERFORMANCE.md](docs/PERFORMANCE.md) | Performance budgets, profiling, scaling |
| [docs/ONBOARDING.md](docs/ONBOARDING.md) | Day-1 guide for new developers |
| [docs/DEV_SETUP.md](docs/DEV_SETUP.md) | Environment setup and build instructions |
| [docs/TESTING.md](docs/TESTING.md) | How to run tests locally and in CI |
| [docs/HEXSPAWNER_SETUP.md](docs/HEXSPAWNER_SETUP.md) | Configure HexSpawner prefabs |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Branching strategy, PR process |
| [scripts/README.md](scripts/README.md) | Build automation and CI tooling |
| [CoreLogic/README.md](CoreLogic/README.md) | CoreLogic library details |

## Third-Party Assets

These libraries are **not committed** to the repo. See [docs/DEV_SETUP.md](docs/DEV_SETUP.md) for download instructions.

| Library | Location | Purpose |
|---------|----------|---------|
| Odin Inspector (Sirenix) | `Assets/Plugins/Sirenix/` | Inspector tooling, serialization |
| ParadoxNotion | `Assets/ParadoxNotion/` | Flow graph helpers |
| SimpleFileBrowser | `Assets/Plugins/SimpleFileBrowser/` | File dialogs |
| NativeFilePicker | `Assets/Plugins/NativeFilePicker/` | Native file dialogs |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the branching strategy, PR process, and coding conventions.

**TL;DR**: Feature branch → PR → CI passes → Code review → Merge.

## License

[Add your license information here]

## Credits

Originally inspired by [this tutorial on 3D hex grids in Unity](https://youtu.be/3ZjjlNqjX8c).
