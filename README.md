# SmartAuth

**Polski README:** [readme_pl.md](readme_pl.md)

SmartAuth is a reference authentication system built with .NET 10, PostgreSQL (pgvector), and a React + Vite SPA. The current implementation focuses on email/password authentication and a basic 2FA flow, with additional methods (TOTP, biometrics) planned as the platform evolves.

## Features (current)
- Email + password registration and login.
- Optional 2FA step with a temporary token flow.
- Minimal API backend with EF Core and PostgreSQL.
- React SPA served via the ASP.NET host in development and production builds.
- Aspire AppHost orchestration for local development.

## Solution layout
- `SmartAuth.AppHost` — Aspire app host orchestrating services (including Postgres).
- `SmartAuth.Api` — Minimal API and authentication logic.
- `SmartAuth.Web` — ASP.NET host for the SPA.
- `SmartAuth.Web/ClientApp` — React + Vite client.
- `SmartAuth.Infrastructure` — EF Core DbContext, migrations, pgvector integration.
- `SmartAuth.Domain` — domain entities and shared types.
- `SmartAuth.ServiceDefaults` — observability defaults (OpenTelemetry, health checks).
- `SmartAuth.Tests` — integration tests (xUnit + Testcontainers).

## Implementation details
### Minimal API usage
- `SmartAuth.Api` uses ASP.NET Minimal APIs with endpoint registration encapsulated in extension methods like `UseAuthEndpoints` and `UseFeatureFlagEndpoints`.
- Shared behaviors are applied through endpoint filters (for example, `MediatorEndpointFilter`).

### Own mediator implementation
- A custom mediator lives in `SmartAuth.Infrastructure/Commons/Mediator.cs` and defines `IRequest`, `IRequestHandler`, and `IMediator`.
- Requests run through validation and then dispatch to handlers, returning `CommandResult` or `CommandResult<T>`.

### Model fetcher on start
- `SmartAuth.AppHost` calls `ModelFetcher.TryFetchModelsIfNeeded` during startup.
- It reads the `ModelFetching` configuration and downloads missing model files using an embedded PowerShell script.

### Auth flow
- **Register**: email + password → create user.
- **Login**: validate credentials; if 2FA is required, return a temporary JWT and available methods.
- **2FA verification**: validate the method-specific code and exchange the temporary token for the final access JWT.

### Response standards
- Handlers return `CommandResult`/`CommandResult<T>` and are mapped to HTTP responses.
- Success: `200 OK` with the payload.
- Errors: JSON with `code`, `message`, `detail`, `status`, `metadata`, and `traceId`.

## Requirements
- .NET SDK 10
- Node.js (LTS recommended) + npm
- Docker (for local database and Testcontainers)

## Quick start (Aspire)
Aspire is the recommended way to run the app locally.

```bash
dotnet run --project SmartAuth.AppHost
```

After startup, check the console for the assigned ports:
- API Swagger: `http://localhost:<api-port>/swagger`
- SPA: `http://localhost:<web-port>/`

## Running projects directly (optional)
If you want to run services without Aspire:

1. Start PostgreSQL with pgvector (example Docker command).
2. Set the connection string:

```bash
export ConnectionStrings__authdb="Host=localhost;Port=5432;Database=authdb;Username=postgres;Password=postgres"
```

3. Run API and Web:

```bash
dotnet run --project SmartAuth.Api

dotnet run --project SmartAuth.Web
```

## Configuration
Key settings live in `appsettings.json` and can be overridden by environment variables:
- `ConnectionStrings:authdb`
- `Jwt` (issuer, audience, key, token lifetimes)
- `FeatureFlags` (enables optional flows)

Example override:

```bash
export Jwt__Key="your-strong-dev-secret"
```

## Tests
Backend:
```bash
dotnet test SmartAuth.sln
```

Frontend:
```bash
cd SmartAuth.Web/ClientApp
npm ci
npm test
```

### Unit tests
- Backend unit/integration tests live in `SmartAuth.Tests` and are executed via `dotnet test`.
- Frontend unit tests run with Vitest from `SmartAuth.Web/ClientApp`.

### Frontend development
- The React SPA lives in `SmartAuth.Web/ClientApp` and uses Vite.
- Run the dev server with `npm run dev` and use the ASP.NET host for API proxying when needed.

## Notes
- The 2FA code flow currently uses a placeholder code in development.
- Aspire AppHost manages service discovery and local container setup.

---
If you need more detail in any area (API contracts, security notes, deployment), tell me and I’ll expand that section.
