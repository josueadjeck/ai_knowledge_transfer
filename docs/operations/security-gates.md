# Security Gates

This project uses lightweight CI gates for the MVP and keeps the path open for stronger enterprise checks later.

## Pull request and main branch checks

The GitHub Actions workflow in `.github/workflows/ci.yml` runs on pushes and pull requests to `main`.

Required checks:

- Restore all .NET projects.
- Build the full solution in Release configuration.
- Run all unit and architecture tests.
- Check direct and transitive NuGet packages for known vulnerabilities.
- Scan source files for common secret patterns such as OpenAI keys, database connection strings and passwords.

## Current limits

The secret scan is intentionally simple and deterministic. It is not a replacement for a dedicated enterprise secret scanner.

Recommended production additions:

- GitHub Advanced Security or an equivalent secret scanning tool.
- CodeQL or another SAST engine.
- Dependency review for pull requests.
- Container image scanning once deployment images exist.
- Branch protection that requires the CI workflow before merge.

## Handling findings

- Do not commit real provider keys, database credentials or customer secrets.
- Rotate any secret that was committed, even if it was later removed.
- Keep local values in environment variables or user secrets.
- Prefer provider-independent configuration keys so OpenAI, Azure OpenAI, customer AI gateways and local models can use the same application contracts.
