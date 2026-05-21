# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

All commands run from `src/`:

```bash
# Restore dependencies
dotnet restore DellarteDellaGuerra.sln

# Build (auto-resolves game folder via BANNERLORD_GAME_DIR env var or registry)
dotnet build DellarteDellaGuerra.sln --configuration Release

# Override game folder explicitly
dotnet build DellarteDellaGuerra.sln --configuration Release -p:GameFolder="D:/SteamLibrary/..."

# Run all tests
dotnet test DellarteDellaGuerra.sln

# Run a single test project
dotnet test DellarteDellaGuerra.Domain.Tests/

# Run a specific test class
dotnet test --filter "ClassName=GetJoustEquipmentUtilTests"
```

## Architecture

This is a Bannerlord v1.3 mod for the English War of the Roses (1471) following clean/hexagonal architecture with four layers:

```
Domain              — Pure business logic, zero Bannerlord references (netstandard2.0)
Infrastructure      — Bannerlord adapters, Harmony patches, events (netstandard2.0)
DellarteDellaGuerra — Application/feature logic: mission behaviors, UI, models (netstandard2.0)
Integration         — DI composition root, SubModule entry point (netstandard2.0)
```

**Entry point:** `src/DellarteDellaGuerra.Integration/SubModule.cs`
**DI setup:** `src/DellarteDellaGuerra.Integration/DI/DadgServiceContainer.cs`

### Two-Stage DI Initialization

The mod uses two DI containers to match Bannerlord's lifecycle:

1. **LoggingContainer** (built in `SubModule` constructor) — NLog only; needed before game starts
2. **DadgServiceContainer** (built in `OnSubModuleLoad`) — all services and patches

The music patch (`MBMusicManagerInitializePatch`) is applied manually in the constructor before either container is built, because it must intercept game startup before `OnSubModuleLoad` fires.

### Harmony Patches

- All patches implement `IPatch` (from `Harmony.DependencyInjection`)
- Registered as singletons in `DadgServiceContainer.RegisterPatches()`
- Applied in bulk via `IHarmonyPatcher.ApplyPatches()` during `OnSubModuleLoad`
- Named: `[TargetClass][TargetMethod]Patch`
- Organized into subdirectories by domain concern (e.g., `Firearm/Patches/`, `SiegeEngines/Campaign/`)

### Port & Adapter Pattern

Domain interfaces live in `Domain/[Feature]/Port/` — these are the contracts.
Infrastructure adapters implement those interfaces and live in `Infrastructure/[Feature]/`.
Example: `ILogger` (domain port) → `NLogLoggerAdapter` (infrastructure adapter).

### Game Version

The target game version is read from `src/supported-game-versions.txt` (currently `v1.3.15`).
A compile-time constant is derived from it (e.g., `e1315`) and available as `#if e1315`.
Override via MSBuild: `-p:OverrideGameVersion=e1.3.15`.

## MCP Tools

A Bannerlord source search MCP server is available for looking up decompiled game code:
- `mcp__bannerlord-search__search_bannerlord_code` — regex search across decompiled source
- `mcp__bannerlord-search__get_bannerlord_class_definition` — full class definition lookup

The session-start hook (`.claude/hooks/session-start.sh`) configures and starts this server automatically.

## Related Modules

The mod's C# code (this repo) depends on XML data and assets from sibling Bannerlord modules:

| Module | Path | Contains |
|--------|------|----------|
| `DellarteDellaGuerra` | `..\DellarteDellaGuerra` | XML data + most assets (weapons, armour, items) |
| `DellarteDellaGuerraMap` | `..\DellarteDellaGuerraMap` | Campaign map scene, GUI, XML related to the campaign map |
| `DellarteDellaGuerraScenes` | `..\DellarteDellaGuerraScenes` | Custom scenes and assets (buildings, prefabs, etc.) |

For reference when looking up vanilla behaviour or game data:

| Module | Path |
|--------|------|
| `Native` | `..\Native` |
| `SandBox` | `..\SandBox` |
| `SandBoxCore` | `..\SandBoxCore` |

## Key Configuration Files

- `config/dadg.config.xml` — mod configuration (hot-reloaded via `DadgConfigWatcher`)
- `config/poc.config.json` — POC Colour Randomiser integration config
- `technical-config/nlog.dev.config` / `nlog.prod.config` — logging config (copied to bin at build time)
- `src/supported-game-versions.txt` — target game version (first line = target, last line = minimum)
