# AI Knowledge Transfer Platform

MVP foundation for an AI-assisted knowledge transfer, onboarding and compliance platform for technical systems.

## Current implementation

- .NET 10 solution with modular monolith structure.
- Blazor Web project with MVP dashboard.
- ASP.NET Core API with project, document and roadmap endpoints.
- Multipart document upload with local file storage.
- Text and Markdown parsing into document chunks.
- Heuristic knowledge extraction for glossary terms, workflow candidates and open questions.
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

Generate a roadmap:

```http
POST http://localhost:5256/api/projects/{projectId}/roadmaps
Content-Type: application/json

{
  "targetRole": "Support Engineer",
  "durationInWeeks": 4
}
```
