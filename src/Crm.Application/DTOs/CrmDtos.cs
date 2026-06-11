using Crm.Domain.Enums;

namespace Crm.Application.DTOs;

public sealed record DashboardSummaryDto(
    int Contacts,
    int Companies,
    int OpenDeals,
    decimal OpenDealAmount,
    int OpenTasks,
    int PendingAgentActions,
    int PendingApprovals);

public class CreateContactRequest
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

public sealed class UpdateContactRequest : CreateContactRequest
{
}

public sealed record ContactDto(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string FullName,
    string? Phone,
    string? Email,
    string? TelegramUsername,
    Guid? CompanyId,
    string? CompanyName,
    string? Position,
    string? Source,
    ContactStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Inn { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public sealed class UpdateCompanyRequest : CreateCompanyRequest
{
}

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? Inn,
    string? Website,
    string? Phone,
    string? Email,
    string? Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public class CreatePipelineRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class UpdatePipelineRequest : CreatePipelineRequest
{
}

public sealed record PipelineDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public class CreatePipelineStageRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Probability { get; set; }
    public bool IsWon { get; set; }
    public bool IsLost { get; set; }
}

public sealed class UpdatePipelineStageRequest : CreatePipelineStageRequest
{
}

public sealed record PipelineStageDto(
    Guid Id,
    Guid PipelineId,
    string Name,
    int SortOrder,
    int Probability,
    bool IsWon,
    bool IsLost,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public class CreateDealRequest
{
    public string Title { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid PipelineId { get; set; }
    public Guid StageId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public int? Probability { get; set; }
    public DealStatus Status { get; set; } = DealStatus.Open;
    public string? Source { get; set; }
    public Guid? ResponsibleUserId { get; set; }
}

public sealed class UpdateDealRequest : CreateDealRequest
{
}

public sealed record DealDto(
    Guid Id,
    string Title,
    Guid? ContactId,
    string? ContactName,
    Guid? CompanyId,
    string? CompanyName,
    Guid PipelineId,
    string? PipelineName,
    Guid StageId,
    string? StageName,
    decimal Amount,
    string Currency,
    int Probability,
    DealStatus Status,
    string? Source,
    Guid? ResponsibleUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt);

public sealed class MoveDealStageRequest
{
    public Guid StageId { get; set; }
}

public class CreateTaskRequest
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
}

public sealed class UpdateTaskRequest : CreateTaskRequest
{
}

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset? DueAt,
    CrmTaskStatus Status,
    CrmTaskPriority Priority,
    Guid? ContactId,
    string? ContactName,
    Guid? CompanyId,
    string? CompanyName,
    Guid? DealId,
    string? DealTitle,
    Guid? ResponsibleUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed class CreateActivityRequest
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

public sealed record ActivityDto(
    Guid Id,
    ActivityType Type,
    string Title,
    string? Description,
    Guid? ContactId,
    string? ContactName,
    Guid? CompanyId,
    string? CompanyName,
    Guid? DealId,
    string? DealTitle,
    Guid? CreatedByUserId,
    Guid? CreatedByAgentId,
    DateTimeOffset CreatedAt);

public sealed class CreateMessageRequest
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

public sealed record MessageDto(
    Guid Id,
    MessageChannel Channel,
    MessageDirection Direction,
    string? ExternalMessageId,
    Guid? ContactId,
    string? ContactName,
    Guid? DealId,
    string? DealTitle,
    string Text,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ConversationDto(
    string Id,
    Guid? ContactId,
    string? ContactName,
    Guid? CompanyId,
    string? CompanyName,
    Guid? DealId,
    string? DealTitle,
    MessageChannel LastChannel,
    MessageDirection LastDirection,
    string LastMessageText,
    DateTimeOffset LastMessageAt,
    ConversationStatus Status,
    int MessageCount,
    int OpenTaskCount,
    IReadOnlyList<MessageDto> Messages);

public sealed record WorkQueueItemDto(
    string Id,
    WorkQueueItemType Type,
    Guid SourceId,
    string Title,
    string? Description,
    ActivityType? ActivityType,
    CrmTaskStatus? TaskStatus,
    CrmTaskPriority? Priority,
    DateTimeOffset? DueAt,
    DateTimeOffset? StartedAt,
    Guid? ContactId,
    string? ContactName,
    Guid? CompanyId,
    string? CompanyName,
    Guid? DealId,
    string? DealTitle,
    Guid? ResponsibleUserId,
    WorkQueueBucket Bucket,
    bool IsOverdue,
    DateTimeOffset SortAt);

public sealed record ContactDuplicateCandidateDto(
    string Id,
    ContactDto PrimaryContact,
    ContactDto DuplicateContact,
    int Confidence,
    string Reason);

public sealed class MergeContactsRequest
{
    public Guid PrimaryContactId { get; set; }
    public Guid DuplicateContactId { get; set; }
}

public sealed class BulkCreateTaskRequest
{
    public IReadOnlyList<Guid> ContactIds { get; set; } = [];
    public IReadOnlyList<Guid> DealIds { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public CrmTaskPriority Priority { get; set; } = CrmTaskPriority.Normal;
    public Guid? ResponsibleUserId { get; set; }
}

public sealed record BulkOperationItemResultDto(
    Guid TargetId,
    CrmEntityType TargetType,
    bool Succeeded,
    string? Message,
    Guid? CreatedTaskId);

public sealed record BulkOperationResultDto(
    int Requested,
    int Succeeded,
    int Failed,
    IReadOnlyList<BulkOperationItemResultDto> Items);

public class CreateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateAgentRequest : CreateAgentRequest
{
}

public sealed record AgentDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class CreateAgentActionRequest
{
    public Guid AgentId { get; set; }
    public AgentActionType ActionType { get; set; }
    public CrmEntityType? TargetEntityType { get; set; }
    public Guid? TargetEntityId { get; set; }
    public string InputJson { get; set; } = "{}";
    public string? ReasoningSummary { get; set; }
    public bool RequiresApproval { get; set; } = true;
}

public sealed class AgentActionDecisionRequest
{
    public Guid? UserId { get; set; }
}

public sealed record AgentActionDto(
    Guid Id,
    Guid AgentId,
    string? AgentName,
    AgentActionType ActionType,
    AgentActionStatus Status,
    CrmEntityType? TargetEntityType,
    Guid? TargetEntityId,
    string InputJson,
    string? ReasoningSummary,
    string? BeforeJson,
    string? AfterJson,
    bool RequiresApproval,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    Guid? RejectedByUserId,
    DateTimeOffset? RejectedAt,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ExecutedAt);

public sealed record AgentHeartbeatRunResultDto(
    string Trigger,
    bool AgentAvailable,
    int Detected,
    int Created,
    int Skipped);

public sealed record ApprovalRequestDto(
    Guid Id,
    CrmEntityType EntityType,
    Guid EntityId,
    string Title,
    string? Description,
    ApprovalStatus Status,
    Guid? RequestedByUserId,
    Guid? RequestedByAgentId,
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    Guid? RejectedByUserId,
    DateTimeOffset? RejectedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
