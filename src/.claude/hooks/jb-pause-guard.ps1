# PreToolUse guard for the bannerlord-game-controller MCP.
# Blocks game commands (exit 2) while the Rider debugger is paused, so Claude
# resolves the breakpoint/exception before poking the running game.
# Reads live pause state from the dadg-jb-debug-bridge on :7777.

$ErrorActionPreference = 'SilentlyContinue'

# Rider registers project paths with forward slashes; a backslash Windows path
# (what CLAUDE_PROJECT_DIR is) returns project_not_found, so normalize first.
$projectDir = ($env:CLAUDE_PROJECT_DIR -replace '\\', '/')
$uri = "http://localhost:7777/state?path=" + [uri]::EscapeDataString($projectDir)

try {
    $state = (Invoke-WebRequest -UseBasicParsing -TimeoutSec 1 -Uri $uri).Content
} catch {
    # Bridge unreachable / not configured — fail open, never block on infra issues.
    exit 0
}

if ($state -match '"isPaused"\s*:\s*true') {
    Write-Output "[JBDebugBridge] Debugger is paused — handle the exception before sending game commands:"
    Write-Output $state
    exit 2
}
exit 0
