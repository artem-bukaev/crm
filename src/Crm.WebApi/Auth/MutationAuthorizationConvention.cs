using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Crm.WebApi.Auth;

/// <summary>
/// Authorization model for the API, enforced centrally with policies (no ad-hoc checks in controllers):
/// - GET/HEAD endpoints stay under the fallback policy: any authenticated principal (human JWT or agent API key).
/// - Every other (mutating) endpoint gets the HumanOnly policy unless the action or controller explicitly
///   declares its own policy (e.g. proposing agent actions allows agents) or [AllowAnonymous] (login).
/// New mutating endpoints are therefore human-only by default.
/// </summary>
public sealed class MutationAuthorizationConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                if (IsReadOnly(action) || HasExplicitAuthorization(controller, action))
                {
                    continue;
                }

                action.Filters.Add(new AuthorizeFilter(AuthConstants.Policies.HumanOnly));
            }
        }
    }

    private static bool IsReadOnly(ActionModel action)
    {
        var httpMethods = action.Attributes
            .OfType<IActionHttpMethodProvider>()
            .SelectMany(x => x.HttpMethods)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return httpMethods.Count > 0 &&
               httpMethods.All(method => HttpMethods.IsGet(method) || HttpMethods.IsHead(method));
    }

    private static bool HasExplicitAuthorization(ControllerModel controller, ActionModel action) =>
        action.Attributes.OfType<IAllowAnonymous>().Any() ||
        controller.Attributes.OfType<IAllowAnonymous>().Any() ||
        action.Attributes.OfType<IAuthorizeData>().Any(x => !string.IsNullOrEmpty(x.Policy)) ||
        controller.Attributes.OfType<IAuthorizeData>().Any(x => !string.IsNullOrEmpty(x.Policy));
}
