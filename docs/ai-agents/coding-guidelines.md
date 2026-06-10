# Coding Guidelines

## General

- Keep changes scoped to the task.
- Prefer existing patterns over new abstractions.
- Update documentation when behavior, commands, architecture, or deployment assumptions change.
- Do not commit build outputs, generated local artifacts, `node_modules`, `dist`, `bin`, `obj`, logs or local IDE files.
- Keep generated OpenAPI TypeScript schema in sync when API contracts change.

## Backend Rules

The backend targets .NET 10.

Respect layer boundaries:

- Domain must not reference Infrastructure or WebApi.
- Application owns business logic, DTOs, service contracts, validators and application exceptions.
- Infrastructure owns EF Core, migrations, DbContext, persistence mapping and external integration implementations.
- WebApi owns HTTP concerns: controllers, filters, middleware, CORS, Swagger and host setup.

Business logic should generally live in `Crm.Application/Services/CrmService.cs`. Controllers should be thin orchestration over `ICrmService`.

Use dependency injection through the existing `AddApplication` and `AddInfrastructure` extension methods.

## Entity and Persistence Rules

- Main CRM entities must be auditable through `IAuditableEntity`.
- Preserve `CreatedAt`, `UpdatedAt`, `IsDeleted`.
- Use soft delete instead of physical delete for CRM data.
- Ensure queries exclude soft-deleted records unless the task explicitly needs historical data.
- Preserve audit logging in `CrmDbContext.SaveChangesAsync`.
- Store enums as strings, matching existing EF Core configuration.
- Use PostgreSQL `jsonb` for structured action/audit payloads.
- Add EF Core migrations for schema changes.
- Do not bypass `CrmDbContext.SaveChangesAsync` for business writes.

## Validation and Errors

- Validate incoming DTOs with FluentValidation.
- Keep the shared action filter and exception middleware behavior consistent.
- Throw application-level exceptions for expected domain/application failures.
- Keep API errors in the existing consistent response shape.

## Background Jobs

- Use Hangfire.
- Do not introduce Quartz.
- Current Hangfire storage is PostgreSQL.
- Hangfire Dashboard is exposed only in Development in `Program.cs`; do not expose it publicly without auth.
- With multiple WebApi replicas, each replica may run a Hangfire server. This is acceptable for Hangfire, but heavy workers can later move to a separate Deployment.

## API Contract

- Keep route naming predictable and resource-oriented.
- Keep request/response DTOs in `Crm.Application/DTOs`.
- Add validators for new request DTOs.
- Add/update tests for important behavior in `tests/Crm.Tests`.
- After changing the API surface, regenerate frontend schema:

```bash
cd src/Crm.WebApp
npm run generate:api
```

## Frontend Rules

Use the existing React + TypeScript + Vite + Ant Design stack.

Frontend conventions:

- Keep API calls in `src/Crm.WebApp/src/api/client.ts`.
- Keep generated OpenAPI types in `src/Crm.WebApp/src/api/generated/schema.ts`.
- Use TanStack Query for server state.
- Use Ant Design components for CRM/admin UI patterns: tables, drawers, forms, tabs, tags, segmented controls and modals.
- Prefer dense, utilitarian CRM screens over marketing-style landing pages.
- Do not add Next.js or SSR.
- Keep layout responsive and avoid text overlap in compact viewports.

## Testing Expectations

For backend changes:

- Run `dotnet build Crm.slnx`.
- Run `dotnet test Crm.slnx`.
- Add focused tests when changing application behavior, validation, action execution, persistence rules or error handling.

For frontend changes:

- Run `npm run build` from `src/Crm.WebApp`.
- Run `npm run lint` when changing substantial UI or TypeScript.

For docs-only changes, tests are usually not required.

## Migrations

When changing the EF Core model:

```bash
dotnet ef migrations add <Name> --project src/Crm.Infrastructure --startup-project src/Crm.WebApi
dotnet ef database update --project src/Crm.Infrastructure --startup-project src/Crm.WebApi
```

For production, prefer applying migrations from CI or a Kubernetes Job rather than relying on startup migration execution.
