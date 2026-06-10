using Crm.Application.DTOs;
using Crm.Domain.Enums;
using FluentValidation;

namespace Crm.Application.Validators;

public sealed class CreateContactRequestValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.FirstName) || !string.IsNullOrWhiteSpace(x.LastName))
            .WithMessage("FirstName or LastName must be provided.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.TelegramUsername).MaximumLength(128).When(x => !string.IsNullOrWhiteSpace(x.TelegramUsername));
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class UpdateContactRequestValidator : AbstractValidator<UpdateContactRequest>
{
    public UpdateContactRequestValidator()
    {
        Include(new CreateContactRequestValidator());
    }
}

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Website).Must(BeUrl).When(x => !string.IsNullOrWhiteSpace(x.Website))
            .WithMessage("Website must be a valid absolute URL.");
    }

    private static bool BeUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        Include(new CreateCompanyRequestValidator());
    }
}

public sealed class CreatePipelineRequestValidator : AbstractValidator<CreatePipelineRequest>
{
    public CreatePipelineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdatePipelineRequestValidator : AbstractValidator<UpdatePipelineRequest>
{
    public UpdatePipelineRequestValidator()
    {
        Include(new CreatePipelineRequestValidator());
    }
}

public sealed class CreatePipelineStageRequestValidator : AbstractValidator<CreatePipelineStageRequest>
{
    public CreatePipelineStageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Probability).InclusiveBetween(0, 100);
        RuleFor(x => x).Must(x => !(x.IsWon && x.IsLost)).WithMessage("Stage cannot be won and lost at the same time.");
    }
}

public sealed class UpdatePipelineStageRequestValidator : AbstractValidator<UpdatePipelineStageRequest>
{
    public UpdatePipelineStageRequestValidator()
    {
        Include(new CreatePipelineStageRequestValidator());
    }
}

public sealed class CreateDealRequestValidator : AbstractValidator<CreateDealRequest>
{
    public CreateDealRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PipelineId).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
        RuleFor(x => x.Probability).InclusiveBetween(0, 100).When(x => x.Probability is not null);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class UpdateDealRequestValidator : AbstractValidator<UpdateDealRequest>
{
    public UpdateDealRequestValidator()
    {
        Include(new CreateDealRequestValidator());
    }
}

public sealed class MoveDealStageRequestValidator : AbstractValidator<MoveDealStageRequest>
{
    public MoveDealStageRequestValidator()
    {
        RuleFor(x => x.StageId).NotEmpty();
    }
}

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        Include(new CreateTaskRequestValidator());
    }
}

public sealed class CreateActivityRequestValidator : AbstractValidator<CreateActivityRequest>
{
    public CreateActivityRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
    }
}

public sealed class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Direction).IsInEnum();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(8000);
    }
}

public sealed class CreateAgentRequestValidator : AbstractValidator<CreateAgentRequest>
{
    public CreateAgentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class UpdateAgentRequestValidator : AbstractValidator<UpdateAgentRequest>
{
    public UpdateAgentRequestValidator()
    {
        Include(new CreateAgentRequestValidator());
    }
}

public sealed class CreateAgentActionRequestValidator : AbstractValidator<CreateAgentActionRequest>
{
    public CreateAgentActionRequestValidator()
    {
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.ActionType).IsInEnum();
        RuleFor(x => x.TargetEntityType).NotNull()
            .When(x => x.ActionType is AgentActionType.UpdateContact
                or AgentActionType.UpdateDealStage
                or AgentActionType.CompleteTask)
            .WithMessage("TargetEntityType is required for actions against an existing entity.");
        RuleFor(x => x.TargetEntityId).NotEmpty()
            .When(x => x.ActionType is AgentActionType.UpdateContact
                or AgentActionType.UpdateDealStage
                or AgentActionType.CompleteTask)
            .WithMessage("TargetEntityId is required for actions against an existing entity.");
        RuleFor(x => x.InputJson).NotEmpty().Must(BeJson).WithMessage("InputJson must be valid JSON.");
        RuleFor(x => x.ReasoningSummary).MaximumLength(2000);
    }

    private static bool BeJson(string value)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class AgentActionDecisionRequestValidator : AbstractValidator<AgentActionDecisionRequest>
{
    public AgentActionDecisionRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().When(x => x.UserId is not null);
    }
}
