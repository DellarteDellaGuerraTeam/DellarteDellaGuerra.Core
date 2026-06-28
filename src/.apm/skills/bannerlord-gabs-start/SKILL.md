---
name: bannerlord-gabs-start
description: >
  Launch the DellarteDellaGuerra Bannerlord mod under the JetBrains debugger AND
  make it a fully GABS-managed instance so all GABP tools work. Use this skill
  whenever the user wants to start Bannerlord for debugging, says things like
  "launch the debug config", "start dadg debug", "start bannerlord with debugger",
  "launch dadg standalone", "debug bannerlord", "start bannerlord for v1.2",
  "start bannerlord for v1.3", or "start the jetbrains debug config".
  Also use it when the user wants to both have the debugger attached AND be able to
  control the game via GABS tools. This skill handles the full setup — bridge.json,
  runtime state, JetBrains session, and GABS connection — in one go.
  Pass "v1.2" or "v1.3" as an argument to target a specific game version (default: v1.3).
---

# DellarteDellaGuerra Debug Launch

Launches Bannerlord via the JetBrains "DellarteDellaGuerra.Integration: Standalone.GABS"
debug config and connects GABS to the running instance. By the end the debugger is
attached and all GABP tools are live.

---

## Version resolution

Determine the target game version from ARGUMENTS (default: v1.3):

| ARGUMENTS contains | `project_path` |
|--------------------|----------------|
| `v1.2`             | `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord-v1.2/Modules/DellarteDellaGuerra.Core/src` |
| `v1.3` or (empty)  | `D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord-v1.3/Modules/DellarteDellaGuerra.Core/src` |

Use this `project_path` in Step 3. All other steps are version-independent.

---

## Step 1 — Check if already running

Call `mcp__bannerlord-game-controller__games_status` with `gameId: "bannerlord"`.

- If **already running and GABP connected** — skip Steps 2–4, go straight to Step 5 (game state check).
- If **running but GABP disconnected** — skip Steps 2–3, go to Step 4 (games_connect) and check if the JetBrains session is still live; if not, attach the debugger first (see crash detection note).
- If **not running** — continue with Steps 2–4.

---

## Step 2 — Prepare the GABS files and ensure AssertAutoIgnore is running

These files live on Windows where GABS reads them. Use PowerShell for the setup steps.

First, ensure Steam is running — Bannerlord requires Steam and will fail without it:

```powershell
if (-not (Get-Process -Name "steam" -ErrorAction SilentlyContinue)) {
    Start-Process "steam://open/main"
    Write-Host "STEAM_STARTED"
} else {
    Write-Host "STEAM_RUNNING"
}
```

If the output is `STEAM_STARTED`, wait 10 seconds for Steam to initialise before continuing.

Rewrite bridge.json (always — GABS may have overwritten it with a random port/token
from a previous managed session):

```powershell
$dir = Join-Path $HOME ".gabs\bannerlord"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$bridgePath = Join-Path $dir "bridge.json"
$content = '{"port": 4825, "token": "80c93be27f02d35bedbe46493ce7a12f3d9976cc195cce2ece727cfdd0f09f9d", "gameId": "bannerlord"}'
Set-Content -Path $bridgePath -Value $content -NoNewline
Write-Host "GABS files ready"
```

Then ensure AssertAutoIgnore is running (it auto-dismisses Bannerlord popups including Safe Mode):

```powershell
$exe = "D:\Bannerlord\Tools\AssertAutoIgnore\AssertAutoIgnore\AssertAutoIgnore.exe"
if (-not (Get-Process -Name "AssertAutoIgnore" -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath $exe -WindowStyle Hidden
    Write-Host "AssertAutoIgnore started"
} else {
    Write-Host "AssertAutoIgnore already running"
}
```

---

## Step 3 — Start the JetBrains debug session

Call `mcp__jetbrains-debugger__start_debug_session`:
- `configuration_name`: `"DellarteDellaGuerra.Integration: Standalone.GABS"`
- `project_path`: the path resolved in the Version resolution section above

Note the session ID from the response — report it to the user.

The launchSettings.json Standalone.GABS profile already includes `Bannerlord.GABS` in the
module list and sets `GABP_SERVER_PORT=4825`, `GABP_TOKEN`, `GABS_GAME_ID`, and
`GABS_BRIDGE_PATH` as env vars — so the game will start the GABP bridge on port 4825
with the token written in Step 2. No further configuration needed.

---

## Step 4 — Wait for GABP bridge

Poll `games_connect` with `gameId: "bannerlord"`, `timeout: 15` every ~15 seconds until
it succeeds. AssertAutoIgnore (started in Step 2) handles any Safe Mode popups in the
background — no special handling needed here.

- **Connected** → follow immediately with `games_status` to confirm, then go to Step 5.
- **Timeout / connection refused** → game is still loading. Wait 15 s and retry.
- **Game process gone** (check via `games_status` returning `stopped`) → game crashed
  during loading. Call `mcp__jetbrains-debugger__get_debug_session_status` immediately
  (50 frames, include variables). Act fast — the JetBrains session closes when the
  process exits.

---

## Step 5 — Wait for the main menu

Once GABP is connected, poll `bannerlord.core.get_game_state` via
`mcp__bannerlord-game-controller__games_call_tool` until
`state == "InitialState"` or `state == "main_menu"` (both mean the main menu is ready).
Use a 15 s timeout per poll. Only take a screenshot if `get_game_state` times out
more than twice consecutively — that signals a stuck state worth diagnosing visually.
Never use `wait_for_state`.

---

## Step 6 — Load a save (if requested)

If the user specified a save name, call `bannerlord.core.load_save` with that name.
If no save was specified, stop here — leave the user at the main menu.

After loading, poll `bannerlord.core.get_game_state` (timeout: 15 s per poll) until
`state == "campaign_map"`. Same screenshot rule — only if stuck twice in a row.

---

## Step 7 — Check blockers and report

Call `bannerlord.core.check_blockers`. Common blockers after loading:
- `paused` — normal, game is paused on load
- `inquiry_active` — popup needs dismissing via `bannerlord.ui.answer_inquiry`
- `menu_siege_strategies` — active siege; use `bannerlord.menu.get_current` to see options

**Crash detection**: If GABP disconnects unexpectedly (connection closed error) at any
point after connecting, immediately call `mcp__jetbrains-debugger__get_debug_session_status`
(50 frames, include variables). Act fast — the JetBrains session closes when the process
exits and the trace is gone. If the call returns "Session not found", the session is
already closed; fall back to Bannerlord crash logs at
`%AppData%\Mount and Blade II Bannerlord\logs\`.

Report to the user:
- JetBrains debug session ID
- Whether the game was already running or freshly launched
- GABS connection status
- Save loaded (if any) and game state confirmed
- Any active blockers
