# Operations Runbook

This runbook describes the MVP operating flow for local or pilot deployments.

## Pre-release checklist

1. Run the CI workflow on `main` and confirm restore, build, publish, build artifact inventory, dependency inventory, SPDX SBOM generation, container policy scan, container image vulnerability scan, container image inventory, container image provenance, container build, tests, vulnerability check, static source security scan and secret scan passed.
2. Confirm API, Web and `security-inventory` artifacts were produced by CI.
3. Review `docs/security/security-concept.md` and `docs/security/data-protection-concept.md` for the target deployment.
4. Confirm runtime secrets are injected by an approved secret manager or platform secret store and `AKT_SECRET_STORE_MODE` metadata is configured for the release.
5. Start the application in the target persistence mode.
6. Open the operations panel or call `GET /api/operations/release-readiness`.
7. Create and preview a backup before deploying or restoring data.
8. Resolve every `Fail` readiness check.
9. Review every `Manual` or `Warning` readiness check with the release owner.
10. Approve the deployment only after runtime health, persistence and backup checks are acceptable.
11. For `AKT_DEPLOYMENT_STRATEGY=BlueGreen`, validate the green slot before switching traffic and keep rollback ownership explicit.

## Runtime health

Use:

```http
GET /health
GET /api/operations/runtime-metrics
GET /api/operations/monitoring-summary
GET /api/operations/roadmap-readiness
```

Expected result:

- Overall status is `ok`.
- Storage is reachable.
- Persistence is configured.
- Database schema mode is visible when database persistence is active.
- Database provider is reviewed: SQLite for local pilots, SQL Server or PostgreSQL for production-like deployments.
- AI provider is either configured or the fallback decision is accepted.
- Monitoring summary is `ok` or contains only accepted manual release-review signals.
- Roadmap readiness identifies no unexpected gaps for the release scope.

## Backup and restore

Use the operations endpoints:

```http
POST /api/operations/backups
GET /api/operations/backups
GET /api/operations/backups/{fileName}/preview
POST /api/operations/backups/{fileName}/restore
```

Restore should only happen after preview. The preview must show project data and audit log data unless the release owner explicitly accepts the gap.

## Release readiness

Use:

```http
GET /api/operations/release-readiness
```

Status meanings:

- `Ready`: all automated checks passed and no manual check remains.
- `NeedsReview`: automated release blockers are clear, but warning or manual checks still need a human decision.
- `Blocked`: at least one required check failed.

Current required checks cover runtime health, persistence configuration, authentication, role permission review, tenancy, tenant isolation review, backups, CI security gates, security and data protection review, secret management review and release approval.
Database mode with `AKT_DB_SCHEMA_MODE=Migrations` applies managed EF migrations. `EnsureCreated` remains available for local MVP runs but appears as a warning for release review.

The `Role permission review` check is manual. Review `GET /api/security/role-review` and confirm critical permissions, non-admin assignments and identity-provider role-claim mapping are accepted for the release.
The `Tenant isolation review` check is manual. Review `GET /api/operations/tenant-isolation-review`; `MultiTenant` mode must not serve multiple customers until persistence, storage, audit, backup, API, UI and export isolation are verified.
The `Security inventory` and `Container image inventory` release checks are manual. Review the uploaded `security-inventory` artifact and confirm `build-artifact-inventory.json`, `dotnet-package-inventory.json`, `sbom.spdx.json`, `vulnerable-packages.json`, `static-source-scan.json`, `container-policy-scan.json`, `container-vulnerability-scan-api.json`, `container-vulnerability-scan-web.json`, `container-images.json` and `container-provenance.json` are present for the release candidate.
The `Security and data protection review` check is manual. Review the concepts under `docs/security` and document accepted residual risks for the target deployment.
The `Secret management review` check is manual for environment-based local runs. In `SecretStore` mode it passes only when provider and rotation-owner metadata are configured, and fails if either is missing.
The `Deployment strategy` check is manual for `SingleSlot`, passes for `BlueGreen` with an approval owner and fails when blue/green metadata is incomplete.

## Deployment baseline

See `docs/operations/deployment.md` for local publish commands, required runtime configuration and current deployment limits.
