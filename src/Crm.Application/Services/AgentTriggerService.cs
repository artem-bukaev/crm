using System.Text.Json;
using Crm.Application.DTOs;
using Crm.Application.Interfaces;
using Crm.Application.Options;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Crm.Application.Services;

public sealed class AgentTriggerService(
    ICrmDataStore db,
    ICrmService crmService,
    IOptions<AgentHeartbeatOptions> options) : IAgentTriggerService
{
    private const string WaitingConversationsTrigger = "WaitingConversations";
    private const string OverdueTasksTrigger = "OverdueTasks";
    private const string StaleDealsTrigger = "StaleDeals";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<AgentHeartbeatRunResultDto> DetectWaitingConversationsAsync(CancellationToken cancellationToken = default)
    {
        var agent = FindHeartbeatAgent();
        if (agent is null)
        {
            return AgentMissing(WaitingConversationsTrigger);
        }

        var now = DateTimeOffset.UtcNow;
        var thresholdHours = Math.Max(0, options.Value.WaitingConversationThresholdHours);
        var cutoff = now.AddHours(-thresholdHours);
        var detected = 0;
        var created = 0;
        var skipped = 0;

        var conversations = Active<Message>()
            .ToList()
            .GroupBy(ConversationHelper.GetConversationId);

        foreach (var conversation in conversations)
        {
            var ordered = conversation
                .OrderBy(ConversationHelper.GetTimestamp)
                .ThenBy(x => x.CreatedAt)
                .ToList();
            var last = ordered[^1];
            if (last.Direction != MessageDirection.Incoming)
            {
                continue;
            }

            var lastAt = ConversationHelper.GetTimestamp(last);
            if (lastAt > cutoff)
            {
                continue;
            }

            detected++;

            var dealId = ordered.LastOrDefault(x => x.DealId is not null)?.DealId;
            var contactId = ordered.LastOrDefault(x => x.ContactId is not null)?.ContactId
                ?? (dealId is null ? null : Active<Deal>().FirstOrDefault(x => x.Id == dealId)?.ContactId);
            var targetType = contactId is not null
                ? CrmEntityType.Contact
                : dealId is not null ? CrmEntityType.Deal : (CrmEntityType?)null;
            var targetId = contactId ?? dealId;

            if (targetType is null || targetId is null || HasPendingAction(AgentActionType.DraftMessage, targetType.Value, targetId.Value))
            {
                skipped++;
                continue;
            }

            var waitingHours = Math.Round((now - lastAt).TotalHours, 1);
            var input = new CreateMessageRequest
            {
                Channel = last.Channel,
                Direction = MessageDirection.Outgoing,
                ContactId = contactId,
                DealId = dealId,
                Text = "[Draft] Follow up on the customer's last message."
            };

            await crmService.CreateAgentActionAsync(new CreateAgentActionRequest
            {
                AgentId = agent.Id,
                ActionType = AgentActionType.DraftMessage,
                TargetEntityType = targetType,
                TargetEntityId = targetId,
                RequiresApproval = true,
                InputJson = Serialize(input),
                ReasoningSummary = FormattableString.Invariant(
                    $"Conversation has an unanswered inbound {last.Channel} message from {lastAt:u} ({waitingHours}h waiting, threshold {thresholdHours}h). Proposing a draft reply.")
            }, cancellationToken);

            created++;
        }

        return new AgentHeartbeatRunResultDto(WaitingConversationsTrigger, true, detected, created, skipped);
    }

    public async Task<AgentHeartbeatRunResultDto> DetectOverdueTasksAsync(CancellationToken cancellationToken = default)
    {
        var agent = FindHeartbeatAgent();
        if (agent is null)
        {
            return AgentMissing(OverdueTasksTrigger);
        }

        var now = DateTimeOffset.UtcNow;
        var detected = 0;
        var created = 0;
        var skipped = 0;

        var overdueTasks = Active<CrmTask>()
            .Where(x => x.Status == CrmTaskStatus.New || x.Status == CrmTaskStatus.InProgress)
            .ToList()
            .Where(x => x.DueAt is not null && x.DueAt.Value < now)
            .ToList();

        foreach (var task in overdueTasks)
        {
            detected++;

            if (HasPendingAction(AgentActionType.AddNote, CrmEntityType.Task, task.Id))
            {
                skipped++;
                continue;
            }

            var overdueDays = Math.Round((now - task.DueAt!.Value).TotalDays, 1);
            var input = new CreateActivityRequest
            {
                Type = ActivityType.Note,
                Title = Truncate($"Overdue task: {task.Title}", 256),
                Description = FormattableString.Invariant(
                    $"Task \"{task.Title}\" was due {task.DueAt.Value:u} and is still {task.Status}. Review, complete or reschedule it."),
                ContactId = task.ContactId,
                CompanyId = task.CompanyId,
                DealId = task.DealId,
                CreatedByAgentId = agent.Id
            };

            await crmService.CreateAgentActionAsync(new CreateAgentActionRequest
            {
                AgentId = agent.Id,
                ActionType = AgentActionType.AddNote,
                TargetEntityType = CrmEntityType.Task,
                TargetEntityId = task.Id,
                RequiresApproval = true,
                InputJson = Serialize(input),
                ReasoningSummary = FormattableString.Invariant(
                    $"Task \"{Truncate(task.Title, 128)}\" was due {task.DueAt.Value:u} ({overdueDays}d overdue) and is still {task.Status}. Proposing a reminder note on the linked records.")
            }, cancellationToken);

            created++;
        }

        return new AgentHeartbeatRunResultDto(OverdueTasksTrigger, true, detected, created, skipped);
    }

    public async Task<AgentHeartbeatRunResultDto> DetectStaleDealsAsync(CancellationToken cancellationToken = default)
    {
        var agent = FindHeartbeatAgent();
        if (agent is null)
        {
            return AgentMissing(StaleDealsTrigger);
        }

        var now = DateTimeOffset.UtcNow;
        var thresholdDays = Math.Max(1, options.Value.StaleDealThresholdDays);
        var cutoff = now.AddDays(-thresholdDays);
        var detected = 0;
        var created = 0;
        var skipped = 0;

        var openDeals = Active<Deal>()
            .Where(x => x.Status == DealStatus.Open)
            .ToList();
        if (openDeals.Count == 0)
        {
            return new AgentHeartbeatRunResultDto(StaleDealsTrigger, true, 0, 0, 0);
        }

        var taskTouches = BuildTouchIndex(Active<CrmTask>().Where(x => x.DealId != null).ToList(), x => x.DealId, x => x.UpdatedAt);
        var activityTouches = BuildTouchIndex(Active<Activity>().Where(x => x.DealId != null).ToList(), x => x.DealId, x => x.CreatedAt);
        var messageTouches = BuildTouchIndex(Active<Message>().Where(x => x.DealId != null).ToList(), x => x.DealId, ConversationHelper.GetTimestamp);

        foreach (var deal in openDeals)
        {
            var lastTouch = deal.UpdatedAt;
            lastTouch = MaxTouch(lastTouch, taskTouches, deal.Id);
            lastTouch = MaxTouch(lastTouch, activityTouches, deal.Id);
            lastTouch = MaxTouch(lastTouch, messageTouches, deal.Id);

            if (lastTouch > cutoff)
            {
                continue;
            }

            detected++;

            if (HasPendingAction(AgentActionType.CreateTask, CrmEntityType.Deal, deal.Id))
            {
                skipped++;
                continue;
            }

            var staleDays = (int)Math.Floor((now - lastTouch).TotalDays);
            var input = new CreateTaskRequest
            {
                Title = Truncate($"Follow up on deal \"{deal.Title}\"", 256),
                Description = FormattableString.Invariant(
                    $"Deal \"{deal.Title}\" has had no recorded activity since {lastTouch:u}. Automatic follow-up proposed by the heartbeat agent."),
                DueAt = now.AddDays(1),
                Priority = CrmTaskPriority.Normal,
                ContactId = deal.ContactId,
                CompanyId = deal.CompanyId,
                DealId = deal.Id,
                ResponsibleUserId = deal.ResponsibleUserId
            };

            await crmService.CreateAgentActionAsync(new CreateAgentActionRequest
            {
                AgentId = agent.Id,
                ActionType = AgentActionType.CreateTask,
                TargetEntityType = CrmEntityType.Deal,
                TargetEntityId = deal.Id,
                RequiresApproval = true,
                InputJson = Serialize(input),
                ReasoningSummary = FormattableString.Invariant(
                    $"Open deal \"{Truncate(deal.Title, 128)}\" has had no activity, task or message touch since {lastTouch:u} ({staleDays} days, threshold {thresholdDays} days). Proposing a follow-up task.")
            }, cancellationToken);

            created++;
        }

        return new AgentHeartbeatRunResultDto(StaleDealsTrigger, true, detected, created, skipped);
    }

    private IQueryable<TEntity> Active<TEntity>() where TEntity : Entity =>
        db.Query<TEntity>().Where(x => !x.IsDeleted);

    private Agent? FindHeartbeatAgent()
    {
        var name = options.Value.AgentName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return Active<Agent>().FirstOrDefault(x => x.IsActive && x.Name == name);
    }

    private bool HasPendingAction(AgentActionType actionType, CrmEntityType targetType, Guid targetId) =>
        Active<AgentAction>().Any(x =>
            x.ActionType == actionType &&
            x.TargetEntityType == targetType &&
            x.TargetEntityId == targetId &&
            (x.Status == AgentActionStatus.Proposed || x.Status == AgentActionStatus.Approved));

    private static Dictionary<Guid, DateTimeOffset> BuildTouchIndex<TEntity>(
        IReadOnlyList<TEntity> items,
        Func<TEntity, Guid?> dealId,
        Func<TEntity, DateTimeOffset> touchedAt) =>
        items
            .Where(x => dealId(x) is not null)
            .GroupBy(x => dealId(x)!.Value)
            .ToDictionary(x => x.Key, x => x.Max(touchedAt));

    private static DateTimeOffset MaxTouch(DateTimeOffset current, Dictionary<Guid, DateTimeOffset> touches, Guid dealId) =>
        touches.TryGetValue(dealId, out var touch) && touch > current ? touch : current;

    private static AgentHeartbeatRunResultDto AgentMissing(string trigger) =>
        new(trigger, false, 0, 0, 0);

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
