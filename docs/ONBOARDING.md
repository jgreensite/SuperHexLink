# Onboarding Guide — Your First Day

Welcome! This guide gets you from zero to productive in under 2 hours.

---

## Step 1: Environment Setup (30 min)

Follow [DEV_SETUP.md](DEV_SETUP.md) to install:
- Unity 2021.3.18f1
- .NET SDK 6.0+
- PowerShell 7+

Then:
```powershell
git clone https://github.com/jgreensite/SuperHexLink.git
cd SuperHexLink
.\scripts\install-git-hooks.ps1
```

## Step 2: Verify Everything Works (10 min)

```powershell
# 1. CoreLogic tests (should pass in < 1 second)
dotnet test CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj

# 2. Build guard (should pass in < 30 seconds)
.\scripts\check-csproj-builds.cmd

# 3. Open Unity, load Assets/Scenes/SampleScene.unity, press Play
```

If all three pass, you're good. If not, check [TESTING.md](TESTING.md) troubleshooting.

## Step 3: Understand the Architecture (20 min)

Read [ARCHITECTURE.md](ARCHITECTURE.md). Key takeaways:

- **CoreLogic** = pure C# library (grid math, serialization). No Unity dependency. Fast tests.
- **Assets/Scripts/** = Unity MonoBehaviours. Spawners create GameObjects from state.
- **GameSpawner** orchestrates everything. It holds `HexSpawner`, `EdgeSpawner`, `CornerSpawner`.
- **Hex.HexState** is the source of truth. The `Hex` MonoBehaviour renders it.
- Save files are JSON in `data/maps/`.

### The 5-Minute Mental Model

```
[JSON file] → GameSpawner.LoadState() → HexSpawner.BuildMe(fromState:true)
                                                ↓
                                        For each hex in state:
                                          Instantiate prefab
                                          Set material from type
                                          Position in grid
                                                ↓
                                        [Visual board in Unity]
```

## Step 4: Make Your First Change (30 min)

Try this exercise to verify you understand the workflow:

### Exercise: Add a new hex type constant

1. Open `Assets/Scripts/GameConstants.cs`
2. Find the hex type constants (e.g., `CAR_TYPE_FOREST`)
3. Add: `public const string CAR_TYPE_SWAMP = "SWAMP";`
4. Run the build guard: `.\scripts\check-csproj-builds.cmd`
5. Revert your change: `git checkout Assets/Scripts/GameConstants.cs`

You've just verified you can edit, build, and revert safely.

## Step 5: Pick Your First Story (15 min)

1. Open [`BACKLOG.md`](../BACKLOG.md)
2. Find a story marked `[ ]` in the **current milestone**
3. Read the **Acceptance Criteria** and **Files to Touch**
4. Create a branch: `git checkout -b feature/<story-slug>`
5. Implement, test, commit, push, open PR

### Good first stories for juniors:
- `E1-F3-S1` — Board hash utility (small, well-scoped, pure logic)
- `E2-F3-S2` — Add CODEOWNERS file (trivial, learn the PR process)
- `E3-F1-S1` — Replace `GameObject.Find` (learn the spawner pattern)

## Step 6: Know Where to Find Things

| I need to... | Look in... |
|--------------|-----------|
| Understand the codebase | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Run tests | [TESTING.md](TESTING.md) |
| Add a feature | [BACKLOG.md](../BACKLOG.md) → pick a story |
| Fix a bug | `.github/ISSUE_TEMPLATE/bug_report.md` → file an issue |
| Understand a script | [scripts/README.md](../scripts/README.md) |
| Check security rules | [SECURITY.md](SECURITY.md) |
| Optimize performance | [PERFORMANCE.md](PERFORMANCE.md) |
| Review conventions | [CONTRIBUTING.md](CONTRIBUTING.md) |

## Step 7: Key Commands Cheat Sheet

```powershell
# Tests
dotnet test CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj  # Fast unit tests
.\scripts\check-csproj-builds.cmd                                    # Build guard
.\scripts\run-local-pester.ps1                                       # Script tests

# Git
git checkout -b feature/my-feature   # New branch
git add -A && git commit -m "feat: my change"
git push origin feature/my-feature   # Push + open PR

# Unity
# Window → General → Test Runner → EditMode → Run All
# SuperHexLink → Map Editor Tool (custom editor window)
```

---

## Common Pitfalls

1. **Don't use `GameObject.Find`** — always use `[SerializeField]` references
2. **Don't add `Debug.Log` in hot paths** — use `ActionLogger` with severity levels
3. **Don't use `MonoBehaviour` constructors** — Unity never calls them. Use `Awake()` or `Init()`
4. **Don't commit Odin Inspector files** — they're in `.gitignore` for licensing reasons
5. **Always run tests before pushing** — the pre-push hook will catch you, but it's faster to catch early

---

**Questions?** Open a GitHub Discussion or ask in the team channel. Welcome aboard!
