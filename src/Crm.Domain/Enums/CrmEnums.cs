namespace Crm.Domain.Enums;

public enum UserRole
{
    Admin,
    Manager
}

public enum ContactStatus
{
    Lead,
    Active,
    Inactive,
    Archived
}

public enum DealStatus
{
    Open,
    Won,
    Lost,
    Canceled
}

public enum CrmTaskStatus
{
    New,
    InProgress,
    Completed,
    Canceled
}

public enum CrmTaskPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum ActivityType
{
    Note,
    Call,
    Email,
    TelegramMessage,
    Meeting,
    SystemEvent,
    AgentAction
}

public enum MessageChannel
{
    Email,
    Telegram,
    WhatsApp,
    LinkedIn,
    WebsiteChat,
    Manual
}

public enum MessageDirection
{
    Incoming,
    Outgoing
}

public enum ConversationStatus
{
    Unread,
    WaitingOnUs,
    WaitingOnThem,
    Closed
}

public enum WorkQueueItemType
{
    Task,
    Activity
}

public enum WorkQueueBucket
{
    Overdue,
    DueToday,
    ThisWeek,
    Upcoming,
    Unassigned
}

public enum AgentActionStatus
{
    Proposed,
    Approved,
    Rejected,
    Executed,
    Failed,
    Canceled
}

public enum AgentActionType
{
    CreateContact,
    UpdateContact,
    CreateDeal,
    UpdateDealStage,
    CreateTask,
    CompleteTask,
    AddNote,
    DraftMessage,
    SendMessage,
    RequestHumanApproval
}

public enum CrmEntityType
{
    Contact,
    Company,
    Pipeline,
    PipelineStage,
    Deal,
    Task,
    Activity,
    Message,
    Agent,
    AgentAction,
    ApprovalRequest,
    User
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Canceled
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
    StageChanged,
    StatusChanged,
    AgentActionProposed,
    AgentActionApproved,
    AgentActionRejected,
    AgentActionExecuted,
    AgentActionFailed
}
