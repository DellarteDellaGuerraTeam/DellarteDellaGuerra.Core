#!/bin/bash
set -euo pipefail

# Only run in remote (Claude Code on the web) environments
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
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
# Primary version comes from the project's MSBuild GameVersion property
# (tracks supported-game-versions.txt). -p:GameFolder=_ skips game-folder
# resolution so a path like "Mount & Blade II Bannerlord" never reaches bash.
PRIMARY_VERSION=$(dotnet msbuild "${CLAUDE_PROJECT_DIR}/src/DellarteDellaGuerra/DellarteDellaGuerra.csproj" \
  -getProperty:GameVersion -nologo -verbosity:quiet \
  "-p:GameFolder=_" \
  2>/dev/null | grep -Eo '[0-9]+\.[0-9]+\.[0-9]+' | head -1)
if [ -z "${PRIMARY_VERSION}" ]; then
  echo "ERROR: Could not resolve GameVersion from MSBuild." >&2
  exit 1
fi
echo "Primary Bannerlord source version: ${PRIMARY_VERSION}"

# Additional versions — add/remove entries here as needed.
# Each gets its own MCP server instance on a successive port (5001, 5002, ...).
ADDITIONAL_VERSIONS=("1.3.1")

# All versions in port order: primary on BASE_PORT, additional on BASE_PORT+1, ...
ALL_VERSIONS=("${PRIMARY_VERSION}" "${ADDITIONAL_VERSIONS[@]}")
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

# Export the primary version's path for session-level use
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

echo "Session start hook completed."
