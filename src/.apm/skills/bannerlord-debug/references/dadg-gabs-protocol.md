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

Config name: `"DellarteDellaGuerra.Integration: Standalone"` (`start_debug_session` also accepts just `Standalone`)  
Project path: `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/Modules/DellarteDellaGuerra.Core/src`

Pass that `project_path` on **every** JetBrains MCP call. Note the literal `&` — do not HTML-escape
it. The path above is the **1.4.7** install; older notes pointing at `…Bannerlord-v1.3/…` are stale
(see "`games_start` can launch the WRONG install" below).

The `launchSettings.json` Standalone profile sets `GABP_SERVER_PORT`, `GABP_TOKEN`, `GABS_GAME_ID`, and `GABS_BRIDGE_PATH` as env vars — no manual configuration needed beyond writing `bridge.json`.

## Session Lifecycle

1. Session-start hook starts `gabs.exe` if not running (idempotent).
2. `bannerlord-gabs-start` skill: writes `bridge.json`, deletes `runtime.json`, starts JetBrains debug session, calls `games_connect`.
3. GABP inside the game creates `runtime.json` and registers with the GABS server.
4. `runtime.json` tracks ownership — if stale after a crash or abandoned session, use `bannerlord-gabs-session-reset` skill to clear it.

## Hazards and workarounds

### `games_start` can launch the WRONG install

There are multiple Bannerlord installs on this disk:

| Path | Status |
|------|--------|
| `Mount & Blade II Bannerlord` | **1.4.7 — the live one. DADG builds deploy here.** |
| `Mount & Blade II Bannerlord-v1.3` | obsolete |
| `Mount & Blade II Bannerlord-v1.2` | obsolete |

`~/.gabs/config.json` has pointed `games_start` at an obsolete install. An agent then watched the
old binary access-violate while loading 1.4.7-era saves and filed a bogus *"Critical: game crashes
~2s after any save load"*. Before trusting **any** crash report, confirm which binary is live:

```powershell
Get-Process Bannerlord | Select-Object Id,Path
```

The GABS MCP server reads `config.json` **once at startup** — editing it mid-session does nothing.
Either restart the MCP server or launch via the JetBrains `Standalone` config, which pins the right
executable through `launchSettings.json`.

### Pinned port means no `bridge.json` is written

When `GABP_SERVER_PORT` is set via env (as the `Standalone` profile does), the mod does **not**
write `~/.gabs/bannerlord/bridge.json`. Write it by hand with the fixed content above, or
`games_connect` chases a stale port from a previous launch and fails.

Never delete or "repair" `~/.gabs/bannerlord/*.json` to unstick a session — that has destroyed a
working session before. Use the `bannerlord-gabs-session-reset` skill instead.

### `conversation.start` immediately after `load_save` crashes the game

`load_save` returns success and `core.get_game_state` reports `campaign_map` **before the map state
has actually settled**. Calling `conversation.start` in that window hits an unhandled NRE in vanilla
`ConversationManager.OpenMapConversation` (`GameStateManager.Current?.ActiveState as MapState` is
still null) and the process dies. After any save load: screenshot to confirm the map really
rendered, and let several seconds of real time pass with time running. Vanilla/GABS timing hazard,
not a DADG bug.

### `menu.get_current` does not prove a menu option exists

It enumerates *registered* options without evaluating their visibility conditions. This produced two
false-positive "Critical" defects in one run. Always corroborate with a screenshot of the rendered
menu. Also: select menu options **by index**, not by option ID — selection by ID misfires.

### `Created GameScope` counts processes, not campaign loads

The ButterLib log line `Created GameScope` looks like a clean "a campaign finished loading" marker.
It is not: across a full day of testing it appeared 15 times but **never more than once per process
lifetime**, including in sessions that loaded several campaigns. A single occurrence does not prove
a second load failed. Treat it as weak corroboration only — `get_campaign_time` sampled twice and a
screenshot are the real evidence that the game is live.

Mod log: `C:\Users\Joe\Documents\Mount and Blade II Bannerlord\Configs\ModLogs\default<YYYYMMDD>.log`

### Parameter names that repeatedly waste calls

| Tool | Correct parameter |
|------|-------------------|
| `bannerlord.party.enter_settlement` | `settlementNameOrId` (not `nameOrId`, not `settlement`) |
| `bannerlord.settlement.get_settlement` | `nameOrId` |
| `bannerlord.conversation.start` | `nameOrId` (not `targetHero`) |
| `bannerlord.core.set_time_speed` | `speed` only |
| `bannerlord.core.wait_for_state` | `expectedState`, `pollIntervalMs` (no `timeoutSeconds`) |

`campaign.advance_time` does not exist — advance time with `set_time_speed` plus polling.

## Breakpoints & Tool Flow

When a breakpoint is hit, the **next** `games_call_tool` invocation will pause and report the hit before executing — giving you a window to inspect state via `mcp__jetbrains-debugger__get_stack_trace` / `get_variables` before resuming.

If GABP disconnects unexpectedly (game crash), **immediately** call `mcp__jetbrains-debugger__get_stack_trace` — the stack is only available while the debugger is paused at the exception. Don't skip this step or the crash cause is lost.
