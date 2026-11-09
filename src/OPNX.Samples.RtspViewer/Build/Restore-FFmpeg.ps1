# Downloads the pinned third-party runtime for local builds; no DLLs are committed.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Destination,
    [string]$ArchivePath = '',
    [ValidateSet('true', 'false')][string]$AllowDownload = 'true'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ffmpeg-runtime.json') -Raw | ConvertFrom-Json
$files = @($manifest.files.PSObject.Properties)
$destinationPath = [IO.Path]::GetFullPath($Destination)
$cacheRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\.cache\ffmpeg'))
[IO.Directory]::CreateDirectory($cacheRoot) | Out-Null
[IO.Directory]::CreateDirectory($destinationPath) | Out-Null

function Get-Sha256([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}

function Test-Runtime([bool]$VerifyHashes = $false) {
    foreach ($file in $files) {
        $path = Join-Path $destinationPath $file.Name
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
        if ($VerifyHashes -and (Get-Sha256 $path) -ne $file.Value) { return $false }
    }
    return $true
}

# Serialize restores from simultaneous Debug/Release builds of the same runtime.
$lock = $null
$deadline = [DateTime]::UtcNow.AddMinutes(2)
$lockPath = Join-Path $destinationPath '.restore.lock'
while ($null -eq $lock) {
    try { $lock = [IO.File]::Open($lockPath, 'OpenOrCreate', 'ReadWrite', 'None') }
    catch [IO.IOException] {
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Another FFmpeg restore is still running. Retry the build.' }
        Start-Sleep -Milliseconds 200
    }
}
try {
    if (Test-Runtime) { Write-Host "FFmpeg $($manifest.version): using local cache."; return }
    if ($ArchivePath) {
        $archive = [IO.Path]::GetFullPath($ArchivePath)
        if (!(Test-Path -LiteralPath $archive -PathType Leaf)) { throw "FFmpeg archive not found: $archive" }
    } else {
        $archive = Join-Path $cacheRoot $manifest.archiveName
    }
    if (!(Test-Path -LiteralPath $archive -PathType Leaf)) {
        if ($AllowDownload -eq 'false') {
            throw 'FFmpeg is not cached. Connect once, or set FFmpegRuntimeArchive to the exact pinned ZIP. See docs/ffmpeg-runtime.md.'
        }
        $temporaryArchive = "$archive.$([Guid]::NewGuid().ToString('N')).download"
        $client = New-Object System.Net.WebClient
        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            $client.Headers.Add('User-Agent', 'OPNX.Samples-FFmpeg-Restore')
            Write-Host "Downloading FFmpeg $($manifest.version) from $($manifest.url)"
            $client.DownloadFile($manifest.url, $temporaryArchive)
            if ((Get-Sha256 $temporaryArchive) -ne $manifest.archiveSha256) {
                throw 'FFmpeg archive SHA-256 mismatch. The downloaded archive will not be used.'
            }
            Move-Item -LiteralPath $temporaryArchive -Destination $archive -Force
        } catch {
            throw "FFmpeg download failed: $($_.Exception.Message) Use the exact ZIP via FFmpegRuntimeArchive, or follow docs/ffmpeg-runtime.md. No latest-version fallback is used."
        } finally {
            $client.Dispose()
            if (Test-Path -LiteralPath $temporaryArchive) { Remove-Item -LiteralPath $temporaryArchive -Force }
        }
    }
    if ((Get-Sha256 $archive) -ne $manifest.archiveSha256) {
        throw "FFmpeg archive SHA-256 mismatch: $archive. Obtain the pinned archive from src/OPNX.Samples.RtspViewer/Build/ffmpeg-runtime.json; no files have been extracted."
    }
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        foreach ($file in $files) {
            $entry = $zip.GetEntry("$($manifest.archiveRoot)/bin/$($file.Name)")
            if ($null -eq $entry) { throw "Missing DLL in FFmpeg ZIP: $($file.Name)" }
            $target = Join-Path $destinationPath $file.Name
            $temporaryDll = "$target.$([Guid]::NewGuid().ToString('N')).tmp"
            try {
                # Extract only named DLLs, never arbitrary paths or executables from the ZIP.
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $temporaryDll, $false)
                if ((Get-Sha256 $temporaryDll) -ne $file.Value) {
                    throw "FFmpeg DLL SHA-256 mismatch: $($file.Name)"
                }
                Move-Item -LiteralPath $temporaryDll -Destination $target -Force
            } finally {
                if (Test-Path -LiteralPath $temporaryDll) { Remove-Item -LiteralPath $temporaryDll -Force }
            }
        }
    } finally { $zip.Dispose() }
    if (!(Test-Runtime $true)) { throw 'FFmpeg runtime verification failed after extraction.' }
    Write-Host "FFmpeg $($manifest.version): extracted and verified seven x64 shared libraries."
} finally { $lock.Dispose() }
