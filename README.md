# Valetax

ASP.NET Core 8 backend for managing independent trees with exception journaling, JWT authentication, PostgreSQL persistence, and local observability tooling.

## Tech Stack

- ASP.NET Core 8 Web API
- Entity Framework Core 8
- PostgreSQL
- Serilog
- OpenTelemetry
- Prometheus
- Grafana
- Jaeger
- xUnit + `WebApplicationFactory` + Testcontainers

## Solution Structure

```text
src/
  Valetax.Api
  Valetax.Application
  Valetax.Domain
  Valetax.Infrastructure
tests/
  Valetax.UnitTests
  Valetax.IntegrationTests
```

## Implemented Features

- Tree retrieval by name with automatic tree creation
- Node creation, rename, and subtree deletion
- Exception journal persistence and journal read API
- Global exception middleware with `SecureException` handling
- JWT token issuance via `rememberMe`
- Authorization on tree and journal endpoints
- Docker-based local development
- Full local observability stack

## API Overview

### Anonymous endpoint

- `POST /api.user.partner.rememberMe`

### Authorized endpoints

- `POST /api.user.tree.get`
- `POST /api.user.tree.node.create`
- `POST /api.user.tree.node.rename`
- `POST /api.user.tree.node.delete`
- `POST /api.user.journal.getRange`
- `POST /api.user.journal.getSingle`

## Local Run with Docker

Minimal stack:

```powershell
docker compose up --build -d
```

Full stack with Grafana, Prometheus, Jaeger, and Loki:

```powershell
docker compose -f docker-compose-full.yml up --build -d
```

Stop the stack:

```powershell
docker compose down
docker compose -f docker-compose-full.yml down
```

### Default URLs

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Jaeger: `http://localhost:16686`
- Loki: `http://localhost:3100`
- Prometheus scrape endpoint: `http://localhost:8080/metrics`

### Default Grafana Credentials

- Username: `admin`
- Password: `admin`

## Local Run Outside Docker

If the API is started outside Docker, configure authentication secrets for `src/Valetax.Api` with `user-secrets`:

```powershell
dotnet user-secrets set "Authentication:RememberMeCode" "valetax-dev-code" --project src/Valetax.Api
dotnet user-secrets set "Authentication:Jwt:Issuer" "Valetax" --project src/Valetax.Api
dotnet user-secrets set "Authentication:Jwt:Audience" "Valetax" --project src/Valetax.Api
dotnet user-secrets set "Authentication:Jwt:SigningKey" "valetax-dev-signing-key-32-characters-min" --project src/Valetax.Api
```

The local PostgreSQL connection string is already configured in `appsettings.json` for `localhost:5432`.

## Authentication

Use the development remember-me code:

- `valetax-dev-code`

Example token request:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8080/api.user.partner.rememberMe?code=valetax-dev-code"
```

The response contains a JWT token:

```json
{
  "token": "<jwt>"
}
```

Use it as a Bearer token for all tree and journal endpoints.

Swagger UI is configured with Bearer authentication, so the token can also be entered through the `Authorize` button.

## Exception Handling

All unhandled request exceptions are written to the exception journal.

Behavior:

- `SecureException` returns HTTP 500 with the original business message
- any other exception returns HTTP 500 with a generic message and event id

The event id from the error response can be used with:

- `POST /api.user.journal.getSingle?id=<eventId>`

## Observability

### Logs

- Serilog writes to file
- full compose additionally pushes logs to Loki

### Traces

- ASP.NET Core and `HttpClient` instrumentation are enabled
- custom application spans are emitted for the main use cases
- traces are exported to Jaeger in the full stack

### Metrics

- ASP.NET Core, runtime, and `HttpClient` metrics are enabled
- custom application metrics are exposed:
  - `valetax_use_case_executions`
  - `valetax_use_case_duration_ms`

Grafana is provisioned with:

- Prometheus datasource
- Loki datasource
- Jaeger datasource
- `Valetax Observability` dashboard

## Database and Migrations

Migrations are stored in:

- `src/Valetax.Infrastructure/Persistence/Migrations`

Docker runs with automatic migration on startup.

Manual commands:

```powershell
dotnet dotnet-ef migrations add <Name> --project src/Valetax.Infrastructure --startup-project src/Valetax.Api --output-dir Persistence/Migrations
dotnet dotnet-ef database update --project src/Valetax.Infrastructure --startup-project src/Valetax.Api
```

## Tests

Run all tests:

```powershell
dotnet test Valetax.sln -p:UseAppHost=false
```

Run integration tests only:

```powershell
dotnet test tests/Valetax.IntegrationTests/Valetax.IntegrationTests.csproj -p:UseAppHost=false
```

Integration tests use:

- `WebApplicationFactory`
- PostgreSQL Testcontainers for DB-backed scenarios

## Notes

- The repository is pinned to .NET SDK 8 through `global.json`.
- NuGet lock files are enabled through `packages.lock.json`.
- Internal test controllers are available only in `Development` and are hidden from Swagger.
