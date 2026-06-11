using System.Security.Cryptography;
using System.Text;
using Crm.Application.DTOs;
using Crm.Application.Exceptions;
using Crm.Application.Interfaces;
using Crm.Domain.Entities;

namespace Crm.Application.Services;

public sealed class AuthService(ICrmDataStore db, IPasswordHasher passwordHasher) : IAuthService
{
    private const string ApiKeyPrefix = "crm_";

    public async Task<UserDto> ValidateCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = db.Query<User>()
            .Where(x => !x.IsDeleted)
            .ToList()
            .FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !user.IsActive || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await Task.FromResult(MapUser(user));
    }

    public Task<UserDto> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = db.Query<User>().FirstOrDefault(x => x.Id == id && !x.IsDeleted)
            ?? throw new NotFoundException($"User {id} was not found.");

        return Task.FromResult(MapUser(user));
    }

    public async Task<AgentApiKeyDto> IssueAgentApiKeyAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = db.Query<Agent>().FirstOrDefault(x => x.Id == agentId && !x.IsDeleted)
            ?? throw new NotFoundException($"Agent {agentId} was not found.");

        var apiKey = ApiKeyPrefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        agent.ApiKeyHash = ComputeApiKeyHash(apiKey);
        await db.SaveChangesAsync(cancellationToken);

        return new AgentApiKeyDto(agent.Id, apiKey, DateTimeOffset.UtcNow);
    }

    public Task<AgentDto?> ResolveAgentByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult<AgentDto?>(null);
        }

        var hash = ComputeApiKeyHash(apiKey.Trim());
        var agent = db.Query<Agent>()
            .FirstOrDefault(x => !x.IsDeleted && x.IsActive && x.ApiKeyHash == hash);

        return Task.FromResult(agent is null
            ? null
            : new AgentDto(agent.Id, agent.Name, agent.Description, agent.IsActive, agent.CreatedAt, agent.UpdatedAt));
    }

    public static string ComputeApiKeyHash(string apiKey) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

    private static UserDto MapUser(User x) =>
        new(x.Id, x.Email, x.DisplayName, x.Role, x.IsActive, x.CreatedAt, x.UpdatedAt);
}
