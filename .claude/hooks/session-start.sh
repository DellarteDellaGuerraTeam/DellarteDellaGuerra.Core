#!/bin/bash
set -euo pipefail

# On Windows/Git Bash: delegate the full hook to WSL2 and exit.
# WSL2 automatically forwards bound ports to Windows localhost, so MCP servers
# started inside WSL2 are reachable by Claude Code on Windows.
if [ "$(uname -s)" != "Linux" ]; then
  if ! command -v wsl.exe >/dev/null 2>&1; then
    echo "Skipping hook: not Linux and WSL2 not available."
    exit 0
  fi
  SCRIPT_PATH="${BASH_SOURCE[0]}"
  # Normalise any Windows/Git-Bash path format to a WSL2 /mnt/<drive>/... path.
  if [[ "${SCRIPT_PATH}" =~ ^/mnt/[a-zA-Z]/ ]]; then
    WSL_SCRIPT="${SCRIPT_PATH}"                          # already WSL2
  elif [[ "${SCRIPT_PATH}" =~ ^/[a-zA-Z]/ ]]; then
    # Git Bash style (/d/foo) — convert to /mnt/d/foo without wslpath.
    DRIVE=$(echo "${SCRIPT_PATH:1:1}" | tr A-Z a-z)
    WSL_SCRIPT="/mnt/${DRIVE}${SCRIPT_PATH:2}"
  else
    # Windows style (D:/foo or D:\foo) — use wslpath.
    # MSYS_NO_PATHCONV=1 prevents Git Bash from mangling the argument before
    # it reaches wsl.exe.
    WIN_SCRIPT=$(cygpath -w "${SCRIPT_PATH}" 2>/dev/null || echo "${SCRIPT_PATH}")
    WSL_SCRIPT=$(MSYS_NO_PATHCONV=1 wsl.exe wslpath -u "${WIN_SCRIPT}" 2>/dev/null | tr -d '\r' || true)
  fi
  if [ -z "${WSL_SCRIPT}" ]; then
    echo "Skipping hook: could not resolve WSL2 path."
    exit 0
  fi
  echo "Delegating to WSL2: ${WSL_SCRIPT}"
  # MSYS_NO_PATHCONV=1 prevents Git Bash from converting /mnt/d/... to
  # C:/Program Files/Git/mnt/d/... before the path reaches wsl.exe.
  MSYS_NO_PATHCONV=1 wsl.exe bash "${WSL_SCRIPT}"
  exit $?
fi

# ── 1. Install .NET SDK if missing ─────────────────────────────────────────
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Installing .NET SDK 10.0 via apt..."
  apt-get update -qq
  apt-get install -y dotnet-sdk-10.0
  echo ".NET SDK installed: $(dotnet --version)"
else
  echo ".NET SDK already present: $(dotnet --version)"
fi

# Tests target net8.0 — ensure the runtime is available
if ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.NETCore.App 8\."; then
  echo "Installing .NET 8 runtime..."
  apt-get install -y dotnet-runtime-8.0 2>/dev/null || apt-get update -qq && apt-get install -y dotnet-runtime-8.0
  echo ".NET 8 runtime installed."
else
  echo ".NET 8 runtime already present."
fi

# dotnet global tools live in ~/.dotnet/tools — add to PATH
export PATH="${HOME}/.dotnet/tools:${PATH}"
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  echo "export PATH=${HOME}/.dotnet/tools:\${PATH}" >> "${CLAUDE_ENV_FILE}"
fi

# ── 2. Resolve Bannerlord source versions ──────────────────────────────────
# Derive project root from this script's location (.claude/hooks/ → ../../)
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MCP_CONFIG="${PROJECT_DIR}/.mcp.json"

if [ ! -f "${MCP_CONFIG}" ]; then
  echo "ERROR: Could not find MCP config at ${MCP_CONFIG}." >&2
  exit 1
fi

# Use .mcp.json as the source of truth. File order determines port order.
mapfile -t ALL_VERSIONS < <(
  awk '
    /"bannerlord-search-[0-9]+\.[0-9]+\.[0-9]+"/ {
      version = $0
      sub(/.*"bannerlord-search-/, "", version)
      sub(/".*/, "", version)
      print version
    }
  ' "${MCP_CONFIG}"
)

if [ "${#ALL_VERSIONS[@]}" -eq 0 ]; then
  echo "ERROR: No bannerlord-search-* MCP servers found in ${MCP_CONFIG}." >&2
  exit 1
fi

echo "Bannerlord source versions from .mcp.json: ${ALL_VERSIONS[*]}"
BASE_PORT=5000

# ── 3. Restore BannerlordSearch.Source for each version ────────────────────
NUGET_CACHE="${HOME}/.nuget/packages"
CONTENT_PATHS=()  # parallel array to ALL_VERSIONS

for VERSION in "${ALL_VERSIONS[@]}"; do
  SOURCE_PKG_DIR="${NUGET_CACHE}/bannerlordsearch.source/${VERSION}"

  if [ ! -d "${SOURCE_PKG_DIR}" ]; then
    echo "Restoring BannerlordSearch.Source v${VERSION}..."
    TEMP_DIR=$(mktemp -d)
    cat > "${TEMP_DIR}/tmp.csproj" << CSPROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BannerlordSearch.Source" Version="${VERSION}" />
  </ItemGroup>
</Project>
CSPROJ
    dotnet restore "${TEMP_DIR}/tmp.csproj" 2>&1 \
      || echo "WARNING: Could not restore BannerlordSearch.Source v${VERSION}."
    rm -rf "${TEMP_DIR}"
  else
    echo "BannerlordSearch.Source v${VERSION} already in NuGet cache."
  fi

  CONTENT=$(find "${NUGET_CACHE}" \
    -ipath "*bannerlordSearch.source/${VERSION}/contentfiles*" \
    -type d -print -quit 2>/dev/null || true)
  if [ -z "${CONTENT}" ]; then
    CONTENT=$(find "${NUGET_CACHE}" \
      -ipath "*bannerlord*source*${VERSION}*contentfiles*" \
      -type d -print -quit 2>/dev/null || true)
  fi

  if [ -n "${CONTENT}" ]; then
    echo "Found source for v${VERSION}: ${CONTENT}"
  else
    echo "WARNING: Could not locate contentFiles for v${VERSION}."
  fi
  CONTENT_PATHS+=("${CONTENT:-}")
done

# Export the first configured version's path for session-level use
if [ -n "${CONTENT_PATHS[0]:-}" ]; then
  export BANNERLORD_SOURCE_PATH="${CONTENT_PATHS[0]}"
  if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    echo "export BANNERLORD_SOURCE_PATH='${CONTENT_PATHS[0]}'" >> "${CLAUDE_ENV_FILE}"
  fi
fi

# ── 4. Install BannerlordSearch.Mcp.Server global tool (idempotent) ──────────
if ! dotnet tool list -g 2>/dev/null | grep -qi "BannerlordSearch.Mcp.Server"; then
  echo "Installing BannerlordSearch.Mcp.Server..."
  dotnet tool install --global BannerlordSearch.Mcp.Server
  echo "BannerlordSearch.Mcp.Server installed."
else
  echo "BannerlordSearch.Mcp.Server already installed."
fi

# ── 5. Start one MCP server instance per version ───────────────────────────
TOOL_CMD=$(dotnet tool list -g 2>/dev/null | grep -i "BannerlordSearch.Mcp.Server" | awk '{print $NF}')
if [ -z "${TOOL_CMD}" ]; then
  echo "ERROR: BannerlordSearch.Mcp.Server not found in global tools. Install may have failed." >&2
  exit 1
fi
echo "Resolved MCP server command: ${TOOL_CMD}"

pkill -f "BannerlordSearch.Mcp.Server" 2>/dev/null || true
sleep 1

STARTED_PORTS=()
for i in "${!ALL_VERSIONS[@]}"; do
  VERSION="${ALL_VERSIONS[$i]}"
  PORT=$((BASE_PORT + i))
  CONTENT="${CONTENT_PATHS[$i]:-}"

  if [ -z "${CONTENT}" ]; then
    echo "Skipping MCP server for v${VERSION} — source not found."
    continue
  fi

  MCP_LOG="/tmp/bannerlord-mcp-server-${VERSION}.log"
  env BANNERLORD_SOURCE_PATH="${CONTENT}" \
    nohup "${TOOL_CMD}" \
      --transport streamable-http \
      --urls "http://localhost:${PORT}" \
    > "${MCP_LOG}" 2>&1 &

  echo "BannerlordSearch.Mcp.Server v${VERSION} starting on port ${PORT} (PID: $!), log: ${MCP_LOG}"
  STARTED_PORTS+=("${PORT}")
done

# Wait for each started server to become ready (up to 60s).
# Use curl without -f so 4xx responses (e.g. "session id required") count as ready.
for PORT in "${STARTED_PORTS[@]}"; do
  ready=0
  for i in $(seq 1 60); do
    if curl -s "http://localhost:${PORT}" >/dev/null 2>&1; then
      echo "MCP server on port ${PORT} is ready."
      ready=1
      break
    fi
    sleep 1
  done
  if [ "${ready}" -eq 0 ]; then
    echo "WARNING: MCP server on port ${PORT} did not become ready within 60 seconds." >&2
  fi
done

# ── 6. Start JB Debug Bridge (global tool) ─────────────────────────────────────
# Env vars read by the tool:
#   JETBRAINS_MONITOR_PORT — local HTTP port for /state (default: 7777)
#   JETBRAINS_MCP_URL      — optional global fallback MCP URL; prefer a per-project
#                            'jetbrains-mcp-url' file instead (multiple IDE instances
#                            each have their own port, so a global default is unreliable)
BRIDGE_PORT="${JETBRAINS_MONITOR_PORT:-7777}"
BRIDGE_LOG="/tmp/dadg-jb-debug-bridge.log"

if ! dotnet tool list -g 2>/dev/null | grep -qi "JetBrainsDebuggerPauseMonitor"; then
  echo "Installing JetBrainsDebuggerPauseMonitor..."
  dotnet tool install --global JetBrainsDebuggerPauseMonitor
  echo "JetBrainsDebuggerPauseMonitor installed."
fi

BRIDGE_CMD=$(dotnet tool list -g 2>/dev/null | grep -i "JetBrainsDebuggerPauseMonitor" | awk '{print $NF}')

if [ -z "${BRIDGE_CMD}" ]; then
  echo "WARNING: jetbrains-debugger-pause-monitor not found in global dotnet tools — skipping."
else
  # If the bridge is already up and responding, leave it alone.
  if curl -s --max-time 1 "http://localhost:${BRIDGE_PORT}/state" >/dev/null 2>&1; then
    echo "dadg-jb-debug-bridge already running on port ${BRIDGE_PORT} — skipping."
  else
    echo "Starting dadg-jb-debug-bridge on port ${BRIDGE_PORT}..."
    env JETBRAINS_MONITOR_PORT="${BRIDGE_PORT}" \
      nohup "${BRIDGE_CMD}" \
      > "${BRIDGE_LOG}" 2>&1 &

    echo "dadg-jb-debug-bridge starting (PID: $!), log: ${BRIDGE_LOG}"

    # Wait up to 30s for the server to respond to any request.
    # /state without ?path= returns 400 by design — that still means the server is up,
    # so we use curl without -f to treat any HTTP response as success.
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
      echo "WARNING: dadg-jb-debug-bridge did not become ready within 30s (JetBrains may not be running yet — bridge will connect when IDE starts)."
    fi
  fi
fi

echo "Session start hook completed."
