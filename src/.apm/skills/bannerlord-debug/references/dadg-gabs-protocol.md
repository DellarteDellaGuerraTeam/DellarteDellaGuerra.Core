# DADG GABS Protocol

Technical details for GABS/GABP — the live Bannerlord debugging and control system.

## What is GABS?

**GABS** (Game Agent Bridge System) lets Claude programmatically control a running Bannerlord instance — driving the UI, querying game state, issuing battle orders, etc.

**GABP** (GABS Bridge Protocol) is the in-process server the game exposes via a `Bannerlord.GABS` module that is included in the DADG debug launch profile.

## File Locations

| File | Path | Purpose |
|------|------|---------|
| `bridge.json` | `C:/Users/<you>/.gabs/bannerlord/bridge.json` | Config read by GABP inside the game: port, token, gameId |
| `runtime.json` | `C:/Users/<you>/.gabs/bannerlord/runtime.json` | Session ownership: ownerPid (GABS server), gamePid (Bannerlord) |
| GABS server binary | `D:\Bannerlord\GABS-release\gabs.exe` | HTTP server on port 8080; started automatically by the session-start hook |

## Fixed bridge.json Content

```json
{"port": 4825, "token": "80c93be27f02d35bedbe46493ce7a12f3d9976cc195cce2ece727cfdd0f09f9d", "gameId": "bannerlord"}
```

Always rewrite `bridge.json` to this before launching (the `bannerlord-gabs-start` skill does it automatically). GABS may overwrite it with a random port/token from a previous managed session.

## Ports

| Port | Service |
|------|---------|
| `4825` | GABP in-process bridge (inside the running game) |
| `8080` | GABS HTTP server (`gabs.exe server --http localhost:8080`) |

## JetBrains Debug Configuration

Config name: `"DellarteDellaGuerra.Integration: Standalone"`  
Project path: `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord-v1.3/Modules/DellarteDellaGuerra.Core/src`

The `launchSettings.json` Standalone profile sets `GABP_SERVER_PORT`, `GABP_TOKEN`, `GABS_GAME_ID`, and `GABS_BRIDGE_PATH` as env vars — no manual configuration needed beyond writing `bridge.json`.

## Session Lifecycle

1. Session-start hook starts `gabs.exe` if not running (idempotent).
2. `bannerlord-gabs-start` skill: writes `bridge.json`, deletes `runtime.json`, starts JetBrains debug session, calls `games_connect`.
3. GABP inside the game creates `runtime.json` and registers with the GABS server.
4. `runtime.json` tracks ownership — if stale after a crash or abandoned session, use `bannerlord-gabs-session-reset` skill to clear it.

## Breakpoints & Tool Flow

When a breakpoint is hit, the **next** `games_call_tool` invocation will pause and report the hit before executing — giving you a window to inspect state via `mcp__jetbrains-debugger__get_stack_trace` / `get_variables` before resuming.

If GABP disconnects unexpectedly (game crash), **immediately** call `mcp__jetbrains-debugger__get_stack_trace` — the stack is only available while the debugger is paused at the exception. Don't skip this step or the crash cause is lost.
