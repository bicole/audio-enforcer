[CmdletBinding()]
param([string]$Configuration = "Release", [string]$Version = "0.1.0", [switch]$SkipTests)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$publish = Join-Path $artifacts "publish\win-x64"
$installerOut = Join-Path $artifacts "installer"
Remove-Item $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item $publish -ItemType Directory -Force | Out-Null
New-Item $installerOut -ItemType Directory -Force | Out-Null
dotnet restore (Join-Path $root "AudioEnforcer.sln")
if (-not $SkipTests) { dotnet test (Join-Path $root "AudioEnforcer.sln") -c $Configuration --no-restore }
dotnet publish (Join-Path $root "src\AudioEnforcer\AudioEnforcer.csproj") -c $Configuration -r win-x64 --self-contained true -o $publish -p:VersionPrefix=$Version -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false
dotnet build (Join-Path $root "installer\AudioEnforcer.Installer.wixproj") -c $Configuration -o $installerOut -p:VersionPrefix=$Version -p:PublishDir=$publish
Write-Host "Artifacts: $artifacts"
