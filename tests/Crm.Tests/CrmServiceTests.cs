using System.Text.Json;
using Crm.Application.DTOs;
using Crm.Application.Exceptions;
using Crm.Application.Services;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Crm.Tests;

public sealed class CrmServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CrmDbContext _db;
    private readonly TestCurrentActor _actor = new();
    private readonly CrmService _service;
    private readonly Pipeline _pipeline;
    private readonly PipelineStage _newStage;
    private readonly PipelineStage _contactedStage;
    private readonly Company _company;
    private readonly Contact _contact;
    private readonly Agent _agent;

    public CrmServiceTests()
    {
        _connection.Open();
        _db = new CrmDbContext(new DbContextOptionsBuilder<CrmDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _actor.UserId = Guid.NewGuid();
        _service = new CrmService(_db, _actor);

        _pipeline = new Pipeline { Name = "Default", IsDefault = true };
        _newStage = new PipelineStage { PipelineId = _pipeline.Id, Name = "New", SortOrder = 10, Probability = 10 };
        _contactedStage = new PipelineStage { PipelineId = _pipeline.Id, Name = "Contacted", SortOrder = 20, Probability = 25 };
        _company = new Company { Name = "Acme" };
        _contact = new Contact { FirstName = "Ivan", LastName = "Petrov", CompanyId = _company.Id };
        _agent = new Agent { Name = "Sales Assistant" };

        _db.AddRange(_pipeline, _newStage, _contactedStage, _company, _contact, _agent);
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateContact_creates_contact()
    {
        var contact = await _service.CreateContactAsync(new CreateContactRequest
        {
            FirstName = "Anna",
            LastName = "Smirnova",
            Email = "anna@example.com",
            CompanyId = _company.Id
        });

        contact.Id.Should().NotBeEmpty();
        contact.CompanyName.Should().Be("Acme");
        _db.Contacts.Count(x => !x.IsDeleted).Should().Be(2);
    }

    [Fact]
    public async Task CreateCompany_creates_company()
    {
        var company = await _service.CreateCompanyAsync(new CreateCompanyRequest
        {
            Name = "Demo Company",
            Website = "https://example.com"
        });

        company.Name.Should().Be("Demo Company");
        _db.Companies.Count(x => !x.IsDeleted).Should().Be(2);
    }

    [Fact]
    public async Task CreateDeal_creates_open_deal()
    {
        var deal = await CreateDealAsync();

        deal.Title.Should().Be("New opportunity");
        deal.Status.Should().Be(DealStatus.Open);
        deal.Probability.Should().Be(_newStage.Probability);
    }

    [Fact]
    public async Task MoveDealStage_changes_stage_and_probability()
    {
        var deal = await CreateDealAsync();

        var moved = await _service.MoveDealStageAsync(deal.Id, new MoveDealStageRequest
        {
            StageId = _contactedStage.Id
        });

        moved.StageId.Should().Be(_contactedStage.Id);
        moved.Probability.Should().Be(_contactedStage.Probability);
    }

    [Fact]
    public async Task CreateTask_creates_task()
    {
        var task = await CreateTaskAsync();

        task.Title.Should().Be("Call Ivan");
        task.Status.Should().Be(CrmTaskStatus.New);
    }

    [Fact]
    public async Task CompleteTask_marks_task_completed()
    {
        var task = await CreateTaskAsync();

        var completed = await _service.CompleteTaskAsync(task.Id);

        completed.Status.Should().Be(CrmTaskStatus.Completed);
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAgentAction_creates_approval_request_when_required()
    {
        var action = await _service.CreateAgentActionAsync(new CreateAgentActionRequest
        {
            AgentId = _agent.Id,
            ActionType = AgentActionType.AddNote,
            TargetEntityType = CrmEntityType.Deal,
            TargetEntityId = Guid.NewGuid(),
            RequiresApproval = true,
            InputJson = JsonSerializer.Serialize(new CreateActivityRequest { Title = "Suggested note" })
        });

        action.Status.Should().Be(AgentActionStatus.Proposed);
        _db.ApprovalRequests.Count(x => x.EntityId == action.Id).Should().Be(1);
    }

    [Fact]
    public async Task ApproveAgentAction_marks_action_approved_with_claims_user()
    {
        var action = await CreateProposedActionAsync();

        var approved = await _service.ApproveAgentActionAsync(action.Id);

        approved.Status.Should().Be(AgentActionStatus.Approved);
        approved.ApprovedByUserId.Should().Be(_actor.UserId);
        var approval = _db.ApprovalRequests.Single(x => x.EntityId == action.Id);
        approval.Status.Should().Be(ApprovalStatus.Approved);
        approval.ApprovedByUserId.Should().Be(_actor.UserId);
    }

    [Fact]
    public async Task RejectAgentAction_marks_action_rejected_with_claims_user()
    {
        var action = await CreateProposedActionAsync();

        var rejected = await _service.RejectAgentActionAsync(action.Id);

        rejected.Status.Should().Be(AgentActionStatus.Rejected);
        rejected.RejectedByUserId.Should().Be(_actor.UserId);
        _db.ApprovalRequests.Single(x => x.EntityId == action.Id).Status.Should().Be(ApprovalStatus.Rejected);
    }

    [Fact]
    public async Task ApproveAgentAction_without_user_identity_is_forbidden()
    {
        var action = await CreateProposedActionAsync();
        _actor.UserId = null;
        _actor.AgentId = _agent.Id;

        var act = () => _service.ApproveAgentActionAsync(action.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
        _db.AgentActions.Single(x => x.Id == action.Id).Status.Should().Be(AgentActionStatus.Proposed);
    }

    [Fact]
    public async Task CreateAgentAction_as_agent_uses_authenticated_agent_identity()
    {
        _actor.UserId = null;
        _actor.AgentId = _agent.Id;

        var action = await _service.CreateAgentActionAsync(new CreateAgentActionRequest
        {
            AgentId = null,
            ActionType = AgentActionType.AddNote,
            RequiresApproval = true,
            InputJson = JsonSerializer.Serialize(new CreateActivityRequest { Title = "Agent proposed note" })
        });

        action.AgentId.Should().Be(_agent.Id);
        action.Status.Should().Be(AgentActionStatus.Proposed);
    }

    [Fact]
    public async Task CreateAgentAction_as_agent_cannot_spoof_another_agent()
    {
        var otherAgent = new Agent { Name = "Other Agent" };
        _db.Add(otherAgent);
        await _db.SaveChangesAsync();

        _actor.UserId = null;
        _actor.AgentId = _agent.Id;

        var act = () => _service.CreateAgentActionAsync(new CreateAgentActionRequest
        {
            AgentId = otherAgent.Id,
            ActionType = AgentActionType.AddNote,
            RequiresApproval = true,
            InputJson = JsonSerializer.Serialize(new CreateActivityRequest { Title = "Spoofed note" })
        });

        await act.Should().ThrowAsync<ForbiddenException>();
        _db.AgentActions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAgentAction_without_agent_identity_requires_body_agent_id()
    {
        var act = () => _service.CreateAgentActionAsync(new CreateAgentActionRequest
        {
            AgentId = null,
            ActionType = AgentActionType.AddNote,
            RequiresApproval = true,
            InputJson = "{}"
        });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ExecuteAgentAction_add_note_creates_activity()
    {
        var action = await _service.CreateAgentActionAsync(new CreateAgentActionRequest
        {
            AgentId = _agent.Id,
            ActionType = AgentActionType.AddNote,
            RequiresApproval = false,
            InputJson = JsonSerializer.Serialize(new CreateActivityRequest
            {
                Type = ActivityType.Note,
                Title = "Agent note",
                DealId = null
            }),
            ReasoningSummary = "Useful follow-up context."
        });

        action.Status.Should().Be(AgentActionStatus.Executed);
        action.AfterJson.Should().NotBeNull();
        _db.Activities.Single(x => x.Title == "Agent note").Type.Should().Be(ActivityType.Note);
    }

    [Fact]
    public async Task GetConversations_groups_messages_by_contact()
    {
        await _service.CreateMessageAsync(new CreateMessageRequest
        {
            ContactId = _contact.Id,
            Direction = MessageDirection.Outgoing,
            Channel = MessageChannel.Email,
            Text = "Добрый день, Иван."
        });
        await _service.CreateMessageAsync(new CreateMessageRequest
        {
            ContactId = _contact.Id,
            Direction = MessageDirection.Incoming,
            Channel = MessageChannel.Email,
            Text = "Спасибо, получил."
        });

        var conversations = await _service.GetConversationsAsync();

        conversations.Should().ContainSingle();
        conversations[0].ContactId.Should().Be(_contact.Id);
        conversations[0].MessageCount.Should().Be(2);
        conversations[0].Status.Should().Be(ConversationStatus.Unread);
    }

    [Fact]
    public async Task GetWorkQueue_returns_overdue_tasks_and_recent_activities()
    {
        await _service.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Overdue follow-up",
            ContactId = _contact.Id,
            CompanyId = _company.Id,
            DueAt = DateTimeOffset.UtcNow.AddDays(-1),
            Priority = CrmTaskPriority.Urgent
        });
        await _service.CreateActivityAsync(new CreateActivityRequest
        {
            Type = ActivityType.Meeting,
            Title = "Intro meeting",
            ContactId = _contact.Id,
            CompanyId = _company.Id
        });

        var queue = await _service.GetWorkQueueAsync();

        queue.Should().Contain(x => x.Type == WorkQueueItemType.Task && x.Bucket == WorkQueueBucket.Overdue);
        queue.Should().Contain(x => x.Type == WorkQueueItemType.Activity && x.ActivityType == ActivityType.Meeting);
    }

    [Fact]
    public async Task GetContactDuplicates_detects_exact_email_match()
    {
        await _service.CreateContactAsync(new CreateContactRequest
        {
            FirstName = "Ivan",
            LastName = "Petrov Duplicate",
            Email = "ivan@example.com",
            CompanyId = _company.Id
        });
        await _service.UpdateContactAsync(_contact.Id, new UpdateContactRequest
        {
            FirstName = _contact.FirstName,
            LastName = _contact.LastName,
            Email = "IVAN@example.com",
            CompanyId = _company.Id
        });

        var duplicates = await _service.GetContactDuplicatesAsync();

        duplicates.Should().ContainSingle(x => x.Confidence == 100 && x.Reason == "Same email");
    }

    [Fact]
    public async Task MergeContacts_moves_related_records_and_soft_deletes_duplicate()
    {
        var duplicate = await _service.CreateContactAsync(new CreateContactRequest
        {
            FirstName = "Ivan",
            LastName = "Petrov",
            Email = "ivan@example.com",
            CompanyId = _company.Id
        });
        var task = await _service.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Duplicate task",
            ContactId = duplicate.Id,
            CompanyId = _company.Id
        });
        var message = await _service.CreateMessageAsync(new CreateMessageRequest
        {
            ContactId = duplicate.Id,
            Text = "Linked to duplicate"
        });

        var merged = await _service.MergeContactsAsync(new MergeContactsRequest
        {
            PrimaryContactId = _contact.Id,
            DuplicateContactId = duplicate.Id
        });

        merged.Id.Should().Be(_contact.Id);
        _db.Contacts.Single(x => x.Id == duplicate.Id).IsDeleted.Should().BeTrue();
        _db.Tasks.Single(x => x.Id == task.Id).ContactId.Should().Be(_contact.Id);
        _db.Messages.Single(x => x.Id == message.Id).ContactId.Should().Be(_contact.Id);
    }

    [Fact]
    public async Task BulkCreateContactTasks_creates_task_for_each_contact()
    {
        var second = await _service.CreateContactAsync(new CreateContactRequest
        {
            FirstName = "Anna",
            LastName = "Smirnova",
            CompanyId = _company.Id
        });

        var result = await _service.BulkCreateContactTasksAsync(new BulkCreateTaskRequest
        {
            ContactIds = [_contact.Id, second.Id],
            Title = "Bulk follow-up",
            Priority = CrmTaskPriority.High
        });

        result.Requested.Should().Be(2);
        result.Succeeded.Should().Be(2);
        _db.Tasks.Count(x => x.Title == "Bulk follow-up").Should().Be(2);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<DealDto> CreateDealAsync() =>
        await _service.CreateDealAsync(new CreateDealRequest
        {
            Title = "New opportunity",
            ContactId = _contact.Id,
            CompanyId = _company.Id,
            PipelineId = _pipeline.Id,
            StageId = _newStage.Id,
            Amount = 120000,
            Currency = "RUB"
        });

    private async Task<TaskDto> CreateTaskAsync() =>
        await _service.CreateTaskAsync(new CreateTaskRequest
        {
            Title = "Call Ivan",
            ContactId = _contact.Id,
            CompanyId = _company.Id,
            Priority = CrmTaskPriority.High
        });

    private async Task<AgentActionDto> CreateProposedActionAsync() =>
        await _service.CreateAgentActionAsync(new CreateAgentActionRequest
        {
            AgentId = _agent.Id,
            ActionType = AgentActionType.AddNote,
            TargetEntityType = CrmEntityType.Deal,
            TargetEntityId = Guid.NewGuid(),
            RequiresApproval = true,
            InputJson = JsonSerializer.Serialize(new CreateActivityRequest
            {
                Type = ActivityType.Note,
                Title = "Proposed note"
            })
        });
}
