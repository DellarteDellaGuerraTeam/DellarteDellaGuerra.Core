---
name: bannerlord-gabs-start
description: >
  Launch the DellarteDellaGuerra Bannerlord mod under the JetBrains debugger AND
  make it a fully GABS-managed instance so all GABP tools work. Use this skill
  whenever the user wants to start Bannerlord for debugging, says things like
  "launch the debug config", "start dadg debug", "start bannerlord with debugger",
  "launch dadg standalone", "debug bannerlord", or "start the jetbrains debug config".
  Also use it when the user wants to both have the debugger attached AND be able to
  control the game via GABS tools. This skill handles the full setup — bridge.json,
  runtime state, JetBrains session, and GABS connection — in one go.
---

# DellarteDellaGuerra Debug Launch

Launches Bannerlord via the JetBrains "DellarteDellaGuerra.Integration: Standalone"
debug config and connects GABS to the running instance. By the end the debugger is
attached and all GABP tools are live.

---

## Step 1 — Check if already running

Call `mcp__bannerlord-game-controller__games_status` with `gameId: "bannerlord"`.

- If **already running and GABP connected** — skip Steps 2–4, go straight to Step 5 (game state check).
- If **running but GABP disconnected** — skip Steps 2–3, go to Step 4 (games_connect) and check if the JetBrains session is still live; if not, attach the debugger first (see crash detection note).
- If **not running** — continue with Steps 2–4.

---

## Step 2 — Prepare the GABS files (PowerShell only)

The Read/Write/Edit tools operate on the Linux side of this environment. These files
live on Windows where GABS reads them — use the `PowerShell` tool for both.

Rewrite bridge.json (always — GABS may have overwritten it with a random port/token
from a previous managed session) and clear any stale runtime.json in one call:

```powershell
$dir = Join-Path $HOME ".gabs\bannerlord"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$bridgePath = Join-Path $dir "bridge.json"
$runtimePath = Join-Path $dir "runtime.json"
$content = '{"port": 4825, "token": "80c93be27f02d35bedbe46493ce7a12f3d9976cc195cce2ece727cfdd0f09f9d", "gameId": "bannerlord"}'
Set-Content -Path $bridgePath -Value $content -NoNewline
Remove-Item $runtimePath -ErrorAction SilentlyContinue
Write-Host "GABS files ready"
```

---

## Step 3 — Start the JetBrains debug session

Call `mcp__jetbrains-debugger__start_debug_session`:
- `configuration_name`: `"DellarteDellaGuerra.Integration: Standalone"`
- `project_path`: `"D:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord-v1.3/Modules/DellarteDellaGuerra.Core/src"`

Note the session ID from the response — report it to the user.

The launchSettings.json Standalone profile already includes `Bannerlord.GABS` in the
module list and sets `GABP_SERVER_PORT=4825`, `GABP_TOKEN`, `GABS_GAME_ID`, and
`GABS_BRIDGE_PATH` as env vars — so the game will start the GABP bridge on port 4825
with the token written in Step 2. No further configuration needed.

---

## Step 4 — Connect GABS

Call `mcp__bannerlord-game-controller__games_connect`:
- `gameId`: `"bannerlord"`
- `timeout`: `90`

The game takes 60–90 s to load. `games_connect` may return a timeout error even though
the connection succeeded in the background — GABS keeps retrying after the tool returns.
Don't treat a timeout as a hard failure. Always follow up with `games_status` to get
the ground truth.

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
point during this skill, immediately call `mcp__jetbrains-debugger__get_stack_trace`
before the debug session closes. The debugger pauses at the exception and the stack
trace is only available in that window — don't skip this step or the crash cause is lost.

Report to the user:
- JetBrains debug session ID
- Whether the game was already running or freshly launched
- GABS connection status
- Save loaded (if any) and game state confirmed
- Any active blockers
