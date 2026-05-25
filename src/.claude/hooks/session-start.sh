#!/bin/bash
set -euo pipefail

# ── Windows / Git-Bash guard — delegate the full hook to WSL2 ────────────────
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

# ── Resolve locations (all lib scripts inherit these variables) ───────────────
HOOKS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "${HOOKS_DIR}/../.." && pwd)"
HOOKS_LIB="${HOOKS_DIR}/lib"

# ── 1. .NET SDK & runtimes ────────────────────────────────────────────────────
source "${HOOKS_LIB}/ensure-dotnet.sh"

# ── 2. Bannerlord Search MCP servers ─────────────────────────────────────────
source "${HOOKS_LIB}/bannerlord-search.sh"

# ── 3. JetBrains Debugger Pause Monitor ──────────────────────────────────────
source "${HOOKS_LIB}/jb-debug-bridge.sh"

# ── 4. GABS HTTP server ───────────────────────────────────────────────────────
source "${HOOKS_LIB}/gabs-server.sh"

echo "Session start hook completed."
