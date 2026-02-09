# Security Guidelines

This document covers the security posture, threat model, and defensive patterns for SuperHexLink.

**Last updated**: February 2026

---

## Threat Model

### Attack Surface

| Surface | Risk | Mitigation |
|---------|------|------------|
| **Save file loading** | Malformed JSON causing crashes, path traversal | Input validation, sandboxed paths, size limits |
| **Legacy format parsing** | Regex injection, integer overflow | `int.TryParse`, bounded regex, defensive defaults |
| **Network (future)** | MITM, replay attacks, state desync | TLS, signed packets, authoritative server |
| **User input (UI)** | Script injection via hex names/group IDs | Input sanitization, length limits |
| **Third-party plugins** | Supply chain vulnerabilities | Vendor audits, pinned versions, Dependabot |

### Trust Boundaries

```
┌─────────────────────────────────────────────┐
│              TRUSTED                        │
│  CoreLogic (pure C#, no I/O, no Unity)     │
│  In-memory state (HexState, GameState)     │
├─────────────────────────────────────────────┤
│              BOUNDARY                       │
│  File I/O (save/load)                      │
│  Network I/O (future multiplayer)          │
│  User input (UI fields, file picker)       │
├─────────────────────────────────────────────┤
│              UNTRUSTED                      │
│  Save files on disk (could be hand-edited) │
│  Network packets (could be spoofed)        │
│  Third-party plugin updates                │
└─────────────────────────────────────────────┘
```

---

## Implemented Defences

### 1. Save File Loading (`GameSpawner.LoadState`)

**Threat**: A malicious or corrupted save file could crash the game or exploit parsing bugs.

**Mitigations**:
- **Path validation**: `ResolveMapPath()` constrains paths to the `data/maps/` directory. Relative paths with `..` are rejected.
- **File size limit**: Files larger than 10 MB are rejected before parsing (prevents memory exhaustion).
- **JSON deserialization**: Uses `JsonConvert.DeserializeObject` with `TypeNameHandling.None` (default) — **never** `TypeNameHandling.Auto` which enables deserialization attacks.
- **Fallback on failure**: If parsing fails, the game logs an error and keeps the current state rather than crashing.

```csharp
// GOOD — safe deserialization
var state = JsonConvert.DeserializeObject<GameSpawnerState>(json);

// BAD — NEVER DO THIS — enables arbitrary type instantiation
var state = JsonConvert.DeserializeObject<GameSpawnerState>(json,
    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
```

### 2. Legacy Format Parsing (`LegacyMapConverter`)

**Threat**: Regex captures on untrusted input could produce empty strings or malformed numbers.

**Mitigations**:
- All `int.Parse` calls replaced with `int.TryParse` (v0.3.0 uplift)
- Regex patterns are bounded and non-greedy
- Failed conversions return sensible defaults rather than throwing

### 3. Input Sanitization

**Rule**: Any string that enters the system from user input or file I/O must be validated before use.

```csharp
// Pattern: validate at the boundary, trust internally
public static bool IsValidHexType(string hexType)
{
    return !string.IsNullOrEmpty(hexType)
        && hexType.Length <= 32
        && ValidHexTypes.Contains(hexType);
}

public static bool IsValidGroupID(string groupId)
{
    return groupId == null  // null is allowed (means "no group")
        || (groupId.Length <= 16 && System.Text.RegularExpressions.Regex.IsMatch(groupId, @"^[a-zA-Z0-9_-]+$"));
}
```

### 4. Secrets Management

**Current state**: No secrets are required for local play.

**Future (networking)**:
- API keys and server credentials must **never** be committed to the repo
- Use Unity's `PlayerPrefs` for user tokens (encrypted on supported platforms)
- Use environment variables for CI secrets (`${{ secrets.UNITY_LICENSE }}`)
- Add `.env` to `.gitignore` before any networking work begins
- **Known issue**: `GameConstants.GAMESERVERREMOTEADDRESS` contains a hardcoded IP (`35.177.228.70`). This **must** be moved to a runtime config file (e.g., `StreamingAssets/server-config.json` or environment variable) before any public or production build.

### 5. Dependency Security

- **Dependabot** enabled via `.github/dependabot.yml` for NuGet and GitHub Actions
- **Odin Inspector** and **ParadoxNotion** are commercial — pinned to known-good versions
- CoreLogic uses only `Newtonsoft.Json` (well-audited, widely used)

---

## Security Checklist for Contributors

Before merging any PR:

- [ ] No `TypeNameHandling.Auto` in any JSON deserialization
- [ ] All file paths validated against directory traversal (`..`, absolute paths)
- [ ] All `int.Parse` / `float.Parse` replaced with `TryParse` on untrusted input
- [ ] No secrets, API keys, or credentials in committed code
- [ ] No `Debug.Log` of sensitive data (player IDs, auth tokens)
- [ ] Input length limits on user-editable string fields

---

## Future Security Work

| Priority | Item | Backlog Ref |
|----------|------|-------------|
| ~~High~~ | ~~Add path traversal guard to `ResolveMapPath`~~ | ✅ Done |
| ~~High~~ | ~~Add JSON file size limit (10 MB) to `LoadState`~~ | ✅ Done |
| **High** | Externalize hardcoded server IP/port from `GameConstants` to runtime config | `E3-F3` |
| Medium | Add schema version validation on load | — |
| Medium | Network packet signing for multiplayer | `E5-F2` |
| Low | Fuzz testing for `LegacyMapConverter` | — |

---

## Reporting Vulnerabilities

If you discover a security vulnerability, please report it privately via GitHub Security Advisories rather than opening a public issue. See [GitHub's guide](https://docs.github.com/en/code-security/security-advisories).
