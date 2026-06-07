# ADR 0001: MVP persistence starts with JSON, production target remains database

## Status

Accepted for MVP.

## Context

The platform needs persistence early so upload, extraction, review, roadmap, audit and backup flows can be validated end to end. The target architecture still includes a database for production use, preferably PostgreSQL or SQL Server with Entity Framework Core.

## Decision

The MVP uses local JSON persistence behind repository/application abstractions. A direct database setup is deferred until the domain model and review/traceability workflows stabilize.

## Why not set up the database immediately?

- It would add local infrastructure, migrations, connection strings and operational setup before the MVP workflows are proven.
- The domain is still moving quickly; JSON keeps schema changes cheap while project, document, knowledge, review and roadmap boundaries are still being shaped.
- The current code already isolates persistence behind `IProjectRepository`, `IAuditLog` and storage abstractions, so the database migration can be introduced without rewriting the application layer.
- For demos and local validation, a zero-dependency JSON store is faster to start, easier to reset and easier to back up.

## Consequences

- JSON is acceptable only for local MVP runs, demos and early workflow validation.
- JSON is not the long-term answer for multi-user concurrency, querying, reporting, compliance retention, identity-linked audit trails or enterprise backup/restore.
- Before production hardening, add an EF Core infrastructure implementation, relational schema, migrations and integration tests.

## Migration trigger

Move from JSON to database when one of these becomes true:

- Multiple users work on the same project concurrently.
- Audit retention and compliance reporting become contractual requirements.
- Search/filter/reporting across many projects is needed.
- Backup/restore must be managed by enterprise operations.
- Deployment leaves local MVP/demo mode.
