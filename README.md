# AI Knowledge Transfer Platform

MVP foundation for an AI-assisted knowledge transfer, onboarding and compliance platform for technical systems.

## Current implementation

- .NET 10 solution with modular monolith structure.
- Blazor Web project with an interactive MVP workflow.
- User-facing error feedback for failed workflow operations.
- ASP.NET Core API with project, document and roadmap endpoints.
- Consistent API validation and JSON error responses for common failure cases.
- Multipart document upload with local file storage.
- Text and Markdown parsing into document chunks.
- Provider-independent knowledge extraction with OpenAI as the first AI provider and heuristic fallback.
- Review workflow for extracted knowledge items.
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
- MVP role permission matrix for Admin, Senior Engineer, Contributor and Viewer.
- JSON-backed audit log for key project, document, knowledge, review, roadmap, traceability and export actions.
- Health endpoint with local storage, persistence and AI provider configuration status.
- JSON persistence backup and restore endpoints for local MVP operation.
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
- Upload a text or Markdown document.
- Analyze the document into chunks.
- Extract knowledge items.
- Submit, approve or reject knowledge items.
- Generate a review-aware roadmap.
- Load traceability and Markdown export output.

Local project data is persisted as JSON under the app's `App_Data` folder. Uploaded files are stored under `App_Data/uploads`.

Persistence note: the MVP intentionally starts with JSON to keep local setup fast while the domain model stabilizes. The production target remains PostgreSQL or SQL Server via a second infrastructure implementation behind the existing repository abstractions. See `docs/decisions/0001-mvp-json-persistence.md`.

Database migration preparation:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Json"
$env:AKT_DB_PROVIDER="SqlServer"
$env:AKT_DB_CONNECTION_STRING="<connection-string>"
```

`Json` remains the active default. The EF Core model exists in Infrastructure so the next implementation can add a provider package and database-backed repositories without changing Domain or Application.

## Verify

```powershell
dotnet build AiKnowledgeTransfer.slnx
dotnet test AiKnowledgeTransfer.slnx
```

## AI provider configuration

Knowledge extraction is provider-independent. OpenAI is the first implemented AI provider. If `OPENAI_API_KEY` is set, the API uses OpenAI first and falls back to the local heuristic extractor if the provider returns no items or fails. If no key is set, the local heuristic extractor is used directly.

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
  "comment": "Confirmed against source."
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
