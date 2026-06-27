# Native Windows SessionStart hook for DADG.
#
# Starts every local service Claude Code depends on, all on the Windows host:
#   1. .NET SDK / runtime sanity check
#   2. Bannerlord-search MCP servers (one per version in .mcp.json) on :5000+
#   3. JetBrains debugger pause monitor ("dadg-jb-debug-bridge") on :7777
#   4. GABS HTTP server (game-controller MCP backend) on :8080
#
# Replaces the previous bash + WSL2 setup. Everything binds Windows localhost
# directly, so .mcp.json (which points at localhost) needs no changes.

$ErrorActionPreference = 'Continue'

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
$TempDir    = $env:TEMP
$ToolsDir   = Join-Path $HOME '.dotnet\tools'

function Test-PortReady {
    param([int]$Port, [int]$TimeoutMs = 1000)
    try {
        $client = [System.Net.Sockets.TcpClient]::new()
        $async  = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        $ok     = $async.AsyncWaitHandle.WaitOne($TimeoutMs)
        if ($ok -and $client.Connected) { $client.EndConnect($async); $client.Close(); return $true }
        $client.Close(); return $false
    } catch { return $false }
}

function Wait-PortReady {
    param([int]$Port, [int]$Seconds = 30)
    for ($i = 0; $i -lt $Seconds; $i++) {
        if (Test-PortReady -Port $Port) { return $true }
        Start-Sleep -Seconds 1
    }
    return $false
}

# ── 1. .NET SDK & runtime ─────────────────────────────────────────────────────
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "ERROR: dotnet not found on PATH. Install the .NET SDK and re-run."
    exit 0
}
Write-Host ".NET SDK present: $(dotnet --version)"
if (-not (dotnet --list-runtimes | Select-String 'Microsoft\.NETCore\.App 8\.')) {
    Write-Host "WARNING: .NET 8 runtime not found — tests target net8.0 and may fail."
} else {
    Write-Host ".NET 8 runtime present."
}

# ── 2. Bannerlord-search MCP servers ──────────────────────────────────────────
$mcpConfigPath = Join-Path $ProjectDir '.mcp.json'
if (-not (Test-Path $mcpConfigPath)) {
    Write-Host "ERROR: .mcp.json not found at $mcpConfigPath — skipping search servers."
} else {
    $mcp = Get-Content $mcpConfigPath -Raw | ConvertFrom-Json
    # Property order is preserved by ConvertFrom-Json, so it sets the port order.
    $versions = @($mcp.mcpServers.PSObject.Properties.Name |
        Where-Object { $_ -match '^bannerlord-search-(.+)$' } |
        ForEach-Object { $Matches[1] })

    if ($versions.Count -eq 0) {
        Write-Host "WARNING: no bannerlord-search-* servers in .mcp.json."
    } else {
        Write-Host "Bannerlord source versions from .mcp.json: $($versions -join ' ')"
        $nugetCache = Join-Path $HOME '.nuget\packages\bannerlordsearch.source'
        $serverExe  = Join-Path $ToolsDir 'BannerlordSearch.Mcp.Server.exe'

        if (-not (Test-Path $serverExe)) {
            Write-Host "Installing BannerlordSearch.Mcp.Server..."
            dotnet tool install --global BannerlordSearch.Mcp.Server | Out-Null
        }

        # Resolve (and restore if missing) the source contentFiles for each version.
        $contentPaths = @{}
        foreach ($v in $versions) {
            $pkgDir  = Join-Path $nugetCache $v
            $content = Join-Path $pkgDir 'contentFiles'
            if (-not (Test-Path $content)) {
                Write-Host "Restoring BannerlordSearch.Source v$v..."
                $tmp = Join-Path $TempDir ("dadg-restore-" + [guid]::NewGuid())
                New-Item -ItemType Directory -Force -Path $tmp | Out-Null
                @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include="BannerlordSearch.Source" Version="$v" /></ItemGroup>
</Project>
"@ | Set-Content -Path (Join-Path $tmp 'tmp.csproj')
                dotnet restore (Join-Path $tmp 'tmp.csproj') 2>&1 | Out-Null
                Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
            }
            if (Test-Path $content) {
                Write-Host "Found source for v${v}: $content"
                $contentPaths[$v] = $content
            } else {
                Write-Host "WARNING: contentFiles for v$v not found — server will be skipped."
            }
        }

        # The server is published against ASP.NET Core 10.0.0 (release). This machine
        # may only have a prerelease v10 runtime (e.g. 10.0.0-rc.2); allow the host to
        # roll forward to it, otherwise startup fails with FrameworkMissing.
        $env:DOTNET_ROLL_FORWARD_TO_PRERELEASE = '1'

        # Only (re)start servers whose port isn't already serving. A previous
        # session's servers persist across Claude Code restarts, so the common case
        # is "all up" — restarting + waiting on them every session blocks (and times
        # out) the session start for no reason.
        $basePort     = 5000
        $startedPorts = @()
        for ($i = 0; $i -lt $versions.Count; $i++) {
            $v = $versions[$i]
            if (-not $contentPaths.ContainsKey($v)) { continue }
            $port = $basePort + $i
            if (Test-PortReady -Port $port) {
                Write-Host "BannerlordSearch.Mcp.Server v$v already up on port $port — skipping."
                continue
            }
            $log  = Join-Path $TempDir "bannerlord-mcp-server-$v.log"
            $env:BANNERLORD_SOURCE_PATH = $contentPaths[$v]
            Start-Process -FilePath $serverExe `
                -ArgumentList @('--transport', 'streamable-http', '--urls', "http://localhost:$port") `
                -RedirectStandardOutput $log -RedirectStandardError "$log.err" `
                -WindowStyle Hidden | Out-Null
            Write-Host "BannerlordSearch.Mcp.Server v$v starting on port $port, log: $log"
            $startedPorts += $port
        }
        Remove-Item Env:\BANNERLORD_SOURCE_PATH -ErrorAction SilentlyContinue

        foreach ($port in $startedPorts) {
            if (Wait-PortReady -Port $port -Seconds 60) {
                Write-Host "MCP server on port $port is ready."
            } else {
                Write-Host "WARNING: MCP server on port $port did not become ready within 60 s."
            }
        }
    }
}

# ── 3. JetBrains Debugger Pause Monitor (dadg-jb-debug-bridge, :7777) ──────────
$bridgePort   = 7777
$bridgeExe    = Join-Path $ToolsDir 'jetbrains-debugger-pause-monitor.exe'
$bridgeLog    = Join-Path $TempDir 'dadg-jb-debug-bridge.log'
# Same endpoint as the jetbrains-debugger server in .mcp.json. Setting it
# explicitly also bypasses the per-request project-dir 'jetbrains-mcp-url' lookup.
$bridgeMcpUrl = if ($env:JETBRAINS_MCP_URL) { $env:JETBRAINS_MCP_URL } else { 'http://127.0.0.1:29202/debugger-mcp/streamable-http' }

if (-not (Test-Path $bridgeExe)) {
    Write-Host "Installing JetBrainsDebuggerPauseMonitor..."
    dotnet tool install --global JetBrainsDebuggerPauseMonitor | Out-Null
}

# Some published builds ship without nlog.config and crash on startup. Provision
# a minimal one next to the assembly if missing (idempotent, version-agnostic).
$bridgeDll = Get-ChildItem -Recurse (Join-Path $HOME '.dotnet\tools\.store') `
    -Filter 'JetBrainsDebuggerPauseMonitor.dll' -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($bridgeDll) {
    $nlogPath = Join-Path $bridgeDll.DirectoryName 'nlog.config'
    if (-not (Test-Path $nlogPath)) {
        @"
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <targets>
    <target name="console" xsi:type="Console"
            layout="`${longdate} `${level:lowercase=true}: `${logger}[`${event-properties:item=EventId_Id:whenEmpty=0}] `${message} `${exception:format=tostring}" />
  </targets>
  <rules>
    <logger name="*" minlevel="Info" writeTo="console" />
  </rules>
</nlog>
"@ | Set-Content -Path $nlogPath -Encoding UTF8
        Write-Host "Provisioned missing nlog.config at $nlogPath."
    }
}

# Drop any WSL2-forwarded bridge from the old setup so the native one owns :7777
# (a WSL2 instance can't reach Rider at 127.0.0.1 and would only answer 503).
if (Get-Command wsl.exe -ErrorAction SilentlyContinue) {
    try { wsl.exe -e pkill -f jetbrains-debugger-pause-monitor 2>$null } catch {}
    Start-Sleep -Seconds 1
}

if (Test-PortReady -Port $bridgePort) {
    Write-Host "dadg-jb-debug-bridge already running on port $bridgePort — skipping."
} else {
    Write-Host "Starting dadg-jb-debug-bridge on port $bridgePort..."
    $env:JETBRAINS_MONITOR_PORT = "$bridgePort"
    $env:JETBRAINS_MCP_URL      = $bridgeMcpUrl
    Start-Process -FilePath $bridgeExe `
        -RedirectStandardOutput $bridgeLog -RedirectStandardError "$bridgeLog.err" `
        -WindowStyle Hidden | Out-Null
    if (Wait-PortReady -Port $bridgePort -Seconds 30) {
        Write-Host "dadg-jb-debug-bridge is ready on port $bridgePort."
    } else {
        Write-Host "WARNING: dadg-jb-debug-bridge did not become ready (Rider may not be running yet — it will connect when the IDE starts)."
    }
}

# ── 4. GABS HTTP server (:8080) ───────────────────────────────────────────────
$gabsPort = 8080
$gabsExe  = 'D:\Bannerlord\GABS-release\gabs.exe'
if (Test-PortReady -Port $gabsPort) {
    Write-Host "GABS HTTP server already reachable on port $gabsPort — skipping."
} elseif (-not (Test-Path $gabsExe)) {
    Write-Host "WARNING: gabs.exe not found at $gabsExe — game-controller MCP unavailable."
} else {
    Write-Host "Starting GABS HTTP server on port $gabsPort ($gabsExe)..."
    Get-Process -Name 'gabs' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Start-Process -FilePath $gabsExe -ArgumentList @('server', '--http', "localhost:$gabsPort") -WindowStyle Hidden | Out-Null
    if (Wait-PortReady -Port $gabsPort -Seconds 30) {
        Write-Host "GABS HTTP server ready on port $gabsPort."
    } else {
        Write-Host "WARNING: GABS HTTP server did not become ready within 30 s."
    }
}

Write-Host "Session start hook completed."

# Non-fatal warnings are reported inline above; never fail the hook on a stray
# $LASTEXITCODE left behind by a native helper (e.g. pkill finding nothing).
exit 0
