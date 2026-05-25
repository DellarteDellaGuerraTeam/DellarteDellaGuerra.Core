#!/bin/bash
# Ensures the GABS HTTP server (game-controller MCP backend) is running.
# Starts gabs.exe via Windows interop if it is not already reachable.
#
# No expected env vars — all configuration is hard-coded here or derived
# from well-known defaults.

GABS_HTTP_PORT=8080
GABS_EXE='D:\Bannerlord\GABS-release\gabs.exe'

if curl -s --max-time 2 "http://localhost:${GABS_HTTP_PORT}/mcp" >/dev/null 2>&1; then
  echo "GABS HTTP server already reachable on port ${GABS_HTTP_PORT} — skipping."
  return 0
fi

if ! command -v powershell.exe >/dev/null 2>&1; then
  echo "WARNING: powershell.exe not found — cannot start GABS HTTP server."
  return 0
fi

echo "Starting GABS HTTP server on port ${GABS_HTTP_PORT} (${GABS_EXE})..."

# gabs.exe is a Windows binary; launch it through WSL2 Windows interop.
# Kill any stale instance first, then start fresh.
# Single-line command avoids bash multi-line quoting edge-cases.
if ! powershell.exe -NoProfile -Command "Get-Process -Name 'gabs' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Seconds 1; Start-Process -FilePath '${GABS_EXE}' -ArgumentList 'server --http localhost:${GABS_HTTP_PORT}' -WindowStyle Hidden"; then
  echo "WARNING: powershell.exe returned a non-zero exit code — GABS may not have started."
fi

ready=0
for i in $(seq 1 30); do
  if curl -s --max-time 1 "http://localhost:${GABS_HTTP_PORT}/mcp" >/dev/null 2>&1; then
    echo "GABS HTTP server ready on port ${GABS_HTTP_PORT}."
    ready=1
    break
  fi
  sleep 1
done
if [ "${ready}" -eq 0 ]; then
  echo "WARNING: GABS HTTP server did not become ready within 30 s — bannerlord-game-controller MCP may be unavailable."
fi
