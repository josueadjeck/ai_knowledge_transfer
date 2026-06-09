# Security Gates

This project uses lightweight CI gates for the MVP and keeps the path open for stronger enterprise checks later.

## Pull request and main branch checks

The GitHub Actions workflow in `.github/workflows/ci.yml` runs on pushes and pull requests to `main`.

Required checks:

- Restore all .NET projects.
- Build the full solution in Release configuration.
- Publish API and Web Release artifacts.
- Generate a transitive .NET package inventory artifact.
- Generate an SPDX 2.3 JSON SBOM baseline from the .NET package inventory.
- Run a deterministic Dockerfile container policy scan.
- Build API and Web container images without pushing them.
- Generate container image metadata inventory for the built images.
- Run all unit and architecture tests.
- Check direct and transitive NuGet packages for known vulnerabilities.
- Run a deterministic static source security scan for dangerous release-code patterns.
- Scan source files for common secret patterns such as OpenAI keys, database connection strings and passwords.

## Current limits

The SPDX JSON SBOM is a dependency-only baseline generated from `dotnet list package --include-transitive`; it is not yet a full build artifact SBOM with file hashes. The container policy scan checks Dockerfile rules such as non-root runtime user, approved versioned .NET base images and predictable COPY usage, but it is not a vulnerability scan of built image layers. The static source security scan is a deterministic baseline for obvious dangerous release-code patterns, not a full SAST engine. The secret scan is intentionally simple and deterministic. It is not a replacement for a dedicated enterprise secret scanner.
Release readiness includes manual `Security inventory` and `Container image inventory` checks so release owners confirm the uploaded artifact exists and was reviewed.

Recommended production additions:

- GitHub Advanced Security or an equivalent secret scanning tool.
- CodeQL or another enterprise SAST engine.
- Full CycloneDX or SPDX SBOM generation with file/package hashes and retention policy.
- Dependency review for pull requests.
- Built image vulnerability scanning and signing before publishing deployment images.
- Branch protection that requires the CI workflow before merge.

## Handling findings

- Do not commit real provider keys, database credentials or customer secrets.
- Rotate any secret that was committed, even if it was later removed.
- Keep local values in environment variables or user secrets.
- Prefer provider-independent configuration keys so OpenAI, Azure OpenAI, customer AI gateways and local models can use the same application contracts.
