#!/bin/bash
# Installs (if needed) and starts the JetBrainsDebuggerPauseMonitor global
# tool, which exposes a local HTTP /state endpoint Claude uses to detect
# whether the Rider debugger is paused before issuing game commands.
#
# Expected env vars (set by session-start.sh):
#   JETBRAINS_MONITOR_PORT — local HTTP port for /state (default: 7777)

BRIDGE_PORT="${JETBRAINS_MONITOR_PORT:-7777}"
BRIDGE_LOG="/tmp/dadg-jb-debug-bridge.log"

if ! dotnet tool list -g 2>/dev/null | grep -qi "JetBrainsDebuggerPauseMonitor"; then
  echo "Installing JetBrainsDebuggerPauseMonitor..."
  dotnet tool install --global JetBrainsDebuggerPauseMonitor
  echo "JetBrainsDebuggerPauseMonitor installed."
fi

BRIDGE_CMD=$(dotnet tool list -g 2>/dev/null | grep -i "JetBrainsDebuggerPauseMonitor" | awk '{print $NF}')

if [ -z "${BRIDGE_CMD}" ]; then
  echo "WARNING: JetBrainsDebuggerPauseMonitor not found in global dotnet tools — skipping."
  return 0
fi

# If the bridge is already up and responding, leave it alone.
if curl -s --max-time 1 "http://localhost:${BRIDGE_PORT}/state" >/dev/null 2>&1; then
  echo "dadg-jb-debug-bridge already running on port ${BRIDGE_PORT} — skipping."
  return 0
fi

echo "Starting dadg-jb-debug-bridge on port ${BRIDGE_PORT}..."
env JETBRAINS_MONITOR_PORT="${BRIDGE_PORT}" \
  nohup "${BRIDGE_CMD}" \
  > "${BRIDGE_LOG}" 2>&1 &

echo "dadg-jb-debug-bridge starting (PID: $!), log: ${BRIDGE_LOG}"

# Wait up to 30 s for the server to respond.
# /state without ?path= returns 400 by design — any HTTP response means it's up.
ready=0
for i in $(seq 1 30); do
  if curl -s --max-time 1 "http://localhost:${BRIDGE_PORT}/state" >/dev/null 2>&1; then
    echo "dadg-jb-debug-bridge is ready on port ${BRIDGE_PORT}."
    ready=1
    break
  fi
  sleep 1
done
if [ "${ready}" -eq 0 ]; then
  echo "WARNING: dadg-jb-debug-bridge did not become ready within 30 s (JetBrains may not be running yet — bridge will connect when IDE starts)."
fi
