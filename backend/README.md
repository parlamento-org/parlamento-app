# Backend API

ASP.NET Core Web API for the Parlamento app.

## Current structure

- `src/`: API layer (`Program.cs`, controllers, Swagger, HTTP-only concerns)
- `Application/`: request DTOs, validators, service contracts
- `Domain/`: entities and enums
- `Infrastructure/`: `DbContext`, service implementations, PostgreSQL migration baseline
- `test/`: unit-test project
- `integration-tests/`: integration-test project

## Database

Primary database: PostgreSQL via `ConnectionStrings__DefaultConnection`.

There is still a SQLite fallback in code for local compatibility, but Docker now targets PostgreSQL and no longer depends on `db.sqlite`.

Configuration is loaded by ASP.NET Core from `appsettings.json`, environment-specific appsettings files, environment variables, and command-line arguments. Environment variables override appsettings values. Nested keys use double underscores, for example `ParliamentOpenData__LatestLegislature`.

### Example connection string

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=parlamento;Username=parlamento;Password=parlamento"
```

## Docker

Start the development stack:

```powershell
docker compose up postgres-parlamento db-parlamento-migrate dev-parlamento-be dev-parlamento-fe
```

Start only the backend side:

```powershell
docker compose up postgres-parlamento db-parlamento-migrate dev-parlamento-be
```

Production-style backend container:

```powershell
docker compose up postgres-parlamento db-parlamento-migrate prod-parlamento-be
```

## Migrations

Apply migrations through the dedicated migration container:

```powershell
docker compose up db-parlamento-migrate
```

If migration files changed, rebuild the migration image before running it:

```powershell
docker compose build --no-cache db-parlamento-migrate
docker compose up db-parlamento-migrate
```

The repository currently contains:

- legacy SQLite migrations in `src/Migrations/`
- PostgreSQL migrations in `Infrastructure/Migrations/`

The importer schema stores compact source metadata and parsed source traceability rows. It keeps `ProjectLaw.SourceHash` for update detection, but does not persist the full raw Open Data JSON payload.

## Parliament Open Data Import

The backend can import Portuguese Parliament Open Data initiatives into PostgreSQL. The importer:

- reads local JSON files for development and tests
- downloads configured legislature feeds in production
- supports both single-object and array-root JSON feeds
- streams array feeds instead of loading the full legislature file into memory
- skips initiatives without a usable phase `250` generality vote
- upserts initiatives by source id and source hash
- records import runs, skips, and per-record errors
- stores core proposal fields on `ProjectLaw`
- stores source traceability in imported author, event, vote, vote block, document, publication, and intervention tables

`VotingResult` and `VotingBlock` are the older app-facing vote summary tables used by the current proposal/feed flow. `ParliamentInitiativeVote` and `ParliamentInitiativeVoteBlock` store all official source vote events and parsed party blocks for traceability.

### Import configuration

Open Data URLs are configured under `ParliamentOpenData`. `appsettings.json` contains defaults for legislatures `XV`, `XVI`, and `XVII`.

For Docker Compose, the root `.env` file provides substitution values, and `docker-compose.yml` passes them into backend containers as ASP.NET environment variables.

Important keys:

```text
ParliamentOpenData__LatestLegislature=XVII
ParliamentOpenData__DailyImport__Enabled=false
ParliamentOpenData__DailyImport__TimeZoneId=Europe/Lisbon
ParliamentOpenData__DailyImport__RunAt=00:00:00
ParliamentOpenData__Legislatures__XV__InitiativesUrl=...
ParliamentOpenData__Legislatures__XVI__InitiativesUrl=...
ParliamentOpenData__Legislatures__XVII__InitiativesUrl=...
```

`appsettings.json` works in production if it is copied into the deployed app, but environment variables are preferred for deployments because they can vary per environment and override image-baked defaults without rebuilding the image.

### Command mode

The API executable also supports one-shot import commands. Command mode builds the normal dependency injection container, runs the import, logs the result, and exits without starting HTTP listeners.

Import a local sample file:

```powershell
dotnet run --project backend/src -- parliament-import --file docs/samples/example_iniciativa.json
```

Import configured legislatures:

```powershell
dotnet run --project backend/src -- parliament-import --legislatures XVII
dotnet run --project backend/src -- parliament-import --legislatures XVII,XVI,XV
dotnet run --project backend/src -- parliament-import --legislatures XVII XVI XV
```

If no legislature is supplied, command mode falls back to `ParliamentOpenData:LatestLegislature`.

### HTTP import endpoints

Manual import endpoints are exposed by `ParliamentImportController`:

```text
POST /parliament-import/local-file
POST /parliament-import/legislatures/{legislature}
POST /parliament-import/legislatures
```

Example local-file body:

```json
{
  "filePath": "docs/samples/example_iniciativa.json"
}
```

Example multi-legislature body:

```json
{
  "legislatures": ["XVII", "XVI", "XV"]
}
```

### Daily background job

The backend registers a hosted service for scheduled imports. It is disabled by default.

When enabled, it runs once per day at `ParliamentOpenData:DailyImport:RunAt` in `ParliamentOpenData:DailyImport:TimeZoneId`, and imports only `ParliamentOpenData:LatestLegislature`.

Enable it with environment variables:

```powershell
$env:ParliamentOpenData__DailyImport__Enabled="true"
$env:ParliamentOpenData__LatestLegislature="XVII"
```

The daily job is intended for the latest legislature only. Older legislatures can be imported manually with command mode or HTTP endpoints.

## Local run without Docker

From `backend/src/`:

```powershell
dotnet run
```

For local PostgreSQL, set `ConnectionStrings__DefaultConnection` first.

## Tests

Unit tests:

```powershell
dotnet test backend/test/test.csproj
```

Integration tests:

```powershell
dotnet test backend/integration-tests/integration-tests.csproj
```

## API documentation

Swagger/OpenAPI is exposed by the development API at:

`http://localhost:8180/swagger/index.html`

Health check endpoint:

`http://localhost:8180/healthz`
