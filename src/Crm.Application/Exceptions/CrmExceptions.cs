using FluentValidation.Results;

namespace Crm.Application.Exceptions;

public abstract class CrmException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string message) : CrmException("NOT_FOUND", message);

public sealed class ConflictException(string message) : CrmException("CONFLICT", message);

public sealed class UnauthorizedException(string message) : CrmException("UNAUTHORIZED", message);

public sealed class ForbiddenException(string message) : CrmException("FORBIDDEN", message);

public sealed class CrmValidationException(IEnumerable<ValidationFailure> failures)
    : FluentValidation.ValidationException(failures);
