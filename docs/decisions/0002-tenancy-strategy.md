# ADR 0002: Tenancy Strategy

## Status

Accepted for MVP foundation.

## Context

The platform starts as a modular monolith for fast MVP validation. Phase 9 requires planning for future multi-tenant operation, but the current product still needs a simple local setup for demos, onboarding pilots and customer discovery.

Full multi-tenancy affects persistence, file storage, search indexes, audit trails, authorization, backups, exports and support operations. Adding partial tenant isolation inside only one layer would create a false sense of security.

## Decision

The MVP runs in `SingleTenant` mode by default.

Tenancy is introduced as an explicit operational configuration:

- `AKT_TENANCY_MODE`: `SingleTenant` or `MultiTenant`
- `AKT_TENANT_CLAIM`: claim used for tenant resolution in future OIDC-backed multi-tenant mode
- `AKT_DEFAULT_TENANT_ID`: default tenant id for single-tenant MVP data

Health reports the active tenancy mode. Release readiness warns when `SingleTenant` mode is active and fails on invalid tenancy configuration. This makes the future migration path visible without pretending that full isolation already exists.

## Consequences

- MVP setup remains easy and local.
- Enterprise deployments get an explicit readiness signal for tenancy.
- Future multi-tenant work must add tenant ids to aggregates, database records, audit events, file storage paths, backup scope, API authorization and UI filtering.
- Until that work is complete, `MultiTenant` is a planned operating mode, not a complete isolation guarantee.
