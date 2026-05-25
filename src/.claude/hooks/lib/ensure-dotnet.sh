#!/bin/bash
# Ensures the .NET SDK (10.0) and .NET 8 runtime are present, then adds
# ~/.dotnet/tools to PATH for the current session.
# Expected env vars (set by session-start.sh):
#   CLAUDE_ENV_FILE  — optional; path to the file Claude reads for env exports

if ! command -v dotnet >/dev/null 2>&1; then
  echo "Installing .NET SDK 10.0 via apt..."
  apt-get update -qq
  apt-get install -y dotnet-sdk-10.0
  echo ".NET SDK installed: $(dotnet --version)"
else
  echo ".NET SDK already present: $(dotnet --version)"
fi

# Tests target net8.0 — ensure the runtime is available.
if ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.NETCore.App 8\."; then
  echo "Installing .NET 8 runtime..."
  apt-get install -y dotnet-runtime-8.0 2>/dev/null \
    || { apt-get update -qq && apt-get install -y dotnet-runtime-8.0; }
  echo ".NET 8 runtime installed."
else
  echo ".NET 8 runtime already present."
fi

# dotnet global tools live in ~/.dotnet/tools — add to PATH.
export PATH="${HOME}/.dotnet/tools:${PATH}"
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  echo "export PATH=${HOME}/.dotnet/tools:\${PATH}" >> "${CLAUDE_ENV_FILE}"
fi
