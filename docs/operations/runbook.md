# Operations Runbook

This runbook describes the MVP operating flow for local or pilot deployments.

## Pre-release checklist

1. Run the CI workflow on `main` and confirm restore, build, publish, tests, vulnerability check and secret scan passed.
2. Confirm API and Web publish artifacts were produced by CI.
3. Start the application in the target persistence mode.
4. Open the operations panel or call `GET /api/operations/release-readiness`.
5. Create and preview a backup before deploying or restoring data.
6. Resolve every `Fail` readiness check.
7. Review every `Manual` or `Warning` readiness check with the release owner.
8. Approve the deployment only after runtime health, persistence and backup checks are acceptable.

## Runtime health

Use:

```http
GET /health
```

Expected result:

- Overall status is `ok`.
- Storage is reachable.
- Persistence is configured.
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

Current required checks cover runtime health, persistence configuration, authentication, tenancy, backups, CI security gates and release approval.

## Deployment baseline

See `docs/operations/deployment.md` for local publish commands, required runtime configuration and current deployment limits.
