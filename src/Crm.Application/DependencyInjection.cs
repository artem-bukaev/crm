using Crm.Application.Interfaces;
using Crm.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));
        services.AddScoped<ICrmService, CrmService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        return services;
    }
}
