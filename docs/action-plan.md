# Action Plan

Этот план фиксирует ближайшие продуктовые улучшения после CRM Core MVP. Он основан на текущем состоянии проекта и идеях из похожей AI-first CRM: inbox-first рабочее место, единый список активностей, AI-панель в контексте экрана, дубли контактов и массовые действия.

## Принципы

- Сначала усиливать ежедневные рабочие сценарии менеджера: сообщения, активности, контакты, сделки.
- AI-функции должны предлагать действия через существующий `AgentAction`/approval flow, а не выполнять скрытые изменения.
- Новые backend возможности должны проходить через `ICrmService`, DTO, FluentValidation и audit log.
- UI должен оставаться CRM/admin-интерфейсом: плотным, сканируемым, без маркетинговых страниц.
- Маленькие полезные срезы важнее больших модулей без завершенного workflow.

## P0 - Conversation Workspace

Цель: превратить `Messages` в полноценный inbox для коммуникаций с клиентами.

MVP-срез:

- Список диалогов слева: контакт, компания, последнее сообщение, канал, время.
- Статусы диалога: `Unread`, `WaitingOnUs`, `WaitingOnThem`, `Closed`.
- Фильтры: all, unread, waiting on us, waiting on them.
- Центральный timeline сообщений по выбранному диалогу.
- Composer с выбором канала и получателя.
- Draft message через `AgentActionType.DraftMessage`.
- Send message через `AgentActionType.SendMessage` с human approval до реальной интеграции.

Backend notes:

- Добавить сущность/DTO `Conversation` или вычисляемую conversation projection поверх `Message`.
- Не ломать существующий `Message` API; лучше добавить отдельные endpoints для conversation views.
- Для отправки пока использовать fake sender, но сохранять action/audit trail.

UI notes:

- Layout: left conversation list, central thread, collapsible right AI drawer.
- Показывать channel badges: Email, Telegram, WhatsApp, LinkedIn/Manual.
- Показывать понятный next step: ответить, ждать клиента, нужен approval.

Definition of done:

- Менеджер может открыть inbox, отфильтровать ожидающие ответа диалоги, посмотреть историю и создать черновик ответа через агента.

## P0 - Activities Work Queue

Цель: сделать `Tasks` и `Activities` главным рабочим списком на день.

MVP-срез:

- Единый экран `Activities` с задачами, встречами, звонками и заметками.
- Быстрые chips: overdue, due today, this week, unassigned.
- Фильтры: mine, team, status, type, priority.
- Переключатель представления: table / grouped.
- Быстрые действия: complete, cancel, edit, create follow-up.

Backend notes:

- Можно начать с projection endpoint, который объединяет `CrmTask` и `Activity`.
- Не смешивать необратимо доменные сущности: task остается task, activity остается timeline event.
- Добавить тесты на overdue/today/week grouping.

UI notes:

- Таблица должна быть плотной: type, title, status, priority, linked entity, assignee, due/start.
- Grouped view группирует по overdue/today/upcoming/unassigned.

Definition of done:

- Менеджер видит все срочные действия на одном экране и закрывает задачу без перехода в отдельную карточку.

## P1 - Contextual AI Side Panel

Цель: дать агенту контекст текущего экрана и безопасные предлагаемые действия.

MVP-срез:

- Collapsible AI drawer справа.
- Контекст: текущая страница, выбранный контакт/сделка/диалог/задача.
- Командная строка: свободный запрос или slash-like команды.
- Список предложенных `AgentAction` с approve/reject/execute.
- Видимый `ReasoningSummary`, без chain-of-thought.

Backend notes:

- Использовать существующие `Agent`, `AgentAction`, `ApprovalRequest`.
- Добавить context payload в create action request, если текущих полей недостаточно.
- Для опасных действий требовать approval.

UI notes:

- Панель должна сворачиваться, чтобы не съедать ширину таблиц.
- Не делать ее обязательной для всех экранов на мобильных/узких viewport.

Definition of done:

- На ключевых экранах можно попросить агента подготовить действие, увидеть результат как `AgentAction` и принять/отклонить его.

## P1 - Contact Duplicates and Merge Queue

Цель: снизить мусор в базе контактов.

MVP-срез:

- Endpoint поиска потенциальных дублей.
- Правила первого среза: exact email, exact phone, normalized full name + company.
- UI badge/button `Duplicates` на экране контактов.
- Очередь дублей: candidate A, candidate B, confidence, reason.
- Действия: merge, ignore.

Backend notes:

- Merge должен быть auditable.
- При merge сохранить связи со сделками, задачами, сообщениями и активностями.
- Начать с простого deterministic matching; AI можно добавить позже как помощника, не как единственный источник истины.

Definition of done:

- Пользователь видит потенциальные дубли и может безопасно объединить две карточки контакта.

## P1 - Bulk Actions for Contacts and Deals

Цель: ускорить массовую операционную работу.

MVP-срез:

- Row selection в таблицах контактов и сделок.
- Bulk create task.
- Bulk assign responsible user.
- Bulk add note/activity.
- Bulk propose agent action.

Backend notes:

- Добавить bulk request DTO с ограничением максимального размера пачки.
- Возвращать per-item result, а не падать целиком на первой ошибке.
- Все изменения должны попадать в audit log.

Definition of done:

- Пользователь выбирает несколько записей и выполняет одну массовую операцию с понятным результатом по каждой записи.

## P2 - Products / Offers Catalog

Цель: дать менеджерам и агентам каталог предложений, которые можно использовать в сделках и коммуникациях.

MVP-срез:

- Product/Offer entity: name, type, status, owner, AI description, updated at.
- Список с поиском, фильтрами по type/status.
- Связь offer -> deal optional.
- Agent can draft a message using selected offer context.

Notes:

- Это не первый приоритет, если ближайший фокус - CRM operations.
- Не смешивать с pipeline/deal: offer describes what is sold, deal describes sales process.

## P2 - Campaigns and Prospects

Цель: подготовить будущий outbound/sales automation слой.

MVP-срез:

- Prospects list separate from contacts or contact status extension.
- Campaign entity with audience and steps.
- Add contacts/prospects to campaign as bulk action.
- Agent drafts campaign message variants through approval flow.

Notes:

- Начинать после inbox и bulk actions, иначе campaigns будут висеть без нормального execution loop.

## P2 - Production Safety

Цель: довести MVP до безопасного пилота.

Items:

- Authentication and authorization.
- RBAC.
- Agent permissions per action type.
- Hangfire dashboard protection.
- Migration job/CI step for Kubernetes.
- Real message integrations behind existing interfaces.
- Better observability for agent actions and failed jobs.

## Suggested Implementation Order

1. Conversation projection + inbox UI skeleton.
2. Conversation filters and status updates.
3. Draft/send message agent actions in the inbox.
4. Activities work queue projection + table view.
5. Activities grouped view and quick actions.
6. Collapsible AI side panel shared component.
7. Contact duplicate detection endpoint + UI queue.
8. Contact/deal bulk actions.
9. Products/offers catalog.
10. Campaigns/prospects module.
11. Production safety hardening.
