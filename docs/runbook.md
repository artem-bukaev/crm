# Runbook

## Requirements

- .NET SDK 10
- Node.js 24+ and npm
- PostgreSQL reachable through a connection string

Connection string:

```bash
export ConnectionStrings__CrmDb='Host=localhost;Port=5432;Database=crm;Username=postgres;Password=postgres'
```

## Authentication Configuration

Configuration keys (section `Auth`):

| Key | Purpose | Dev default |
| --- | --- | --- |
| `Auth:Jwt:SigningKey` | Symmetric JWT signing key, required, min 32 chars. Keep it in secrets/env (`Auth__Jwt__SigningKey`) outside Development. | `crm-dev-only-jwt-signing-key-do-not-use-in-production` (appsettings.Development.json) |
| `Auth:Jwt:Issuer` | JWT issuer | `crm-api` |
| `Auth:Jwt:Audience` | JWT audience | `crm-clients` |
| `Auth:Jwt:ExpiryMinutes` | Token lifetime | `480` |
| `Auth:SeedAdmin:Email` | Seed admin email (seeded only when the Users table is empty; skipped when blank) | `admin@crm.local` |
| `Auth:SeedAdmin:Password` | Seed admin password | `Admin123!` |
| `Auth:SeedAdmin:DisplayName` | Seed admin display name | `Dev Admin` |

Dev admin credentials: `admin@crm.local` / `Admin123!` (Development only; override through configuration for any shared environment).

Login and use the API as a human:

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@crm.local","password":"Admin123!"}' | jq -r .token)

curl -s http://localhost:5080/api/auth/me -H "Authorization: Bearer $TOKEN"
```

Issue an API key for an agent (human JWT required, plaintext key returned exactly once):

```bash
curl -s -X POST http://localhost:5080/api/agents/<agent-id>/api-key \
  -H "Authorization: Bearer $TOKEN"
```

Call the API as an agent (read + propose actions only):

```bash
curl -s http://localhost:5080/api/contacts -H 'X-Api-Key: crm_...'

curl -s -X POST http://localhost:5080/api/agent-actions \
  -H 'X-Api-Key: crm_...' \
  -H 'Content-Type: application/json' \
  -d '{"actionType":"AddNote","inputJson":"{\"title\":\"Agent note\"}","reasoningSummary":"...","requiresApproval":true}'
```

Agents do not pass `agentId` in the body; it is taken from the authenticated key. Mutating endpoints other than `POST /api/agent-actions` return 403 for agents.

Hangfire dashboard (`/hangfire`): Admin users only. In Development, requests from the local machine are also allowed, so `http://localhost:5080/hangfire` keeps working without extra setup.

## Restore and Build

```bash
dotnet restore
dotnet build Crm.slnx
```

Frontend dependencies and build:

```bash
cd src/Crm.WebApp
npm install
npm run build
cd ../..
```

## Database

Apply migrations:

```bash
dotnet ef database update --project src/Crm.Infrastructure --startup-project src/Crm.WebApi
```

Add a migration after EF model changes:

```bash
dotnet ef migrations add <Name> --project src/Crm.Infrastructure --startup-project src/Crm.WebApi
```

## Local Run With Aspire

```bash
dotnet run --project src/Crm.AppHost --launch-profile http
```

Default local URLs:

- Aspire Dashboard: `http://localhost:18888`
- Web API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Hangfire Dashboard: `http://localhost:5080/hangfire`
- Frontend dev server: `http://localhost:5173`
- Ready probe: `http://localhost:5080/health/ready`
- Live probe: `http://localhost:5080/health/live`

VS Code/Cursor can also use the `Aspire AppHost` debug configuration or the `run: aspire apphost` task.

## Local Run Without Aspire

API:

```bash
dotnet run --project src/Crm.WebApi --launch-profile http
```

Frontend:

```bash
cd src/Crm.WebApp
npm run dev
```

## API Type Generation

Start the API first, then run:

```bash
cd src/Crm.WebApp
npm run generate:api
```

Generated file:

```text
src/Crm.WebApp/src/api/generated/schema.ts
```

## Tests and Checks

Backend:

```bash
dotnet test Crm.slnx
```

Frontend:

```bash
cd src/Crm.WebApp
npm run build
npm run lint
```

Docs-only changes do not normally require full test execution, but run relevant checks if the docs change commands or operational assumptions.

## Troubleshooting

If the API fails on startup, check:

- `ConnectionStrings__CrmDb` is set.
- PostgreSQL is reachable.
- Migrations have been applied.
- `Database:ApplyMigrationsOnStartup` is configured intentionally.
- `Auth:Jwt:SigningKey` is configured (at least 32 characters); outside Development it must come from environment/secrets.

If API calls return 401, check:

- The `Authorization: Bearer <token>` header (humans) or `X-Api-Key` header (agents) is present.
- The token is not expired and was signed with the current `Auth:Jwt:SigningKey`.
- The agent is active and its key has not been rotated.

If frontend API calls fail, check:

- API is listening on `http://localhost:5080`.
- CORS allowed origins include the frontend origin.
- `VITE_API_BASE_URL` is set when using a non-default API URL.
