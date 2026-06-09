# Security Gates

This project uses lightweight CI gates for the MVP and keeps the path open for stronger enterprise checks later.

## Pull request and main branch checks

The GitHub Actions workflow in `.github/workflows/ci.yml` runs on pushes and pull requests to `main`.

Required checks:

- Restore all .NET projects.
- Build the full solution in Release configuration.
- Publish API and Web Release artifacts.
- Generate a build artifact file inventory with SHA256 hashes for API and Web publish outputs.
- Generate a transitive .NET package inventory artifact.
- Generate an SPDX 2.3 JSON SBOM baseline from the .NET package inventory.
- Generate a CycloneDX 1.6 JSON build SBOM from the package inventory and build artifact file inventory.
- Run a deterministic Dockerfile container policy scan.
- Build API and Web container images without pushing them.
- Scan built API and Web container images for high and critical vulnerabilities.
- Generate container image metadata inventory for the built images.
- Generate container image provenance from the built image metadata, expected CI tags and GitHub run context.
- Run a registry-backed container signing policy scan that requires target registry, signer identity, verification policy and provenance subjects before image promotion.
- Run all unit and architecture tests.
- Check direct and transitive NuGet packages for known vulnerabilities.
- Run a deterministic static source security scan for dangerous release-code patterns.
- Generate a release attestation manifest with hashes for security evidence and a 90-day artifact retention target.
- Scan source files for common secret patterns such as OpenAI keys, database connection strings and passwords.

## Current limits

The SPDX JSON SBOM is a dependency baseline generated from `dotnet list package --include-transitive`. The build artifact inventory adds file-level SHA256 hashes for API and Web publish outputs. The CycloneDX build SBOM combines NuGet package components and publish-output file components with SHA256 hashes. The release attestation manifest ties SBOMs, vulnerability reports, provenance and signing policy evidence together with SHA256 hashes, and CI uploads `security-inventory` with 90-day retention. The container policy scan checks Dockerfile rules such as non-root runtime user, approved versioned .NET base images and predictable COPY usage. The built image vulnerability scan uses Trivy and blocks high or critical fixed vulnerabilities in API and Web images. Container provenance records image ids, expected tags, GitHub commit/run context and runtime metadata so a release owner can connect a deployment image back to the CI run. The signing policy scan blocks promotion when registry-backed signing metadata or provenance subjects are missing; actual registry push/sign/verify remains platform-specific release automation. The static source security scan is a deterministic baseline for obvious dangerous release-code patterns, not a full SAST engine. The secret scan is intentionally simple and deterministic. It is not a replacement for a dedicated enterprise secret scanner.
Release readiness includes manual `Security inventory` and `Container image inventory` checks so release owners confirm the uploaded artifact exists and was reviewed.

Recommended production additions:

- GitHub Advanced Security or an equivalent secret scanning tool.
- CodeQL or another enterprise SAST engine.
- External long-term evidence retention beyond GitHub Actions artifact retention.
- Dependency review for pull requests.
- Registry push/sign/verify automation using the approved registry and signer.
- Direct integration with the customer's approved secret manager.
- Branch protection that requires the CI workflow before merge.

## Handling findings

- Do not commit real provider keys, database credentials or customer secrets.
- Rotate any secret that was committed, even if it was later removed.
- Keep local values in environment variables or user secrets.
- Inject pilot and enterprise secrets through an approved secret manager or hosting platform.
- Prefer provider-independent configuration keys so OpenAI, Azure OpenAI, customer AI gateways and local models can use the same application contracts.
