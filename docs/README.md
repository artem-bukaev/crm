# Project Documentation

Эта папка - основная документация проекта для людей, Codex, Claude Code и других AI-агентов.

Цель документов - держать в одном месте контекст продукта, правила разработки, план действий, локальный runbook, деплой и безопасную модель работы CRM с будущими AI-интеграциями.

## Что читать

Всегда читать перед изменениями:

1. `project-context.md` - что строим, какие модули есть, какие ограничения важны.
2. `coding-guidelines.md` - стиль и правила изменений backend/frontend.
3. `runbook.md` - команды запуска, сборки, тестов и генерации API типов.

Читать по ситуации:

- `action-plan.md` - если задача касается roadmap, приоритизации или выбора следующего продуктового среза.
- `agent-action-layer.md` - если задача касается AI-агентов, approval flow, audit log, сообщений, действий или автоматизаций.
- `deployment.md` - если задача касается Docker, Kubernetes, Yandex Cloud, health probes, secrets или production конфигурации.

## Коротко о проекте

CRM Core MVP - API-first CRM на .NET 10 с React UI, PostgreSQL, audit log, Hangfire, action layer для AI-агентов, локальным запуском через Aspire и Kubernetes deployment-артефактами для Yandex Cloud.

Основная схема:

```text
AI Agent -> CRM API -> CRM Core -> PostgreSQL
```

AI-агенты не должны напрямую менять базу данных. Все изменения должны идти через явный API/action layer, логироваться и при необходимости требовать подтверждения человека.

## Главные правила

- Использовать .NET 10.
- Использовать Hangfire для фоновых задач. Quartz не добавлять.
- PostgreSQL считается внешней зависимостью, Docker Compose для локальной базы не добавлять.
- Сохранять Clean Architecture-like структуру решения.
- Все основные CRM-сущности используют `CreatedAt`, `UpdatedAt`, `IsDeleted`.
- Удаление делать через soft delete.
- Изменения бизнес-сущностей должны попадать в audit log.
- Входные DTO валидировать через FluentValidation.
- Ошибки API возвращать в едином формате.
- Frontend API contract держать синхронным с OpenAPI schema.

## Карта документации

- `project-context.md` - продукт, scope MVP, архитектура, сущности.
- `action-plan.md` - ближайшие продуктовые улучшения и suggested implementation order.
- `coding-guidelines.md` - правила кода, слои, тесты, миграции, frontend стиль.
- `agent-action-layer.md` - безопасная модель AI-действий.
- `runbook.md` - локальный запуск, команды, проверки.
- `deployment.md` - Docker/Kubernetes/Yandex Cloud.

## Если контекст устарел

Если код расходится с документацией, сначала проверь фактическое состояние проекта, затем обнови документацию в том же изменении. Не оставляй инструкции для агентов устаревшими.
