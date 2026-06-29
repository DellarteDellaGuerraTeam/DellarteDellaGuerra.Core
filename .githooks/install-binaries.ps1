$ErrorActionPreference = "Stop"

function Show-InstallError {
    param (
        [string]$Message
    )

    $PopupMessage = "Could not install the DellarteDellaGuerra binaries due to:`n`n$Message`n`nThe binaries may be locked by Bannerlord, Rider, or another running process. Close the program using them and try updating the branch again."
    $PopupTitle = "DellarteDellaGuerra binary install failed"

    try {
        Add-Type -AssemblyName PresentationFramework -ErrorAction Stop

        [System.Windows.MessageBox]::Show(
            $PopupMessage,
            $PopupTitle,
            [System.Windows.MessageBoxButton]::OK,
            [System.Windows.MessageBoxImage]::Warning
        ) | Out-Null
    }
    catch {
        try {
            Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop

            [System.Windows.Forms.MessageBox]::Show(
                $PopupMessage,
                $PopupTitle,
                [System.Windows.Forms.MessageBoxButtons]::OK,
                [System.Windows.Forms.MessageBoxIcon]::Warning
            ) | Out-Null
        }
        catch {
            Write-Host "WARNING: Could not install the DellarteDellaGuerra binaries due to:"
            Write-Host $Message
        }
    }
}

# Branches where the binary install should run.
$AllowedBranches = @(
    "develop",
    "develop-tmp1"
)

$Url = "https://github.com/DellarteDellaGuerraTeam/DellarteDellaGuerra.Core/releases/download/latest-develop-bin/latest-develop-bin.zip"

$RepoRoot = (git rev-parse --show-toplevel).Trim()
$CurrentBranch = (git rev-parse --abbrev-ref HEAD).Trim()

Write-Host "Repo root: $RepoRoot"
Write-Host "Current branch: $CurrentBranch"

if ($AllowedBranches -notcontains $CurrentBranch) {
    Write-Host "Not on an allowed branch, skipping binary download."
    exit 0
}

# Repo root is expected to be:
#   GameRoot\Modules\DellarteDellaGuerra.Core
#
# ZIP contains:
#   Modules\DellarteDellaGuerra.Core\...
#
# Therefore:
#   1. Extract ZIP into a temp folder.
#   2. Copy extracted contents into GameRoot.
$GameRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "..\.."))

Write-Host "Game root / installation target: $GameRoot"

if (-not (Test-Path $GameRoot)) {
    $Message = "Game root does not exist: $GameRoot"
    Show-InstallError -Message $Message
    throw $Message
}

$Archive = Join-Path $env:TEMP "latest-develop-bin.zip"
$ExtractDir = Join-Path $env:TEMP "latest-develop-bin-extract"

try {
    if (Test-Path $Archive) {
        Remove-Item $Archive -Force -ErrorAction Stop
    }

    if (Test-Path $ExtractDir) {
        Remove-Item $ExtractDir -Recurse -Force -ErrorAction Stop
    }

    Write-Host "Downloading latest develop binaries..."
    Write-Host "URL: $Url"

    Invoke-WebRequest -Uri $Url -OutFile $Archive -UseBasicParsing -ErrorAction Stop

    if (-not (Test-Path $Archive)) {
        throw "Download failed. Archive was not created."
    }

    Write-Host "Downloaded archive: $Archive"
    Write-Host "Archive size: $((Get-Item $Archive).Length) bytes"

    Write-Host "Unblocking archive..."
    Unblock-File -Path $Archive -ErrorAction Stop

    Write-Host "Extracting archive into temp folder..."
    New-Item -ItemType Directory -Path $ExtractDir -Force -ErrorAction Stop | Out-Null
    Expand-Archive -Path $Archive -DestinationPath $ExtractDir -Force -ErrorAction Stop

    $ArtifactRoot = $ExtractDir

    $ExpectedPath = Join-Path $ArtifactRoot "Modules\DellarteDellaGuerra.Core\bin\Win64_Shipping_Client"

    if (-not (Test-Path $ExpectedPath)) {
        Write-Host "Archive contents found:"
        Get-ChildItem -Path $ArtifactRoot -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 50 |
            ForEach-Object {
                Write-Host $_.FullName
            }

        throw "Archive does not contain expected path: Modules\DellarteDellaGuerra.Core\bin\Win64_Shipping_Client"
    }

    $FinalModuleRoot = Join-Path $GameRoot "Modules\DellarteDellaGuerra.Core"
    $FinalBinRoot = Join-Path $FinalModuleRoot "bin"
    $FinalDllDir = Join-Path $FinalBinRoot "Win64_Shipping_Client"

    try {
        if (Test-Path $FinalBinRoot) {
            Write-Host "Cleaning existing binaries..."
            Remove-Item -Path $FinalBinRoot -Recurse -Force -ErrorAction Stop
        }

        Write-Host "Copying archive contents into game root..."
        Copy-Item -Path (Join-Path $ArtifactRoot "*") -Destination $GameRoot -Recurse -Force -ErrorAction Stop

        if (-not (Test-Path $FinalDllDir)) {
            throw "Expected DLL directory was not found after copy: $FinalDllDir"
        }

        Write-Host "Unblocking installed files..."
        Get-ChildItem -Path $FinalBinRoot -Recurse -File -ErrorAction Stop | ForEach-Object {
            Unblock-File -Path $_.FullName -ErrorAction Stop
        }

        Write-Host "DellarteDellaGuerra binaries installed successfully."
        Write-Host "Installed to:"
        Write-Host $FinalBinRoot
    }
    catch {
        $ErrorMessage = $_.Exception.Message

        Write-Host ""
        Write-Host "WARNING: Could not install the DellarteDellaGuerra binaries due to:"
        Write-Host $ErrorMessage
        Write-Host ""

        Show-InstallError -Message $ErrorMessage

        exit 1
    }
}
catch {
    $ErrorMessage = $_.Exception.Message

    Write-Host ""
    Write-Host "WARNING: Could not download or prepare the DellarteDellaGuerra binaries due to:"
    Write-Host $ErrorMessage
    Write-Host ""

    Show-InstallError -Message $ErrorMessage

    exit 1
}
finally {
    if (Test-Path $Archive) {
        Remove-Item $Archive -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $ExtractDir) {
        Remove-Item $ExtractDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
