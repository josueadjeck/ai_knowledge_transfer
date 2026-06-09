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
$env:AKT_TENANT_BOUNDARY_MODE="DedicatedDeployment"
$env:AKT_TENANT_BOUNDARY_OWNER="Operations"
```

For enterprise-style pilot operation:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Database"
$env:AKT_DB_PROVIDER="Sqlite"
$env:AKT_DB_CONNECTION_STRING="Data Source=App_Data/knowledge-transfer.db"
$env:AKT_DB_SCHEMA_MODE="Migrations"
$env:AKT_AUTH_MODE="Oidc"
$env:AKT_AUTH_AUTHORITY="https://login.microsoftonline.com/<tenant-id>/v2.0"
$env:AKT_AUTH_CLIENT_ID="<application-client-id>"
$env:AKT_AUTH_ROLE_CLAIM="roles"
$env:AKT_TENANT_BOUNDARY_MODE="DedicatedDeployment"
$env:AKT_TENANT_BOUNDARY_OWNER="Operations"
$env:AKT_SECRET_STORE_MODE="SecretStore"
$env:AKT_SECRET_STORE_PROVIDER="AzureKeyVault"
$env:AKT_SECRET_ROTATION_OWNER="Operations"
$env:AKT_DEPLOYMENT_STRATEGY="BlueGreen"
$env:AKT_DEPLOYMENT_APPROVAL_OWNER="Release Owner"
```

For a SQL Server backed pilot or production-like deployment, use `AKT_DB_PROVIDER=SqlServer` and provide a SQL Server connection string through the approved secret manager or hosting platform:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Database"
$env:AKT_DB_PROVIDER="SqlServer"
$env:AKT_DB_CONNECTION_STRING="<sql-server-connection-string>"
$env:AKT_DB_SCHEMA_MODE="Migrations"
```

For a PostgreSQL backed pilot or production-like deployment, use `AKT_DB_PROVIDER=Postgres` and provide a PostgreSQL connection string through the approved secret manager or hosting platform:

```powershell
$env:AKT_PERSISTENCE_PROVIDER="Database"
$env:AKT_DB_PROVIDER="Postgres"
$env:AKT_DB_CONNECTION_STRING="<postgres-connection-string>"
$env:AKT_DB_SCHEMA_MODE="Migrations"
```

Optional AI provider configuration:

```powershell
$env:OPENAI_API_KEY="<your-api-key>"
$env:OPENAI_MODEL="gpt-5.4-mini"
$env:OPENAI_BASE_URL="https://api.openai.com/v1/"
```

## Deployment Checks

Before deploying:

1. Confirm CI restore, build, publish, build artifact inventory, dependency inventory, SPDX SBOM generation, CycloneDX build SBOM generation, container policy scan, container image vulnerability scan, container image inventory, container image provenance, container signing policy scan, container build, tests, vulnerability scan, static source security scan and secret scan passed.
2. Confirm `GET /health` is `ok`.
3. Confirm `GET /api/operations/monitoring-summary` is `ok` or contains only accepted manual release-review signals.
4. Confirm `GET /api/operations/release-readiness` has no `Fail` checks.
5. Create and preview a backup.
6. Confirm `OPENAI_API_KEY`, database credentials and OIDC/client credentials are injected by an approved secret manager or platform secret store and `AKT_SECRET_STORE_MODE=SecretStore` has provider and rotation-owner metadata.
7. Confirm warning and manual checks are accepted by a release owner.

Database deployments can use `AKT_DB_SCHEMA_MODE=Migrations` to apply the managed EF initial migration. `EnsureCreated` remains available for local MVP runs but is reported as a release-readiness warning in database mode. Release readiness warns for SQLite and passes SQL Server or PostgreSQL as production-capable relational providers.

## Blue/Green Release Mode

`AKT_DEPLOYMENT_STRATEGY=SingleSlot` is the local default and remains a manual release review item because downtime and rollback risk must be accepted.

`AKT_DEPLOYMENT_STRATEGY=BlueGreen` is the production-oriented mode. It requires `AKT_DEPLOYMENT_APPROVAL_OWNER` so the release-readiness check can confirm that a human owner is accountable for slot switch, validation and rollback approval.

The concrete slot implementation remains platform-specific. Use the target hosting platform's blue/green, deployment-slot or traffic-switching mechanism, validate `GET /health`, `GET /api/operations/monitoring-summary` and smoke-test the Web UI on the green slot before switching traffic.

## Current Limits

The MVP deployment baseline generates build artifact file hashes, SPDX dependency SBOM, CycloneDX build SBOM and container provenance, validates registry-backed image signing policy metadata, validates secret-store release metadata, supports a blue/green release-readiness mode and supports dedicated single-tenant deployment isolation. It does not yet provide platform-specific registry push/sign/verify automation, external SBOM attestation storage and retention, automated provider-specific secret retrieval or shared multi-tenant production storage.
