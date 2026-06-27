---
description: Core project layout, architecture, build commands, and high-risk editing rules for DADG.
---

# DADG Project Overview

This repository is the C# source for DellarteDellaGuerra, a Mount & Blade II: Bannerlord mod set during the English War of the Roses.

## Module Layout

- `src/`: this repository, C# game logic, Harmony patches, dependency injection, and feature code.
- `../DellarteDellaGuerra`: main content module with XML, armour, weapons, animation assets, music, and compiled asset cache.
- `../DellarteDellaGuerraMap`: England campaign map, campaign UI, map XML, sandbox XML, and UI assets.
- `../DellarteDellaGuerraScenes`: custom mission scenes.

Never hand-edit `RuntimeDataCache/`; regenerate it through Bannerlord tooling.

## Architecture

The codebase is migrating toward hexagonal architecture:

- `DellarteDellaGuerra.Domain`: pure business logic, no Bannerlord references.
- `DellarteDellaGuerra.Infrastructure`: Bannerlord adapters, Harmony patches, events.
- `DellarteDellaGuerra`: application and feature logic.
- `DellarteDellaGuerra.Integration`: DI composition root and `SubModule` entry point.

Entry point: `DellarteDellaGuerra.Integration/SubModule.cs`.
DI setup: `DellarteDellaGuerra.Integration/DI/DadgServiceContainer.cs`.

## Build And Test

Run commands from `src/`:

```powershell
dotnet restore DellarteDellaGuerra.sln
dotnet build DellarteDellaGuerra.sln --configuration Release
dotnet test DellarteDellaGuerra.sln
dotnet test --filter "ClassName=GetJoustEquipmentUtilTests"
```

Test coverage is sparse. Scale verification to risk and use live debugging checks when the affected behavior is mostly in Bannerlord runtime state.

## Controlled Dependencies

`Bannerlord.ExpandedTemplate` and `Bannerlord.Cannons` live under `src/submodules/` and are project-referenced by DADG. Edit them in place there, not in legacy standalone module folders. Rebuild DADG to deploy their DLLs into the running game's loaded bin folder.

## Debugging

Prefer the `bannerlord-debug` skill for crashes, CLR exceptions, GABS, Rider debugging, or any fix/relaunch/verify loop. DADG bugs are often data bugs in XML or scene files rather than C# bugs.
