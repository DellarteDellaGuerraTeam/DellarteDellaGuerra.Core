param([string]$BatchDirectory = '.codex-work/ui-asset-skin/shared-surfaces-01')
$ErrorActionPreference = 'Stop'
function Get-Sha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
$sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$batch = [IO.Path]::GetFullPath((Join-Path $sourceRoot $BatchDirectory))
if (!$batch.StartsWith((Join-Path $sourceRoot '.codex-work/'), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Batch must be in the workspace staging directory.'
}
$mapRoot = [IO.Path]::GetFullPath((Join-Path $sourceRoot '../../DellarteDellaGuerraMap'))
$report = Get-Content -LiteralPath (Join-Path $batch 'report.json') -Raw | ConvertFrom-Json
$entries = @([pscustomobject]@{
    Relative = $report.atlas_relative
    Before = $report.source_sha256
    After = $report.output_sha256
})
foreach ($part in $report.parts) {
    if ($part.loose_path) {
        $entries += [pscustomobject]@{ Relative = $part.loose_path; Before = $part.original_loose_sha256; After = $part.output_loose_sha256 }
    }
}
foreach ($brush in $report.brush_overrides) {
    $entries += [pscustomobject]@{ Relative = $brush.relative; Before = $brush.before; After = $brush.after }
}
if ((Get-Sha256 $report.sprite_data) -ne $report.sprite_data_sha256) { throw 'Sprite contract changed.' }
foreach ($consumer in $report.consumers) {
    $consumerPath = Join-Path ([IO.Path]::GetDirectoryName($mapRoot)) $consumer.path
    if ((Get-Sha256 $consumerPath) -ne $consumer.sha256) { throw "Consumer changed: $consumerPath" }
}
foreach ($entry in $entries) {
    $from = [IO.Path]::GetFullPath((Join-Path $batch $entry.Relative))
    $to = [IO.Path]::GetFullPath((Join-Path $mapRoot $entry.Relative))
    $isPng = [IO.Path]::GetExtension($to) -eq '.png'
    $isBrush = [IO.Path]::GetDirectoryName($to) -eq (Join-Path $mapRoot 'GUI\Brushes') -and [IO.Path]::GetExtension($to) -eq '.xml'
    if (!$from.StartsWith($batch + '\', [StringComparison]::OrdinalIgnoreCase) -or
        !$to.StartsWith($mapRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or
        (!$isPng -and !$isBrush)) { throw 'Invalid source/brush deployment path.' }
    $before = if (Test-Path -LiteralPath $to) { Get-Sha256 $to } else { $null }
    if ((Get-Sha256 $from) -ne $entry.After -or $before -ne $entry.Before) {
        throw "Source or destination changed since batch build: $($entry.Relative)"
    }
    if ($before) {
        $probe = [IO.File]::Open($to, 'Open', 'ReadWrite', 'None')
        $probe.Dispose()
    }
}
$backup = Join-Path $sourceRoot ('.codex-work/ui-asset-skin/shared-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$manifest = @()
foreach ($entry in $entries) {
    $from = Join-Path $batch $entry.Relative
    $to = Join-Path $mapRoot $entry.Relative
    $saved = $null
    if ($entry.Before) {
        $saved = Join-Path $backup ($manifest.Count.ToString() + '-' + [IO.Path]::GetFileName($to))
        Copy-Item -LiteralPath $to -Destination $saved
    }
    $manifest += [pscustomobject]@{ Target = $to; Backup = $saved; Before = $entry.Before; After = $entry.After }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8
    Copy-Item -LiteralPath $from -Destination $to
    if ((Get-Sha256 $to) -ne $entry.After) { throw "Copy verification failed: $to" }
}
Write-Output "Updated $($entries.Count) source/brush files; backup: $backup"
Write-Output "Compiled textures unchanged. Import/re-import $($report.editor_reimport) in the editor before runtime verification."
