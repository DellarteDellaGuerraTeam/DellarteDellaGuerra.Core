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

## Steps

### 1. Read current GABS state

Read `C:/Users/Joe/.gabs/bannerlord/runtime.json` to get:
- `ownerPid` -- the GABS session that owns the game
- `gamePid` -- the actual Bannerlord process

If the file does not exist, skip to step 3.

### 2. Kill the game and owner processes

Kill Bannerlord only — do NOT kill the ownerPid, as that process IS the GABS HTTP server:
```powershell
Get-Process -Name "Bannerlord*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "Bannerlord*" -ErrorAction SilentlyContinue
```

If still alive:
  Get-Process -Name "Bannerlord*" | Stop-Process -Force

### 3. Clean the GABS folder

Delete stale files via WSL bash (PowerShell path protection blocks this):
  rm -f /mnt/c/Users/Joe/.gabs/bannerlord/bridge.json /mnt/c/Users/Joe/.gabs/bannerlord/runtime.json

### 4. Restart the GABS HTTP server

The server runs as a background process on port 8080. Kill any stale instance and relaunch:
```powershell
Get-Process -Name "gabs" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Process -FilePath "D:\Bannerlord\GABS-release\gabs.exe" -ArgumentList "server --http localhost:8080" -WindowStyle Hidden
Start-Sleep -Seconds 2
```

Then verify the MCP controller is reachable by calling `mcp__bannerlord-game-controller__games_status`.
If it returns "Unable to connect", wait 2 more seconds and retry once.

Report to the user that the session is clean and the GABS server is ready.

## Notes

- Never kill the GABS HTTP server process during cleanup — kill only Bannerlord.exe.
- The ownerPid in runtime.json IS the GABS server — do not stop it.
