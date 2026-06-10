# Project Context

## Назначение

Проект - базовая CRM-система, подготовленная к управлению через AI-агентов. Сейчас это надежное ядро CRM с API-first архитектурой, PostgreSQL, минимальным React UI, Aspire для локального запуска и Kubernetes manifests для Yandex Cloud.

Ключевая идея: AI-агент может читать данные и предлагать действия, но не должен обходить CRM Core и писать в базу напрямую.

## Технологический стек

Backend:

- .NET 10
- ASP.NET Core Web API
- EF Core 10
- PostgreSQL
- FluentValidation
- OpenAPI/Swagger
- Serilog
- Hangfire + PostgreSQL storage
- .NET Aspire AppHost/ServiceDefaults

Frontend:

- React
- TypeScript
- Vite
- Ant Design
- React Router
- TanStack Query
- React Hook Form
- Zod
- TypeScript API types generated from OpenAPI

Deployment:

- Docker images for WebApi and WebApp
- Kubernetes manifests in `deploy/`
- Target platform: Yandex Cloud Managed Kubernetes

## Solution Layout

```text
src/
  Crm.Domain/
  Crm.Application/
  Crm.Infrastructure/
  Crm.WebApi/
  Crm.WebApp/
  Crm.AppHost/
  Crm.ServiceDefaults/
tests/
  Crm.Tests/
docker/
deploy/
docs/
```

Layer ownership:

- `Crm.Domain` - entities, enums, common entity abstractions. No dependencies on Infrastructure or WebApi.
- `Crm.Application` - DTOs, service interfaces, business logic, validators, application exceptions.
- `Crm.Infrastructure` - EF Core DbContext, migrations, seed data, fake integrations, persistence implementation.
- `Crm.WebApi` - controllers, middleware, Swagger, CORS, Hangfire setup, API host composition.
- `Crm.WebApp` - React SPA.
- `Crm.AppHost` - Aspire local orchestration for API, frontend and external PostgreSQL connection.
- `Crm.ServiceDefaults` - shared service defaults and health endpoints.
- `Crm.Tests` - integration-style application service tests.

## Implemented CRM Surface

The MVP includes API and UI support for:

- Dashboard summary
- Contacts
- Companies
- Pipelines
- Pipeline stages
- Deals
- Tasks
- Activities and timeline
- Messages
- Agents
- Agent actions
- Approval requests

The application service is centered around `ICrmService` and `CrmService`. Controllers should stay thin and delegate business behavior to the application layer.

## Persistence Model

EF Core persistence lives in `CrmDbContext`.

Important persistence behavior:

- Auditable entities have `CreatedAt`, `UpdatedAt`, `IsDeleted`.
- `SaveChangesAsync` stamps timestamps and writes `AuditLog` records.
- Soft-deleted data is filtered by application queries through the `Active<TEntity>()` pattern in `CrmService`.
- Enum values are stored as strings.
- JSON payload columns for agent actions and audit records use PostgreSQL `jsonb`.

## Local Infrastructure

PostgreSQL is external. It may be local or remote, but it is not started by Docker Compose in this repository.

Expected connection string key:

```text
ConnectionStrings__CrmDb
```

Default local API URL:

```text
http://localhost:5080
```

Default frontend dev server:

```text
http://localhost:5173
```

## Production Gaps To Respect

The MVP is deployment-ready as a starting point, but before real production exposure the project still needs:

- Authentication and authorization, for example Keycloak/Identity.
- RBAC and agent permission model.
- Hangfire dashboard protection.
- CI/CD migration strategy, preferably a Kubernetes Job or CI step.
- Real implementations for currently fake integrations.

## Product Action Plan

Current product priorities are tracked in `action-plan.md`.

Nearest planned improvements:

- Conversation workspace for messages and AI-assisted replies.
- Activities work queue for tasks, meetings, calls and follow-ups.
- Contextual AI side panel connected to `AgentAction` and approvals.
- Contact duplicates and merge queue.
- Bulk actions for contacts and deals.
