using System.Text.Json;
using Crm.Application.DTOs;
using Crm.Application.Options;
using Crm.Application.Services;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crm.Tests;

public sealed class AgentTriggerServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CrmDbContext _db;
    private readonly CrmService _service;
    private readonly AgentTriggerService _triggerService;
    private readonly AgentHeartbeatOptions _options;
    private readonly Pipeline _pipeline;
    private readonly PipelineStage _newStage;
    private readonly Company _company;
    private readonly Contact _contact;
    private readonly Agent _heartbeatAgent;

    public AgentTriggerServiceTests()
    {
        _connection.Open();
        _db = new CrmDbContext(new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        // Heartbeat jobs run in the background with no authenticated actor.
        _service = new CrmService(_db, new TestCurrentActor());

        _options = new AgentHeartbeatOptions();
        _triggerService = new AgentTriggerService(_db, _service, Microsoft.Extensions.Options.Options.Create(_options));

        _pipeline = new Pipeline { Name = "Default", IsDefault = true };
        _newStage = new PipelineStage { PipelineId = _pipeline.Id, Name = "New", SortOrder = 10, Probability = 10 };
        _company = new Company { Name = "Acme" };
        _contact = new Contact { FirstName = "Ivan", LastName = "Petrov", CompanyId = _company.Id };
        _heartbeatAgent = new Agent { Name = AgentHeartbeatOptions.DefaultAgentName, IsActive = true };

        _db.AddRange(_pipeline, _newStage, _company, _contact, _heartbeatAgent);
        _db.SaveChanges();
    }

    [Fact]
    public async Task DetectWaitingConversations_creates_draft_message_proposal()
    {
        await CreateWaitingConversationAsync(hoursAgo: 5);

        var result = await _triggerService.DetectWaitingConversationsAsync();

        result.Should().Be(new AgentHeartbeatRunResultDto("WaitingConversations", true, 1, 1, 0));

        var action = _db.AgentActions.Single();
        action.AgentId.Should().Be(_heartbeatAgent.Id);
        action.ActionType.Should().Be(AgentActionType.DraftMessage);
        action.Status.Should().Be(AgentActionStatus.Proposed);
        action.RequiresApproval.Should().BeTrue();
        action.TargetEntityType.Should().Be(CrmEntityType.Contact);
        action.TargetEntityId.Should().Be(_contact.Id);
        action.ReasoningSummary.Should().Contain("unanswered inbound").And.Contain("threshold 4h");

        var input = JsonSerializer.Deserialize<CreateMessageRequest>(action.InputJson, JsonOptions)!;
        input.ContactId.Should().Be(_contact.Id);
        input.Direction.Should().Be(MessageDirection.Outgoing);
        input.Channel.Should().Be(MessageChannel.Email);
        input.Text.Should().NotBeNullOrWhiteSpace();

        _db.ApprovalRequests.Count(x => x.EntityId == action.Id && x.Status == ApprovalStatus.Pending).Should().Be(1);
    }

    [Fact]
    public async Task DetectWaitingConversations_second_run_creates_nothing_new()
    {
        await CreateWaitingConversationAsync(hoursAgo: 5);

        await _triggerService.DetectWaitingConversationsAsync();
        var secondRun = await _triggerService.DetectWaitingConversationsAsync();

        secondRun.Should().Be(new AgentHeartbeatRunResultDto("WaitingConversations", true, 1, 0, 1));
        _db.AgentActions.Count().Should().Be(1);
    }

    [Fact]
    public async Task DetectWaitingConversations_ignores_recent_and_answered_conversations()
    {
        await CreateWaitingConversationAsync(hoursAgo: 1);
        await _service.CreateMessageAsync(new CreateMessageRequest
        {
            ContactId = _contact.Id,
            Channel = MessageChannel.Email,
            Direction = MessageDirection.Outgoing,
            Text = "We already replied.",
            SentAt = DateTimeOffset.UtcNow
        });

        var result = await _triggerService.DetectWaitingConversationsAsync();

        result.Detected.Should().Be(0);
        result.Created.Should().Be(0);
        _db.AgentActions.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectOverdueTasks_creates_add_note_proposal()
    {
        var task = await CreateOverdueTaskAsync();

        var result = await _triggerService.DetectOverdueTasksAsync();

        result.Should().Be(new AgentHeartbeatRunResultDto("OverdueTasks", true, 1, 1, 0));

        var action = _db.AgentActions.Single();
        action.AgentId.Should().Be(_heartbeatAgent.Id);
        action.ActionType.Should().Be(AgentActionType.AddNote);
        action.Status.Should().Be(AgentActionStatus.Proposed);
        action.RequiresApproval.Should().BeTrue();
        action.TargetEntityType.Should().Be(CrmEntityType.Task);
        action.TargetEntityId.Should().Be(task.Id);
        action.ReasoningSummary.Should().Contain("Call Ivan").And.Contain("overdue");

        var input = JsonSerializer.Deserialize<CreateActivityRequest>(action.InputJson, JsonOptions)!;
        input.Type.Should().Be(ActivityType.Note);
        input.Title.Should().Contain("Call Ivan");
        input.ContactId.Should().Be(_contact.Id);
        input.CompanyId.Should().Be(_company.Id);
        input.CreatedByAgentId.Should().Be(_heartbeatAgent.Id);
    }

    [Fact]
    public async Task DetectOverdueTasks_second_run_creates_nothing_new()
    {
        await CreateOverdueTaskAsync();

        await _triggerService.DetectOverdueTasksAsync();
        var secondRun = await _triggerService.DetectOverdueTasksAsync();

        secondRun.Should().Be(new AgentHeartbeatRunResultDto("OverdueTasks", true, 1, 0, 1));
        _db.AgentActions.Count().Should().Be(1);
    }

    [Fact]
    public async Task DetectOverdueTasks_ignores_completed_and_future_tasks()
    {
        var completed = await CreateOverdueTaskAsync();
        await _service.CompleteTaskAsync(completed.Id);
        await _service.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Future task",
            ContactId = _contact.Id,
            DueAt = DateTimeOffset.UtcNow.AddDays(2)
        });

        var result = await _triggerService.DetectOverdueTasksAsync();

        result.Detected.Should().Be(0);
        _db.AgentActions.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectStaleDeals_creates_follow_up_task_proposal()
    {
        var deal = CreateStaleDeal(daysAgo: 10);

        var result = await _triggerService.DetectStaleDealsAsync();

        result.Should().Be(new AgentHeartbeatRunResultDto("StaleDeals", true, 1, 1, 0));

        var action = _db.AgentActions.Single();
        action.AgentId.Should().Be(_heartbeatAgent.Id);
        action.ActionType.Should().Be(AgentActionType.CreateTask);
        action.Status.Should().Be(AgentActionStatus.Proposed);
        action.RequiresApproval.Should().BeTrue();
        action.TargetEntityType.Should().Be(CrmEntityType.Deal);
        action.TargetEntityId.Should().Be(deal.Id);
        action.ReasoningSummary.Should().Contain("Stale deal").And.Contain("threshold 7 days");

        var input = JsonSerializer.Deserialize<CreateTaskRequest>(action.InputJson, JsonOptions)!;
        input.Title.Should().Contain("Stale deal");
        input.DealId.Should().Be(deal.Id);
        input.ContactId.Should().Be(_contact.Id);
        input.CompanyId.Should().Be(_company.Id);
        input.DueAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DetectStaleDeals_second_run_creates_nothing_new()
    {
        CreateStaleDeal(daysAgo: 10);

        await _triggerService.DetectStaleDealsAsync();
        var secondRun = await _triggerService.DetectStaleDealsAsync();

        secondRun.Should().Be(new AgentHeartbeatRunResultDto("StaleDeals", true, 1, 0, 1));
        _db.AgentActions.Count().Should().Be(1);
    }

    [Fact]
    public async Task DetectStaleDeals_ignores_recently_touched_deals()
    {
        var staleByUpdate = CreateStaleDeal(daysAgo: 10);
        await _service.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Recent touch",
            DealId = staleByUpdate.Id
        });

        var result = await _triggerService.DetectStaleDealsAsync();

        result.Detected.Should().Be(0);
        _db.AgentActions.Should().BeEmpty();
    }

    [Fact]
    public async Task Detections_skip_without_creating_when_heartbeat_agent_is_missing()
    {
        var service = new AgentTriggerService(_db, _service, Microsoft.Extensions.Options.Options.Create(
            new AgentHeartbeatOptions { AgentName = "Missing Agent" }));
        await CreateOverdueTaskAsync();
        await CreateWaitingConversationAsync(hoursAgo: 5);
        CreateStaleDeal(daysAgo: 10);

        var results = new[]
        {
            await service.DetectWaitingConversationsAsync(),
            await service.DetectOverdueTasksAsync(),
            await service.DetectStaleDealsAsync()
        };

        results.Should().AllSatisfy(x =>
        {
            x.AgentAvailable.Should().BeFalse();
            x.Created.Should().Be(0);
        });
        _db.AgentActions.Should().BeEmpty();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task CreateWaitingConversationAsync(double hoursAgo)
    {
        await _service.CreateMessageAsync(new CreateMessageRequest
        {
            ContactId = _contact.Id,
            Channel = MessageChannel.Email,
            Direction = MessageDirection.Outgoing,
            Text = "Hello Ivan.",
            SentAt = DateTimeOffset.UtcNow.AddHours(-hoursAgo - 1)
        });
        await _service.CreateMessageAsync(new CreateMessageRequest
        {
            ContactId = _contact.Id,
            Channel = MessageChannel.Email,
            Direction = MessageDirection.Incoming,
            Text = "Please send the offer.",
            ReceivedAt = DateTimeOffset.UtcNow.AddHours(-hoursAgo)
        });
    }

    private async Task<TaskDto> CreateOverdueTaskAsync() =>
        await _service.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Call Ivan",
            ContactId = _contact.Id,
            CompanyId = _company.Id,
            DueAt = DateTimeOffset.UtcNow.AddDays(-2)
        });

    private Deal CreateStaleDeal(int daysAgo)
    {
        var deal = new Deal
        {
            Title = "Stale deal",
            ContactId = _contact.Id,
            CompanyId = _company.Id,
            PipelineId = _pipeline.Id,
            StageId = _newStage.Id,
            Amount = 50000,
            Currency = "RUB",
            Status = DealStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-daysAgo)
        };

        // Synchronous SaveChanges bypasses the auditing timestamp stamping,
        // which lets the test keep the artificially old UpdatedAt value.
        _db.Add(deal);
        _db.SaveChanges();

        return deal;
    }
}
