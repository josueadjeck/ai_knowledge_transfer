# Deployment Baseline

The MVP deployment baseline is a release publish of the API and Web projects plus container image builds that are verified in CI.

## Local Publish

```powershell
dotnet publish src\AiKnowledgeTransfer.Api\AiKnowledgeTransfer.Api.csproj --configuration Release --output artifacts\api
dotnet publish src\AiKnowledgeTransfer.Web\AiKnowledgeTransfer.Web.csproj --configuration Release --output artifacts\web
```

The `artifacts/` folder is ignored by Git.

## Container Build

```powershell
docker build --file Dockerfile.api --tag ai-knowledge-transfer-api:local .
docker build --file Dockerfile.web --tag ai-knowledge-transfer-web:local .
```

Both images listen on container port `8080`. Persist `/app/App_Data` when using local JSON, SQLite or backup files.

Example API run:

```powershell
docker run --rm -p 5256:8080 -v ${PWD}\App_Data\Api:/app/App_Data -e AKT_PERSISTENCE_PROVIDER=Json ai-knowledge-transfer-api:local
```

Example Web run:

```powershell
docker run --rm -p 5280:8080 -v ${PWD}\App_Data\Web:/app/App_Data -e AKT_PERSISTENCE_PROVIDER=Json ai-knowledge-transfer-web:local
```

## Runtime Configuration

For local MVP or pilot operation:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Json"
$env:AKT_TENANCY_MODE="SingleTenant"
```

For enterprise-style pilot operation:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Database"
$env:AKT_DB_PROVIDER="Sqlite"
$env:AKT_DB_CONNECTION_STRING="Data Source=App_Data/knowledge-transfer.db"
$env:AKT_DB_SCHEMA_MODE="EnsureCreated"
$env:AKT_AUTH_MODE="Oidc"
$env:AKT_AUTH_AUTHORITY="https://login.microsoftonline.com/<tenant-id>/v2.0"
$env:AKT_AUTH_CLIENT_ID="<application-client-id>"
$env:AKT_AUTH_ROLE_CLAIM="roles"
```

Optional AI provider configuration:

```powershell
$env:OPENAI_API_KEY="<your-api-key>"
$env:OPENAI_MODEL="gpt-5.4-mini"
$env:OPENAI_BASE_URL="https://api.openai.com/v1/"
```

## Deployment Checks

Before deploying:

1. Confirm CI restore, build, publish, container build, tests, vulnerability scan and secret scan passed.
2. Confirm `GET /health` is `ok`.
3. Confirm `GET /api/operations/release-readiness` has no `Fail` checks.
4. Create and preview a backup.
5. Confirm warning and manual checks are accepted by a release owner.

Database deployments currently use EF `EnsureCreated` as the MVP schema mode. Treat the database schema warning as a release-owner decision until managed EF migrations are added.

## Current Limits

The MVP deployment baseline does not yet provide image signing, SBOM export, external secret management, managed database migrations, blue/green deployment or tenant-isolated production storage. Those remain Phase 9 follow-up work.
