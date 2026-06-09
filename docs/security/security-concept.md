# Security Concept

This document describes the current MVP security baseline and the controls that must be reviewed before a pilot or release deployment.

## Scope

The platform stores technical onboarding knowledge, source references, review decisions, exports, operational audit events and uploaded documents. The MVP is suitable for controlled pilots, but production enterprise use still requires external identity, hardened hosting, tenant isolation decisions and customer-specific data protection review.

## Identity and Access

The application supports two authentication modes:

- `Demo`: local workflow mode. Anonymous API calls resolve to the Viewer role and the Web UI exposes a role selector.
- `Oidc`: enterprise mode. API endpoints require a valid bearer token, and the Web app uses cookie plus OpenID Connect sign-in.

Release readiness fails when OIDC mode is incomplete and warns when Demo mode is active. Enterprise deployments should use OIDC with a configured authority, client id and role claim.

## Roles and Permissions

The MVP roles are:

| Role | Intent |
| --- | --- |
| Admin | Operates the system, backups, release checks and administrative functions. |
| Senior Engineer | Reviews, approves and rejects generated knowledge and exports. |
| Contributor | Creates projects, uploads documents and starts analysis flows. |
| Viewer | Reads approved or visible project information without mutating state. |

API permission gates protect project, document, review, roadmap, export, audit and operations endpoints. The Blazor UI uses the same authorization service for action availability.
Before release, administrators should review `GET /api/security/role-review` and confirm non-admin roles with critical permissions are intentional. The review also checks that identity-provider role claims map to the intended application roles.

## Data and Storage Controls

Current storage options:

- JSON persistence for local MVP and demo runs.
- SQLite database persistence for local relational pilots.
- SQL Server database persistence for production-like enterprise deployments.
- PostgreSQL database persistence for production-like enterprise deployments.
- Local file storage for uploads and backup archives.

Database mode supports `AKT_DB_SCHEMA_MODE=Migrations` for managed EF schema application. JSON mode remains the default for quick local setup but is not the target for enterprise production. SQLite is treated as local pilot persistence; SQL Server and PostgreSQL are the current production-capable relational providers.

## Secret Management

Runtime secrets such as AI provider keys, database credentials and future provider-specific credentials must be injected by an approved secret manager or hosting platform. See `docs/security/secret-management.md`.

## AI Provider Controls

Knowledge extraction is provider-independent. OpenAI is the first provider, with heuristic fallback when the provider is unavailable or returns no usable result. The provider API key is read from environment configuration and must not be committed.

Before release, teams must confirm:

- The selected provider is approved for the data classification of the uploaded documents.
- Prompt and response data handling is acceptable for the customer.
- Fallback operation is either acceptable or explicitly disabled by deployment policy.

## CI and Release Gates

The CI pipeline runs restore, build, publish, tests, vulnerability checks, a deterministic static source security scan, a deterministic secret scan, dependency inventory generation, SPDX SBOM generation, Dockerfile container policy scanning, container builds, built image vulnerability scanning, container image metadata inventory and container image provenance generation.

Release readiness requires manual review of:

- CI security gates.
- Dependency and vulnerability inventory.
- Container image inventory.
- Security and data protection concepts.
- Secret management review.
- Human release approval.

## Auditability

Key project, document, knowledge, review, export, traceability and operations actions are written to the audit log. Audit data is available through API and UI filters and is included in backup archives.

## Current Limitations

- Demo authentication is not acceptable for enterprise production.
- Full tenant isolation is planned but not implemented end to end.
- Local file storage is suitable for controlled pilots, not for hardened multi-node production.
- CI static source scanning and secret scanning are baselines, not replacements for enterprise security tooling.
- Registry-backed image signing enforcement, full SBOM with file hashes and enterprise SAST remain follow-up work.

## Release Review

Before a release, the release owner must compare this concept with the target deployment, confirm all manual release-readiness checks and document accepted residual risks.
