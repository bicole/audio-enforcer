## Change summary

- 

## Validation

- [ ] `dotnet format AudioEnforcer.sln --verify-no-changes`
- [ ] `dotnet test AudioEnforcer.sln -c Release`
- [ ] `./build.ps1`

## Release intent

Normal PRs need no release label. To create a release after merge, apply exactly one of:

- `release:patch`
- `release:minor`
- `release:major`

Draft PRs do not run the blocking quality gate until marked ready for review.
