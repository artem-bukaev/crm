using System.Text.Json;
using Crm.Application.DTOs;
using Crm.Application.Exceptions;
using Crm.Application.Interfaces;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;

namespace Crm.Application.Services;

public sealed class CrmService(ICrmDataStore db) : ICrmService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var openDeals = Active<Deal>().Where(x => x.Status == DealStatus.Open).ToList();

        return Task.FromResult(new DashboardSummaryDto(
            Active<Contact>().Count(),
            Active<Company>().Count(),
            openDeals.Count,
            openDeals.Sum(x => x.Amount),
            Active<CrmTask>().Count(x => x.Status == CrmTaskStatus.New || x.Status == CrmTaskStatus.InProgress),
            Active<AgentAction>().Count(x => x.Status == AgentActionStatus.Proposed || x.Status == AgentActionStatus.Approved),
            Active<ApprovalRequest>().Count(x => x.Status == ApprovalStatus.Pending)));
    }

    public Task<IReadOnlyList<ContactDto>> GetContactsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ContactDto>>(Active<Contact>()
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Select(MapContact)
            .ToList());

    public Task<ContactDto> GetContactAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapContact(GetRequired<Contact>(id, "Contact")));

    public async Task<ContactDto> CreateContactAsync(CreateContactRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCompanyExists(request.CompanyId);

        var contact = new Contact();
        ApplyContact(contact, request);
        db.Add(contact);
        await db.SaveChangesAsync(cancellationToken);

        return MapContact(contact);
    }

    public async Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCompanyExists(request.CompanyId);

        var contact = GetRequired<Contact>(id, "Contact");
        ApplyContact(contact, request);
        await db.SaveChangesAsync(cancellationToken);

        return MapContact(contact);
    }

    public async Task DeleteContactAsync(Guid id, CancellationToken cancellationToken = default)
    {
        GetRequired<Contact>(id, "Contact").IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ContactDuplicateCandidateDto>> GetContactDuplicatesAsync(CancellationToken cancellationToken = default)
    {
        var contacts = Active<Contact>()
            .ToList()
            .OrderBy(x => x.CreatedAt)
            .ToList();

        var candidates = new List<ContactDuplicateCandidateDto>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < contacts.Count; i++)
        {
            for (var j = i + 1; j < contacts.Count; j++)
            {
                var primary = contacts[i];
                var duplicate = contacts[j];
                var match = ResolveDuplicateMatch(primary, duplicate);
                if (match is null)
                {
                    continue;
                }

                var key = $"{primary.Id:N}:{duplicate.Id:N}";
                if (!seen.Add(key))
                {
                    continue;
                }

                candidates.Add(new ContactDuplicateCandidateDto(
                    key,
                    MapContact(primary),
                    MapContact(duplicate),
                    match.Value.Confidence,
                    match.Value.Reason));
            }
        }

        return Task.FromResult<IReadOnlyList<ContactDuplicateCandidateDto>>(candidates
            .OrderByDescending(x => x.Confidence)
            .ThenBy(x => x.PrimaryContact.FullName)
            .ToList());
    }

    public async Task<ContactDto> MergeContactsAsync(MergeContactsRequest request, CancellationToken cancellationToken = default)
    {
        var primary = GetRequired<Contact>(request.PrimaryContactId, "Primary contact");
        var duplicate = GetRequired<Contact>(request.DuplicateContactId, "Duplicate contact");

        primary.FirstName ??= duplicate.FirstName;
        primary.LastName ??= duplicate.LastName;
        primary.MiddleName ??= duplicate.MiddleName;
        primary.Phone ??= duplicate.Phone;
        primary.Email ??= duplicate.Email;
        primary.TelegramUsername ??= duplicate.TelegramUsername;
        primary.CompanyId ??= duplicate.CompanyId;
        primary.Position ??= duplicate.Position;
        primary.Source ??= duplicate.Source;
        if (primary.Status is ContactStatus.Lead && duplicate.Status is not ContactStatus.Lead)
        {
            primary.Status = duplicate.Status;
        }

        foreach (var deal in Active<Deal>().Where(x => x.ContactId == duplicate.Id))
        {
            deal.ContactId = primary.Id;
            deal.CompanyId ??= primary.CompanyId;
        }

        foreach (var task in Active<CrmTask>().Where(x => x.ContactId == duplicate.Id))
        {
            task.ContactId = primary.Id;
            task.CompanyId ??= primary.CompanyId;
        }

        foreach (var activity in Active<Activity>().Where(x => x.ContactId == duplicate.Id))
        {
            activity.ContactId = primary.Id;
            activity.CompanyId ??= primary.CompanyId;
        }

        foreach (var message in Active<Message>().Where(x => x.ContactId == duplicate.Id))
        {
            message.ContactId = primary.Id;
        }

        duplicate.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);

        return MapContact(primary);
    }

    public Task<BulkOperationResultDto> BulkCreateContactTasksAsync(BulkCreateTaskRequest request, CancellationToken cancellationToken = default) =>
        BulkCreateTasksAsync(
            request.ContactIds.Distinct().ToList(),
            CrmEntityType.Contact,
            request,
            id =>
            {
                var contact = Active<Contact>().FirstOrDefault(x => x.Id == id);
                if (contact is null)
                {
                    return (false, "Contact was not found.", null);
                }

                return (true, null, new CrmTask
                {
                    Title = request.Title.Trim(),
                    Description = Trim(request.Description),
                    DueAt = request.DueAt,
                    Status = CrmTaskStatus.New,
                    Priority = request.Priority,
                    ContactId = contact.Id,
                    CompanyId = contact.CompanyId,
                    ResponsibleUserId = request.ResponsibleUserId
                });
            },
            cancellationToken);

    public Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CompanyDto>>(Active<Company>()
            .OrderBy(x => x.Name)
            .ToList()
            .Select(MapCompany)
            .ToList());

    public Task<CompanyDto> GetCompanyAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapCompany(GetRequired<Company>(id, "Company")));

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = new Company();
        ApplyCompany(company, request);
        db.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        return MapCompany(company);
    }

    public async Task<CompanyDto> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = GetRequired<Company>(id, "Company");
        ApplyCompany(company, request);
        await db.SaveChangesAsync(cancellationToken);

        return MapCompany(company);
    }

    public async Task DeleteCompanyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        GetRequired<Company>(id, "Company").IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PipelineDto>> GetPipelinesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PipelineDto>>(Active<Pipeline>()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToList()
            .Select(MapPipeline)
            .ToList());

    public Task<PipelineDto> GetPipelineAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapPipeline(GetRequired<Pipeline>(id, "Pipeline")));

    public async Task<PipelineDto> CreatePipelineAsync(CreatePipelineRequest request, CancellationToken cancellationToken = default)
    {
        if (request.IsDefault)
        {
            foreach (var pipeline in Active<Pipeline>().Where(x => x.IsDefault))
            {
                pipeline.IsDefault = false;
            }
        }

        var entity = new Pipeline();
        ApplyPipeline(entity, request);
        db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return MapPipeline(entity);
    }

    public async Task<PipelineDto> UpdatePipelineAsync(Guid id, UpdatePipelineRequest request, CancellationToken cancellationToken = default)
    {
        var entity = GetRequired<Pipeline>(id, "Pipeline");
        if (request.IsDefault)
        {
            foreach (var pipeline in Active<Pipeline>().Where(x => x.Id != id && x.IsDefault))
            {
                pipeline.IsDefault = false;
            }
        }

        ApplyPipeline(entity, request);
        await db.SaveChangesAsync(cancellationToken);

        return MapPipeline(entity);
    }

    public async Task DeletePipelineAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (Active<Deal>().Any(x => x.PipelineId == id))
        {
            throw new ConflictException("Pipeline has active deals.");
        }

        GetRequired<Pipeline>(id, "Pipeline").IsDeleted = true;
        foreach (var stage in Active<PipelineStage>().Where(x => x.PipelineId == id))
        {
            stage.IsDeleted = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PipelineStageDto>> GetPipelineStagesAsync(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        GetRequired<Pipeline>(pipelineId, "Pipeline");
        return Task.FromResult<IReadOnlyList<PipelineStageDto>>(Active<PipelineStage>()
            .Where(x => x.PipelineId == pipelineId)
            .OrderBy(x => x.SortOrder)
            .ToList()
            .Select(MapStage)
            .ToList());
    }

    public async Task<PipelineStageDto> CreatePipelineStageAsync(Guid pipelineId, CreatePipelineStageRequest request, CancellationToken cancellationToken = default)
    {
        GetRequired<Pipeline>(pipelineId, "Pipeline");

        var stage = new PipelineStage { PipelineId = pipelineId };
        ApplyStage(stage, request);
        db.Add(stage);
        await db.SaveChangesAsync(cancellationToken);

        return MapStage(stage);
    }

    public async Task<PipelineStageDto> UpdatePipelineStageAsync(Guid id, UpdatePipelineStageRequest request, CancellationToken cancellationToken = default)
    {
        var stage = GetRequired<PipelineStage>(id, "Pipeline stage");
        ApplyStage(stage, request);
        await db.SaveChangesAsync(cancellationToken);

        return MapStage(stage);
    }

    public async Task DeletePipelineStageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (Active<Deal>().Any(x => x.StageId == id))
        {
            throw new ConflictException("Pipeline stage has active deals.");
        }

        GetRequired<PipelineStage>(id, "Pipeline stage").IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DealDto>> GetDealsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DealDto>>(Active<Deal>()
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Select(MapDeal)
            .ToList());

    public Task<DealDto> GetDealAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapDeal(GetRequired<Deal>(id, "Deal")));

    public async Task<DealDto> CreateDealAsync(CreateDealRequest request, CancellationToken cancellationToken = default)
    {
        EnsureDealReferences(request.ContactId, request.CompanyId, request.PipelineId, request.StageId);

        var stage = GetRequired<PipelineStage>(request.StageId, "Pipeline stage");
        var deal = new Deal();
        ApplyDeal(deal, request);
        deal.Probability = request.Probability ?? stage.Probability;
        ApplyDealStageFlags(deal, stage);
        db.Add(deal);
        await db.SaveChangesAsync(cancellationToken);

        return MapDeal(deal);
    }

    public async Task<DealDto> UpdateDealAsync(Guid id, UpdateDealRequest request, CancellationToken cancellationToken = default)
    {
        EnsureDealReferences(request.ContactId, request.CompanyId, request.PipelineId, request.StageId);

        var stage = GetRequired<PipelineStage>(request.StageId, "Pipeline stage");
        var deal = GetRequired<Deal>(id, "Deal");
        ApplyDeal(deal, request);
        deal.Probability = request.Probability ?? stage.Probability;
        ApplyDealStageFlags(deal, stage);
        await db.SaveChangesAsync(cancellationToken);

        return MapDeal(deal);
    }

    public async Task<DealDto> MoveDealStageAsync(Guid id, MoveDealStageRequest request, CancellationToken cancellationToken = default)
    {
        var deal = GetRequired<Deal>(id, "Deal");
        var stage = GetRequired<PipelineStage>(request.StageId, "Pipeline stage");

        if (stage.PipelineId != deal.PipelineId)
        {
            throw new ConflictException("Pipeline stage does not belong to deal pipeline.");
        }

        deal.StageId = stage.Id;
        deal.Probability = stage.Probability;
        ApplyDealStageFlags(deal, stage);

        db.Add(new Activity
        {
            Type = ActivityType.SystemEvent,
            Title = "Deal stage changed",
            Description = $"Deal moved to {stage.Name}.",
            DealId = deal.Id
        });

        await db.SaveChangesAsync(cancellationToken);
        return MapDeal(deal);
    }

    public async Task<DealDto> MarkDealWonAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = GetRequired<Deal>(id, "Deal");
        var wonStage = Active<PipelineStage>().FirstOrDefault(x => x.PipelineId == deal.PipelineId && x.IsWon);
        if (wonStage is not null)
        {
            deal.StageId = wonStage.Id;
            deal.Probability = wonStage.Probability;
        }

        deal.Status = DealStatus.Won;
        deal.ClosedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapDeal(deal);
    }

    public async Task<DealDto> MarkDealLostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deal = GetRequired<Deal>(id, "Deal");
        var lostStage = Active<PipelineStage>().FirstOrDefault(x => x.PipelineId == deal.PipelineId && x.IsLost);
        if (lostStage is not null)
        {
            deal.StageId = lostStage.Id;
            deal.Probability = lostStage.Probability;
        }

        deal.Status = DealStatus.Lost;
        deal.ClosedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapDeal(deal);
    }

    public async Task DeleteDealAsync(Guid id, CancellationToken cancellationToken = default)
    {
        GetRequired<Deal>(id, "Deal").IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<BulkOperationResultDto> BulkCreateDealTasksAsync(BulkCreateTaskRequest request, CancellationToken cancellationToken = default) =>
        BulkCreateTasksAsync(
            request.DealIds.Distinct().ToList(),
            CrmEntityType.Deal,
            request,
            id =>
            {
                var deal = Active<Deal>().FirstOrDefault(x => x.Id == id);
                if (deal is null)
                {
                    return (false, "Deal was not found.", null);
                }

                return (true, null, new CrmTask
                {
                    Title = request.Title.Trim(),
                    Description = Trim(request.Description),
                    DueAt = request.DueAt,
                    Status = CrmTaskStatus.New,
                    Priority = request.Priority,
                    ContactId = deal.ContactId,
                    CompanyId = deal.CompanyId,
                    DealId = deal.Id,
                    ResponsibleUserId = request.ResponsibleUserId
                });
            },
            cancellationToken);

    public Task<IReadOnlyList<TaskDto>> GetTasksAsync(CrmTaskStatus? status, CancellationToken cancellationToken = default)
    {
        var query = Active<CrmTask>();
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        return Task.FromResult<IReadOnlyList<TaskDto>>(query
            .OrderBy(x => x.Status == CrmTaskStatus.Completed)
            .ThenBy(x => x.DueAt ?? DateTimeOffset.MaxValue)
            .ToList()
            .Select(MapTask)
            .ToList());
    }

    public Task<TaskDto> GetTaskAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapTask(GetRequired<CrmTask>(id, "Task")));

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTaskReferences(request.ContactId, request.CompanyId, request.DealId);

        var task = new CrmTask();
        ApplyTask(task, request);
        db.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return MapTask(task);
    }

    public async Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTaskReferences(request.ContactId, request.CompanyId, request.DealId);

        var task = GetRequired<CrmTask>(id, "Task");
        ApplyTask(task, request);
        if (task.Status != CrmTaskStatus.Completed)
        {
            task.CompletedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapTask(task);
    }

    public async Task<TaskDto> CompleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = GetRequired<CrmTask>(id, "Task");
        task.Status = CrmTaskStatus.Completed;
        task.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapTask(task);
    }

    public async Task<TaskDto> CancelTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = GetRequired<CrmTask>(id, "Task");
        task.Status = CrmTaskStatus.Canceled;
        await db.SaveChangesAsync(cancellationToken);

        return MapTask(task);
    }

    public async Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default)
    {
        GetRequired<CrmTask>(id, "Task").IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(CancellationToken cancellationToken = default) =>
        GetTimelineAsync(null, null, null, cancellationToken);

    public Task<IReadOnlyList<ActivityDto>> GetTimelineAsync(Guid? contactId, Guid? companyId, Guid? dealId, CancellationToken cancellationToken = default)
    {
        var query = Active<Activity>();

        if (contactId is not null)
        {
            query = query.Where(x => x.ContactId == contactId);
        }

        if (companyId is not null)
        {
            query = query.Where(x => x.CompanyId == companyId);
        }

        if (dealId is not null)
        {
            query = query.Where(x => x.DealId == dealId);
        }

        return Task.FromResult<IReadOnlyList<ActivityDto>>(query
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Select(MapActivity)
            .ToList());
    }

    public Task<IReadOnlyList<WorkQueueItemDto>> GetWorkQueueAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var recentActivityCutoff = now.AddDays(-30);
        var items = new List<WorkQueueItemDto>();

        items.AddRange(Active<CrmTask>()
            .Where(x => x.Status == CrmTaskStatus.New || x.Status == CrmTaskStatus.InProgress)
            .ToList()
            .Select(x => MapWorkQueueTask(x, now)));

        items.AddRange(Active<Activity>()
            .ToList()
            .Where(x => x.CreatedAt >= recentActivityCutoff && x.Type != ActivityType.Note)
            .Select(x => MapWorkQueueActivity(x, now)));

        return Task.FromResult<IReadOnlyList<WorkQueueItemDto>>(items
            .OrderBy(x => x.Bucket)
            .ThenBy(x => x.SortAt)
            .ToList());
    }

    public async Task<ActivityDto> CreateActivityAsync(CreateActivityRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTaskReferences(request.ContactId, request.CompanyId, request.DealId);

        var activity = new Activity();
        ApplyActivity(activity, request);
        db.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        return MapActivity(activity);
    }

    public Task<IReadOnlyList<MessageDto>> GetMessagesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MessageDto>>(Active<Message>()
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Select(MapMessage)
            .ToList());

    public Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        var conversations = Active<Message>()
            .ToList()
            .GroupBy(GetConversationId)
            .Select(MapConversation)
            .OrderByDescending(x => x.LastMessageAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ConversationDto>>(conversations);
    }

    public Task<MessageDto> GetMessageAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapMessage(GetRequired<Message>(id, "Message")));

    public async Task<MessageDto> CreateMessageAsync(CreateMessageRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTaskReferences(request.ContactId, null, request.DealId);

        var message = new Message();
        ApplyMessage(message, request);
        db.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        return MapMessage(message);
    }

    public Task<IReadOnlyList<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AgentDto>>(Active<Agent>()
            .OrderBy(x => x.Name)
            .ToList()
            .Select(MapAgent)
            .ToList());

    public Task<AgentDto> GetAgentAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapAgent(GetRequired<Agent>(id, "Agent")));

    public async Task<AgentDto> CreateAgentAsync(CreateAgentRequest request, CancellationToken cancellationToken = default)
    {
        var agent = new Agent();
        ApplyAgent(agent, request);
        db.Add(agent);
        await db.SaveChangesAsync(cancellationToken);

        return MapAgent(agent);
    }

    public async Task<AgentDto> UpdateAgentAsync(Guid id, UpdateAgentRequest request, CancellationToken cancellationToken = default)
    {
        var agent = GetRequired<Agent>(id, "Agent");
        ApplyAgent(agent, request);
        await db.SaveChangesAsync(cancellationToken);

        return MapAgent(agent);
    }

    public Task<IReadOnlyList<AgentActionDto>> GetAgentActionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AgentActionDto>>(Active<AgentAction>()
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Select(MapAgentAction)
            .ToList());

    public Task<AgentActionDto> GetAgentActionAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(MapAgentAction(GetRequired<AgentAction>(id, "Agent action")));

    public async Task<AgentActionDto> CreateAgentActionAsync(CreateAgentActionRequest request, CancellationToken cancellationToken = default)
    {
        GetRequired<Agent>(request.AgentId, "Agent");

        var action = new AgentAction
        {
            AgentId = request.AgentId,
            ActionType = request.ActionType,
            Status = request.RequiresApproval ? AgentActionStatus.Proposed : AgentActionStatus.Approved,
            TargetEntityType = request.TargetEntityType,
            TargetEntityId = request.TargetEntityId,
            InputJson = request.InputJson,
            ReasoningSummary = request.ReasoningSummary,
            RequiresApproval = request.RequiresApproval
        };

        db.Add(action);
        if (request.RequiresApproval)
        {
            db.Add(new ApprovalRequest
            {
                EntityType = CrmEntityType.AgentAction,
                EntityId = action.Id,
                Title = $"Approve {action.ActionType}",
                Description = action.ReasoningSummary,
                RequestedByAgentId = action.AgentId
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return request.RequiresApproval
            ? MapAgentAction(action)
            : await ExecuteAgentActionAsync(action.Id, cancellationToken);
    }

    public async Task<AgentActionDto> ApproveAgentActionAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var action = GetRequired<AgentAction>(id, "Agent action");
        if (action.Status is AgentActionStatus.Executed or AgentActionStatus.Rejected or AgentActionStatus.Canceled)
        {
            throw new ConflictException($"Agent action cannot be approved from {action.Status} state.");
        }

        action.Status = AgentActionStatus.Approved;
        action.ApprovedByUserId = request.UserId;
        action.ApprovedAt = DateTimeOffset.UtcNow;

        var approval = Active<ApprovalRequest>().FirstOrDefault(x =>
            x.EntityType == CrmEntityType.AgentAction &&
            x.EntityId == action.Id &&
            x.Status == ApprovalStatus.Pending);
        if (approval is not null)
        {
            approval.Status = ApprovalStatus.Approved;
            approval.ApprovedByUserId = request.UserId;
            approval.ApprovedAt = action.ApprovedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapAgentAction(action);
    }

    public async Task<AgentActionDto> RejectAgentActionAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var action = GetRequired<AgentAction>(id, "Agent action");
        if (action.Status is AgentActionStatus.Executed or AgentActionStatus.Rejected or AgentActionStatus.Canceled)
        {
            throw new ConflictException($"Agent action cannot be rejected from {action.Status} state.");
        }

        action.Status = AgentActionStatus.Rejected;
        action.RejectedByUserId = request.UserId;
        action.RejectedAt = DateTimeOffset.UtcNow;

        var approval = Active<ApprovalRequest>().FirstOrDefault(x =>
            x.EntityType == CrmEntityType.AgentAction &&
            x.EntityId == action.Id &&
            x.Status == ApprovalStatus.Pending);
        if (approval is not null)
        {
            approval.Status = ApprovalStatus.Rejected;
            approval.RejectedByUserId = request.UserId;
            approval.RejectedAt = action.RejectedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapAgentAction(action);
    }

    public async Task<AgentActionDto> ExecuteAgentActionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var action = GetRequired<AgentAction>(id, "Agent action");

        if (action.Status == AgentActionStatus.Executed)
        {
            throw new ConflictException("Agent action has already been executed.");
        }

        if (action.RequiresApproval && action.Status != AgentActionStatus.Approved)
        {
            throw new ConflictException("Agent action must be approved before execution.");
        }

        try
        {
            action.BeforeJson = SnapshotTarget(action);

            switch (action.ActionType)
            {
                case AgentActionType.CreateContact:
                    var contact = await CreateContactAsync(ReadInput<CreateContactRequest>(action), cancellationToken);
                    action.TargetEntityType = CrmEntityType.Contact;
                    action.TargetEntityId = contact.Id;
                    action.AfterJson = Serialize(contact);
                    break;

                case AgentActionType.UpdateContact:
                    EnsureTarget(action, CrmEntityType.Contact);
                    action.AfterJson = Serialize(await UpdateContactAsync(action.TargetEntityId!.Value, ReadInput<UpdateContactRequest>(action), cancellationToken));
                    break;

                case AgentActionType.CreateDeal:
                    var deal = await CreateDealAsync(ReadInput<CreateDealRequest>(action), cancellationToken);
                    action.TargetEntityType = CrmEntityType.Deal;
                    action.TargetEntityId = deal.Id;
                    action.AfterJson = Serialize(deal);
                    break;

                case AgentActionType.UpdateDealStage:
                    EnsureTarget(action, CrmEntityType.Deal);
                    action.AfterJson = Serialize(await MoveDealStageAsync(action.TargetEntityId!.Value, ReadInput<MoveDealStageRequest>(action), cancellationToken));
                    break;

                case AgentActionType.CreateTask:
                    var task = await CreateTaskAsync(ReadInput<CreateTaskRequest>(action), cancellationToken);
                    action.TargetEntityType = CrmEntityType.Task;
                    action.TargetEntityId = task.Id;
                    action.AfterJson = Serialize(task);
                    break;

                case AgentActionType.CompleteTask:
                    EnsureTarget(action, CrmEntityType.Task);
                    action.AfterJson = Serialize(await CompleteTaskAsync(action.TargetEntityId!.Value, cancellationToken));
                    break;

                case AgentActionType.AddNote:
                    var note = await CreateActivityAsync(ReadInput<CreateActivityRequest>(action), cancellationToken);
                    action.TargetEntityType = CrmEntityType.Activity;
                    action.TargetEntityId = note.Id;
                    action.AfterJson = Serialize(note);
                    break;

                case AgentActionType.DraftMessage:
                case AgentActionType.SendMessage:
                    var message = await CreateMessageAsync(ReadInput<CreateMessageRequest>(action), cancellationToken);
                    action.TargetEntityType = CrmEntityType.Message;
                    action.TargetEntityId = message.Id;
                    action.AfterJson = Serialize(message);
                    break;

                case AgentActionType.RequestHumanApproval:
                    db.Add(new ApprovalRequest
                    {
                        EntityType = action.TargetEntityType ?? CrmEntityType.AgentAction,
                        EntityId = action.TargetEntityId ?? action.Id,
                        Title = "Human approval requested",
                        Description = action.ReasoningSummary,
                        RequestedByAgentId = action.AgentId
                    });
                    action.AfterJson = Serialize(new { approvalRequested = true });
                    break;

                default:
                    throw new ConflictException($"Unsupported action type {action.ActionType}.");
            }

            action.Status = AgentActionStatus.Executed;
            action.ErrorMessage = null;
            action.ExecutedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex) when (ex is not ConflictException { Code: "CONFLICT" })
        {
            action.Status = AgentActionStatus.Failed;
            action.ErrorMessage = ex.Message;
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapAgentAction(action);
    }

    public Task<IReadOnlyList<ApprovalRequestDto>> GetApprovalsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApprovalRequestDto>>(Active<ApprovalRequest>()
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Select(MapApproval)
            .ToList());

    public async Task<ApprovalRequestDto> ApproveRequestAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var approval = GetRequired<ApprovalRequest>(id, "Approval request");
        approval.Status = ApprovalStatus.Approved;
        approval.ApprovedByUserId = request.UserId;
        approval.ApprovedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapApproval(approval);
    }

    public async Task<ApprovalRequestDto> RejectRequestAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var approval = GetRequired<ApprovalRequest>(id, "Approval request");
        approval.Status = ApprovalStatus.Rejected;
        approval.RejectedByUserId = request.UserId;
        approval.RejectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return MapApproval(approval);
    }

    private IQueryable<TEntity> Active<TEntity>() where TEntity : Entity =>
        db.Query<TEntity>().Where(x => !x.IsDeleted);

    private TEntity GetRequired<TEntity>(Guid id, string name) where TEntity : Entity =>
        Active<TEntity>().FirstOrDefault(x => x.Id == id)
        ?? throw new NotFoundException($"{name} {id} was not found.");

    private async Task<BulkOperationResultDto> BulkCreateTasksAsync(
        IReadOnlyList<Guid> targetIds,
        CrmEntityType targetType,
        BulkCreateTaskRequest request,
        Func<Guid, (bool Succeeded, string? Message, CrmTask? Task)> createTask,
        CancellationToken cancellationToken)
    {
        var results = new List<BulkOperationItemResultDto>();
        var createdTasks = new List<CrmTask>();

        foreach (var targetId in targetIds)
        {
            var result = createTask(targetId);
            if (!result.Succeeded || result.Task is null)
            {
                results.Add(new BulkOperationItemResultDto(targetId, targetType, false, result.Message, null));
                continue;
            }

            db.Add(result.Task);
            createdTasks.Add(result.Task);
            results.Add(new BulkOperationItemResultDto(targetId, targetType, true, null, result.Task.Id));
        }

        if (createdTasks.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new BulkOperationResultDto(
            targetIds.Count,
            results.Count(x => x.Succeeded),
            results.Count(x => !x.Succeeded),
            results);
    }

    private static (int Confidence, string Reason)? ResolveDuplicateMatch(Contact a, Contact b)
    {
        if (!string.IsNullOrWhiteSpace(a.Email) &&
            !string.IsNullOrWhiteSpace(b.Email) &&
            string.Equals(a.Email.Trim(), b.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return (100, "Same email");
        }

        var aPhone = NormalizePhone(a.Phone);
        var bPhone = NormalizePhone(b.Phone);
        if (aPhone.Length >= 7 && aPhone == bPhone)
        {
            return (95, "Same phone");
        }

        var aName = NormalizeName(a);
        var bName = NormalizeName(b);
        if (a.CompanyId is not null &&
            a.CompanyId == b.CompanyId &&
            aName.Length > 0 &&
            aName == bName)
        {
            return (80, "Same name and company");
        }

        return null;
    }

    private static string NormalizePhone(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

    private static string NormalizeName(Contact contact) =>
        string.Join(" ", new[] { contact.LastName, contact.FirstName, contact.MiddleName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim().ToUpperInvariant()));

    private static string GetConversationId(Message message) =>
        ConversationHelper.GetConversationId(message);

    private ConversationDto MapConversation(IGrouping<string, Message> group)
    {
        var ordered = group
            .OrderBy(MessageTimestamp)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        var last = ordered[^1];
        var dealId = ordered.LastOrDefault(x => x.DealId is not null)?.DealId;
        var deal = dealId is null ? null : Active<Deal>().FirstOrDefault(x => x.Id == dealId);
        var contactId = ordered.LastOrDefault(x => x.ContactId is not null)?.ContactId ?? deal?.ContactId;
        var contact = contactId is null ? null : Active<Contact>().FirstOrDefault(x => x.Id == contactId);
        var companyId = contact?.CompanyId ?? deal?.CompanyId;
        var company = companyId is null ? null : Active<Company>().FirstOrDefault(x => x.Id == companyId);
        var openTaskCount = Active<CrmTask>()
            .ToList()
            .Count(x =>
                (x.Status == CrmTaskStatus.New || x.Status == CrmTaskStatus.InProgress) &&
                ((contactId is not null && x.ContactId == contactId) || (dealId is not null && x.DealId == dealId)));
        var lastMessageAt = MessageTimestamp(last);

        return new ConversationDto(
            group.Key,
            contactId,
            contact is null ? null : MapContact(contact).FullName,
            companyId,
            company?.Name,
            dealId,
            deal?.Title,
            last.Channel,
            last.Direction,
            last.Text,
            lastMessageAt,
            ResolveConversationStatus(last, lastMessageAt, openTaskCount),
            ordered.Count,
            openTaskCount,
            ordered.Select(MapMessage).ToList());
    }

    private static DateTimeOffset MessageTimestamp(Message message) =>
        ConversationHelper.GetTimestamp(message);

    private static ConversationStatus ResolveConversationStatus(Message last, DateTimeOffset lastMessageAt, int openTaskCount)
    {
        if (last.Direction == MessageDirection.Incoming)
        {
            return lastMessageAt >= DateTimeOffset.UtcNow.AddDays(-1)
                ? ConversationStatus.Unread
                : ConversationStatus.WaitingOnUs;
        }

        if (openTaskCount == 0 && lastMessageAt < DateTimeOffset.UtcNow.AddDays(-30))
        {
            return ConversationStatus.Closed;
        }

        return ConversationStatus.WaitingOnThem;
    }

    private void EnsureCompanyExists(Guid? companyId)
    {
        if (companyId is not null)
        {
            GetRequired<Company>(companyId.Value, "Company");
        }
    }

    private void EnsureDealReferences(Guid? contactId, Guid? companyId, Guid pipelineId, Guid stageId)
    {
        EnsureTaskReferences(contactId, companyId, null);

        var stage = GetRequired<PipelineStage>(stageId, "Pipeline stage");
        if (stage.PipelineId != pipelineId)
        {
            throw new ConflictException("Pipeline stage does not belong to pipeline.");
        }

        GetRequired<Pipeline>(pipelineId, "Pipeline");
    }

    private void EnsureTaskReferences(Guid? contactId, Guid? companyId, Guid? dealId)
    {
        if (contactId is not null)
        {
            GetRequired<Contact>(contactId.Value, "Contact");
        }

        if (companyId is not null)
        {
            GetRequired<Company>(companyId.Value, "Company");
        }

        if (dealId is not null)
        {
            GetRequired<Deal>(dealId.Value, "Deal");
        }
    }

    private static void ApplyContact(Contact entity, CreateContactRequest request)
    {
        entity.FirstName = Trim(request.FirstName);
        entity.LastName = Trim(request.LastName);
        entity.MiddleName = Trim(request.MiddleName);
        entity.Phone = Trim(request.Phone);
        entity.Email = Trim(request.Email);
        entity.TelegramUsername = Trim(request.TelegramUsername);
        entity.CompanyId = request.CompanyId;
        entity.Position = Trim(request.Position);
        entity.Source = Trim(request.Source);
        entity.Status = request.Status;
    }

    private static void ApplyCompany(Company entity, CreateCompanyRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.LegalName = Trim(request.LegalName);
        entity.Inn = Trim(request.Inn);
        entity.Website = Trim(request.Website);
        entity.Phone = Trim(request.Phone);
        entity.Email = Trim(request.Email);
        entity.Address = Trim(request.Address);
    }

    private static void ApplyPipeline(Pipeline entity, CreatePipelineRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Description = Trim(request.Description);
        entity.IsDefault = request.IsDefault;
    }

    private static void ApplyStage(PipelineStage entity, CreatePipelineStageRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.SortOrder = request.SortOrder;
        entity.Probability = request.Probability;
        entity.IsWon = request.IsWon;
        entity.IsLost = request.IsLost;
    }

    private static void ApplyDeal(Deal entity, CreateDealRequest request)
    {
        entity.Title = request.Title.Trim();
        entity.ContactId = request.ContactId;
        entity.CompanyId = request.CompanyId;
        entity.PipelineId = request.PipelineId;
        entity.StageId = request.StageId;
        entity.Amount = request.Amount;
        entity.Currency = request.Currency.Trim().ToUpperInvariant();
        entity.Status = request.Status;
        entity.Source = Trim(request.Source);
        entity.ResponsibleUserId = request.ResponsibleUserId;
        entity.ClosedAt = request.Status is DealStatus.Won or DealStatus.Lost or DealStatus.Canceled
            ? entity.ClosedAt ?? DateTimeOffset.UtcNow
            : null;
    }

    private static void ApplyDealStageFlags(Deal deal, PipelineStage stage)
    {
        if (stage.IsWon)
        {
            deal.Status = DealStatus.Won;
            deal.ClosedAt = DateTimeOffset.UtcNow;
        }
        else if (stage.IsLost)
        {
            deal.Status = DealStatus.Lost;
            deal.ClosedAt = DateTimeOffset.UtcNow;
        }
        else if (deal.Status is DealStatus.Won or DealStatus.Lost)
        {
            deal.Status = DealStatus.Open;
            deal.ClosedAt = null;
        }
    }

    private static void ApplyTask(CrmTask entity, CreateTaskRequest request)
    {
        entity.Title = request.Title.Trim();
        entity.Description = Trim(request.Description);
        entity.DueAt = request.DueAt;
        entity.Status = request.Status;
        entity.Priority = request.Priority;
        entity.ContactId = request.ContactId;
        entity.CompanyId = request.CompanyId;
        entity.DealId = request.DealId;
        entity.ResponsibleUserId = request.ResponsibleUserId;
        entity.CompletedAt = request.Status == CrmTaskStatus.Completed ? entity.CompletedAt ?? DateTimeOffset.UtcNow : null;
    }

    private static void ApplyActivity(Activity entity, CreateActivityRequest request)
    {
        entity.Type = request.Type;
        entity.Title = request.Title.Trim();
        entity.Description = Trim(request.Description);
        entity.ContactId = request.ContactId;
        entity.CompanyId = request.CompanyId;
        entity.DealId = request.DealId;
        entity.CreatedByUserId = request.CreatedByUserId;
        entity.CreatedByAgentId = request.CreatedByAgentId;
    }

    private static void ApplyMessage(Message entity, CreateMessageRequest request)
    {
        entity.Channel = request.Channel;
        entity.Direction = request.Direction;
        entity.ExternalMessageId = Trim(request.ExternalMessageId);
        entity.ContactId = request.ContactId;
        entity.DealId = request.DealId;
        entity.Text = request.Text.Trim();
        entity.ReceivedAt = request.ReceivedAt;
        entity.SentAt = request.SentAt ?? (request.Direction == MessageDirection.Outgoing ? DateTimeOffset.UtcNow : null);
    }

    private static void ApplyAgent(Agent entity, CreateAgentRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Description = Trim(request.Description);
        entity.IsActive = request.IsActive;
    }

    private WorkQueueItemDto MapWorkQueueTask(CrmTask task, DateTimeOffset now)
    {
        var mapped = MapTask(task);
        var isOverdue = task.DueAt is not null && task.DueAt.Value.Date < now.Date;

        return new WorkQueueItemDto(
            $"task:{task.Id:N}",
            WorkQueueItemType.Task,
            task.Id,
            mapped.Title,
            mapped.Description,
            null,
            mapped.Status,
            mapped.Priority,
            mapped.DueAt,
            null,
            mapped.ContactId,
            mapped.ContactName,
            mapped.CompanyId,
            mapped.CompanyName,
            mapped.DealId,
            mapped.DealTitle,
            mapped.ResponsibleUserId,
            ResolveTaskBucket(task, now),
            isOverdue,
            task.DueAt ?? task.CreatedAt);
    }

    private WorkQueueItemDto MapWorkQueueActivity(Activity activity, DateTimeOffset now)
    {
        var mapped = MapActivity(activity);

        return new WorkQueueItemDto(
            $"activity:{activity.Id:N}",
            WorkQueueItemType.Activity,
            activity.Id,
            mapped.Title,
            mapped.Description,
            mapped.Type,
            null,
            null,
            null,
            mapped.CreatedAt,
            mapped.ContactId,
            mapped.ContactName,
            mapped.CompanyId,
            mapped.CompanyName,
            mapped.DealId,
            mapped.DealTitle,
            null,
            ResolveActivityBucket(activity, now),
            false,
            activity.CreatedAt);
    }

    private static WorkQueueBucket ResolveTaskBucket(CrmTask task, DateTimeOffset now)
    {
        if (task.DueAt is null)
        {
            return task.ResponsibleUserId is null ? WorkQueueBucket.Unassigned : WorkQueueBucket.Upcoming;
        }

        if (task.DueAt.Value.Date < now.Date)
        {
            return WorkQueueBucket.Overdue;
        }

        if (task.DueAt.Value.Date == now.Date)
        {
            return WorkQueueBucket.DueToday;
        }

        if (task.DueAt.Value <= now.AddDays(7))
        {
            return WorkQueueBucket.ThisWeek;
        }

        return task.ResponsibleUserId is null ? WorkQueueBucket.Unassigned : WorkQueueBucket.Upcoming;
    }

    private static WorkQueueBucket ResolveActivityBucket(Activity activity, DateTimeOffset now)
    {
        if (activity.CreatedAt.Date == now.Date)
        {
            return WorkQueueBucket.DueToday;
        }

        return activity.CreatedAt >= now.AddDays(-7)
            ? WorkQueueBucket.ThisWeek
            : WorkQueueBucket.Upcoming;
    }

    private ContactDto MapContact(Contact x)
    {
        var company = x.CompanyId is null ? null : Active<Company>().FirstOrDefault(c => c.Id == x.CompanyId);
        var fullName = string.Join(" ", new[] { x.LastName, x.FirstName, x.MiddleName }.Where(v => !string.IsNullOrWhiteSpace(v)));

        return new ContactDto(x.Id, x.FirstName, x.LastName, x.MiddleName, fullName, x.Phone, x.Email,
            x.TelegramUsername, x.CompanyId, company?.Name, x.Position, x.Source, x.Status, x.CreatedAt, x.UpdatedAt);
    }

    private static CompanyDto MapCompany(Company x) =>
        new(x.Id, x.Name, x.LegalName, x.Inn, x.Website, x.Phone, x.Email, x.Address, x.CreatedAt, x.UpdatedAt);

    private static PipelineDto MapPipeline(Pipeline x) =>
        new(x.Id, x.Name, x.Description, x.IsDefault, x.CreatedAt, x.UpdatedAt);

    private static PipelineStageDto MapStage(PipelineStage x) =>
        new(x.Id, x.PipelineId, x.Name, x.SortOrder, x.Probability, x.IsWon, x.IsLost, x.CreatedAt, x.UpdatedAt);

    private DealDto MapDeal(Deal x)
    {
        var contact = x.ContactId is null ? null : Active<Contact>().FirstOrDefault(c => c.Id == x.ContactId);
        var company = x.CompanyId is null ? null : Active<Company>().FirstOrDefault(c => c.Id == x.CompanyId);
        var pipeline = Active<Pipeline>().FirstOrDefault(p => p.Id == x.PipelineId);
        var stage = Active<PipelineStage>().FirstOrDefault(s => s.Id == x.StageId);

        return new DealDto(x.Id, x.Title, x.ContactId, contact is null ? null : MapContact(contact).FullName,
            x.CompanyId, company?.Name, x.PipelineId, pipeline?.Name, x.StageId, stage?.Name, x.Amount,
            x.Currency, x.Probability, x.Status, x.Source, x.ResponsibleUserId, x.CreatedAt, x.UpdatedAt, x.ClosedAt);
    }

    private TaskDto MapTask(CrmTask x)
    {
        var contact = x.ContactId is null ? null : Active<Contact>().FirstOrDefault(c => c.Id == x.ContactId);
        var company = x.CompanyId is null ? null : Active<Company>().FirstOrDefault(c => c.Id == x.CompanyId);
        var deal = x.DealId is null ? null : Active<Deal>().FirstOrDefault(d => d.Id == x.DealId);

        return new TaskDto(x.Id, x.Title, x.Description, x.DueAt, x.Status, x.Priority, x.ContactId,
            contact is null ? null : MapContact(contact).FullName, x.CompanyId, company?.Name, x.DealId,
            deal?.Title, x.ResponsibleUserId, x.CreatedAt, x.UpdatedAt, x.CompletedAt);
    }

    private ActivityDto MapActivity(Activity x)
    {
        var contact = x.ContactId is null ? null : Active<Contact>().FirstOrDefault(c => c.Id == x.ContactId);
        var company = x.CompanyId is null ? null : Active<Company>().FirstOrDefault(c => c.Id == x.CompanyId);
        var deal = x.DealId is null ? null : Active<Deal>().FirstOrDefault(d => d.Id == x.DealId);

        return new ActivityDto(x.Id, x.Type, x.Title, x.Description, x.ContactId,
            contact is null ? null : MapContact(contact).FullName, x.CompanyId, company?.Name, x.DealId,
            deal?.Title, x.CreatedByUserId, x.CreatedByAgentId, x.CreatedAt);
    }

    private MessageDto MapMessage(Message x)
    {
        var contact = x.ContactId is null ? null : Active<Contact>().FirstOrDefault(c => c.Id == x.ContactId);
        var deal = x.DealId is null ? null : Active<Deal>().FirstOrDefault(d => d.Id == x.DealId);

        return new MessageDto(x.Id, x.Channel, x.Direction, x.ExternalMessageId, x.ContactId,
            contact is null ? null : MapContact(contact).FullName, x.DealId, deal?.Title, x.Text,
            x.ReceivedAt, x.SentAt, x.CreatedAt, x.UpdatedAt);
    }

    private static AgentDto MapAgent(Agent x) =>
        new(x.Id, x.Name, x.Description, x.IsActive, x.CreatedAt, x.UpdatedAt);

    private AgentActionDto MapAgentAction(AgentAction x)
    {
        var agent = Active<Agent>().FirstOrDefault(a => a.Id == x.AgentId);

        return new AgentActionDto(x.Id, x.AgentId, agent?.Name, x.ActionType, x.Status, x.TargetEntityType,
            x.TargetEntityId, x.InputJson, x.ReasoningSummary, x.BeforeJson, x.AfterJson, x.RequiresApproval,
            x.ApprovedByUserId, x.ApprovedAt, x.RejectedByUserId, x.RejectedAt, x.ErrorMessage, x.CreatedAt,
            x.UpdatedAt, x.ExecutedAt);
    }

    private static ApprovalRequestDto MapApproval(ApprovalRequest x) =>
        new(x.Id, x.EntityType, x.EntityId, x.Title, x.Description, x.Status, x.RequestedByUserId,
            x.RequestedByAgentId, x.ApprovedByUserId, x.ApprovedAt, x.RejectedByUserId, x.RejectedAt,
            x.CreatedAt, x.UpdatedAt);

    private string? SnapshotTarget(AgentAction action) =>
        action.TargetEntityType switch
        {
            CrmEntityType.Contact when action.TargetEntityId is not null => Serialize(MapContact(GetRequired<Contact>(action.TargetEntityId.Value, "Contact"))),
            CrmEntityType.Company when action.TargetEntityId is not null => Serialize(MapCompany(GetRequired<Company>(action.TargetEntityId.Value, "Company"))),
            CrmEntityType.Deal when action.TargetEntityId is not null => Serialize(MapDeal(GetRequired<Deal>(action.TargetEntityId.Value, "Deal"))),
            CrmEntityType.Task when action.TargetEntityId is not null => Serialize(MapTask(GetRequired<CrmTask>(action.TargetEntityId.Value, "Task"))),
            CrmEntityType.Activity when action.TargetEntityId is not null => Serialize(MapActivity(GetRequired<Activity>(action.TargetEntityId.Value, "Activity"))),
            CrmEntityType.Message when action.TargetEntityId is not null => Serialize(MapMessage(GetRequired<Message>(action.TargetEntityId.Value, "Message"))),
            _ => null
        };

    private static void EnsureTarget(AgentAction action, CrmEntityType expected)
    {
        if (action.TargetEntityType != expected || action.TargetEntityId is null)
        {
            throw new ConflictException($"Agent action must target {expected}.");
        }
    }

    private static T ReadInput<T>(AgentAction action)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(action.InputJson, JsonOptions)
                ?? throw new ConflictException("InputJson cannot be empty.");
        }
        catch (JsonException ex)
        {
            throw new ConflictException($"InputJson is invalid: {ex.Message}");
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
