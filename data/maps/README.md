# Map Data Files

This directory contains saved map files in JSON format used by the game's save/load system.

## Structure

| File Pattern | Description |
|---|---|
| `0_gameSpawnerState.json` | Top-level game state (grid config, land configs, number configs) |
| `1_hexSpawnerState.json` | All hex tile positions, types, and properties |
| `2_edgeSpawnerState.json` | Edge (road) placements between hexes |
| `3_cornerSpawnerState.json` | Corner (settlement/city) placements at hex vertices |
| `map.json` | Legacy single-file format (pre-v0.2.0) |

## Directories

- **`standard/`** — Reference maps for standard board layouts (4-player, 6-player seafarers)
- **`old/`** — Legacy format maps kept for backwards-compatibility testing with `LegacyMapConverter`
- **`backup/`** — Manual backups of specific configurations

## Notes

- These files are **committed to the repo** as test fixtures and reference data.
- The save/load system writes to this directory at runtime. Use `.gitignore` or `git stash` if you don't want local edits to show in `git status`.
- See `docs/SECURITY.md` for file size and path traversal guards applied during load.
