# Releases and WinGet

## One-time setup

Run the repository setup script from an authenticated GitHub CLI session:

```powershell
gh auth login --web
.\scripts\Configure-Repository.ps1
```

It creates `release:patch`, `release:minor`, and `release:major` labels and protects `main` with the required `Quality gate` status check. The rule is strict, requires resolved conversations, rejects force pushes and deletion, and allows repository administrators to explicitly bypass it.

Create a GitHub Actions repository secret named `WINGET_CREATE_GITHUB_TOKEN`. Use a dedicated classic GitHub PAT with the minimum `public_repo` scope required by WingetCreate. Never commit or paste this token into workflow files or logs.

## Pull-request checks

`PR validation` runs when a non-draft PR is opened, updated, reopened, or marked ready. Its `Quality gate` job verifies formatting, runs tests with warnings treated as errors, builds the MSI, and smoke-tests WinGet manifest generation.

Draft PRs skip the gate. Marking a draft ready triggers it. Branch protection makes the job blocking for ordinary merges; a repository administrator can explicitly bypass it.

## Release PRs

After the initial manual release, apply exactly one label before merging a release PR:

- `release:patch` increments `x.y.z` to `x.y.(z+1)`.
- `release:minor` increments `x.y.z` to `x.(y+1).0`.
- `release:major` increments `x.y.z` to `(x+1).0.0`.

After the labeled PR merges into `main`, `Release` rebuilds and tests the merge commit, creates a `vX.Y.Z` GitHub release, uploads the MSI, generates production manifests with its SHA256, and submits a PR to `microsoft/winget-pkgs` through Microsoft's WingetCreate utility.

The first community submission and every update remain subject to Microsoft's automated checks and human review. A missing or invalid `WINGET_CREATE_GITHUB_TOKEN` fails the WinGet submission after the GitHub release is created; fix the secret and rerun the workflow.

## Initial or manual release

Run the Release workflow manually with an exact version. `publish_winget` defaults to false, allowing a GitHub release without a community submission. For the first release, use `0.1.0`; enable `publish_winget` only when the installer is ready for public distribution.

## Local WinGet test

Open an elevated PowerShell window and run:

```powershell
Set-Location D:\repos\audio-enforcer
.\scripts\Test-WinGetLocal.ps1 -Version 0.1.0
```

The script builds and tests the project, generates three manifests with the real MSI hash, enables `LocalManifestFiles`, serves the MSI only on `127.0.0.1`, and invokes `winget install --manifest`. It stops the temporary server in a `finally` block.

To reuse an existing MSI:

```powershell
.\scripts\Test-WinGetLocal.ps1 -Version 0.1.0 -SkipBuild
```

Community validation may reject the loopback HTTP URL because production manifests use publisher-hosted HTTPS. The script warns and continues; release manifests use the HTTPS GitHub Release URL and exact SHA256.
