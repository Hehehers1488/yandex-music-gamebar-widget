<#
.SYNOPSIS
    Creates the code-signing certificate used to sign the MSIX package.
.DESCRIPTION
    Generates a self-signed code-signing certificate with a FIXED subject
    (CN=YMusicWidget). Reuse the SAME certificate (the generated .pfx) for all
    releases so that package updates install "over" previous versions.
    Stores the .pfx (private key) and .cer (public) in <repo>\certs\.
    The .pfx must be kept secret (add to GitHub Secrets for CI builds).
.EXAMPLE
    .\tools\make-cert.ps1
    .\tools\make-cert.ps1 -Password "my-password"
#>
param(
    [string]$Password = "YMusicWidget",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$certDir = Join-Path $PSScriptRoot "..\certs"
$certName = "YMusicWidget"
$pfxPath = Join-Path $certDir "$certName.pfx"
$cerPath = Join-Path $certDir "$certName.cer"

New-Item -ItemType Directory -Force -Path $certDir | Out-Null

if ((Test-Path $pfxPath) -and -not $Force) {
    Write-Host "Certificate already exists: $pfxPath (reuse it!)" -ForegroundColor Green
    exit 0
}

Write-Host "Creating self-signed code-signing certificate CN=$certName ..."
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=$certName" `
    -CertStoreLocation Cert:\CurrentUser\My `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(3)

$securePassword = ConvertTo-SecureString $Password -AsPlainText -Force

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host "Created: $pfxPath" -ForegroundColor Green
Write-Host "Created: $cerPath" -ForegroundColor Green
Write-Host "Pfx password: $Password" -ForegroundColor Yellow
Write-Host ""
Write-Host "IMPORTANT: keep $pfxPath secret. For CI builds, upload it (base64) as the" 
Write-Host "CERT_BASE64 secret and the password as CERT_PASSWORD in GitHub repo settings."
