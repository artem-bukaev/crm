using System.Text.Json.Serialization;
using Crm.Application;
using Crm.Infrastructure;
using Crm.Infrastructure.Persistence;
using Crm.ServiceDefaults;
using Crm.WebApi.Filters;
using Crm.WebApi.Middleware;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("CrmDb")
    ?? throw new InvalidOperationException("Connection string 'CrmDb' is not configured.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services
    .AddControllers(options => options.Filters.Add<FluentValidationActionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Crm API",
        Version = "v1",
        Description = "API-first CRM core with auditable AI agent actions."
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (builder.Environment.IsDevelopment())
        {
            origins = origins
                .Concat([
                    "http://localhost:5173",
                    "http://127.0.0.1:5173",
                    "http://localhost:5174",
                    "http://127.0.0.1:5174",
                    "http://localhost:5175",
                    "http://127.0.0.1:5175"
                ])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        options => options.UseNpgsqlConnection(connectionString),
        new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true }));
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire");
}

app.MapDefaultEndpoints();
app.MapControllers();

await InitializeDatabaseAsync(app);

await app.RunAsync();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment());

    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<CrmDbInitializer>();
    await initializer.InitializeAsync(applyMigrations);
}
