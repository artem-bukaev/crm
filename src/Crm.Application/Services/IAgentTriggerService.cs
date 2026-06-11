using Crm.Application.DTOs;

namespace Crm.Application.Services;

/// <summary>
/// Detects CRM trigger conditions (heartbeat checks) and creates
/// <c>AgentAction</c> proposals through the auditable action layer.
/// All proposals are created with <c>RequiresApproval = true</c>; nothing is executed automatically.
/// </summary>
public interface IAgentTriggerService
{
    /// <summary>
    /// Detects conversations whose last message is inbound and older than the configured
    /// threshold and proposes a <c>DraftMessage</c> action for each.
    /// </summary>
    Task<AgentHeartbeatRunResultDto> DetectWaitingConversationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects open tasks with a due date in the past and proposes an <c>AddNote</c>
    /// action on the linked records for each.
    /// </summary>
    Task<AgentHeartbeatRunResultDto> DetectOverdueTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects open deals with no touch (deal update, task, activity, message) within the
    /// configured number of days and proposes a <c>CreateTask</c> follow-up for each.
    /// </summary>
    Task<AgentHeartbeatRunResultDto> DetectStaleDealsAsync(CancellationToken cancellationToken = default);
}
