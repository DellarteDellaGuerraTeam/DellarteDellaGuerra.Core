---
name: bannerlord-gabs-start
description: >
  Launch the current Bannerlord mod under the JetBrains debugger AND make it a
  fully GABS-managed instance so all GABP tools work. Use this skill whenever the
  user wants to start Bannerlord for debugging, says things like "launch the debug
  config", "start bannerlord with debugger", "launch standalone", "debug bannerlord",
  "start bannerlord for v1.4", or "start the jetbrains debug config".
  Also use it when the user wants to both have the debugger attached AND be able to
  control the game via GABS tools. This skill handles the full setup — bridge.json,
  runtime state, JetBrains session, and GABS connection — in one go.
  Pass a game version as an argument when the project has several installs.
---

# Bannerlord Debug Launch

Launches Bannerlord via the mod's JetBrains debug run configuration and connects
GABS to the running instance. By the end the debugger is attached and all GABP
tools are live.

---

## Project resolution

Take these from the project's Bannerlord agent settings (an instructions/rules file
shipped by the mod's own agent package):

- the JetBrains debug run configuration name (Step 3)
- `project_path` — the folder Rider opened, per game version when ARGUMENTS names one

If there are no settings, `project_path` is the project folder holding `apm.yml`,
and the run configuration is the launch profile in `**/Properties/launchSettings.json`
that loads `Bannerlord.GABS`; ask the user if several match. Write `project_path` with
forward slashes. All other steps are version-independent.

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
from a previous managed session). The port and token must match the
`GABP_SERVER_PORT` and `GABP_TOKEN` environment variables of the debug profile in
the mod's `launchSettings.json`; read them from there, never invent a token:

```powershell
$port  = <GABP_SERVER_PORT from the launch profile>
$token = '<GABP_TOKEN from the launch profile>'
$dir = Join-Path $HOME ".gabs\bannerlord"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$bridgePath = Join-Path $dir "bridge.json"
$content = @{ port = $port; token = $token; gameId = "bannerlord" } | ConvertTo-Json -Compress
Set-Content -Path $bridgePath -Value $content -NoNewline
Write-Host "GABS files ready"
```

Then ensure AssertAutoIgnore is running (it auto-dismisses Bannerlord popups including
Safe Mode). Its path comes from the `ASSERT_AUTO_IGNORE_EXE` environment variable; if
it is unset, skip this and expect to dismiss popups yourself:

```powershell
$exe = $env:ASSERT_AUTO_IGNORE_EXE
if (-not $exe) {
    Write-Host "ASSERT_AUTO_IGNORE_EXE not set - skipping"
} elseif (-not (Get-Process -Name "AssertAutoIgnore" -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath $exe -WindowStyle Hidden
    Write-Host "AssertAutoIgnore started"
} else {
    Write-Host "AssertAutoIgnore already running"
}
```

---

## Step 3 — Start the JetBrains debug session

Call `mcp__jetbrains-debugger__start_debug_session`:
- `configuration_name`: the debug run configuration resolved in Project resolution
- `project_path`: the path resolved in Project resolution

Note the session ID from the response — report it to the user.

The debug launch profile is expected to include `Bannerlord.GABS` in the module list
and set `GABP_SERVER_PORT`, `GABP_TOKEN`, `GABS_GAME_ID`, and `GABS_BRIDGE_PATH` as env
vars — so the game starts the GABP bridge on that port with the token written in Step 2.
If the profile lacks them, stop and tell the user; GABS cannot connect without them.

---

## Step 4 — Wait for GABP bridge

Poll `games_connect` with `gameId: "bannerlord"`, `timeout: 15` every ~15 seconds until
it succeeds. AssertAutoIgnore (started in Step 2) handles any Safe Mode popups in the
background — no special handling needed here.

**Readiness rule:** `games_connect` is the authoritative GABP-readiness check. A Rider
session with zero threads/frames and `games_status == stopped` do **not** prove that the
Bannerlord window has not reached the main menu; they can simply mean GABP has not
registered yet. If the user reports that the game is at the menu, retry `games_connect`
immediately before diagnosing a launch failure. `games_connect` confirms only the bridge;
Step 5 confirms the UI state.

- **Connected** → follow immediately with `games_status` to confirm, then go to Step 5.
- **Timeout / connection refused** → game is still loading. Wait 15 s and retry.
- **`games_status` returns `stopped`** → GABP is unavailable, not necessarily that the
  game exited. Verify that the Bannerlord process or debugger session has actually ended
  before treating it as a crash. If it has ended, call
  `mcp__jetbrains-debugger__get_debug_session_status` immediately (50 frames, include
  variables). Act fast — the JetBrains session closes when the process exits.

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
