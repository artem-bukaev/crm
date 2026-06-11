using Crm.Application.DTOs;
using Crm.Application.Services;

namespace Crm.WebApi.Jobs;

/// <summary>
/// Thin Hangfire recurring job entry points for heartbeat trigger detection.
/// All business logic lives in <see cref="IAgentTriggerService"/>; jobs only run it and log a summary.
/// </summary>
public sealed class AgentHeartbeatJobs(IAgentTriggerService triggerService, ILogger<AgentHeartbeatJobs> logger)
{
    public const string WaitingConversationsJobId = "agent-heartbeat:waiting-conversations";
    public const string OverdueTasksJobId = "agent-heartbeat:overdue-tasks";
    public const string StaleDealsJobId = "agent-heartbeat:stale-deals";

    public Task DetectWaitingConversationsAsync(CancellationToken cancellationToken) =>
        RunAsync(triggerService.DetectWaitingConversationsAsync, cancellationToken);

    public Task DetectOverdueTasksAsync(CancellationToken cancellationToken) =>
        RunAsync(triggerService.DetectOverdueTasksAsync, cancellationToken);

    public Task DetectStaleDealsAsync(CancellationToken cancellationToken) =>
        RunAsync(triggerService.DetectStaleDealsAsync, cancellationToken);

    private async Task RunAsync(
        Func<CancellationToken, Task<AgentHeartbeatRunResultDto>> detect,
        CancellationToken cancellationToken)
    {
        AgentHeartbeatRunResultDto result;
        try
        {
            result = await detect(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent heartbeat trigger run failed");
            throw;
        }

        if (!result.AgentAvailable)
        {
            logger.LogWarning(
                "Agent heartbeat {Trigger} skipped: heartbeat agent is missing or inactive. Check the AgentHeartbeat:AgentName configuration and seed data",
                result.Trigger);
            return;
        }

        logger.LogInformation(
            "Agent heartbeat {Trigger}: detected {Detected}, created {Created}, skipped {Skipped}",
            result.Trigger,
            result.Detected,
            result.Created,
            result.Skipped);
    }
}
