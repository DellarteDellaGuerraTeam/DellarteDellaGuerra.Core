# apm lifecycle workaround for an apm-cli codegen bug.
#
# apm resolves "${PLUGIN_ROOT}" in hook command templates to either the
# literal token "$CLAUDE_PROJECT_DIR" or, non-deterministically on some
# installs, PowerShell's "$env:CLAUDE_PROJECT_DIR" syntax. Claude Code runs
# hook commands through a POSIX shell (git bash on Windows), which expands
# "$CLAUDE_PROJECT_DIR" correctly (it's a real env var Claude Code sets) but
# mangles "$env:CLAUDE_PROJECT_DIR": bash treats "$env" as a (usually unset)
# variable, expands it to empty, and leaves the literal ":CLAUDE_PROJECT_DIR/..."
# suffix behind, so pwsh gets a garbage path and the hook fails outright.
#
# This script runs as an apm lifecycle script (see apm.yml) after every
# `apm install`/`apm update` and does a plain literal-text replace of the
# broken token in .claude/settings.json. It intentionally avoids
# ConvertFrom-Json/ConvertTo-Json: PowerShell collapses single-element JSON
# arrays to bare objects on both the read and write side, which would corrupt
# the hooks array shape Claude Code expects.

$ErrorActionPreference = 'Stop'

$ProjectDir = $null
$dir = (Resolve-Path $PSScriptRoot).Path
while ($dir) {
    if ((Test-Path (Join-Path $dir 'DellarteDellaGuerra.sln')) -and (Test-Path (Join-Path $dir '.mcp.json'))) {
        $ProjectDir = $dir
        break
    }
    $parent = Split-Path -Parent $dir
    if ([string]::IsNullOrEmpty($parent) -or $parent -eq $dir) { break }
    $dir = $parent
}
if (-not $ProjectDir) {
    $ProjectDir = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}

$settingsPath = Join-Path $ProjectDir '.claude\settings.json'
if (-not (Test-Path $settingsPath)) {
    Write-Host "fix-claude-hook-vars: $settingsPath not found, nothing to do."
    exit 0
}

$raw = Get-Content -Raw -Path $settingsPath
$broken = '$env:CLAUDE_PROJECT_DIR'
$working = '$CLAUDE_PROJECT_DIR'

if ($raw.Contains($broken)) {
    $fixed = $raw.Replace($broken, $working)
    [System.IO.File]::WriteAllText($settingsPath, $fixed, [System.Text.UTF8Encoding]::new($false))
    Write-Host "fix-claude-hook-vars: patched $settingsPath (replaced $broken with $working)."
} else {
    Write-Host "fix-claude-hook-vars: $settingsPath already clean."
}

exit 0
