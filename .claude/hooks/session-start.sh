#!/bin/sh
set -eu

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

# Persist dotnet on PATH for the session
echo "export DOTNET_ROOT=${DOTNET_INSTALL_DIR}" >> "${CLAUDE_ENV_FILE}"
echo "export PATH=${DOTNET_INSTALL_DIR}:${DOTNET_INSTALL_DIR}/tools:\${PATH}" >> "${CLAUDE_ENV_FILE}"

# ── 2. Restore BannerlordSearch.Source v1.3.1 into NuGet global cache ──────
NUGET_CACHE="${HOME}/.nuget/packages"
SOURCE_PKG_DIR="${NUGET_CACHE}/bannerlordSearch.source/1.3.1"

if [ ! -d "${SOURCE_PKG_DIR}" ]; then
  echo "Restoring BannerlordSearch.Source v1.3.1..."
  TEMP_DIR=$(mktemp -d)
  trap "rm -rf ${TEMP_DIR}" EXIT

  cat > "${TEMP_DIR}/tmp.csproj" << 'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BannerlordSearch.Source" Version="1.3.1" />
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
  -ipath "*bannerlordSearch.source/1.3.1/contentfiles*" \
  -type d | head -1)

if [ -z "${SOURCE_CONTENTFILES}" ]; then
  # Broader fallback search
  SOURCE_CONTENTFILES=$(find "${NUGET_CACHE}" \
    -ipath "*bannerlord*source*1.3.1*contentfiles*" \
    -type d | head -1)
fi

if [ -n "${SOURCE_CONTENTFILES}" ]; then
  echo "BANNERLORD_SOURCE_PATH=${SOURCE_CONTENTFILES}"
  echo "export BANNERLORD_SOURCE_PATH=${SOURCE_CONTENTFILES}" >> "${CLAUDE_ENV_FILE}"
  export BANNERLORD_SOURCE_PATH="${SOURCE_CONTENTFILES}"
else
  echo "WARNING: Could not locate BannerlordSearch.Source contentFiles in NuGet cache."
fi

# ── 4. Install BannerlordSearch.Mcp.Server global tool (idempotent) ────────
if ! dotnet tool list -g 2>/dev/null | grep -qi "BannerlordSearch.Mcp.Server"; then
  echo "Installing BannerlordSearch.Mcp.Server..."
  dotnet tool install --global BannerlordSearch.Mcp.Server
  echo "BannerlordSearch.Mcp.Server installed."
else
  echo "BannerlordSearch.Mcp.Server already installed."
fi

# ── 5. Start MCP server on http://localhost:5000 (streamable HTTP) ─────────
MCP_LOG="/tmp/bannerlord-mcp-server.log"

if curl -sf "http://localhost:5000" >/dev/null 2>&1 || \
   curl -sf "http://localhost:5000/mcp" >/dev/null 2>&1; then
  echo "BannerlordSearch.Mcp.Server already running on http://localhost:5000, skipping start."
else
  pkill -f "BannerlordSearch.Mcp.Server" 2>/dev/null || true
  sleep 1

  nohup dotnet tool run BannerlordSearch.Mcp.Server -- \
    --transport streamable-http \
    --urls "http://localhost:5000" \
    > "${MCP_LOG}" 2>&1 &

  MCP_PID=$!
  echo "BannerlordSearch.Mcp.Server starting (PID: ${MCP_PID}), log: ${MCP_LOG}"

  # Wait for server to become ready (up to 30s)
  i=1
  while [ "$i" -le 30 ]; do
    if curl -sf "http://localhost:5000" >/dev/null 2>&1 || \
       curl -sf "http://localhost:5000/mcp" >/dev/null 2>&1; then
      echo "MCP server is ready."
      break
    fi
    sleep 1
    i=$((i + 1))
  done
fi

echo "Session start hook completed."
