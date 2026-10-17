[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version = '0.1.0',
    [ValidateRange(1024, 65535)][int]$Port = 8765,
    [switch]$SkipBuild,
    [switch]$SkipValidation
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if ($env:OS -ne 'Windows_NT') {
    throw 'This script must run on Windows.'
}
if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw 'WinGet is required. Install or update App Installer from Microsoft.'
}
if (-not (Test-Administrator)) {
    throw 'Open PowerShell as Administrator. Elevation is required to enable LocalManifestFiles.'
}

if (-not $SkipBuild) {
    & (Join-Path $root 'build.ps1') -Version $Version
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }
}

$msi = Get-ChildItem (Join-Path $root 'artifacts\installer\*.msi') | Select-Object -First 1
if (-not $msi) {
    throw 'No MSI found under artifacts\installer. Run without -SkipBuild first.'
}

$manifestDirectory = Join-Path $root "artifacts\winget-local\$Version"
$installerUrl = "http://127.0.0.1:$Port/$($msi.Name)"
& (Join-Path $PSScriptRoot 'New-WinGetManifest.ps1') -Version $Version -InstallerPath $msi.FullName -InstallerUrl $installerUrl -OutputDirectory $manifestDirectory

winget settings --enable LocalManifestFiles
if ($LASTEXITCODE -ne 0) {
    throw "Could not enable LocalManifestFiles; winget exited with $LASTEXITCODE."
}

$serverJob = Start-Job -ArgumentList $msi.FullName, $Port -ScriptBlock {
    param($InstallerPath, $ListenPort)

    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $ListenPort)
    $listener.Start()
    try {
        while ($true) {
            $client = $listener.AcceptTcpClient()
            try {
                $stream = $client.GetStream()
                $reader = [IO.StreamReader]::new($stream, [Text.Encoding]::ASCII, $false, 1024, $true)
                $request = $reader.ReadLine()
                while (($line = $reader.ReadLine()) -ne '') {
                    if ($null -eq $line) { break }
                }

                $method = ($request -split ' ')[0]
                $bytes = [IO.File]::ReadAllBytes($InstallerPath)
                $header = "HTTP/1.1 200 OK`r`nContent-Type: application/octet-stream`r`nContent-Length: $($bytes.Length)`r`nConnection: close`r`n`r`n"
                $headerBytes = [Text.Encoding]::ASCII.GetBytes($header)
                $stream.Write($headerBytes, 0, $headerBytes.Length)
                if ($method -ne 'HEAD') {
                    $stream.Write($bytes, 0, $bytes.Length)
                }
                $stream.Flush()
            } finally {
                $client.Dispose()
            }
        }
    } finally {
        $listener.Stop()
    }
}

try {
    Start-Sleep -Seconds 2
    if ($serverJob.State -eq 'Failed') {
        throw (Receive-Job $serverJob | Out-String)
    }

    if (-not $SkipValidation) {
        winget validate --manifest $manifestDirectory --disable-interactivity
        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'Validation rejected the loopback HTTP URL; continuing with the local install test.'
        }
    }

    winget install --manifest $manifestDirectory --architecture x64 --scope user --force --accept-package-agreements --disable-interactivity
    if ($LASTEXITCODE -ne 0) {
        throw "Local WinGet installation failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Audio Enforcer was built and installed through the local WinGet manifest.' -ForegroundColor Green
} finally {
    Stop-Job $serverJob -ErrorAction SilentlyContinue
    Remove-Job $serverJob -Force -ErrorAction SilentlyContinue
}
