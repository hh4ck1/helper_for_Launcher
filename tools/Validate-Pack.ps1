$ErrorActionPreference = 'Stop'

$manifestPath = Join-Path $PSScriptRoot '..\channel\stable\manifest.json'
$signaturePath = Join-Path $PSScriptRoot '..\channel\stable\manifest.sig'
$publicKeyPath = Join-Path $PSScriptRoot '..\public-key.pem'

$manifestBytes = [System.IO.File]::ReadAllBytes($manifestPath)
$signature = [Convert]::FromBase64String(
    [System.IO.File]::ReadAllText($signaturePath).Trim())
$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.ImportFromPem([System.IO.File]::ReadAllText($publicKeyPath))
$valid = $rsa.VerifyData(
    $manifestBytes,
    $signature,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256,
    [System.Security.Cryptography.RSASignaturePadding]::Pss)
if (-not $valid) {
    throw 'Manifest signature is invalid.'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = $manifest.packVersion
foreach ($file in $manifest.files) {
    $relativePath = $file.path
    $expectedSize = $file.size
    $expectedHash = $file.sha256
    $localPath = Join-Path $PSScriptRoot "..\packs\$version\$relativePath"
    $item = Get-Item -LiteralPath $localPath
    if ($item.Length -ne $expectedSize) {
        throw "Size mismatch: $relativePath"
    }

    $actualHash = (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "SHA-256 mismatch: $relativePath"
    }
}

Write-Host "Pack $version is valid."
