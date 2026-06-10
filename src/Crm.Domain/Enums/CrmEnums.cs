namespace Crm.Domain.Enums;

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
    WebsiteChat,
    Manual
}

public enum MessageDirection
{
    Incoming,
    Outgoing
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
    ApprovalRequest
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
