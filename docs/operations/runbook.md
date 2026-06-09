# Operations Runbook

This runbook describes the MVP operating flow for local or pilot deployments.

## Pre-release checklist

1. Run the CI workflow on `main` and confirm restore, build, publish, dependency inventory, container image inventory, container build, tests, vulnerability check and secret scan passed.
2. Confirm API, Web and `security-inventory` artifacts were produced by CI.
3. Review `docs/security/security-concept.md` and `docs/security/data-protection-concept.md` for the target deployment.
4. Start the application in the target persistence mode.
5. Open the operations panel or call `GET /api/operations/release-readiness`.
6. Create and preview a backup before deploying or restoring data.
7. Resolve every `Fail` readiness check.
8. Review every `Manual` or `Warning` readiness check with the release owner.
9. Approve the deployment only after runtime health, persistence and backup checks are acceptable.

## Runtime health

Use:

```http
GET /health
```

Expected result:

- Overall status is `ok`.
- Storage is reachable.
- Persistence is configured.
- Database schema mode is visible when database persistence is active.
- AI provider is either configured or the fallback decision is accepted.

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

Current required checks cover runtime health, persistence configuration, authentication, tenancy, backups, CI security gates, security and data protection review and release approval.
Database mode with `AKT_DB_SCHEMA_MODE=Migrations` applies managed EF migrations. `EnsureCreated` remains available for local MVP runs but appears as a warning for release review.

The `Security inventory` and `Container image inventory` release checks are manual. Review the uploaded `security-inventory` artifact and confirm `dotnet-package-inventory.json`, `vulnerable-packages.json` and `container-images.json` are present for the release candidate.
The `Security and data protection review` check is manual. Review the concepts under `docs/security` and document accepted residual risks for the target deployment.

## Deployment baseline

See `docs/operations/deployment.md` for local publish commands, required runtime configuration and current deployment limits.
