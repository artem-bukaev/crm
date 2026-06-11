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
- JWT bearer auth for humans + API-key auth for agents (см. `runbook.md` и `agent-action-layer.md`)

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
- Authentication: login/me endpoints, Users with roles (Admin/Manager), agent API keys

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

## Authentication Model (MVP slice)

Implemented in the current codebase:

- `User` entity (unique Email, DisplayName, PBKDF2 PasswordHash via ASP.NET Identity `PasswordHasher`, Role enum Admin/Manager).
- `POST /api/auth/login` issues a JWT (symmetric key from configuration), `GET /api/auth/me` returns the current user.
- Agents authenticate with an API key in the `X-Api-Key` header ("AgentApiKey" scheme); only a SHA-256 hash is stored on `Agent.ApiKeyHash`. `POST /api/agents/{id}/api-key` issues/rotates a key and returns the plaintext exactly once.
- Authentication is required globally through a fallback policy (JWT or AgentApiKey). Anonymous access: login, health endpoints, Swagger in Development.
- Authorization is policy-based: GET endpoints accept humans and agents; every mutating endpoint is human-only by convention (`MutationAuthorizationConvention`), except `POST /api/agent-actions` which also accepts agents.
- Approve/reject/execute of agent actions and approval decisions are human-only, and the acting user id comes from JWT claims, never from request bodies. Agents cannot spoof another agent's id when proposing actions.
- Hangfire dashboard is restricted to Admin users (local requests are also allowed in Development).

## Production Gaps To Respect

The MVP is deployment-ready as a starting point, but before real production exposure the project still needs:

- External identity provider integration (e.g. Keycloak/OIDC), refresh tokens, password reset and user management UI. The current JWT + seeded-admin auth is an MVP slice.
- Finer-grained RBAC and per-agent permission/scoping model (current model: role enum for humans, read+propose for agents).
- Browser access to the Hangfire dashboard outside Development requires a reverse proxy that injects an Admin JWT (the dashboard filter itself is in place).
- Secret management for `Auth:Jwt:SigningKey` and seeded admin credentials (Kubernetes Secrets / vault), plus key rotation.
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
