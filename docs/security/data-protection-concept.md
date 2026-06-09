# Data Protection Concept

This document describes which data the MVP processes, where it is stored and which operational decisions are required before pilot or enterprise deployment.

## Data Categories

The platform can process:

- Project metadata such as name, description and owner.
- Uploaded technical documents and their source metadata.
- Parsed document chunks and parser diagnostics.
- Extracted knowledge items, quality labels and review history.
- Roadmaps, onboarding start packages and export histories.
- Audit events for project, document, review, export and operations actions.
- Runtime health and release-readiness metadata.
- User identity claims such as role and tenant claim when OIDC is active.

The platform should be treated as potentially handling confidential technical information.

## Storage Locations

| Data | Current location |
| --- | --- |
| Project aggregates | JSON file or SQLite database, depending on persistence mode. |
| Audit events | JSON file or SQLite database, depending on persistence mode. |
| Uploaded documents | Local upload storage. |
| Backups | Local backup ZIP archives. |
| CI inventories | GitHub Actions artifacts for the workflow run. |

Operators must protect the application data directory, backup directory and CI artifacts according to the target environment classification.

## AI Provider Processing

When OpenAI or another external provider is configured, selected document chunk text may be sent to that provider for extraction. Guardrails limit chunk count and extracted characters before provider calls.

Before using a provider with customer documents, the deployment owner must verify:

- Contractual permission to process the document content with the provider.
- Region, retention and logging behavior of the provider.
- Whether sensitive documents require local or customer-hosted AI instead.

## Retention and Deletion

The MVP supports backups and restore, but it does not yet provide a full retention policy engine or automated data deletion workflow.

Pilot operation should define:

- How long uploaded documents and backups are kept.
- Who may delete project data and backup archives.
- How deletion requests are documented.
- Whether exports may leave the platform and where they are stored.

## Access and Confidentiality

Access is controlled through application roles and API permission gates. For enterprise deployment, OIDC mode should be enabled so user identity and role assignment come from the customer's identity provider.

Backups contain project data, audit logs and uploaded files. They must be stored, transmitted and deleted with the same or higher protection level as the original data.

## Tenancy

The MVP defaults to `SingleTenant`. Multi-tenant mode is visible as configuration and release-readiness metadata, but complete tenant isolation still requires dedicated implementation across persistence, storage, audit, backup, API authorization and UI filtering.

Until that work is complete, one deployment should serve one customer or one agreed data boundary.

## Data Protection Review Checklist

Before a supervised pilot starts, confirm:

- Data classification of the uploaded documents is known.
- OIDC is configured or Demo mode is accepted only for local testing.
- The AI provider is approved for the document classification.
- Backup storage and retention are defined.
- Export handling and external sharing rules are defined.
- Tenant boundary assumptions are documented.
- Residual risks are reviewed by the release owner.

## Current Gaps

- No automated retention or deletion workflow.
- No built-in encryption key management beyond hosting platform controls.
- No full data processing agreement workflow.
- No complete multi-tenant isolation.
- No enterprise DLP integration.
