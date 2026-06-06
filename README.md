# AI Knowledge Transfer Platform

MVP foundation for an AI-assisted knowledge transfer, onboarding and compliance platform for technical systems.

## Current implementation

- .NET 10 solution with modular monolith structure.
- Blazor Web project with MVP dashboard.
- ASP.NET Core API with project, document and roadmap endpoints.
- Multipart document upload with local file storage.
- Text and Markdown parsing into document chunks.
- Provider-independent knowledge extraction with OpenAI as the first AI provider and heuristic fallback.
- Review workflow for extracted knowledge items.
- Roadmap generation that prefers approved knowledge and marks unapproved items as review notes.
- Project details endpoint with documents, knowledge items and generated roadmaps.
- Domain model for projects, document versions, knowledge items and onboarding roadmaps.
- Application services for project creation, document registration and deterministic roadmap generation.
- In-memory repository for local development.
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
