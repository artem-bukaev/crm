namespace Crm.WebApi.Auth;

/// <summary>
/// Bound from the "Auth:Jwt" configuration section.
/// The signing key must come from configuration/secrets and is never hardcoded.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "crm-api";
    public string Audience { get; set; } = "crm-clients";
    public int ExpiryMinutes { get; set; } = 480;
}
