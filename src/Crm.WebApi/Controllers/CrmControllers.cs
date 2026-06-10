using Crm.Application.DTOs;
using Crm.Application.Services;
using Crm.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Crm.WebApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<DashboardSummaryDto> Get(CancellationToken cancellationToken) => crm.GetDashboardAsync(cancellationToken);
}

[ApiController]
[Route("api/contacts")]
public sealed class ContactsController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ContactDto>> Get(CancellationToken cancellationToken) => crm.GetContactsAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ContactDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetContactAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ContactDto>> Create(CreateContactRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateContactAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<ContactDto> Update(Guid id, UpdateContactRequest request, CancellationToken cancellationToken) =>
        crm.UpdateContactAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await crm.DeleteContactAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CompanyDto>> Get(CancellationToken cancellationToken) => crm.GetCompaniesAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<CompanyDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetCompanyAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create(CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateCompanyAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<CompanyDto> Update(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken) =>
        crm.UpdateCompanyAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await crm.DeleteCompanyAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/pipelines")]
public sealed class PipelinesController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PipelineDto>> Get(CancellationToken cancellationToken) => crm.GetPipelinesAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<PipelineDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetPipelineAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<PipelineDto>> Create(CreatePipelineRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreatePipelineAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<PipelineDto> Update(Guid id, UpdatePipelineRequest request, CancellationToken cancellationToken) =>
        crm.UpdatePipelineAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await crm.DeletePipelineAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{pipelineId:guid}/stages")]
    public Task<IReadOnlyList<PipelineStageDto>> GetStages(Guid pipelineId, CancellationToken cancellationToken) =>
        crm.GetPipelineStagesAsync(pipelineId, cancellationToken);

    [HttpPost("{pipelineId:guid}/stages")]
    public async Task<ActionResult<PipelineStageDto>> CreateStage(Guid pipelineId, CreatePipelineStageRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreatePipelineStageAsync(pipelineId, request, cancellationToken);
        return Created($"/api/pipeline-stages/{result.Id}", result);
    }
}

[ApiController]
[Route("api/pipeline-stages")]
public sealed class PipelineStagesController(ICrmService crm) : ControllerBase
{
    [HttpPut("{id:guid}")]
    public Task<PipelineStageDto> Update(Guid id, UpdatePipelineStageRequest request, CancellationToken cancellationToken) =>
        crm.UpdatePipelineStageAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await crm.DeletePipelineStageAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/deals")]
public sealed class DealsController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<DealDto>> Get(CancellationToken cancellationToken) => crm.GetDealsAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<DealDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetDealAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<DealDto>> Create(CreateDealRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateDealAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<DealDto> Update(Guid id, UpdateDealRequest request, CancellationToken cancellationToken) =>
        crm.UpdateDealAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/move-stage")]
    public Task<DealDto> MoveStage(Guid id, MoveDealStageRequest request, CancellationToken cancellationToken) =>
        crm.MoveDealStageAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/mark-won")]
    public Task<DealDto> MarkWon(Guid id, CancellationToken cancellationToken) =>
        crm.MarkDealWonAsync(id, cancellationToken);

    [HttpPost("{id:guid}/mark-lost")]
    public Task<DealDto> MarkLost(Guid id, CancellationToken cancellationToken) =>
        crm.MarkDealLostAsync(id, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await crm.DeleteDealAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/tasks")]
public sealed class TasksController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TaskDto>> Get([FromQuery] CrmTaskStatus? status, CancellationToken cancellationToken) =>
        crm.GetTasksAsync(status, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<TaskDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetTaskAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateTaskAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<TaskDto> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        crm.UpdateTaskAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/complete")]
    public Task<TaskDto> Complete(Guid id, CancellationToken cancellationToken) => crm.CompleteTaskAsync(id, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<TaskDto> Cancel(Guid id, CancellationToken cancellationToken) => crm.CancelTaskAsync(id, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await crm.DeleteTaskAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/activities")]
public sealed class ActivitiesController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ActivityDto>> Get(CancellationToken cancellationToken) => crm.GetActivitiesAsync(cancellationToken);

    [HttpGet("timeline")]
    public Task<IReadOnlyList<ActivityDto>> Timeline([FromQuery] Guid? contactId, [FromQuery] Guid? companyId, [FromQuery] Guid? dealId, CancellationToken cancellationToken) =>
        crm.GetTimelineAsync(contactId, companyId, dealId, cancellationToken);

    [HttpPost]
    public Task<ActivityDto> Create(CreateActivityRequest request, CancellationToken cancellationToken) =>
        crm.CreateActivityAsync(request, cancellationToken);
}

[ApiController]
[Route("api/messages")]
public sealed class MessagesController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<MessageDto>> Get(CancellationToken cancellationToken) => crm.GetMessagesAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<MessageDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetMessageAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<MessageDto>> Create(CreateMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateMessageAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
}

[ApiController]
[Route("api/agents")]
public sealed class AgentsController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AgentDto>> Get(CancellationToken cancellationToken) => crm.GetAgentsAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AgentDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetAgentAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create(CreateAgentRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateAgentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<AgentDto> Update(Guid id, UpdateAgentRequest request, CancellationToken cancellationToken) =>
        crm.UpdateAgentAsync(id, request, cancellationToken);
}

[ApiController]
[Route("api/agent-actions")]
public sealed class AgentActionsController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AgentActionDto>> Get(CancellationToken cancellationToken) => crm.GetAgentActionsAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<AgentActionDto> Get(Guid id, CancellationToken cancellationToken) => crm.GetAgentActionAsync(id, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<AgentActionDto>> Create(CreateAgentActionRequest request, CancellationToken cancellationToken)
    {
        var result = await crm.CreateAgentActionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/approve")]
    public Task<AgentActionDto> Approve(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken) =>
        crm.ApproveAgentActionAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public Task<AgentActionDto> Reject(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken) =>
        crm.RejectAgentActionAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/execute")]
    public Task<AgentActionDto> Execute(Guid id, CancellationToken cancellationToken) =>
        crm.ExecuteAgentActionAsync(id, cancellationToken);
}

[ApiController]
[Route("api/approvals")]
public sealed class ApprovalsController(ICrmService crm) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ApprovalRequestDto>> Get(CancellationToken cancellationToken) => crm.GetApprovalsAsync(cancellationToken);

    [HttpPost("{id:guid}/approve")]
    public Task<ApprovalRequestDto> Approve(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken) =>
        crm.ApproveRequestAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    public Task<ApprovalRequestDto> Reject(Guid id, AgentActionDecisionRequest request, CancellationToken cancellationToken) =>
        crm.RejectRequestAsync(id, request, cancellationToken);
}
