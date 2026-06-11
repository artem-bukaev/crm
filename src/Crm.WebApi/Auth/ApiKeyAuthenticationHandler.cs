using System.Security.Claims;
using System.Text.Encodings.Web;
using Crm.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Crm.WebApi.Auth;

/// <summary>
/// Authenticates AI agents by the X-Api-Key header. The key is resolved to an Agent
/// through its SHA-256 hash and produces a principal carrying the AgentId claim.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var header) ||
            string.IsNullOrWhiteSpace(header.ToString()))
        {
            return AuthenticateResult.NoResult();
        }

        var authService = Context.RequestServices.GetRequiredService<IAuthService>();
        var agent = await authService.ResolveAgentByApiKeyAsync(header.ToString(), Context.RequestAborted);
        if (agent is null)
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(AuthConstants.AgentIdClaim, agent.Id.ToString()),
            new Claim(ClaimTypes.Name, agent.Name),
            new Claim(AuthConstants.ActorTypeClaim, AuthConstants.AgentActorType)
        ], Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        ApiErrorWriter.WriteAsync(Context, StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "A valid API key or bearer token is required.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        ApiErrorWriter.WriteAsync(Context, StatusCodes.Status403Forbidden, "FORBIDDEN", "The authenticated identity is not allowed to perform this operation.");
}
