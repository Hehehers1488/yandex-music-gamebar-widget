<#
.SYNOPSIS
    End-user installer for the Yandex Music Game Bar widget.
.DESCRIPTION
    Downloads the signed MSIX + certificate for the requested release and installs
    the widget. Works on Windows 10 2004+ (build 19041).
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

if ([System.Environment]::OSVersion.Version.Build -lt 19041) {
    Write-Warning "This widget targets Windows 10 2004 (build 19041)+. Your build: $([System.Environment]::OSVersion.Version.Build)"
}

$apiUrl = "https://api.github.com/repos/$Owner/$Repo/releases/$Tag"
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
Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null

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
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Error "Certificate trust or deployment failed. Run this script again as Administrator to also trust the certificate machine-wide."
        throw
    }
    Write-Host "Trusting certificate machine-wide (LocalMachine\Root) and retrying ..."
    Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
    Add-AppxPackage -Path $msixPath -ForceApplicationShutdown -ForceUpdateFromAnyVersion
}

Write-Host ""
Write-Host "Done! Open Xbox Game Bar (Win+G) -> Widgets -> Yandex Music, then play a track." -ForegroundColor Green
