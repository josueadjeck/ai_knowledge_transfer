# Deployment Baseline

The MVP deployment baseline is a release publish of the API and Web projects. Container images can be added later, but the first deployment gate is that both applications can be published as deterministic Release artifacts.

## Local Publish

```powershell
dotnet publish src\AiKnowledgeTransfer.Api\AiKnowledgeTransfer.Api.csproj --configuration Release --output artifacts\api
dotnet publish src\AiKnowledgeTransfer.Web\AiKnowledgeTransfer.Web.csproj --configuration Release --output artifacts\web
```

The `artifacts/` folder is ignored by Git.

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

1. Confirm CI restore, build, publish, tests, vulnerability scan and secret scan passed.
2. Confirm `GET /health` is `ok`.
3. Confirm `GET /api/operations/release-readiness` has no `Fail` checks.
4. Create and preview a backup.
5. Confirm warning and manual checks are accepted by a release owner.

## Current Limits

The MVP deployment baseline does not yet provide container image hardening, external secret management, managed database migrations, blue/green deployment or tenant-isolated production storage. Those remain Phase 9 follow-up work.
