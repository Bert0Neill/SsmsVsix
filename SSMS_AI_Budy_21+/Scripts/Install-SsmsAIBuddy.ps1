#Requires -Version 5.1
<#
.SYNOPSIS
  Installs the SSMS AI Buddy VSIX into SSMS 21 or 22 (correct per-instance store).

.DESCRIPTION
  SSMS 21+ does NOT load extensions from:
    Program Files\...\Common7\IDE\Extensions\YourFolder

  Extensions must be installed under:
    %LocalAppData%\Microsoft\SSMS\{major}.0_{InstallationID}\Extensions\

  and the shell must run:
    Ssms.exe /updateConfiguration

  InstallationID comes from SSMS.isolation.ini (e.g. e4abd3f9 for SSMS 22).
#>
[CmdletBinding()]
param(
    [string]$VsixPath,
    [ValidateSet('21', '22', 'Auto')]
    [string]$SsmsVersion = 'Auto',
    [string]$SsmsInstallRoot,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

function Find-SsmsRoot {
    param([string]$Version, [string]$Override)
    if ($Override -and (Test-Path -LiteralPath $Override)) {
        return $Override.TrimEnd('\')
    }
    $candidates = @()
    if ($Version -eq '22' -or $Version -eq 'Auto') {
        $candidates += "${env:ProgramFiles}\Microsoft SQL Server Management Studio 22"
    }
    if ($Version -eq '21' -or $Version -eq 'Auto') {
        $candidates += "${env:ProgramFiles}\Microsoft SQL Server Management Studio 21"
    }
    foreach ($root in $candidates) {
        $ide = Join-Path $root 'Release\Common7\IDE'
        if (Test-Path -LiteralPath (Join-Path $ide 'Ssms.exe')) { return $root }
    }
    throw "SSMS install not found. Pass -SsmsInstallRoot."
}

function Get-SsmsInstanceInfo {
    param([string]$Root)
    $ini = Join-Path $Root 'Release\Common7\IDE\SSMS.isolation.ini'
    if (-not (Test-Path -LiteralPath $ini)) {
        throw "Missing $ini"
    }
    $info = @{}
    Get-Content -LiteralPath $ini | ForEach-Object {
        if ($_ -match '^\s*([^=]+)=(.*)$') {
            $info[$Matches[1].Trim()] = $Matches[2].Trim().Trim('"')
        }
    }
    if (-not $info.InstallationID) { throw "InstallationID not found in SSMS.isolation.ini" }
    $major = if ($info.DisplayVersion -match '^(\d+)') { $Matches[1] } else { '22' }
    return [pscustomobject]@{
        InstallationId = $info.InstallationID
        MajorVersion     = $major
        InstanceFolder   = Join-Path $env:LOCALAPPDATA "Microsoft\SSMS\$major.0_$($info.InstallationID)"
        SsmsExe          = Join-Path $Root "Release\Common7\IDE\Ssms.exe"
    }
}

function Install-VsixToSsmsInstance {
    param(
        [string]$VsixPath,
        [string]$InstanceExtensionsRoot,
        [string]$ExtensionSubFolder
    )
    $dest = Join-Path $InstanceExtensionsRoot $ExtensionSubFolder
    if (Test-Path -LiteralPath $dest) {
        Remove-Item -LiteralPath $dest -Recurse -Force
    }
    New-Item -ItemType Directory -Path $dest -Force | Out-Null

    $temp = Join-Path ([IO.Path]::GetTempPath()) ("SsmsAIBuddy_" + [guid]::NewGuid().ToString('N'))
    try {
        Copy-Item -LiteralPath $VsixPath -Destination "$temp.zip" -Force
        Expand-Archive -LiteralPath "$temp.zip" -DestinationPath $temp -Force
        Get-ChildItem -LiteralPath $temp -File | Copy-Item -Destination $dest -Force
        # satellite resource folders (if any)
        Get-ChildItem -LiteralPath $temp -Directory | Where-Object { $_.Name -match '^[a-z]{2}(-[A-Z][a-z]+)?$' } |
            ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dest $_.Name) -Recurse -Force }
    }
    finally {
        Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath "$temp.zip" -Force -ErrorAction SilentlyContinue
    }
    return $dest
}

if (-not $VsixPath) {
    $VsixPath = Join-Path $PSScriptRoot '..\bin\Release\SSMS_AIBuddy_21Plus.vsix'
}
$VsixPath = (Resolve-Path -LiteralPath $VsixPath).Path

$ssmsRoot = Find-SsmsRoot -Version $SsmsVersion -Override $SsmsInstallRoot
$instance = Get-SsmsInstanceInfo -Root $ssmsRoot
$extensionsRoot = Join-Path $instance.InstanceFolder 'Extensions'

if (-not (Test-Path -LiteralPath $extensionsRoot)) {
    New-Item -ItemType Directory -Path $extensionsRoot -Force | Out-Null
}

# Stable subfolder name (matches VSIX identity); updateConfiguration indexes this folder.
$extensionFolder = 'SsmsQueryEditorAI.662a8cda-e7a8-4788-a94c-70c865eab234'

Write-Host "SSMS:              $($instance.SsmsExe)"
Write-Host "Instance data:     $($instance.InstanceFolder)"
Write-Host "Extension target:  $(Join-Path $extensionsRoot $extensionFolder)"
Write-Host "VSIX:              $VsixPath"

$deployed = Install-VsixToSsmsInstance -VsixPath $VsixPath -InstanceExtensionsRoot $extensionsRoot -ExtensionSubFolder $extensionFolder

Write-Host "Refreshing extension catalog (Ssms.exe /updateConfiguration)..."
$proc = Start-Process -FilePath $instance.SsmsExe -ArgumentList '/updateConfiguration' -Wait -PassThru -WindowStyle Hidden
if ($proc.ExitCode -ne 0 -and $null -ne $proc.ExitCode) {
    Write-Warning "updateConfiguration exit code: $($proc.ExitCode)"
}

Write-Host ""
Write-Host "Installed. Restart SSMS $($instance.MajorVersion), then check Tools -> Configure SQL AI Buddy."
Write-Host "Remove obsolete manual deploy if present:"
Write-Host "  $(Join-Path (Split-Path $instance.SsmsExe) 'Extensions\SsmsAIBuddy')"
Write-Host ""
Write-Host "Activity log (enable: set VsLogActivity=1 before starting SSMS):"
Write-Host "  $env:LOCALAPPDATA\Microsoft\SSMS\$($instance.MajorVersion).0_$($instance.InstallationId)\"
