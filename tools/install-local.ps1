<#
.SYNOPSIS
    Installs a locally built MSIX package (signs it with the local cert if needed).
.DESCRIPTION
    Looks for a built package in <repo>\AppPackages\ and installs it via Add-AppxPackage.
    Imports the local .cer into CurrentUser\TrustedPeople and enables Developer Mode
    if the OS requires it. Run from the repo root.
.EXAMPLE
    .\tools\install-local.ps1 -Arch x64
#>
param(
    [string]$Arch = "x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$packageDir = Join-Path $root "AppPackages"
$certDir = Join-Path $root "certs"
$certName = "YMusicWidget"
$cerPath = Join-Path $certDir "$certName.cer"

if (-not (Test-Path $packageDir)) {
    throw "No AppPackages directory found at $packageDir. Build first (see README)."
}

$msix = Get-ChildItem $packageDir -Recurse -Filter "YMusicGameBarWidget_*_$Arch*.msix" -ErrorAction SilentlyContinue |
    Select-Object -First 1

if (-not $msix) {
    $msix = Get-ChildItem $packageDir -Recurse -Filter "*.msix" | Select-Object -First 1
}
if (-not $msix) {
    throw "No .msix found under $packageDir"
}

Write-Host "Using package: $($msix.FullName)"

if (Test-Path $cerPath) {
    Write-Host "Importing cert into Trusted People..."
    Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
}

Write-Host "Installing $($msix.Name) ..."
try {
    Add-AppxPackage -Path $msix.FullName -ForceApplicationShutdown -ForceUpdateFromAnyVersion
}
catch {
    Write-Warning "Add-AppxPackage failed: $($_.Exception.Message)"
    Write-Host "Enabling Developer Mode and retrying..."
    $unlock = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock"
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Error "Developer Mode requires an elevated PowerShell. Run again as Administrator."
        throw
    }
    New-Item -Path $unlock -Force | Out-Null
    Set-ItemProperty -Path $unlock -Name AllowDevelopmentWithoutDevLicense -Value 1 -Type DWord
    Add-AppxPackage -Path $msix.FullName -ForceApplicationShutdown -ForceUpdateFromAnyVersion
}

Write-Host "Installed. Open Xbox Game Bar (Win+G) -> Widgets -> Yandex Music." -ForegroundColor Green
