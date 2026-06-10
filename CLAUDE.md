# Claude Code Project Instructions

Read `docs/README.md` before working in this repository. It is the entry point for project context, coding rules, local commands, deployment notes, roadmap, and CRM AI-action safety rules.

Minimum reading order:

1. `docs/README.md`
2. `docs/project-context.md`
3. `docs/coding-guidelines.md`
4. `docs/runbook.md`

Task-specific reading:

- Roadmap and prioritization: `docs/action-plan.md`
- AI-agent/API action work: `docs/agent-action-layer.md`
- Docker, Kubernetes, Yandex Cloud: `docs/deployment.md`

Hard constraints:

- .NET 10, ASP.NET Core Web API, EF Core 10, PostgreSQL.
- React + TypeScript + Vite + Ant Design frontend.
- Hangfire only for background jobs. Do not add Quartz.
- No direct database writes from AI agents or integrations. Use CRM API and the auditable action layer.
- No Docker Compose PostgreSQL for local development; PostgreSQL is external.
- Keep generated files, build outputs, `node_modules`, and `dist` out of commits.
