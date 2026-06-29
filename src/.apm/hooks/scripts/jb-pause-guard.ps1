# PreToolUse guard for the bannerlord-game-controller MCP.
# Denies game commands while the Rider debugger is paused, so Claude resolves the
# breakpoint/exception before poking a frozen game. Reads live pause state from
# the dadg-jb-debug-bridge on :7777.
#
# Blocks via the JSON-stdout decision (hookSpecificOutput.permissionDecision=deny)
# rather than exit code 2: when this script is launched as `pwsh -Command "& (...)"`,
# an `exit 2` is swallowed and surfaces as exit 1, which PreToolUse treats as a
# non-blocking error and ignores. JSON-on-stdout works with exit 0, so it is
# immune to that wrapper quirk, and its reason is fed back to the model.

$ErrorActionPreference = 'SilentlyContinue'

function Write-Decision {
    param([string]$Decision, [string]$Reason)
    $payload = @{
        hookSpecificOutput = @{
            hookEventName            = 'PreToolUse'
            permissionDecision       = $Decision
            permissionDecisionReason = $Reason
        }
    } | ConvertTo-Json -Compress -Depth 6
    Write-Output $payload
    exit 0
}

# Derive the src project dir from this script's own location, so the guard does
# not depend on CLAUDE_PROJECT_DIR being set/expanded correctly by whatever shell
# launched it. This script may live under .claude/hooks or .apm/hooks/scripts.
$root = $null
$dir = (Resolve-Path $PSScriptRoot).Path
while ($dir) {
    if ((Test-Path (Join-Path $dir 'DellarteDellaGuerra.sln')) -and (Test-Path (Join-Path $dir '.mcp.json'))) {
        $root = $dir
        break
    }
    $parent = Split-Path -Parent $dir
    if ([string]::IsNullOrEmpty($parent) -or $parent -eq $dir) { break }
    $dir = $parent
}
if (-not $root) {
    $root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}

# Rider registers project paths with forward slashes; a backslash Windows path
# returns project_not_found, so normalize.
$projectDir = ($root -replace '\\', '/')
$uri = "http://localhost:7777/state?path=" + [uri]::EscapeDataString($projectDir)

try {
    $state = (Invoke-WebRequest -UseBasicParsing -TimeoutSec 1 -Uri $uri).Content
} catch {
    # Bridge unreachable / not configured — fail open, never block on infra issues.
    exit 0
}

if ($state -notmatch '"isPaused"\s*:\s*true') {
    exit 0
}

# Paused. Build a compact reason with the location + exception if we can parse it,
# falling back to the raw state on any parse failure.
$detail = $state
try {
    $status = ($state | ConvertFrom-Json).statusText | ConvertFrom-Json
    $loc = $status.currentLocation
    $exc = ($status.variables | Where-Object { $_.name -eq '$exception' } | Select-Object -First 1).value
    $detail = "state=$($status.state) reason=$($status.pausedReason) at $($loc.file):$($loc.line)"
    if ($exc) { $detail += " | exception: $($exc -split "`r?`n" | Select-Object -First 1)" }
} catch {}

Write-Decision 'deny' ("The Rider debugger is PAUSED — do not send game-controller commands; the game is frozen and the call will hang or act on stale state. " +
    "Inspect the stack/exception with the jetbrains-debugger tools, then fix or resume execution before retrying. [$detail]")
