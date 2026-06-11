using System.Security.Claims;
using Crm.Application.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Crm.WebApi.Auth;

/// <summary>
/// Resolves the acting user/agent from the authenticated principal's claims.
/// The acting identity is never taken from request bodies.
/// </summary>
public sealed class HttpContextCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public Guid? UserId => GetGuidClaim(JwtRegisteredClaimNames.Sub);

    public Guid? AgentId => GetGuidClaim(AuthConstants.AgentIdClaim);

    private Guid? GetGuidClaim(string claimType)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
