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

Future work should add RBAC, agent permissions and per-action policy checks before production AI integrations are enabled.

## Integration Guidance

Current integration implementations are fake placeholders in Infrastructure. Real AI/runtime/message integrations should:

- be injected behind interfaces;
- avoid direct EF Core access where an application service method exists;
- be idempotent where possible;
- log operational failures without leaking secrets;
- keep external API keys in secrets, never in source code.
