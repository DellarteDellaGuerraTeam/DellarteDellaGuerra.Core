#!/bin/bash
set -euo pipefail

DOTNET_INSTALL_DIR="${HOME}/.dotnet"
export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
export PATH="${DOTNET_INSTALL_DIR}:${DOTNET_INSTALL_DIR}/tools:${PATH}"

# ── 1. Install .NET SDK if missing ─────────────────────────────────────────
if ! command -v dotnet >/dev/null 2>&1 || [ ! -f "${DOTNET_INSTALL_DIR}/dotnet" ]; then
  echo "Installing .NET SDK..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh \
    | bash -s -- --channel 10.0 --install-dir "${DOTNET_INSTALL_DIR}"
  echo ".NET SDK installed."
else
  echo ".NET SDK already present: $(dotnet --version)"
fi

# Persist dotnet on PATH for the session (only when running inside Claude Code)
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  echo "export DOTNET_ROOT=${DOTNET_INSTALL_DIR}" >> "${CLAUDE_ENV_FILE}"
  echo "export PATH=${DOTNET_INSTALL_DIR}:${DOTNET_INSTALL_DIR}/tools:\${PATH}" >> "${CLAUDE_ENV_FILE}"
fi

# ── 2. Restore BannerlordSearch.Source into NuGet global cache ─────────────
BANNERLORD_SOURCE_VERSION="1.2.12"
NUGET_CACHE="${HOME}/.nuget/packages"
SOURCE_PKG_DIR="${NUGET_CACHE}/bannerlordSearch.source/${BANNERLORD_SOURCE_VERSION}"

if [ ! -d "${SOURCE_PKG_DIR}" ]; then
  echo "Restoring BannerlordSearch.Source v${BANNERLORD_SOURCE_VERSION}..."
  TEMP_DIR=$(mktemp -d)
  trap "rm -rf ${TEMP_DIR}" EXIT

  cat > "${TEMP_DIR}/tmp.csproj" << CSPROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BannerlordSearch.Source" Version="${BANNERLORD_SOURCE_VERSION}" />
  </ItemGroup>
</Project>
CSPROJ

  dotnet restore "${TEMP_DIR}/tmp.csproj" 2>&1
  echo "BannerlordSearch.Source restored."
else
  echo "BannerlordSearch.Source already in NuGet cache."
fi

# ── 3. Locate contentFiles and export BANNERLORD_SOURCE_PATH ───────────────
SOURCE_CONTENTFILES=$(find "${NUGET_CACHE}" \
  -ipath "*bannerlordSearch.source/${BANNERLORD_SOURCE_VERSION}/contentfiles*" \
  -type d -print -quit)

if [ -z "${SOURCE_CONTENTFILES}" ]; then
  # Broader fallback search
  SOURCE_CONTENTFILES=$(find "${NUGET_CACHE}" \
    -ipath "*bannerlord*source*${BANNERLORD_SOURCE_VERSION}*contentfiles*" \
    -type d -print -quit)
fi

if [ -n "${SOURCE_CONTENTFILES}" ]; then
  echo "BANNERLORD_SOURCE_PATH=${SOURCE_CONTENTFILES}"
  if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    echo "export BANNERLORD_SOURCE_PATH=${SOURCE_CONTENTFILES}" >> "${CLAUDE_ENV_FILE}"
  fi
  export BANNERLORD_SOURCE_PATH="${SOURCE_CONTENTFILES}"
else
  echo "WARNING: Could not locate BannerlordSearch.Source contentFiles in NuGet cache."
fi

# ── 4. Install BannerlordSearch.Mcp.Server global tool (idempotent) ──────────
if ! dotnet tool list -g 2>/dev/null | grep -qi "BannerlordSearch.Mcp.Server"; then
  echo "Installing BannerlordSearch.Mcp.Server..."
  dotnet tool install --global BannerlordSearch.Mcp.Server
  echo "BannerlordSearch.Mcp.Server installed."
else
  echo "BannerlordSearch.Mcp.Server already installed."
fi

# ── 5. Start MCP server on http://localhost:5000 (streamable HTTP) ────────────
MCP_LOG="/tmp/bannerlord-mcp-server.log"

# Resolve the actual command name registered by the global tool
# (dotnet tool run is for local manifest tools only; global tools are called directly)
TOOL_CMD=$(dotnet tool list -g 2>/dev/null | grep -i "BannerlordSearch.Mcp.Server" | awk '{print $NF}')
if [ -z "${TOOL_CMD}" ]; then
  echo "ERROR: BannerlordSearch.Mcp.Server not found in global tools. Install may have failed." >&2
  exit 1
fi
echo "Resolved MCP server command: ${TOOL_CMD}"

if curl -s "http://localhost:5000" >/dev/null 2>&1; then
  echo "BannerlordSearch.Mcp.Server already running on http://localhost:5000, skipping start."
else
  pkill -f "BannerlordSearch.Mcp.Server" 2>/dev/null || true
  sleep 1

  nohup "${TOOL_CMD}" \
    --transport streamable-http \
    --urls "http://localhost:5000" \
    > "${MCP_LOG}" 2>&1 &

  MCP_PID=$!
  echo "BannerlordSearch.Mcp.Server starting (PID: ${MCP_PID}), log: ${MCP_LOG}"

  # Wait for server to become ready (up to 60s).
  # Use curl without -f so that 4xx responses (e.g. 400 "session id required")
  # are still treated as "server is up".
  ready=0
  for i in $(seq 1 60); do
    if curl -s "http://localhost:5000" >/dev/null 2>&1; then
      echo "MCP server is ready."
      ready=1
      break
    fi
    sleep 1
  done

  if [ "${ready}" -eq 0 ]; then
    echo "ERROR: MCP server did not become ready within 60 seconds. Check ${MCP_LOG}." >&2
    exit 1
  fi
fi

echo "Session start hook completed."
