using Crm.Domain.Enums;

namespace Crm.Application.DTOs;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record LoginResponseDto(
    string Token,
    DateTimeOffset ExpiresAt,
    UserDto User);

/// <summary>
/// Returned exactly once when an agent API key is issued or rotated.
/// Only a SHA-256 hash of the key is persisted.
/// </summary>
public sealed record AgentApiKeyDto(
    Guid AgentId,
    string ApiKey,
    DateTimeOffset IssuedAt);
