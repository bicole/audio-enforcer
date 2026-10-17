[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [Parameter(Mandatory)][ValidateScript({ Test-Path $_ -PathType Leaf })][string]$InstallerPath,
    [Parameter(Mandatory)][ValidatePattern('^https?://')][string]$InstallerUrl,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$templateDirectory = Join-Path $root 'winget'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$hash = (Get-FileHash -Path $InstallerPath -Algorithm SHA256).Hash.ToUpperInvariant()
$templates = @(Get-ChildItem -Path $templateDirectory -Filter '*.yaml.in')
if ($templates.Count -ne 3) {
    throw "Expected three WinGet templates in '$templateDirectory'."
}

foreach ($template in $templates) {
    $content = Get-Content -Path $template.FullName -Raw
    $content = $content.Replace('INSTALLER_URL', $InstallerUrl).Replace('VERSION', $Version).Replace('SHA256', $hash)
    $destination = Join-Path $OutputDirectory $template.Name.Substring(0, $template.Name.Length - 3)
    [IO.File]::WriteAllText($destination, $content, [Text.UTF8Encoding]::new($false))
}

$unresolved = Get-ChildItem -Path $OutputDirectory -Filter '*.yaml' | Select-String -Pattern 'INSTALLER_URL|VERSION|SHA256'
if ($unresolved) {
    throw 'One or more WinGet template tokens were not replaced.'
}

Write-Host "Generated WinGet manifests in $OutputDirectory"
Write-Host "Installer SHA256: $hash"
