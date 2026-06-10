# CRM Core MVP

API-first CRM на .NET 10 с React UI, PostgreSQL, audit log, action layer для AI-агентов, локальным запуском через Aspire и deployment-артефактами для Kubernetes в Yandex Cloud.

## Документация для AI-агентов

Codex, Claude Code и другие AI-агенты должны начинать работу с `docs/ai-agents/README.md`.

Корневые файлы `AGENTS.md` и `CLAUDE.md` указывают на эту документацию и фиксируют обязательные правила проекта.

## Что реализовано

- Clean Architecture-like solution: `Crm.Domain`, `Crm.Application`, `Crm.Infrastructure`, `Crm.WebApi`, `Crm.WebApp`, `Crm.AppHost`, `Crm.ServiceDefaults`.
- REST API + Swagger для Contacts, Companies, Pipelines, PipelineStages, Deals, Tasks, Activities/Timeline, Messages, Agents, AgentActions, Approvals.
- EF Core 10 + PostgreSQL, миграция `InitialCreate`, seed data, soft delete, timestamps, audit log в `SaveChanges`.
- FluentValidation для входных DTO и единый формат API ошибок.
- Hangfire + PostgreSQL storage вместо Quartz.
- Agent Action flow: propose, approve, reject, execute; поддержаны базовые действия `CreateContact`, `UpdateContact`, `CreateDeal`, `UpdateDealStage`, `CreateTask`, `CompleteTask`, `AddNote`, `DraftMessage`, `SendMessage`, `RequestHumanApproval`.
- React + TypeScript + Vite + Ant Design + React Router + TanStack Query UI.
- Generated OpenAPI types: `src/Crm.WebApp/src/api/generated/schema.ts`.
- Dockerfile и Kubernetes manifests под Yandex Cloud Managed Kubernetes в `docker/` и `deploy/`.
- 10 интеграционных тестов на ключевые сценарии Application service.

## Быстрый старт

Требования:

- .NET SDK 10
- Node.js 24+ и npm
- PostgreSQL, доступный локально или через connection string

Connection string задаётся через environment variable или appsettings:

```bash
export ConnectionStrings__CrmDb='Host=localhost;Port=5432;Database=crm;Username=postgres;Password=postgres'
```

Установка зависимостей и сборка:

```bash
dotnet restore
dotnet build Crm.slnx

cd src/Crm.WebApp
npm install
npm run build
cd ../..
```

Применить миграции:

```bash
dotnet ef database update --project src/Crm.Infrastructure --startup-project src/Crm.WebApi
```

Запуск через Aspire:

```bash
dotnet run --project src/Crm.AppHost
```

Локальные URL по умолчанию:

- Web API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger`
- Hangfire Dashboard: `http://localhost:5080/hangfire`
- Frontend dev server: `http://localhost:5173`
- Health probes: `http://localhost:5080/health/ready`, `http://localhost:5080/health/live`

Отдельный запуск без Aspire:

```bash
dotnet run --project src/Crm.WebApi --launch-profile http

cd src/Crm.WebApp
npm run dev
```

Регенерация TypeScript OpenAPI schema:

```bash
cd src/Crm.WebApp
npm run generate:api
```

Тесты:

```bash
dotnet test Crm.slnx
```

## Deployment в Yandex Cloud Kubernetes

Docker images:

```bash
docker build -f docker/Crm.WebApi.Dockerfile -t cr.yandex/<registry-id>/crm-webapi:<tag> .
docker build -f docker/Crm.WebApp.Dockerfile -t cr.yandex/<registry-id>/crm-webapp:<tag> .
```

Kubernetes manifests лежат в `deploy/`. Перед применением нужно:

- заменить image names в `deploy/webapi.yaml` и `deploy/webapp.yaml`;
- создать `crm-secrets` с `ConnectionStrings__CrmDb`;
- настроить `deploy/ingress.example.yaml`: host, subnet IDs, security group IDs, TLS secret/static IP.

Подробности: `deploy/README.md`.

## Архитектурные замечания

- Полноценную авторизацию лучше добавить до первого реального production доступа: Keycloak/Identity, RBAC, agent permissions, защита Hangfire dashboard.
- Для нескольких реплик WebApi Hangfire server будет запущен в каждой реплике. Это штатно для Hangfire, но для тяжёлых фоновых задач можно вынести worker в отдельный Deployment.
- Frontend сейчас собран как MVP bundle с Ant Design. Для роста продукта стоит разнести страницы через lazy loading.
- AgentAction не хранит chain-of-thought, только `ReasoningSummary`, что правильно для будущих AI-интеграций.
- Для production миграции лучше выполнять отдельным Kubernetes Job/CI step, а не включать `ApplyMigrationsOnStartup`.

---

# Исходное описание проекта

# Проект: базовая CRM на .NET с подготовкой под AI-агентов

## 1. Цель проекта

Нужно разработать базовую CRM-систему на .NET, которая в будущем будет управляться AI-агентами через API.

Сейчас задача — сделать надежное ядро CRM, API-first архитектуру, базовые сущности, локальный запуск через .NET Aspire и минимальный frontend UI.

CRM должна быть спроектирована так, чтобы AI-агенты могли безопасно читать данные, предлагать действия и выполнять разрешенные операции через API.

Главная архитектурная идея:

```text
AI Agent -> CRM API -> CRM Core -> PostgreSQL
```

AI-агенты не должны напрямую менять базу данных. Все действия должны проходить через явный action layer, логироваться и при необходимости требовать подтверждения человеком.

---

## 2. Технологический стек

### Backend

- .NET 9, допустимо .NET 8 если .NET 9 недоступен
- ASP.NET Core Web API
- PostgreSQL
- EF Core
- FluentValidation
- OpenAPI / Swagger
- Serilog
- Hangfire или Quartz для фоновых задач
- .NET Aspire для локального запуска и тестирования

### Frontend

UI реализовать как SPA:

- React
- TypeScript
- Vite
- Ant Design
- React Router
- TanStack Query
- React Hook Form
- Zod
- TypeScript API client, сгенерированный из OpenAPI / Swagger

Для первого MVP использовать Ant Design, потому что он быстрее подходит для CRM/админки: таблицы, формы, фильтры, модальные окна, drawer, tabs, layout.

Next.js не использовать. SEO и SSR для внутренней CRM не нужны.

---

## 3. Локальная инфраструктура

Нужно подключить .NET Aspire для локального запуска и тестирования.

База данных PostgreSQL не должна подниматься через Docker Compose. Она уже запущена отдельно на рабочем компьютере.

Connection string должен браться из конфигурации или environment variables.

Пример переменной:

```text
ConnectionStrings__CrmDb=Host=localhost;Port=5432;Database=crm;Username=postgres;Password=postgres
```

Добавить Aspire-проекты:

```text
Crm.AppHost
Crm.ServiceDefaults
Crm.WebApi
Crm.WebApp
```

`Crm.AppHost` должен запускать:

- backend API;
- frontend UI;
- внешнюю PostgreSQL как external resource / connection string.

Docker Compose для PostgreSQL не нужен.

---

## 4. Архитектура решения

Использовать Clean Architecture или близкую структуру:

```text
src/
  Crm.Domain/
    Entities/
    Enums/
    ValueObjects/

  Crm.Application/
    DTOs/
    Services/
    Validators/
    Interfaces/
    UseCases/

  Crm.Infrastructure/
    Persistence/
    Repositories/
    BackgroundJobs/
    Integrations/

  Crm.WebApi/
    Controllers/
    Middleware/
    Program.cs

  Crm.WebApp/
    src/
      api/
      components/
      pages/
      routes/
      features/

  Crm.AppHost/

  Crm.ServiceDefaults/

tests/
  Crm.Tests/
```

Общие правила:

1. Domain не должен зависеть от Infrastructure или WebApi.
2. Application содержит бизнес-логику, DTO, интерфейсы сервисов, валидаторы.
3. Infrastructure содержит EF Core, DbContext, миграции, репозитории, внешние интеграции.
4. WebApi содержит controllers, middleware, auth, Swagger.
5. WebApp содержит React UI.
6. Все изменения бизнес-сущностей должны логироваться.
7. Для всех основных сущностей добавить `CreatedAt`, `UpdatedAt`, `IsDeleted`.
8. Удаление делать через soft delete.
9. Ошибки API возвращать в едином формате.
10. Все входные DTO валидировать через FluentValidation.

---

## 5. Основные сущности

### 5.1 Contact

Контакт физического лица.

Поля:

```text
Id
FirstName
LastName
MiddleName nullable
Phone nullable
Email nullable
TelegramUsername nullable
CompanyId nullable
Position nullable
Source nullable
Status
CreatedAt
UpdatedAt
IsDeleted
```

---

### 5.2 Company

Компания клиента или контрагента.

Поля:

```text
Id
Name
LegalName nullable
INN nullable
Website nullable
Phone nullable
Email nullable
Address nullable
CreatedAt
UpdatedAt
IsDeleted
```

---

### 5.3 Pipeline

Воронка продаж.

Поля:

```text
Id
Name
Description nullable
IsDefault
CreatedAt
UpdatedAt
IsDeleted
```

---

### 5.4 PipelineStage

Этап воронки.

Поля:

```text
Id
PipelineId
Name
SortOrder
Probability
IsWon
IsLost
CreatedAt
UpdatedAt
IsDeleted
```

---

### 5.5 Deal

Сделка.

Поля:

```text
Id
Title
ContactId nullable
CompanyId nullable
PipelineId
StageId
Amount
Currency
Probability
Status
Source nullable
ResponsibleUserId nullable
CreatedAt
UpdatedAt
ClosedAt nullable
IsDeleted
```

Статусы:

```text
Open
Won
Lost
Canceled
```

---

### 5.6 Task

Задача менеджеру или агенту.

Поля:

```text
Id
Title
Description nullable
DueAt nullable
Status
Priority
ContactId nullable
CompanyId nullable
DealId nullable
ResponsibleUserId nullable
CreatedAt
UpdatedAt
CompletedAt nullable
IsDeleted
```

TaskStatus:

```text
New
InProgress
Completed
Canceled
```

TaskPriority:

```text
Low
Normal
High
Urgent
```

---

### 5.7 Activity

Активность — любое действие в CRM: звонок, письмо, сообщение, встреча, комментарий, системное событие.

Поля:

```text
Id
Type
Title
Description nullable
ContactId nullable
CompanyId nullable
DealId nullable
CreatedByUserId nullable
CreatedByAgentId nullable
CreatedAt
IsDeleted
```

ActivityType:

```text
Note
Call
Email
TelegramMessage
Meeting
SystemEvent
AgentAction
```

---

### 5.8 Message

Сообщение из внешнего или внутреннего канала.

Поля:

```text
Id
Channel
Direction
ExternalMessageId nullable
ContactId nullable
DealId nullable
Text
ReceivedAt nullable
SentAt nullable
CreatedAt
UpdatedAt
IsDeleted
```

MessageChannel:

```text
Email
Telegram
WhatsApp
WebsiteChat
Manual
```

MessageDirection:

```text
Incoming
Outgoing
```

---

### 5.9 Agent

AI-агент или системный агент.

Поля:

```text
Id
Name
Description nullable
IsActive
CreatedAt
UpdatedAt
IsDeleted
```

---

### 5.10 AgentAction

Действие, предложенное или выполненное AI-агентом.

AI-агенты не должны напрямую менять бизнес-сущности. Они должны создавать `AgentAction`. Если действие не требует подтверждения, система может выполнить его автоматически. Если требует — действие ждет approval от пользователя.

Поля:

```text
Id
AgentId
ActionType
Status
TargetEntityType
TargetEntityId nullable
InputJson
ReasoningSummary nullable
BeforeJson nullable
AfterJson nullable
RequiresApproval
ApprovedByUserId nullable
ApprovedAt nullable
RejectedByUserId nullable
RejectedAt nullable
ErrorMessage nullable
CreatedAt
ExecutedAt nullable
IsDeleted
```

AgentActionStatus:

```text
Proposed
Approved
Rejected
Executed
Failed
Canceled
```

AgentActionType examples:

```text
CreateContact
UpdateContact
CreateDeal
UpdateDealStage
CreateTask
CompleteTask
AddNote
DraftMessage
SendMessage
```

TargetEntityType examples:

```text
Contact
Company
Deal
Task
Activity
Message
```

---

### 5.11 Approval

Можно реализовать approval прямо внутри `AgentAction`, но архитектурно лучше заложить отдельную сущность `ApprovalRequest`, чтобы потом подтверждать не только действия агентов.

Поля:

```text
Id
EntityType
EntityId
Title
Description nullable
Status
RequestedByUserId nullable
RequestedByAgentId nullable
ApprovedByUserId nullable
ApprovedAt nullable
RejectedByUserId nullable
RejectedAt nullable
CreatedAt
UpdatedAt
IsDeleted
```

ApprovalStatus:

```text
Pending
Approved
Rejected
Canceled
```

---

## 6. API

API должен быть REST, удобный для людей и AI-агентов.

Swagger / OpenAPI обязателен.

### 6.1 Contacts

```text
GET    /api/contacts
GET    /api/contacts/{id}
POST   /api/contacts
PUT    /api/contacts/{id}
DELETE /api/contacts/{id}
```

---

### 6.2 Companies

```text
GET    /api/companies
GET    /api/companies/{id}
POST   /api/companies
PUT    /api/companies/{id}
DELETE /api/companies/{id}
```

---

### 6.3 Pipelines

```text
GET    /api/pipelines
GET    /api/pipelines/{id}
POST   /api/pipelines
PUT    /api/pipelines/{id}
DELETE /api/pipelines/{id}
```

---

### 6.4 Pipeline Stages

```text
GET    /api/pipelines/{pipelineId}/stages
POST   /api/pipelines/{pipelineId}/stages
PUT    /api/pipeline-stages/{id}
DELETE /api/pipeline-stages/{id}
```

---

### 6.5 Deals

```text
GET    /api/deals
GET    /api/deals/{id}
POST   /api/deals
PUT    /api/deals/{id}
POST   /api/deals/{id}/move-stage
POST   /api/deals/{id}/mark-won
POST   /api/deals/{id}/mark-lost
DELETE /api/deals/{id}
```

---

### 6.6 Tasks

```text
GET    /api/tasks
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
POST   /api/tasks/{id}/complete
POST   /api/tasks/{id}/cancel
DELETE /api/tasks/{id}
```

---

### 6.7 Activities

```text
GET  /api/activities
GET  /api/activities/timeline
POST /api/activities
```

`/api/activities/timeline` должен уметь фильтровать по:

```text
contactId
companyId
dealId
```

---

### 6.8 Messages

```text
GET  /api/messages
GET  /api/messages/{id}
POST /api/messages
```

На первом этапе отправку во внешние каналы не реализовывать. `POST /api/messages` может создавать ручное или черновое сообщение.

---

### 6.9 Agents

```text
GET  /api/agents
GET  /api/agents/{id}
POST /api/agents
PUT  /api/agents/{id}
```

---

### 6.10 Agent Actions

```text
GET  /api/agent-actions
GET  /api/agent-actions/{id}
POST /api/agent-actions
POST /api/agent-actions/{id}/approve
POST /api/agent-actions/{id}/reject
POST /api/agent-actions/{id}/execute
```

Правила:

1. `POST /api/agent-actions` создает предложенное действие.
2. Если `RequiresApproval = true`, действие получает статус `Proposed`.
3. Если `RequiresApproval = false`, действие может быть выполнено автоматически.
4. `approve` меняет статус на `Approved`.
5. `reject` меняет статус на `Rejected`.
6. `execute` выполняет действие через application service.
7. До и после выполнения сохранять `BeforeJson` и `AfterJson`, если применимо.
8. Ошибки сохранять в `ErrorMessage`, статус менять на `Failed`.

---

## 7. Action layer для AI-агентов

Нужно не просто сделать CRUD, а заложить слой бизнес-действий.

Примеры действий:

```text
create_lead / create_contact
update_contact
create_deal
update_deal_stage
schedule_followup / create_task
complete_task
add_note
summarize_call
create_message_draft
send_message, пока без реальной отправки
request_human_approval
```

Каждое действие агента должно иметь:

```text
who/what initiated
input
reasoning summary
before state
after state
approval required: yes/no
result
error message
rollback possibility, если возможно
```

Важно: не хранить полный chain-of-thought агента. Хранить только краткое объяснение решения в поле `ReasoningSummary`.

---

## 8. Audit log

Нужно добавить базовый audit log.

Сущность `AuditLog`:

```text
Id
EntityType
EntityId
Action
UserId nullable
AgentId nullable
BeforeJson nullable
AfterJson nullable
CreatedAt
```

AuditAction examples:

```text
Created
Updated
Deleted
StageChanged
StatusChanged
AgentActionProposed
AgentActionApproved
AgentActionRejected
AgentActionExecuted
```

Все изменения основных бизнес-сущностей должны попадать в audit log.

---

## 9. Seed data

Добавить seed-данные:

### Default pipeline

```text
Name: Default Sales Pipeline
IsDefault: true
```

Stages:

```text
New          probability 10
Contacted    probability 25
Negotiation  probability 60
Won          probability 100, IsWon = true
Lost         probability 0, IsLost = true
```

### Demo company

```text
Name: Demo Company
Website: https://example.com
```

### Demo contact

```text
FirstName: Ivan
LastName: Petrov
Email: ivan.petrov@example.com
Phone: +79990000000
```

### Demo deal

```text
Title: Demo Deal
Amount: 100000
Currency: RUB
Stage: New
```

### Demo agent

```text
Name: Sales Assistant Agent
Description: Demo AI agent for sales assistance
IsActive: true
```

---

## 10. Frontend UI

Сделать минимальный рабочий UI на React + TypeScript + Vite + Ant Design.

Основные экраны:

```text
Contacts
Companies
Deals
Deal Kanban by PipelineStage
Tasks
Activities / Timeline
Messages
Agents
Agent Actions
Approvals
```

### 10.1 Layout

Сделать базовый layout:

- sidebar menu;
- top header;
- content area;
- responsive behavior необязателен на первом этапе.

Меню:

```text
Dashboard
Contacts
Companies
Deals
Tasks
Timeline
Messages
Agents
Agent Actions
Approvals
```

### 10.2 Contacts UI

- таблица контактов;
- создание контакта;
- редактирование контакта;
- просмотр карточки контакта;
- связь с компанией.

### 10.3 Companies UI

- таблица компаний;
- создание компании;
- редактирование компании;
- просмотр карточки компании.

### 10.4 Deals UI

- таблица сделок;
- kanban по стадиям pipeline;
- создание сделки;
- редактирование сделки;
- перемещение сделки между стадиями.

### 10.5 Tasks UI

- список задач;
- фильтр по статусу;
- создание задачи;
- завершение задачи.

### 10.6 Timeline UI

- общий timeline активностей;
- фильтр по контакту, компании, сделке;
- отображение notes, calls, messages, system events, agent actions.

### 10.7 Agent Actions UI

- список действий агентов;
- просмотр input/reasoning/before/after;
- approve;
- reject;
- execute.

### 10.8 API client

Сгенерировать TypeScript API client из OpenAPI.

Допустимо использовать:

```text
NSwag
openapi-typescript
orval
```

Предпочтительно выбрать один способ и описать в README.

---

## 11. Backend validation

Использовать FluentValidation для всех create/update DTO.

Примеры правил:

### Contact

- FirstName или LastName должны быть заполнены.
- Email должен быть валидным, если указан.
- Phone должен быть строкой ограниченной длины, если указан.

### Company

- Name обязателен.
- Email должен быть валидным, если указан.
- Website должен быть валидным URL, если указан.

### Deal

- Title обязателен.
- Amount не меньше 0.
- Currency обязателен.
- PipelineId обязателен.
- StageId обязателен.

### Task

- Title обязателен.
- Priority обязателен.
- Status обязателен.

### AgentAction

- AgentId обязателен.
- ActionType обязателен.
- TargetEntityType обязателен, если действие относится к существующей сущности.
- InputJson обязателен.

---

## 12. Error handling

Добавить middleware для обработки ошибок.

Единый формат ошибки:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": []
  }
}
```

Типы ошибок:

```text
VALIDATION_ERROR
NOT_FOUND
CONFLICT
UNAUTHORIZED
FORBIDDEN
INTERNAL_ERROR
```

---

## 13. Конфигурация

Конфигурация должна поддерживать:

```text
appsettings.json
appsettings.Development.json
environment variables
Aspire configuration
```

Основные настройки:

```text
ConnectionStrings:CrmDb
Serilog
AllowedHosts
Cors
```

Для разработки CORS должен позволять frontend dev server.

---

## 14. Swagger / OpenAPI

Swagger обязателен.

Требования:

1. Все endpoints должны иметь понятные request/response DTO.
2. Не отдавать EF entities напрямую наружу.
3. Добавить XML comments или endpoint descriptions там, где полезно.
4. Swagger должен быть доступен в Development.
5. OpenAPI JSON должен использоваться для генерации TypeScript client.

---

## 15. Безопасность

На первом этапе полноценную авторизацию можно не реализовывать, но код должен быть готов к подключению auth.

Заложить поля:

```text
CreatedByUserId
UpdatedByUserId
ResponsibleUserId
ApprovedByUserId
RejectedByUserId
```

В будущем планируется подключить:

```text
Keycloak или ASP.NET Identity
RBAC
permissions
agent permissions
approval rules
```

Важно: действия AI-агентов должны иметь отдельную permission-модель в будущем.

---

## 16. Будущие интеграции

Сейчас интеграции реализовывать не нужно, но код должен быть спроектирован так, чтобы их было легко добавить.

Будущие интеграции:

```text
Telegram
Email
WhatsApp
Website chat
AmoCRM sync
AI-agent runtime
Vector search / pgvector
Full audit log
Role-based permissions
Calendar
Telephony
```

Для интеграций предусмотреть папку:

```text
Crm.Infrastructure/Integrations
```

И интерфейсы в Application:

```text
IMessageSender
IExternalCrmSyncService
IAgentRuntime
IEmbeddingService
```

Реализация пока может быть stub / fake.

---

## 17. Минимальные тесты

Добавить базовые unit/integration tests:

1. Создание контакта.
2. Создание компании.
3. Создание сделки.
4. Перемещение сделки между стадиями.
5. Создание задачи.
6. Завершение задачи.
7. Создание AgentAction.
8. Approve AgentAction.
9. Reject AgentAction.
10. Execute AgentAction для простого действия, например AddNote или CreateTask.

---

## 18. README

Добавить README с инструкцией:

1. Как настроить PostgreSQL connection string.
2. Как запустить миграции.
3. Как запустить проект через Aspire.
4. Где открыть Swagger.
5. Где открыть frontend UI.
6. Как сгенерировать TypeScript API client.
7. Как запустить тесты.

Пример запуска:

```bash
dotnet restore
dotnet build
dotnet ef database update --project src/Crm.Infrastructure --startup-project src/Crm.WebApi
dotnet run --project src/Crm.AppHost
```

---

## 19. Что нужно сделать первым этапом

Сначала реализовать backend-ядро и базовый UI.

Приоритеты:

1. Создать solution и структуру проектов.
2. Подключить Aspire.
3. Подключить PostgreSQL через external connection string.
4. Создать Domain entities и enums.
5. Создать DbContext и EF configurations.
6. Добавить миграции.
7. Реализовать seed data.
8. Реализовать CRUD endpoints для Contacts, Companies, Deals, Tasks.
9. Реализовать Pipelines и PipelineStages.
10. Реализовать Activities / Timeline.
11. Реализовать Agents и AgentActions.
12. Реализовать approve/reject/execute для AgentActions.
13. Добавить Swagger.
14. Добавить React UI.
15. Подключить generated TypeScript API client.
16. Добавить README.

---

## 20. Важные архитектурные ограничения

1. Не делать AI-агентов частью backend-монолита на первом этапе.
2. Не позволять агентам напрямую менять данные без action layer.
3. Не хранить полный chain-of-thought агентов.
4. Хранить только краткое объяснение в `ReasoningSummary`.
5. Не делать зависимость от AmoCRM или другой готовой CRM в ядре.
6. AmoCRM sync может появиться позже как внешняя интеграция.
7. Backend должен быть API-first.
8. Frontend должен работать только через API.
9. Не смешивать EF entities и API DTO.
10. Не делать слишком абстрактный generic CRUD, если есть бизнес-операция.

---

## 21. Итоговый результат

Нужно получить рабочий monorepo / solution:

```text
.NET backend
React frontend
Aspire AppHost
PostgreSQL external connection
EF Core migrations
Swagger
Seed data
Basic CRM UI
Agent Actions / Approvals foundation
README
```

Проект должен запускаться локально одной командой через Aspire:

```bash
dotnet run --project src/Crm.AppHost
```

После запуска должны быть доступны:

```text
Aspire dashboard
Backend API
Swagger
Frontend UI
```
