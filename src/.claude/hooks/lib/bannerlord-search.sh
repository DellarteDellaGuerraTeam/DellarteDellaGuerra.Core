#!/bin/bash
# Reads bannerlord-search-* server versions from .mcp.json, restores the
# matching BannerlordSearch.Source NuGet packages, installs the
# BannerlordSearch.Mcp.Server global tool, and starts one HTTP server
# instance per version.
#
# Expected env vars (set by session-start.sh):
#   PROJECT_DIR      — absolute path to the project root
#   CLAUDE_ENV_FILE  — optional; path to the file Claude reads for env exports

MCP_CONFIG="${PROJECT_DIR}/.mcp.json"

if [ ! -f "${MCP_CONFIG}" ]; then
  echo "ERROR: Could not find MCP config at ${MCP_CONFIG}." >&2
  exit 1
fi

# ── Resolve versions from .mcp.json ──────────────────────────────────────────
# File order determines port order.
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

# ── Restore BannerlordSearch.Source for each version ─────────────────────────
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

# Export the first configured version's path for session-level use.
if [ -n "${CONTENT_PATHS[0]:-}" ]; then
  export BANNERLORD_SOURCE_PATH="${CONTENT_PATHS[0]}"
  if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    echo "export BANNERLORD_SOURCE_PATH='${CONTENT_PATHS[0]}'" >> "${CLAUDE_ENV_FILE}"
  fi
fi

# ── Install BannerlordSearch.Mcp.Server global tool (idempotent) ─────────────
if ! dotnet tool list -g 2>/dev/null | grep -qi "BannerlordSearch.Mcp.Server"; then
  echo "Installing BannerlordSearch.Mcp.Server..."
  dotnet tool install --global BannerlordSearch.Mcp.Server
  echo "BannerlordSearch.Mcp.Server installed."
else
  echo "BannerlordSearch.Mcp.Server already installed."
fi

# ── Start one MCP server instance per version ─────────────────────────────────
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

# Wait for each server to become ready (up to 60 s).
# curl without -f: 4xx responses (e.g. "session id required") count as ready.
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
    echo "WARNING: MCP server on port ${PORT} did not become ready within 60 s." >&2
  fi
done
