using Crm.Application.DTOs;
using Crm.Domain.Enums;

namespace Crm.Application.Services;

public interface ICrmService
{
    Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContactDto>> GetContactsAsync(CancellationToken cancellationToken = default);
    Task<ContactDto> GetContactAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ContactDto> CreateContactAsync(CreateContactRequest request, CancellationToken cancellationToken = default);
    Task<ContactDto> UpdateContactAsync(Guid id, UpdateContactRequest request, CancellationToken cancellationToken = default);
    Task DeleteContactAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContactDuplicateCandidateDto>> GetContactDuplicatesAsync(CancellationToken cancellationToken = default);
    Task<ContactDto> MergeContactsAsync(MergeContactsRequest request, CancellationToken cancellationToken = default);
    Task<BulkOperationResultDto> BulkCreateContactTasksAsync(BulkCreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken = default);
    Task<CompanyDto> GetCompanyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<CompanyDto> UpdateCompanyAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);
    Task DeleteCompanyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PipelineDto>> GetPipelinesAsync(CancellationToken cancellationToken = default);
    Task<PipelineDto> GetPipelineAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PipelineDto> CreatePipelineAsync(CreatePipelineRequest request, CancellationToken cancellationToken = default);
    Task<PipelineDto> UpdatePipelineAsync(Guid id, UpdatePipelineRequest request, CancellationToken cancellationToken = default);
    Task DeletePipelineAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PipelineStageDto>> GetPipelineStagesAsync(Guid pipelineId, CancellationToken cancellationToken = default);
    Task<PipelineStageDto> CreatePipelineStageAsync(Guid pipelineId, CreatePipelineStageRequest request, CancellationToken cancellationToken = default);
    Task<PipelineStageDto> UpdatePipelineStageAsync(Guid id, UpdatePipelineStageRequest request, CancellationToken cancellationToken = default);
    Task DeletePipelineStageAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DealDto>> GetDealsAsync(CancellationToken cancellationToken = default);
    Task<DealDto> GetDealAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DealDto> CreateDealAsync(CreateDealRequest request, CancellationToken cancellationToken = default);
    Task<DealDto> UpdateDealAsync(Guid id, UpdateDealRequest request, CancellationToken cancellationToken = default);
    Task<DealDto> MoveDealStageAsync(Guid id, MoveDealStageRequest request, CancellationToken cancellationToken = default);
    Task<DealDto> MarkDealWonAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DealDto> MarkDealLostAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteDealAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BulkOperationResultDto> BulkCreateDealTasksAsync(BulkCreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskDto>> GetTasksAsync(CrmTaskStatus? status, CancellationToken cancellationToken = default);
    Task<TaskDto> GetTaskAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskDto> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskDto> UpdateTaskAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default);
    Task<TaskDto> CompleteTaskAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskDto> CancelTaskAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityDto>> GetTimelineAsync(Guid? contactId, Guid? companyId, Guid? dealId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkQueueItemDto>> GetWorkQueueAsync(CancellationToken cancellationToken = default);
    Task<ActivityDto> CreateActivityAsync(CreateActivityRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(CancellationToken cancellationToken = default);
    Task<MessageDto> GetMessageAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MessageDto> CreateMessageAsync(CreateMessageRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<AgentDto> GetAgentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentDto> CreateAgentAsync(CreateAgentRequest request, CancellationToken cancellationToken = default);
    Task<AgentDto> UpdateAgentAsync(Guid id, UpdateAgentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentActionDto>> GetAgentActionsAsync(CancellationToken cancellationToken = default);
    Task<AgentActionDto> GetAgentActionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgentActionDto> CreateAgentActionAsync(CreateAgentActionRequest request, CancellationToken cancellationToken = default);
    Task<AgentActionDto> ApproveAgentActionAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AgentActionDto> RejectAgentActionAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default);
    Task<AgentActionDto> ExecuteAgentActionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequestDto>> GetApprovalsAsync(CancellationToken cancellationToken = default);
    Task<ApprovalRequestDto> ApproveRequestAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default);
    Task<ApprovalRequestDto> RejectRequestAsync(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken = default);
}
