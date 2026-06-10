using System.Text.Json;
using Crm.Application.DTOs;
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
        _service = new CrmService(_db);

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
    public async Task ApproveAgentAction_marks_action_approved()
    {
        var action = await CreateProposedActionAsync();

        var approved = await _service.ApproveAgentActionAsync(action.Id, new AgentActionDecisionRequest
        {
            UserId = Guid.NewGuid()
        });

        approved.Status.Should().Be(AgentActionStatus.Approved);
        _db.ApprovalRequests.Single(x => x.EntityId == action.Id).Status.Should().Be(ApprovalStatus.Approved);
    }

    [Fact]
    public async Task RejectAgentAction_marks_action_rejected()
    {
        var action = await CreateProposedActionAsync();

        var rejected = await _service.RejectAgentActionAsync(action.Id, new AgentActionDecisionRequest
        {
            UserId = Guid.NewGuid()
        });

        rejected.Status.Should().Be(AgentActionStatus.Rejected);
        _db.ApprovalRequests.Single(x => x.EntityId == action.Id).Status.Should().Be(ApprovalStatus.Rejected);
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
