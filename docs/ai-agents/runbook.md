# Runbook

## Requirements

- .NET SDK 10
- Node.js 24+ and npm
- PostgreSQL reachable through a connection string

Connection string:

```bash
export ConnectionStrings__CrmDb='Host=localhost;Port=5432;Database=crm;Username=postgres;Password=postgres'
```

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
dotnet run --project src/Crm.AppHost
```

Default local URLs:

- Web API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Hangfire Dashboard: `http://localhost:5080/hangfire`
- Frontend dev server: `http://localhost:5173`
- Ready probe: `http://localhost:5080/health/ready`
- Live probe: `http://localhost:5080/health/live`

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

If frontend API calls fail, check:

- API is listening on `http://localhost:5080`.
- CORS allowed origins include the frontend origin.
- `VITE_API_BASE_URL` is set when using a non-default API URL.
