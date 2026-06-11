using Crm.Application.DTOs;

namespace Crm.Application.Services;

public interface IAuthService
{
    /// <summary>Validates email/password credentials. Throws <see cref="Exceptions.UnauthorizedException"/> on failure.</summary>
    Task<UserDto> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Issues (or rotates) the API key for an agent. The plaintext key is returned exactly once.</summary>
    Task<AgentApiKeyDto> IssueAgentApiKeyAsync(Guid agentId, CancellationToken cancellationToken = default);

    /// <summary>Resolves an active agent by plaintext API key. Returns null when the key is unknown.</summary>
    Task<AgentDto?> ResolveAgentByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
