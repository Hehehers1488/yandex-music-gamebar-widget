<#
.SYNOPSIS
    End-user installer for the Yandex Music Game Bar widget.
.DESCRIPTION
    Downloads the signed MSIX + certificate for the requested release and installs
    the widget. Works on Windows 10 2004+ (build 19041).

    AppX deployment validates the package signature in the system context and only
    reads MACHINE certificate stores, so the signing certificate must be trusted
    machine-wide (requires Administrator). install.bat requests elevation; if you
    run this script directly, run it from an elevated PowerShell.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\install.ps1
    powershell -ExecutionPolicy Bypass -File .\install.ps1 -Arch arm64
    powershell -ExecutionPolicy Bypass -File .\install.ps1 -Tag v1.0.0
#>
param(
    [string]$Tag = "latest",
    [ValidateSet("x64", "arm64")]
    [string]$Arch = "x64",
    [string]$Owner = "Hehehers1488",
    [string]$Repo = "yandex-music-gamebar-widget"
)

$ErrorActionPreference = "Stop"

function Add-CertificateToStore {
    param([string]$StoreName, [string]$StoreLocation, [string]$CertPath)
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($StoreName, $StoreLocation)
    $store.Open("ReadWrite")
    $store.Add((New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertPath)))
    $store.Close()
}

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if ([System.Environment]::OSVersion.Version.Build -lt 19041) {
    Write-Warning "This widget targets Windows 10 2004 (build 19041)+. Your build: $([System.Environment]::OSVersion.Version.Build)"
}

if (-not $isAdmin) {
    Write-Warning "AppX deployment needs machine-wide certificate trust, which requires Administrator."
    Write-Warning "Run this script from an elevated PowerShell, or rerun install.bat (it requests elevation automatically)."
}

$apiUrl = if ($Tag -eq "latest") {
    "https://api.github.com/repos/$Owner/$Repo/releases/latest"
} else {
    "https://api.github.com/repos/$Owner/$Repo/releases/tags/$Tag"
}
$release = Invoke-RestMethod -Uri $apiUrl -Headers @{ "User-Agent" = "ymusic-widget-installer" }

$msixAsset = $release.assets | Where-Object { $_.name -like "YMusicGameBarWidget_*_${Arch}.msix" } | Select-Object -First 1
$cerAsset = $release.assets | Where-Object { $_.name -eq "YMusicGameBarWidget.cer" } | Select-Object -First 1
if (-not $msixAsset -or -not $cerAsset) {
    Write-Error "Required assets not found in release $Tag (arch=$Arch). Check $Owner/$Repo releases."
    exit 1
}

$msixUrl = $msixAsset.browser_download_url
$cerUrl = $cerAsset.browser_download_url

$tmp = Join-Path $env:TEMP "ymusic-widget"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

$msixPath = Join-Path $tmp $msixAsset.name
$cerPath = Join-Path $tmp $cerAsset.name

Write-Host "Downloading $msixUrl ..."
Invoke-WebRequest -Uri $msixUrl -OutFile $msixPath -UseBasicParsing
Write-Host "Downloading certificate ..."
Invoke-WebRequest -Uri $cerUrl -OutFile $cerPath -UseBasicParsing

$thumbprint = (Get-PfxCertificate $msixPath).Thumbprint
Write-Host "Trusting certificate $thumbprint ..."

# User stores: best effort, no admin required.
Add-CertificateToStore -StoreName "TrustedPeople" -StoreLocation "CurrentUser" -CertPath $cerPath
Add-CertificateToStore -StoreName "Root" -StoreLocation "CurrentUser" -CertPath $cerPath

# Machine stores: required by AppX deployment, needs Administrator.
if ($isAdmin) {
    Write-Host "Trusting certificate machine-wide ..."
    Add-CertificateToStore -StoreName "TrustedPeople" -StoreLocation "LocalMachine" -CertPath $cerPath
    Add-CertificateToStore -StoreName "Root" -StoreLocation "LocalMachine" -CertPath $cerPath
}

$existing = Get-AppxPackage -Name "YMusicGameBarWidget"
if ($existing) {
    Write-Host "Existing install found, removing it first (version $($existing.Version)) ..."
    Remove-AppxPackage -Package $existing.PackageFullName
}

Write-Host "Installing $msixPath ..."
try {
    Add-AppxPackage -Path $msixPath -ForceApplicationShutdown -ForceUpdateFromAnyVersion
}
catch {
    Write-Warning "Add-AppxPackage failed: $($_.Exception.Message)"
    if (-not $isAdmin) {
        Write-Host "This usually means the certificate was not trusted machine-wide." -ForegroundColor Yellow
    }
    Write-Error "Installation failed. Close this window and rerun install.bat - it will request Administrator rights and try again."
    throw
}

Write-Host ""
Write-Host "Done! Open Xbox Game Bar (Win+G) -> Widgets -> Yandex Music, then play a track." -ForegroundColor Green
