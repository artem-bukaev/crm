using Crm.Domain.Common;
using Crm.Domain.Enums;

namespace Crm.Domain.Entities;

public sealed class Contact : Entity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TelegramUsername { get; set; }
    public Guid? CompanyId { get; set; }
    public string? Position { get; set; }
    public string? Source { get; set; }
    public ContactStatus Status { get; set; } = ContactStatus.Lead;
}

public sealed class Company : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Inn { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public sealed class Pipeline : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class PipelineStage : Entity
{
    public Guid PipelineId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Probability { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
}

public sealed class Deal : Entity
{
    public string Title { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public int Probability { get; set; }
    public DealStatus Status { get; set; } = DealStatus.Open;
    public string? Source { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class CrmTask : Entity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public CrmTaskStatus Status { get; set; } = CrmTaskStatus.New;
    public CrmTaskPriority Priority { get; set; } = CrmTaskPriority.Normal;
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class Activity : Entity
{
    public ActivityType Type { get; set; } = ActivityType.Note;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? CreatedByAgentId { get; set; }
}

public sealed class Message : Entity
{
    public MessageChannel Channel { get; set; } = MessageChannel.Manual;
    public MessageDirection Direction { get; set; } = MessageDirection.Outgoing;
    public string? ExternalMessageId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? DealId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset? ReceivedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

public sealed class Agent : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AgentAction : Entity
{
    public Guid AgentId { get; set; }
    public AgentActionType ActionType { get; set; }
    public AgentActionStatus Status { get; set; } = AgentActionStatus.Proposed;
    public CrmEntityType? TargetEntityType { get; set; }
    public Guid? TargetEntityId { get; set; }
    public string InputJson { get; set; } = "{}";
    public string? ReasoningSummary { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }
}

public sealed class ApprovalRequest : Entity
{
    public CrmEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public Guid? RequestedByUserId { get; set; }
    public Guid? RequestedByAgentId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CrmEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }
    public Guid? UserId { get; set; }
    public Guid? AgentId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
