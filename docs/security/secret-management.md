# Secret Management

This document defines the MVP secret handling baseline and the release expectations for pilot or enterprise deployments.

## Scope

The application can require secrets or sensitive configuration for:

- `OPENAI_API_KEY` for the first implemented AI provider.
- `AKT_DB_CONNECTION_STRING` when database persistence is active.
- OIDC client credentials or identity-provider metadata if the hosting environment requires them.
- Future provider-specific credentials for Azure OpenAI, customer AI gateways, local model gateways, blob storage or production databases.

## Local Development

Local development may use environment variables or developer user secrets. Values must not be committed to source control, scripts, docs, Dockerfiles or CI workflow files.

The CI workflow includes a deterministic secret pattern scan. This is a baseline safety net, not a complete enterprise secret scanner.

## Pilot and Enterprise Deployments

For non-local deployments, secrets should be injected by the hosting platform or a dedicated secret manager, for example:

- GitHub Actions encrypted secrets for CI-only values.
- Azure Key Vault, AWS Secrets Manager, HashiCorp Vault or a customer-approved equivalent.
- Kubernetes Secrets backed by an external secret operator.
- Platform-managed environment injection with restricted read access.

Secrets must not be baked into container images. Dockerfiles should remain credential-free and images should receive secrets only at runtime.

## Runtime Configuration

Secret management mode is configured without exposing secret values:

```powershell
$env:AKT_SECRET_STORE_MODE="Environment" # local default
$env:AKT_SECRET_STORE_MODE="SecretStore" # pilot/enterprise target
$env:AKT_SECRET_STORE_PROVIDER="MountedFiles"
$env:AKT_SECRET_MOUNT_PATH="/run/secrets"
$env:AKT_SECRET_ROTATION_OWNER="Operations"
```

`SecretStore` mode requires both `AKT_SECRET_STORE_PROVIDER` and `AKT_SECRET_ROTATION_OWNER`. The first automated retrieval provider is `MountedFiles`, which also requires `AKT_SECRET_MOUNT_PATH`. The application reads files named after the required setting, for example `/run/secrets/OPENAI_API_KEY` or `/run/secrets/AKT_DB_CONNECTION_STRING`, and trims trailing whitespace.

Health reports only whether provider, mount path and rotation-owner metadata are configured, not any secret value.

## Rotation and Incident Response

Before release, the release owner must confirm:

- Each required secret has an owner.
- Rotation steps are documented for AI provider keys, database credentials and OIDC/client credentials.
- Revocation is possible without rebuilding application images.
- Any accidentally committed secret was rotated, not only removed from Git history.
- Access to secret stores is limited to operators and deployment automation that need it.

## Logging and Diagnostics

Runtime health and release-readiness checks should report only whether required configuration is present. They must not expose raw key values, connection strings, tokens or client secrets.

## Release Review Checklist

Confirm before deployment:

- Required secrets are stored in an approved secret manager or platform secret store, and `AKT_SECRET_STORE_MODE=SecretStore` is configured with provider, mount path when required, and rotation owner metadata.
- No production secret is present in repository files, Dockerfiles, images or CI logs.
- The CI secret pattern scan passed.
- Runtime configuration shows required secrets as configured without exposing values.
- Rotation and revocation responsibilities are assigned.
- The deployment uses environment injection or mounted secret files rather than hardcoded values.

## Current Gaps

- `MountedFiles` secret retrieval is implemented. Direct SDK integrations for provider-specific systems such as Azure Key Vault, AWS Secrets Manager or HashiCorp Vault can be added behind the same resolver when a target platform is selected.
- CI secret scanning is deterministic and lightweight, not a full DLP or historical secret scan.
- Secret rotation is an operational procedure, not an automated in-app workflow.
