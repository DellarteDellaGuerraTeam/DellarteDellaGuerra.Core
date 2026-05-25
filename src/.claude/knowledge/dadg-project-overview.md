# DADG Project Overview

This file provides project-specific context for working on this codebase.

## Project

This repository (`DellarteDellaGuerra.Core`) is the **C# source code** for *DellarteDellaGuerra* (DADG), a Bannerlord mod set during the English War of the Roses (1471), opposing the House of York and Lancaster for the English crown.

The mod is heavily inspired by **The Old Realms** mod (firearms, jousting, cannons). The C# code lives here; assets, XML, scenes, and maps live in separate sibling content modules.

## DADG Module Structure

All modules sit under the Bannerlord `Modules/` folder:

| Module | Path | Contents |
|--------|------|----------|
| **DellarteDellaGuerra.Core** | *(this repo)* `src/` | C# game logic, Harmony patches, DI, all features |
| **DellarteDellaGuerra** | `../DellarteDellaGuerra` | Main content: armour, weapons, animation assets, music tracks, general XML (`ModuleData/`); compiled asset cache (`RuntimeDataCache/`) |
| **DellarteDellaGuerraMap** | `../DellarteDellaGuerraMap` | England campaign map (`SceneObj/`, `SceneEdit/`), campaign UI (`GUI/` + `Assets/`, `AssetSources/`, `RuntimeDataCache/`), map & sandbox XML (characters, equipment, settlements) |
| **DellarteDellaGuerraScenes** | `../DellarteDellaGuerraScenes` | All custom Bannerlord scenes (`SceneObj/`, `SceneEdit/`), scene-specific prefabs and assets (houses, walls, …) |

> `RuntimeDataCache/` contains compiled Bannerlord asset files. Never edit by hand — regenerate via the Bannerlord Modding Kit.

## Dependencies

| Dependency | Type | Notes |
|-----------|------|-------|
| **Lib.Harmony** | Hard / external | Runtime method patching |
| **Bannerlord.ExpandedTemplate** | Business / **controlled** | Equipment randomisation, siege templates; API bound in `SubModule` constructor via `BannerlordExpandedTemplateApi` |
| **Bannerlord.Cannons** | Business / **controlled** | Adds cannons to campaign & missions |
| **POC (Bannerlord POC mod)** | Optional / external | Banner & heraldry customisation; config overridden to `../config/poc.config.json` via `PocConfigReaderOverriderPatch` |

`ExpandedTemplate` and `Cannons` are our own modules — their source lives at the same Bannerlord modules level as this repo.

## Build & Test Commands

All commands run from `src/`:

```bash
dotnet restore DellarteDellaGuerra.sln
dotnet build DellarteDellaGuerra.sln --configuration Release
dotnet test DellarteDellaGuerra.sln
dotnet test --filter "ClassName=GetJoustEquipmentUtilTests"
```

> **Test coverage is very sparse.** `Domain.Tests` has one test file; `Infrastructure.Tests` and `Integration.Tests` exist but contain no test source files.

## Architecture

Hexagonal architecture (active migration in progress) with four C# projects:

```
Domain              — Pure business logic, zero Bannerlord references (netstandard2.0)
Infrastructure      — Bannerlord adapters, Harmony patches, events (netstandard2.0)
DellarteDellaGuerra — Application/feature logic: mission behaviours, UI, models (netstandard2.0)
Integration         — DI composition root, SubModule entry point (netstandard2.0)
```

**Entry point:** `src/DellarteDellaGuerra.Integration/SubModule.cs`
**DI setup:** `src/DellarteDellaGuerra.Integration/DI/DadgServiceContainer.cs`

Two-stage DI: `LoggingContainer` (constructor) then `DadgServiceContainer` (OnSubModuleLoad). The music patch and `BannerlordExpandedTemplateApi` are wired in the constructor before either container is built.

Harmony patches all implement `IPatch`, registered in `DadgServiceContainer.RegisterPatches()`, applied via `IHarmonyPatcher.ApplyPatches()`.

## Feature Domains

| Feature | Key code paths | Notes |
|---------|---------------|-------|
| **Firearms** | `DellarteDellaGuerra/Firearm/`, `Infrastructure/Firearm/` | Multi-phase black-powder reload, smoke effects, skill registration |
| **Cannons / Siege Engines** | `Infrastructure/SiegeEngines/`, `Integration/SiegeEngines/` | Fully data-driven — adding a cannon is pure XML. See `doc/adding-a-cannon.md` |
| **Tournament** | `Domain/Tournament/`, `DellarteDellaGuerra/Tournament/` | Custom reward model + jousting tournament with its own mission logic |
| **Music** | `Integration/Music/Patches/` | Overrides PSAI (Bannerlord's soundtrack system) to load custom `soundtrack.xml` |
| **POC integration** | `Infrastructure/Poc/Patches/` | Redirects POC config path. Docs: `doc/poc.integration.md` |
| **Display Compiling Shaders** | `Domain/DisplayCompilingShaders/`, `DellarteDellaGuerra/DisplayCompilingShaders/` | In-game HUD counter during shader load |
| **Steam file paths** | `Infrastructure/Steam/Patches/` | Fixes settlement & distance-cache paths for Steam Workshop |
| **Main menu** | `DellarteDellaGuerra/MainMenu/` | Adds DADG campaign button; removes vanilla options |
| **Character creation** | `Infrastructure/CharacterCreation/Patches/` | Disables sort in culture selection menu |

## Key Paths

| Path | Purpose |
|------|---------|
| `../log/` | Mod runtime logs |
| `../config/dadg.config.xml` | Mod config (hot-reloaded) |
| `../config/poc.config.json` | POC integration config |
| `doc/` | Developer guides (`adding-a-cannon.md`, `poc.integration.md`) |
| `src/supported-game-versions.txt` | Target game version (currently `v1.3.15`) |

## Native XML Reference

| Module | XML path |
|--------|----------|
| **Native** | `../Native/ModuleData/` |
| **SandBox** | `../SandBox/ModuleData/` |
| **SandBoxCore** | `../SandBoxCore/ModuleData/` |

## MCP Tools & Debugging

**Bannerlord source search** (two versioned servers, started automatically):
- `mcp__bannerlord-search-1_3_1__search_bannerlord_code` / `get_bannerlord_class_definition` (prefer this)
- `mcp__bannerlord-search-1_2_12__*` (for version comparisons)

For searching the mod's own source, prefer **JetBrains MCP tools** — they operate on live source.

**Available skills:**

| Skill | Purpose |
|-------|---------|
| `bannerlord-gabs-start` | Launch DADG under JetBrains debugger with full GABS connection |
| `bannerlord-gabs-session-reset` | Kill Bannerlord and clean GABS session state |
| `bannerlord-gabs-battle-manager` | Drive a battle or siege from deployment to campaign map |
| `bannerlord-music-classify` | Convert audio → OGG, classify for PSAI, update `soundtrack.xml` |
| `bannerlord-sprite-analyze` | Show sprite atlas occupancy map |
| `bannerlord-sprite-list` | List sprites in a `SpriteData.xml` |
| `bannerlord-sprite-extract` | Crop and export a named sprite from an atlas PNG |
| `bannerlord-sprite-inject` | Add a PNG to the mod's SpriteParts and register it in SpriteData XML |

**Live debugging (GABS):** Use `bannerlord-gabs-start` skill → set breakpoints via `mcp__jetbrains-debugger__set_breakpoint` → drive the game with `mcp__bannerlord-game-controller__games_*` tools. When a breakpoint fires, the next tool call pauses and reports it.

For deeper detail on module folder layout, feature implementations, or GABS protocol internals, read the other files in `.claude/knowledge/`.
