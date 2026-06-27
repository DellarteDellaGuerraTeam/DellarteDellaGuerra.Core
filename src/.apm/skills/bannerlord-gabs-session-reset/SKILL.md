---
name: bannerlord-gabs-session-reset
description: >
  Bannerlord + GABS session cleanup. Use this skill whenever the game is stuck,
  a GABS session owns the process and will not release it, or the user says things
  like "reset bannerlord", "clean restart", "kill and restart", "session is stuck",
  "can''t connect to game", "clean up gabs", or "start fresh".
  Also use when games_connect fails because another session owns the process.
---

# bannerlord-gabs-session-reset

Kill Bannerlord and reset the GABS session to a clean state.

## Why this is needed

The GABS bridge system tracks ownership via runtime.json. If a session crashes
or is abandoned, the stale ownership record blocks new connections.

The GABS HTTP server lifecycle (port 8080) is managed by the **session-start hook**,
which starts it automatically and idempotently at the beginning of every Claude session.
This skill only cleans the game side — it does not kill or restart the GABS server
unless the server is found to be unreachable.

## Steps

### 1. Read current GABS state

Read `C:/Users/Joe/.gabs/bannerlord/runtime.json` to get:
- `ownerPid` -- the GABS session that owns the game
- `gamePid` -- the actual Bannerlord process

If the file does not exist, skip to step 3.

### 2. Kill the Bannerlord process

Kill Bannerlord only — do NOT touch the GABS HTTP server:
```powershell
Get-Process -Name "Bannerlord*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "Bannerlord*" -ErrorAction SilentlyContinue
```

If still alive:
  Get-Process -Name "Bannerlord*" | Stop-Process -Force

### 3. Clean the GABS bridge files

Delete stale files with PowerShell:

```powershell
$dir = Join-Path $HOME ".gabs\bannerlord"
foreach ($name in @("bridge.json", "runtime.json")) {
    $path = Join-Path $dir $name
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}
```

### 4. Verify the GABS server is reachable

Call `mcp__bannerlord-game-controller__games_status`.

- **Responds** → GABS is healthy. Report success and stop here.
- **"Unable to connect"** → the server is not running (e.g. it was never started or crashed).
  Restart it and retry:

```powershell
Get-Process -Name "gabs" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Start-Process -FilePath "D:\Bannerlord\GABS-release\gabs.exe" -ArgumentList "server --http localhost:8080" -WindowStyle Hidden
Start-Sleep -Seconds 3
```

Then call `games_status` again. If it still fails, report the error so the user can
investigate (log file: check Windows Event Viewer or the process stdout).

Report to the user that the session is clean and the GABS server is ready.

## Notes

- **Never kill the GABS HTTP server as part of the normal reset** — only kill Bannerlord.exe.
- The session-start hook ensures gabs.exe is already running at session open; the reset
  skill only falls back to restarting it when `games_status` confirms it is unreachable.
- The `ownerPid` in runtime.json IS the GABS server process — never stop it proactively.
