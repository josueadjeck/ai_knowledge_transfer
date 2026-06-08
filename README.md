# AI Knowledge Transfer Platform

MVP foundation for an AI-assisted knowledge transfer, onboarding and compliance platform for technical systems.

## Current implementation

- .NET 10 solution with modular monolith structure.
- Blazor Web project with an interactive MVP workflow.
- User-facing error feedback for failed workflow operations.
- ASP.NET Core API with project, document and roadmap endpoints.
- Consistent API validation and JSON error responses for common failure cases.
- Multipart document upload with local file storage.
- Central upload validation for supported file types and 10 MB maximum file size.
- Document cards show source, content type, file size, version, upload time and storage status.
- Text and Markdown parsing into document chunks.
- PDF text extraction into document chunks for text-based PDF files.
- Word `.docx` text extraction into document chunks.
- Shared chunking logic across text, PDF and Word parsers.
- Provider-independent knowledge extraction with OpenAI as the first AI provider and heuristic fallback.
- Knowledge extraction responses and the Blazor workflow show the active provider and whether fallback was used.
- Extracted knowledge items retain source chunk, provider, model, fallback and quality metadata for review, traceability and export.
- Review workflow for extracted knowledge items.
- Editable reviewer and review comment fields in the Blazor review workflow.
- Review actions can update knowledge quality status such as `NeedsClarification`, `Verified` or `RejectedSource`.
- Roadmap generation that prefers approved knowledge and marks unapproved items as review notes.
- Markdown export for project handover documents.
- Traceability matrix for source, knowledge, review and roadmap/export usage.
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
- JSON-backed audit log for key project, document, knowledge, review, roadmap, traceability and export actions.
- Health endpoint with local storage, persistence and AI provider configuration status.
- Health output reports active persistence mode and database provider configuration.
- Blazor operations panel shows health components for storage, persistence and AI provider status.
- JSON persistence backup and restore endpoints for local MVP operation.
- Backup preview before restore, including manifest, entries, warnings and upload counts.
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
- Create or select a project.
- Upload a text, Markdown, text-based PDF or Word `.docx` document.
- Analyze the document into chunks.
- Inspect extracted chunks per analyzed document.
- Extract knowledge items.
- Inspect source, provider and quality metadata for extracted knowledge items.
- Filter knowledge review items by type and review status.
- Submit, approve or reject knowledge items.
- Set or automatically derive review quality status while submitting, approving or rejecting knowledge items.
- Generate a review-aware roadmap.
- Inspect roadmap weeks with learning goals, exercises, acceptance criteria and review notes.
- Load traceability and Markdown export output.
- Inspect traceability rows with source, knowledge, review state, roadmap usage and export state.
- Inspect and filter audit log events by action and target type.
- Inspect Markdown export metadata and section previews before reading the full export text.

Local project data is persisted as JSON under the app's `App_Data` folder. Uploaded files are stored under `App_Data/uploads`.

Persistence note: the MVP intentionally starts with JSON to keep local setup fast while the domain model stabilizes. The production target remains PostgreSQL or SQL Server via a second infrastructure implementation behind the existing repository abstractions. See `docs/decisions/0001-mvp-json-persistence.md`.

Database migration preparation:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Json" # default
$env:AKT_DB_PROVIDER="Sqlite"
$env:AKT_DB_CONNECTION_STRING="Data Source=App_Data/knowledge-transfer.db"
```

`Json` remains the active default. SQLite is prepared as the first local relational provider for model and repository tests. Set `AKT_PERSISTENCE_PROVIDER=Database` with `AKT_DB_PROVIDER=Sqlite` to activate the database registration path; PostgreSQL or SQL Server can be added later for production-like deployments without changing Domain or Application.
When database mode is active, API and Web initialize the SQLite schema on startup.

## Verify

```powershell
dotnet build AiKnowledgeTransfer.slnx
dotnet test AiKnowledgeTransfer.slnx
```

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

Generate a roadmap:

```http
POST http://localhost:5256/api/projects/{projectId}/roadmaps
Content-Type: application/json

{
  "targetRole": "Support Engineer",
  "durationInWeeks": 4
}
```

Generated roadmap weeks include `reviewNotes`. Approved knowledge items are added to learning goals and exercises where they fit; draft, in-review or rejected items remain visible as review notes.

Export a project handover document as Markdown:

```http
GET http://localhost:5256/api/projects/{projectId}/exports/markdown
```

Get the traceability matrix:

```http
GET http://localhost:5256/api/projects/{projectId}/traceability
```

Get the MVP role permission matrix:

```http
GET http://localhost:5256/api/security/roles
```

Get audit events:

```http
GET http://localhost:5256/api/audit
GET http://localhost:5256/api/projects/{projectId}/audit
```

Get operational health:

```http
GET http://localhost:5256/health
```

Create, list and restore local JSON persistence backups:

```http
POST http://localhost:5256/api/operations/backups
GET http://localhost:5256/api/operations/backups
POST http://localhost:5256/api/operations/backups/{fileName}/restore
```
