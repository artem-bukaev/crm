# Agent Action Layer

## Safety Model

AI agents must operate through the CRM API and explicit action layer.

Allowed path:

```text
AI Agent -> CRM API -> ICrmService -> CrmDbContext -> PostgreSQL
```

Disallowed path:

```text
AI Agent -> PostgreSQL
```

The reason is auditability and human control. Agent activity must be visible as structured CRM actions, not hidden direct database mutations.

## Agent Authentication and Authorization

Agents authenticate against the CRM API with an API key:

- A human issues or rotates a key via `POST /api/agents/{id}/api-key`. The plaintext key (`crm_...`) is returned exactly once; the CRM stores only its SHA-256 hash on the `Agent`.
- The agent sends the key in the `X-Api-Key` header on every request. The "AgentApiKey" authentication scheme resolves it to the Agent and produces a principal with the agent id claim.
- Deactivated (`IsActive = false`) or soft-deleted agents are rejected even with a valid key.

What an authenticated agent may do:

- READ: all GET endpoints.
- PROPOSE: `POST /api/agent-actions`.

What an agent must NOT do (enforced with authorization policies, not controller checks):

- Approve, reject or execute agent actions.
- Decide approval requests.
- Call any other mutating endpoint directly (create/update/delete of contacts, deals, tasks, messages, agents, pipelines, and so on). All mutations go through proposed actions plus human approval.

Identity rules:

- When the caller is an authenticated agent, the `AgentId` recorded on a proposed `AgentAction` always comes from the authenticated identity. A different `AgentId` in the request body is rejected with 403.
- Approve/reject decisions are attributed to the human user id from JWT claims; request bodies carry no user id.

## Core Concepts

`Agent` represents an automation actor.

`AgentAction` represents a proposed or executable operation. It stores:

- `ActionType`
- `Status`
- target entity metadata
- `InputJson`
- optional `BeforeJson`
- optional `AfterJson`
- `ReasoningSummary`
- error details

`ApprovalRequest` represents human approval for sensitive operations.

`AuditLog` records business entity mutations through EF Core save changes.

## Supported Action Types

Current action types:

- `CreateContact`
- `UpdateContact`
- `CreateDeal`
- `UpdateDealStage`
- `CreateTask`
- `CompleteTask`
- `AddNote`
- `DraftMessage`
- `SendMessage`
- `RequestHumanApproval`

When adding a new action type:

1. Add the enum value.
2. Add request payload handling in application code.
3. Validate required fields.
4. Add execution logic through normal CRM services/entities.
5. Record before/after payloads when useful.
6. Add tests for proposed, approved/rejected and executed behavior.
7. Update frontend types and UI if the action is user-facing.
8. Update this document.

## Status Flow

Typical flow:

```text
Proposed -> Approved -> Executed
Proposed -> Rejected
Approved -> Failed
```

Do not silently execute sensitive actions without preserving the approval model. If in doubt, require human approval.

## Reasoning Policy

Store only a concise `ReasoningSummary`.

Do not store chain-of-thought, hidden prompts or raw model scratchpads. The CRM should keep business-relevant reasoning summaries that are safe to audit and show to users.

## Human Approval

Use approval requests for actions that:

- send external messages;
- materially change deal state;
- create tasks for humans;
- could affect customer communication;
- are ambiguous or have low confidence;
- are irreversible or hard to undo.

Authentication and the read+propose authorization model for agents are implemented (see "Agent Authentication and Authorization" above). Future work should still add finer-grained per-agent permissions and per-action policy checks before production AI integrations are enabled.

## Heartbeat Triggers

Hangfire recurring jobs ("heartbeats") scan the CRM for trigger conditions and create `AgentAction` proposals through `ICrmService.CreateAgentActionAsync`, always with `RequiresApproval = true` (status `Proposed`, with a pending `ApprovalRequest`). Nothing is executed automatically; humans or AI agents review the proposals through the normal approval flow.

Detection logic lives in `IAgentTriggerService`/`AgentTriggerService` in `Crm.Application`. Thin job wrappers live in `Crm.WebApi/Jobs/AgentHeartbeatJobs.cs` and only run the service and log summary counts (detected/created/skipped) via Serilog.

Triggers and proposal mapping:

| Trigger | Condition | Proposal | Target |
| --- | --- | --- | --- |
| Waiting conversation | Last message in a conversation is inbound and older than `WaitingConversationThresholdHours` | `DraftMessage` with a placeholder reply payload | Contact (fallback: Deal) |
| Overdue task | Open task (`New`/`InProgress`) with `DueAt` in the past | `AddNote` reminder on the task's linked contact/company/deal | Task |
| Stale deal | Open deal with no touch (deal update, task, activity, message) within `StaleDealThresholdDays` | `CreateTask` follow-up linked to the deal | Deal |

Each proposal carries a concise `ReasoningSummary` describing the entity, the condition and the threshold. No chain-of-thought is stored.

Actor: proposals are created by a dedicated system agent (default name `CRM Heartbeat`), seeded idempotently by `CrmDbInitializer` on startup. If the configured agent is missing or inactive, jobs log a warning and skip the run without failing.

Idempotency: a new proposal is skipped while a pending `AgentAction` (status `Proposed` or `Approved`) of the same `ActionType` already targets the same entity. After a proposal is executed or rejected, a new one may be created on a later run if the condition still holds.

Configuration (thresholds, cron schedules, agent name, enable switch) comes from the `AgentHeartbeat` section, see `runbook.md`.

## Integration Guidance

Current integration implementations are fake placeholders in Infrastructure. Real AI/runtime/message integrations should:

- be injected behind interfaces;
- avoid direct EF Core access where an application service method exists;
- be idempotent where possible;
- log operational failures without leaking secrets;
- keep external API keys in secrets, never in source code.
