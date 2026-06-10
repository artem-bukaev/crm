using Crm.Application.Interfaces;
using Crm.Infrastructure.Integrations;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CrmDb")
            ?? throw new InvalidOperationException("Connection string 'CrmDb' is not configured.");

        services.AddDbContext<CrmDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICrmDataStore>(sp => sp.GetRequiredService<CrmDbContext>());
        services.AddScoped<CrmDbInitializer>();

        services.AddSingleton<IMessageSender, FakeMessageSender>();
        services.AddSingleton<IExternalCrmSyncService, FakeExternalCrmSyncService>();
        services.AddSingleton<IAgentRuntime, FakeAgentRuntime>();
        services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();

        return services;
    }
}
