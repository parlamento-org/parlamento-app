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

The repository currently contains:

- legacy SQLite migrations in `src/Migrations/`
- a PostgreSQL baseline migration in `Infrastructure/Migrations/`

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
