# AI Agent Instructions

This repository keeps shared instructions for Codex, Claude Code, and other AI coding agents in `docs/ai-agents/`.

Before making code changes, read:

1. `docs/ai-agents/README.md`
2. `docs/ai-agents/project-context.md`
3. `docs/ai-agents/coding-guidelines.md`
4. `docs/ai-agents/runbook.md`

If the task touches AI-agent behavior, also read `docs/ai-agents/agent-action-layer.md`.
If the task touches Kubernetes, Docker, or Yandex Cloud deployment, also read `docs/ai-agents/deployment.md`.

Critical project rules:

- The solution targets .NET 10.
- Use Hangfire for background jobs. Do not introduce Quartz.
- AI agents must interact with CRM data through the Web API and explicit action layer, not through direct database writes.
- Preserve Clean Architecture boundaries: Domain has no Infrastructure/WebApi dependencies; Application owns business logic and DTOs; Infrastructure owns EF Core and integrations; WebApi owns controllers and middleware; WebApp owns React UI.
- PostgreSQL is an external dependency for local development. Do not add Docker Compose for PostgreSQL.
- Use soft delete for CRM entities and keep audit logging intact.
- Validate incoming DTOs with FluentValidation and keep API errors in the shared error format.
- Regenerate frontend API types after OpenAPI contract changes.
