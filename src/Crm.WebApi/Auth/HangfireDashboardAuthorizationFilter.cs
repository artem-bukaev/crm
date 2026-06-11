using Crm.Domain.Enums;
using Hangfire.Dashboard;

namespace Crm.WebApi.Auth;

/// <summary>
/// Restricts the Hangfire dashboard to authenticated Admin users.
/// In Development, requests from the local machine are also allowed so the dashboard
/// stays usable during local runs (browsers cannot attach the JWT to dashboard navigation).
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter(bool allowLocalRequests) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (allowLocalRequests && IsLocalRequest(httpContext))
        {
            return true;
        }

        var user = httpContext.User;
        return user.Identity?.IsAuthenticated == true &&
               user.HasClaim(AuthConstants.ActorTypeClaim, AuthConstants.UserActorType) &&
               user.IsInRole(nameof(UserRole.Admin));
    }

    private static bool IsLocalRequest(HttpContext httpContext)
    {
        var connection = httpContext.Connection;
        if (connection.RemoteIpAddress is null)
        {
            return true;
        }

        return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress) ||
               connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
    }
}
