using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddConnectionString("CrmDb");
var crmDbConnectionString = builder.Configuration.GetConnectionString("CrmDb")
    ?? throw new InvalidOperationException("Connection string 'CrmDb' is not configured.");

builder.AddExecutable("crm-api", "dotnet", "../Crm.WebApi", "run", "--launch-profile", "http")
    .WithEnvironment("ConnectionStrings__CrmDb", crmDbConnectionString)
    .WithHttpEndpoint(targetPort: 5080, port: 5080, isProxied: false)
    .WithExternalHttpEndpoints();

builder.AddExecutable("crm-webapp", "npm", "../Crm.WebApp", "run", "dev", "--", "--host", "0.0.0.0")
    .WithHttpEndpoint(targetPort: 5173, port: 5173, isProxied: false)
    .WithEnvironment("VITE_API_BASE_URL", "http://localhost:5080")
    .WithExternalHttpEndpoints();

builder.Build().Run();
