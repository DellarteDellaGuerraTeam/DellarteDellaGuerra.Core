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

## Step 4 — Wait for GABP bridge (handles Safe Mode + crash detection)

**Do not call `games_connect` with timeout > 15** — long timeouts expire the MCP session.

Instead, use Monitor (timeout 120 000 ms, not persistent) to wait for `runtime.json`
(written by the GABP module inside the game when it is ready). The same monitor
auto-dismisses the "Safe Mode?" dialog that appears after any crash — this dialog keeps
the process at ~76 MB forever until dismissed:

```bash
safe_mode_dismissed=false
until [ -f /mnt/c/Users/Joe/.gabs/bannerlord/runtime.json ]; do
  title=$(powershell.exe -NonInteractive -Command '(Get-Process -Name "Bannerlord" -ErrorAction SilentlyContinue | Select-Object -First 1).MainWindowTitle' 2>/dev/null | tr -d '\r\n')
  if [ -z "$title" ]; then echo "DEAD: game process gone"; exit 1; fi
  if [ "$title" = "Safe Mode" ] && [ "$safe_mode_dismissed" = "false" ]; then
    powershell.exe -NonInteractive -Command '
      Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
      $p = Get-Process -Name "Bannerlord" -ErrorAction SilentlyContinue | Select-Object -First 1
      $root = [System.Windows.Automation.AutomationElement]::RootElement
      $w = $root.FindFirst("Children", (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)))
      $b = $w.FindFirst("Descendants", (New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition(
          [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
          [System.Windows.Automation.ControlType]::Button)),
        (New-Object System.Windows.Automation.PropertyCondition(
          [System.Windows.Automation.AutomationElement]::NameProperty, "No")))))
      if ($b) { $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
    ' 2>/dev/null
    echo "SAFE_MODE_DISMISSED"
    safe_mode_dismissed=true
  fi
  sleep 3
done
echo "GABP_READY"
```

**On each event:**
- **`SAFE_MODE_DISMISSED`** — dialog found and dismissed (always click "No" — runs normally
  with full modules). Game will now continue loading; keep waiting for `GABP_READY`.
- **`GABP_READY`** — GABP module is up. Call `games_connect` with `gameId: "bannerlord"`,
  `timeout: 15`. Follow immediately with `games_status` to confirm.
- **`DEAD` / monitor timeout (120 s)** — game crashed during loading. Call
  `mcp__jetbrains-debugger__get_debug_session_status` immediately (50 frames, include
  variables). Act fast — the window closes when the process exits.

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
