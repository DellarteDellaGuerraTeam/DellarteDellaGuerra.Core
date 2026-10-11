param([switch]$GuiOnly, [switch]$IncludeEditor, [switch]$IncludeAtlasSource)
$ErrorActionPreference = 'Stop'
function Get-Sha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '') }
    finally { $stream.Dispose(); $algorithm.Dispose() }
}
$sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$mapRoot = [IO.Path]::GetFullPath((Join-Path $sourceRoot '../../DellarteDellaGuerraMap'))
$buildRoot = Join-Path $sourceRoot '.codex-work/ui-asset-skin/build/DadgRecruitment'
$texture = Join-Path $mapRoot 'Assets/GauntletUI/ui_dadg_recruitment_1_tex.tpac'
if (!(Test-Path -LiteralPath $texture)) { throw 'Import the source atlas in the editor before deploying GUI registration.' }
$backup = Join-Path $sourceRoot ('.codex-work/ui-asset-skin/deploy-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
$copies = @(
    @((Join-Path $buildRoot 'GUI/DadgRecruitmentSpriteData.xml'), (Join-Path $mapRoot 'GUI/DadgRecruitmentSpriteData.xml')),
    @((Join-Path $PSScriptRoot 'DADGRecruitment.xml'), (Join-Path $mapRoot 'GUI/Brushes/DADGRecruitment.xml'))
)
# Preserve the sprite sources separately from legacy SpriteParts generator inputs.
if ($IncludeAtlasSource) {
    $copies += ,@((Join-Path $buildRoot 'AssetSources/GauntletUI/ui_dadg_recruitment_1.png'),
                  (Join-Path $mapRoot 'AssetSources/GauntletUI/ui_dadg_recruitment_1.png'))
}
$spriteSource = Join-Path $buildRoot 'GUI/SpriteParts/ui_dadg_recruitment/DADG/Recruitment'
foreach ($spriteFile in Get-ChildItem -LiteralPath $spriteSource -Filter '*.png') {
    $copies += ,@($spriteFile.FullName, (Join-Path $mapRoot ('GUI/DadgRecruitmentSpriteParts/ui_dadg_recruitment/DADG/Recruitment/' + $spriteFile.Name)))
}
if (!$GuiOnly) {
    $platforms = @('Win64_Shipping_Client')
    if ($IncludeEditor) { $platforms += 'Win64_Shipping_wEditor' }
    foreach ($platform in $platforms) {
        foreach ($extension in @('dll', 'pdb')) {
            $name = 'DellarteDellaGuerra.Integration.' + $extension
            $copies += ,@((Join-Path $sourceRoot "DellarteDellaGuerra.Integration/bin/x64/Debug/netstandard2.0/$name"),
                         (Join-Path $sourceRoot "../bin/$platform/$name"))
        }
    }
}
# Check all existing destinations for locks before changing any deployment file.
foreach ($pair in $copies) {
    if (!(Test-Path -LiteralPath $pair[0])) { throw "Missing build input: $($pair[0])" }
    if (Test-Path -LiteralPath $pair[1]) {
        $probe = [IO.File]::Open($pair[1], 'Open', 'ReadWrite', 'None')
        $probe.Dispose()
    }
}
$manifest = @()
foreach ($pair in $copies) {
    $oldFile = $null
    if (Test-Path -LiteralPath $pair[1]) {
        $oldFile = Join-Path $backup ($manifest.Count.ToString() + '-' + [IO.Path]::GetFileName($pair[1]))
        Copy-Item -LiteralPath $pair[1] -Destination $oldFile
    }
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($pair[1])) -Force | Out-Null
    Copy-Item -LiteralPath $pair[0] -Destination $pair[1]
    $hash = Get-Sha256 $pair[1]
    if ($hash -ne (Get-Sha256 $pair[0])) { throw "Copy mismatch: $($pair[1])" }
    $manifest += [pscustomobject]@{ Target = $pair[1]; Backup = $oldFile; SHA256 = $hash }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8
Write-Output "Deployed $($manifest.Count) files. Backup and hashes: $backup"
