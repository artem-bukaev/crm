namespace Crm.Application.Options;

/// <summary>
/// Configuration for the Hangfire heartbeat jobs that detect CRM trigger
/// conditions and create <c>AgentAction</c> proposals through the action layer.
/// Bound from the <c>AgentHeartbeat</c> configuration section.
/// </summary>
public sealed class AgentHeartbeatOptions
{
    public const string SectionName = "AgentHeartbeat";
    public const string DefaultAgentName = "CRM Heartbeat";

    public const string DefaultWaitingConversationsCron = "*/15 * * * *";
    public const string DefaultOverdueTasksCron = "*/15 * * * *";
    public const string DefaultStaleDealsCron = "0 * * * *";

    /// <summary>Master switch for all heartbeat recurring jobs.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Name of the active system agent used as the actor for heartbeat proposals.</summary>
    public string AgentName { get; set; } = DefaultAgentName;

    /// <summary>A conversation whose last message is inbound and older than this is considered waiting on us.</summary>
    public double WaitingConversationThresholdHours { get; set; } = 4;

    /// <summary>An open deal with no touch (deal update, task, activity, message) within this many days is considered stale.</summary>
    public int StaleDealThresholdDays { get; set; } = 7;

    /// <summary>Cron schedule for the waiting-conversations detection job.</summary>
    public string WaitingConversationsCron { get; set; } = DefaultWaitingConversationsCron;

    /// <summary>Cron schedule for the overdue-tasks detection job.</summary>
    public string OverdueTasksCron { get; set; } = DefaultOverdueTasksCron;

    /// <summary>Cron schedule for the stale-deals detection job.</summary>
    public string StaleDealsCron { get; set; } = DefaultStaleDealsCron;
}
