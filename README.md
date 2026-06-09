# AI Knowledge Transfer Platform

MVP foundation for an AI-assisted knowledge transfer, onboarding and compliance platform for technical systems.

## Current implementation

- .NET 10 solution with modular monolith structure.
- Blazor Web project with an interactive MVP workflow.
- User-facing error feedback for failed workflow operations.
- ASP.NET Core API with project, document and roadmap endpoints.
- Consistent API validation and JSON error responses for common failure cases.
- Document analysis limit violations return a stable `document_analysis_limit_exceeded` API error code.
- Multipart document upload with local file storage.
- Central upload validation for supported file types and 10 MB maximum file size.
- Document cards show source, content type, file size, version, upload time and storage status.
- Text and Markdown parsing into document chunks.
- PDF text extraction into document chunks for text-based PDF files.
- Word `.docx` text extraction into document chunks.
- Shared chunking logic across text, PDF and Word parsers.
- Document chunks retain parser-specific source references for review and traceability.
- Document chunks include deterministic quality labels such as `UsableText` and `ReviewShortText`.
- Document analysis responses and the Blazor workflow summarize chunk quality counts.
- Document analysis has configurable guardrails for maximum chunks and extracted characters per document.
- Supported parser capabilities are available through API and visible in the Blazor document workflow.
- Document analysis responses and the Blazor workflow show parser name, chunk count and parser detail.
- PDF analysis reports that scanned PDFs need OCR when no extractable text is available.
- Provider-independent knowledge extraction with OpenAI as the first AI provider and heuristic fallback.
- Knowledge extraction has configurable guardrails before AI provider calls.
- Knowledge extraction limit violations return a stable `knowledge_extraction_limit_exceeded` API error code.
- Knowledge extraction responses and the Blazor workflow show the active provider and whether fallback was used.
- Extracted knowledge items retain source chunk, provider, model, fallback and quality metadata for review, traceability and export.
- Review workflow for extracted knowledge items.
- Editable reviewer and review comment fields in the Blazor review workflow.
- Review actions can update knowledge quality status such as `NeedsClarification`, `Verified` or `RejectedSource`.
- Knowledge items keep review history entries for submit, approve and reject actions.
- Review summary endpoint and dashboard cards show counts by review status, quality status, type and final usable knowledge.
- Roadmap generation that uses approved and verified knowledge as final content and marks other items as review notes.
- Onboarding readiness check for whether a project can start a supervised pilot onboarding.
- Onboarding start package with first-week roadmap content, starter tasks and review warnings.
- Markdown export for project handover documents.
- Export history endpoint and dashboard list for generated Markdown exports.
- Export approvals for generated Markdown handover documents.
- Traceability matrix for source, knowledge, review, review history and roadmap/export usage.
- Compliance matrix endpoint and Blazor view for compliant vs open evidence, including explicit export approval as a gate.
- Project details endpoint with documents, knowledge items and generated roadmaps.
- Domain model for projects, document versions, knowledge items and onboarding roadmaps.
- Application services for project creation, document registration and deterministic roadmap generation.
- In-memory repository for local development.
- JSON file persistence for projects in local MVP runs.
- Documented persistence decision: JSON is for MVP speed; database remains the production target.
- EF Core database model scaffold for the future relational persistence implementation.
- SQLite-backed project repository foundation for local relational persistence tests.
- SQLite-backed audit log foundation for local relational persistence tests.
- Database initialization for SQLite-backed local relational runs.
- MVP role permission matrix for Admin, Senior Engineer, Contributor and Viewer.
- Identity resolution foundation for claim-based roles with the MVP role selector as a demo fallback.
- API permission gates for project, document, review, roadmap, export, audit and operations endpoints.
- Tenancy mode is explicit in health and release readiness; MVP defaults to single-tenant operation.
- JSON-backed audit log for key project, document, knowledge, review, roadmap, traceability and export actions.
- Health endpoint with local storage, persistence and AI provider configuration status.
- Health output reports active persistence mode and database provider configuration.
- Health output reports the active document analysis guardrails.
- Health output reports the active knowledge extraction guardrails.
- Blazor operations panel shows health components for storage, persistence and AI provider status.
- Runtime metrics endpoint and Blazor operations panel show uptime, process and memory indicators.
- Structured operational logs for backup, restore, backup preview and release-readiness checks.
- JSON persistence backup and restore endpoints for local MVP operation.
- Backup preview before restore, including manifest, entries, warnings and upload counts.
- Security and data protection concepts for pilot/release review.
- Release readiness endpoint and dashboard check for runtime health, backups, persistence, authentication mode, security inventory, security/data protection review and manual release gates.
- GitHub Actions CI for restore, build, publish, dependency inventory, SPDX SBOM generation, container image inventory, container builds, tests, vulnerability checks, static source security scanning and basic secret scanning.
- Unit and architecture tests.

## Run locally

Start the API:

```powershell
dotnet run --project src\AiKnowledgeTransfer.Api\AiKnowledgeTransfer.Api.csproj --launch-profile http
```

API URL:

```text
http://localhost:5256
```

Start the web UI:

```powershell
dotnet run --project src\AiKnowledgeTransfer.Web\AiKnowledgeTransfer.Web.csproj --launch-profile http
```

Web URL:

```text
http://localhost:5280
```

The web UI can run the MVP workflow directly:

- Select an MVP role and see actions enabled or disabled by permission.
- Inspect the active demo identity source while the platform is prepared for claim-based roles.
- Create or select a project.
- Upload a text, Markdown, text-based PDF or Word `.docx` document.
- Inspect supported parser formats before analysis.
- Analyze the document into chunks.
- Inspect parser diagnostics after analysis.
- Inspect extracted chunks, quality labels and parser-specific source references per analyzed document.
- Extract knowledge items.
- Filter knowledge review items by type, review status and quality status.
- Inspect review summary counts for final, open and quality-gated knowledge.
- Run bulk review, approve or reject actions on the currently filtered knowledge list.
- Inspect source, provider and quality metadata for extracted knowledge items.
- Filter knowledge review items by type and review status.
- Submit, approve or reject knowledge items.
- Set or automatically derive review quality status while submitting, approving or rejecting knowledge items.
- Inspect the latest review history entries per knowledge item.
- Generate a review-aware roadmap.
- Inspect onboarding readiness before starting a new employee.
- Inspect the onboarding start package for the first supervised onboarding steps.
- Inspect roadmap weeks with learning goals, exercises, acceptance criteria and review notes.
- Load traceability and Markdown export output.
- Inspect export history after generated Markdown exports.
- Approve a generated Markdown export and inspect export approval history.
- Inspect traceability rows with source, knowledge, review state, review history, roadmap usage and export state.
- Inspect compliance rows with evidence status, export readiness, export approval and open gaps.
- Inspect and filter audit log events by action and target type.
- Inspect Markdown export metadata and section previews before reading the full export text.
- Check release readiness before deployment or data restore.

Local project data is persisted as JSON under the app's `App_Data` folder. Uploaded files are stored under `App_Data/uploads`.

Persistence note: the MVP intentionally starts with JSON to keep local setup fast while the domain model stabilizes. The production target remains PostgreSQL or SQL Server via a second infrastructure implementation behind the existing repository abstractions. See `docs/decisions/0001-mvp-json-persistence.md`.

Database migration preparation:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Json" # default
$env:AKT_DB_PROVIDER="Sqlite"
$env:AKT_DB_CONNECTION_STRING="Data Source=App_Data/knowledge-transfer.db"
$env:AKT_DB_SCHEMA_MODE="EnsureCreated"
```

`Json` remains the active default. SQLite is prepared as the first local relational provider for model and repository tests. Set `AKT_PERSISTENCE_PROVIDER=Database` with `AKT_DB_PROVIDER=Sqlite` to activate the database registration path; PostgreSQL or SQL Server can be added later for production-like deployments without changing Domain or Application.
When database mode is active, API and Web initialize the SQLite schema on startup.
`AKT_DB_SCHEMA_MODE=EnsureCreated` keeps the local MVP schema strategy. `AKT_DB_SCHEMA_MODE=Migrations` applies the managed EF initial migration and is the preferred path for production-like database pilots.

## Verify

```powershell
dotnet build AiKnowledgeTransfer.slnx
dotnet test AiKnowledgeTransfer.slnx
```

The GitHub Actions workflow runs restore, Release build, API/Web publish, transitive package inventory, SPDX SBOM generation, API/Web container builds, container image metadata inventory, tests, NuGet package vulnerability checks, a deterministic static source security scan and common secret pattern checks on `main` and pull requests. See `docs/operations/security-gates.md`.
For operational release steps, see `docs/operations/runbook.md`, `docs/operations/deployment.md`, `docs/security/security-concept.md` and `docs/security/data-protection-concept.md`.

## AI provider configuration

Knowledge extraction is provider-independent. OpenAI is the first implemented AI provider. If `OPENAI_API_KEY` is set, the API uses OpenAI first and falls back to the local heuristic extractor if the provider returns no items or fails. If no key is set, the local heuristic extractor is used directly.
The extraction API response and Blazor workflow report the provider name, fallback flag and provider detail for each extraction run.

Environment variables:

```powershell
$env:OPENAI_API_KEY="<your-api-key>"
$env:OPENAI_MODEL="gpt-5.4-mini"
$env:OPENAI_BASE_URL="https://api.openai.com/v1/"
```

`OPENAI_MODEL` and `OPENAI_BASE_URL` are optional. `OPENAI_BASE_URL` is intentionally configurable so future providers such as Azure OpenAI, customer-hosted AI gateways, or local model gateways can be added without changing the application layer.

## Document analysis limits

Uploads are capped at 10 MB. Parsed analysis output is additionally limited so unexpectedly large or noisy documents cannot create oversized analysis jobs.
If a parsed document exceeds these limits, the API returns `document_analysis_limit_exceeded` and the web UI recommends splitting, shortening or deliberately raising the configured limit.

Optional environment variables:

```powershell
$env:AKT_ANALYSIS_MAX_CHUNKS="250"
$env:AKT_ANALYSIS_MAX_CHARACTERS="500000"
```

Invalid or missing values fall back to the defaults above.

## Knowledge extraction limits

Knowledge extraction is additionally capped before provider calls, so large documents do not accidentally create oversized OpenAI, Azure OpenAI, customer-AI or local-model requests.

Optional environment variables:

```powershell
$env:AKT_EXTRACTION_MAX_CHUNKS="80"
$env:AKT_EXTRACTION_MAX_CHARACTERS="120000"
```

If a document exceeds these limits, the API returns `knowledge_extraction_limit_exceeded`. Invalid or missing values fall back to the defaults above.

## Authentication configuration

The local MVP defaults to demo identity mode. In demo mode, anonymous API requests resolve to the Viewer role and the Web UI role selector creates a demo identity for workflow testing.

OIDC configuration is prepared through environment variables:

```powershell
$env:AKT_AUTH_MODE="Oidc"
$env:AKT_AUTH_AUTHORITY="https://login.microsoftonline.com/<tenant-id>/v2.0"
$env:AKT_AUTH_CLIENT_ID="<application-client-id>"
$env:AKT_AUTH_ROLE_CLAIM="roles"
```

When `AKT_AUTH_MODE=Oidc`, health and release-readiness checks require authority and client id to be configured. The API enables JWT bearer authentication in OIDC mode and validates issuer, audience and the configured role claim. Anonymous requests are denied by permission gates in OIDC mode; demo mode keeps the local Viewer fallback for workflow testing.
Release readiness treats demo authentication as a warning: useful for local MVP work, but not sufficient for an enterprise deployment without a conscious release decision.
The Blazor workflow shows the active authentication mode. The demo role selector is only available in demo mode; in OIDC mode, Web permissions are evaluated through the same authorization service as API permission gates. The Web app exposes `/auth/login` and `/auth/logout` for cookie-backed OIDC sign-in and sign-out.

## Tenancy configuration

The MVP defaults to `SingleTenant`. Multi-tenant operation is planned, but complete tenant isolation still needs dedicated work across persistence, storage, audit, backup, API authorization and UI filtering. See `docs/decisions/0002-tenancy-strategy.md`.

Optional environment variables:

```powershell
$env:AKT_TENANCY_MODE="SingleTenant"
$env:AKT_TENANT_CLAIM="tenant_id"
$env:AKT_DEFAULT_TENANT_ID="default"
```

Health reports the active tenancy mode. Release readiness warns on `SingleTenant` and fails on invalid tenancy configuration.

## API examples

Create a project:

```http
POST http://localhost:5256/api/projects
Content-Type: application/json

{
  "name": "M3 Platform",
  "description": "Technical onboarding for system knowledge transfer",
  "owner": "Engineering"
}
```

Register a document:

```http
POST http://localhost:5256/api/projects/{projectId}/documents
Content-Type: application/json

{
  "fileName": "architecture.md",
  "contentType": "text/markdown",
  "source": "Architecture repository",
  "sizeInBytes": 2048
}
```

Upload a document file:

```http
POST http://localhost:5256/api/projects/{projectId}/documents/upload
Content-Type: multipart/form-data

file=<document file>
source=Manual upload
```

List supported document parsers:

```http
GET http://localhost:5256/api/document-parsers
```

Analyze an uploaded text or Markdown document:

```http
POST http://localhost:5256/api/projects/{projectId}/documents/{documentId}/analyze
```

Extract first knowledge items from an analyzed document:

```http
POST http://localhost:5256/api/projects/{projectId}/documents/{documentId}/extract-knowledge
```

Submit, approve or reject a knowledge item:

```http
POST http://localhost:5256/api/projects/{projectId}/knowledge-items/{knowledgeItemId}/submit-review
POST http://localhost:5256/api/projects/{projectId}/knowledge-items/{knowledgeItemId}/approve
POST http://localhost:5256/api/projects/{projectId}/knowledge-items/{knowledgeItemId}/reject
Content-Type: application/json

{
  "reviewer": "Senior Engineer",
  "comment": "Confirmed against source.",
  "qualityStatus": "Verified"
}
```

Bulk submit, approve or reject knowledge items:

```http
POST http://localhost:5256/api/projects/{projectId}/knowledge-items/bulk-submit-review
POST http://localhost:5256/api/projects/{projectId}/knowledge-items/bulk-approve
POST http://localhost:5256/api/projects/{projectId}/knowledge-items/bulk-reject
Content-Type: application/json

{
  "knowledgeItemIds": [
    "00000000-0000-0000-0000-000000000000"
  ],
  "reviewer": "Senior Engineer",
  "comment": "Batch confirmed against source.",
  "qualityStatus": "Verified"
}
```

Get review statistics for a project:

```http
GET http://localhost:5256/api/projects/{projectId}/knowledge-items/review-summary
```

Generate a roadmap:

```http
POST http://localhost:5256/api/projects/{projectId}/roadmaps
Content-Type: application/json

{
  "targetRole": "Support Engineer",
  "durationInWeeks": 4
}
```

Generated roadmap weeks include `reviewNotes`. Only approved knowledge with `qualityStatus` `Verified` is added to learning goals and exercises where it fits; draft, in-review, rejected or not-yet-verified items remain visible as review notes.

Check onboarding readiness:

```http
GET http://localhost:5256/api/projects/{projectId}/onboarding-readiness
```

Get the onboarding start package:

```http
GET http://localhost:5256/api/projects/{projectId}/onboarding-start-package
```

Export a project handover document as Markdown:

```http
GET http://localhost:5256/api/projects/{projectId}/exports/markdown
```

Get export history:

```http
GET http://localhost:5256/api/projects/{projectId}/exports/history
```

Approve and list Markdown export approvals:

```http
POST http://localhost:5256/api/projects/{projectId}/exports/approvals
Content-Type: application/json

{
  "fileName": "project-knowledge-transfer.md",
  "reviewer": "Senior Engineer",
  "comment": "Ready for supervised onboarding."
}

GET http://localhost:5256/api/projects/{projectId}/exports/approvals
```

Get the traceability matrix:

```http
GET http://localhost:5256/api/projects/{projectId}/traceability
```

Get the compliance matrix:

```http
GET http://localhost:5256/api/projects/{projectId}/compliance
```

Get the MVP role permission matrix:

```http
GET http://localhost:5256/api/security/roles
```

Get the current resolved user:

```http
GET http://localhost:5256/api/security/current-user
```

API endpoints enforce the same permission model as the Web UI. In demo mode, anonymous requests resolve to the Viewer role; mutating endpoints require a role claim with the necessary permission. In OIDC mode, protected API endpoints require a valid bearer token before permissions are evaluated.

Get audit events:

```http
GET http://localhost:5256/api/audit
GET http://localhost:5256/api/projects/{projectId}/audit
```

Get operational health:

```http
GET http://localhost:5256/health
```

Get release readiness:

```http
GET http://localhost:5256/api/operations/release-readiness
```

Get runtime metrics:

```http
GET http://localhost:5256/api/operations/runtime-metrics
```

Create, list and restore local JSON persistence backups:

```http
POST http://localhost:5256/api/operations/backups
GET http://localhost:5256/api/operations/backups
POST http://localhost:5256/api/operations/backups/{fileName}/restore
```
