# Contributing to SuperHexLink

Thank you for your interest in contributing to SuperHexLink! This document provides guidelines for contributing to the project.

## Development Setup

1. Follow the [Development Setup Guide](DEV_SETUP.md) to configure your environment
2. Install the pre-push hook to catch issues early:
   ```powershell
   .\scripts\install-prepush-hook.ps1
   ```

## Code Style

### C# Conventions
- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use 4 spaces for indentation
- Place opening braces on new line
- Use `this.` prefix for member access (Unity convention)

### StyleCop and Analyzers
- CoreLogic library enforces StyleCop rules (zero warnings required)
- Unity code follows Unity conventions (more relaxed)
- Run `dotnet build` to check for analyzer warnings

### Naming Conventions
- **Classes**: PascalCase (e.g., `HexSpawner`)
- **Methods**: PascalCase (e.g., `GenerateBoard()`)
- **Fields**: camelCase with `_` prefix for private (e.g., `_hexGrid`)
- **Properties**: PascalCase (e.g., `HexType`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `MAX_HEX_COUNT`)

## Git Workflow

### Branching Strategy
- `main` - Stable, production-ready code
- `feature/*` - New features (e.g., `feature/hex-state-refactor`)
- `bugfix/*` - Bug fixes
- `hotfix/*` - Urgent production fixes

### Commit Messages
Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build process, tooling, dependencies

**Examples**:
```
feat(corelogic): add hex distance calculation
fix(spawner): prevent null reference in BuildMe
docs(readme): update setup instructions
test(hexgrid): add neighbor calculation tests
```

### Pull Request Process

1. **Create a feature branch** from `main`:
   ```powershell
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes** following code style guidelines

3. **Write tests** for new functionality:
   - CoreLogic changes → NUnit tests
   - Unity changes → Unity Test Runner tests

4. **Run pre-push checks**:
   ```powershell
   .\scripts\check-csproj-builds.cmd
   dotnet test CoreLogic\tests\CoreLogic.Tests\CoreLogic.Tests.csproj
   ```

5. **Commit changes** with descriptive messages

6. **Push to your branch**:
   ```powershell
   git push origin feature/your-feature-name
   ```

7. **Create Pull Request** on GitHub:
   - Provide clear description of changes
   - Reference any related issues
   - Ensure CI passes
   - Request review from maintainers

### PR Review Checklist
- [ ] Code follows style guidelines
- [ ] Tests added for new functionality
- [ ] All tests pass (CoreLogic + Unity)
- [ ] Documentation updated if needed
- [ ] No new compiler warnings
- [ ] Commit messages follow conventions
- [ ] PR description clearly explains changes

## Testing Guidelines

### CoreLogic Tests
- **Location**: `CoreLogic/tests/CoreLogic.Tests/`
- **Framework**: NUnit
- **Naming**: `MethodName_Scenario_ExpectedBehavior`
- **Coverage**: Aim for high coverage on public APIs

**Example**:
```csharp
[Test]
public void AxialToCube_ValidInput_ReturnsCorrectCubeCoordinates()
{
    var axial = new AxialCoord(1, 2);
    var (x, y, z) = HexGrid.AxialToCube(axial);
    
    Assert.AreEqual(1, x);
    Assert.AreEqual(-3, y);
    Assert.AreEqual(2, z);
}
```

### Unity Tests
- **Location**: `Assets/Tests/Editor/` (EditMode) or `Assets/Tests/PlayMode/`
- **Framework**: Unity Test Runner (NUnit)
- **Scope**: Integration tests for Unity-specific logic

## Architecture Guidelines

### CoreLogic Library
- **No Unity dependencies**: Must be pure .NET Standard 2.0
- **Deterministic**: No Random without seed, no DateTime.Now
- **Immutable where possible**: Prefer readonly structs and records
- **Serialization-first**: Design for JSON round-trip

### Unity Code
- **Prefer composition over inheritance**
- **Use dependency injection** where practical
- **Avoid GameObject.Find**: Use explicit references
- **MonoBehaviours for presentation only**: Keep logic in CoreLogic

### State Management
- Use backing state pattern (see `Hex.cs` example)
- Separate runtime state from serialized state
- Validate state transitions

## Documentation

### Code Comments
- **XML comments** for all public APIs in CoreLogic
- **Inline comments** for complex logic or non-obvious decisions
- **TODO comments** should include issue number if applicable

### Documentation Files
When adding new features, update relevant docs:
- `README.md` - High-level overview
- `docs/ARCHITECTURE.md` - Architectural decisions
- `docs/TODO.md` - Add new tasks
- Component-specific READMEs (e.g., `CoreLogic/README.md`)

## Issue Reporting

### Bug Reports
Include:
1. Description of the bug
2. Steps to reproduce
3. Expected behavior
4. Actual behavior
5. Unity version and platform
6. Relevant logs or screenshots

### Feature Requests
Include:
1. Description of the feature
2. Use case / problem it solves
3. Proposed implementation (if any)
4. Alternatives considered

## Third-Party Code

### Adding Dependencies
1. Justify the dependency (file an issue first)
2. Check license compatibility
3. Document in README and ARCHITECTURE
4. Update `.editorconfig` to exclude from analysis if needed

### Modifying Third-Party Code
- Avoid modifying vendor code directly
- If necessary, document changes clearly
- Consider contributing upstream

### Excluded Directories
The following are excluded from style checks:
- `Assets/Plugins/` (third-party Unity assets)
- `Assets/ParadoxNotion/` (NodeCanvas)
- `scripts/actions-runner/` (GitHub Actions artifacts)

## Release Process

1. Update version numbers in relevant files
2. Update CHANGELOG.md
3. Tag release: `git tag -a v1.0.0 -m "Release 1.0.0"`
4. Push tags: `git push --tags`
5. Create GitHub Release with notes
6. Build and upload artifacts

## Getting Help

- **Documentation**: Check `docs/` directory first
- **Issues**: Search existing issues on GitHub
- **Discussions**: Use GitHub Discussions for questions
- **TODOs**: See `docs/TODO.md` for known work items

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on the code, not the person
- Help others learn and grow

## Recognition

Contributors will be recognized in:
- GitHub contributors list
- Release notes for significant contributions
- Project credits

Thank you for contributing to SuperHexLink! 🎉
