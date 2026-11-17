[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Repository = 'bicole/audio-enforcer',
    [string]$Branch = 'main'
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI is required: https://cli.github.com/'
}

gh auth status
if ($LASTEXITCODE -ne 0) {
    throw 'Authenticate first with: gh auth login --web'
}

$labels = @(
    @{ Name = 'release:patch'; Color = '0E8A16'; Description = 'Release a backward-compatible patch after merge' },
    @{ Name = 'release:minor'; Color = '1D76DB'; Description = 'Release a backward-compatible feature version after merge' },
    @{ Name = 'release:major'; Color = 'B60205'; Description = 'Release a breaking major version after merge' }
)

foreach ($label in $labels) {
    if ($PSCmdlet.ShouldProcess("$Repository label $($label.Name)", 'Create or update')) {
        gh label create $label.Name --repo $Repository --color $label.Color --description $label.Description --force
        if ($LASTEXITCODE -ne 0) {
            throw "Could not configure label '$($label.Name)'."
        }
    }
}

$protection = @{
    required_status_checks = @{ strict = $true; contexts = @('Quality gate') }
    enforce_admins = $false
    required_pull_request_reviews = $null
    restrictions = $null
    required_linear_history = $true
    allow_force_pushes = $false
    allow_deletions = $false
    block_creations = $false
    required_conversation_resolution = $true
    lock_branch = $false
    allow_fork_syncing = $true
} | ConvertTo-Json -Depth 10

if ($PSCmdlet.ShouldProcess("$Repository branch $Branch", 'Apply branch protection')) {
    $protection | gh api --method PUT -H 'Accept: application/vnd.github+json' -H 'X-GitHub-Api-Version: 2022-11-28' "repos/$Repository/branches/$Branch/protection" --input -
    if ($LASTEXITCODE -ne 0) {
        throw 'Branch protection failed. Refresh GitHub CLI authorization with repository administration permission, then rerun.'
    }
}

Write-Host "Configured release labels and required 'Quality gate' check for ${Repository}:$Branch." -ForegroundColor Green
Write-Host 'Repository administrators are the bypass actors; normal collaborators cannot merge while the check is failing.'
