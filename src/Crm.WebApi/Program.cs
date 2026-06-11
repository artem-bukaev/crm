using System.Text;
using System.Text.Json.Serialization;
using Crm.Application;
using Crm.Application.Interfaces;
using Crm.Application.Options;
using Crm.Infrastructure;
using Crm.Infrastructure.Persistence;
using Crm.ServiceDefaults;
using Crm.WebApi.Auth;
using Crm.WebApi.Filters;
using Crm.WebApi.Jobs;
using Crm.WebApi.Middleware;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Auth:Jwt:SigningKey must be configured with at least 32 characters. " +
        "Provide it through configuration or secrets; it must never be hardcoded.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpContextCurrentActor>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = AuthConstants.RoleClaim,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return ApiErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                    "UNAUTHORIZED", "A valid bearer token or API key is required.");
            },
            OnForbidden = context =>
                ApiErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                    "FORBIDDEN", "The authenticated identity is not allowed to perform this operation.")
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthConstants.AgentApiKeyScheme, null);

builder.Services.AddAuthorization(options =>
{
    string[] allSchemes = [JwtBearerDefaults.AuthenticationScheme, AuthConstants.AgentApiKeyScheme];

    // Everything requires authentication by default. Anonymous access is limited to
    // login, health endpoints and Swagger in Development.
    options.FallbackPolicy = new AuthorizationPolicyBuilder(allSchemes)
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AuthConstants.Policies.HumanOrAgent, policy => policy
        .AddAuthenticationSchemes(allSchemes)
        .RequireAuthenticatedUser());

    options.AddPolicy(AuthConstants.Policies.HumanOnly, policy => policy
        .AddAuthenticationSchemes(allSchemes)
        .RequireAuthenticatedUser()
        .RequireClaim(AuthConstants.ActorTypeClaim, AuthConstants.UserActorType));

    options.AddPolicy(AuthConstants.Policies.AdminOnly, policy => policy
        .AddAuthenticationSchemes(allSchemes)
        .RequireAuthenticatedUser()
        .RequireClaim(AuthConstants.ActorTypeClaim, AuthConstants.UserActorType)
        .RequireRole(nameof(Crm.Domain.Enums.UserRole.Admin)));
});

builder.Services.Configure<AgentHeartbeatOptions>(builder.Configuration.GetSection(AgentHeartbeatOptions.SectionName));
builder.Services.AddScoped<AgentHeartbeatJobs>();

builder.Services.AddScoped<FluentValidationActionFilter>();
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<FluentValidationActionFilter>();
        options.Conventions.Add(new MutationAuthorizationConvention());
    })
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
app.UseAuthentication();

// Swagger (Development only) and the Hangfire dashboard short-circuit before the
// fallback authorization policy. Swagger stays anonymous in Development; the Hangfire
// dashboard is gated by its own filter: Admin users only, plus local requests in Development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter(app.Environment.IsDevelopment())]
});

app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapControllers();

await InitializeDatabaseAsync(app);
ConfigureAgentHeartbeatJobs(app);

await app.RunAsync();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment());

    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<CrmDbInitializer>();
    await initializer.InitializeAsync(applyMigrations);
}

static void ConfigureAgentHeartbeatJobs(WebApplication app)
{
    var options = app.Services.GetRequiredService<IOptions<AgentHeartbeatOptions>>().Value;
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();

    if (!options.Enabled)
    {
        recurringJobs.RemoveIfExists(AgentHeartbeatJobs.WaitingConversationsJobId);
        recurringJobs.RemoveIfExists(AgentHeartbeatJobs.OverdueTasksJobId);
        recurringJobs.RemoveIfExists(AgentHeartbeatJobs.StaleDealsJobId);
        return;
    }

    recurringJobs.AddOrUpdate<AgentHeartbeatJobs>(
        AgentHeartbeatJobs.WaitingConversationsJobId,
        job => job.DetectWaitingConversationsAsync(CancellationToken.None),
        CronOrDefault(options.WaitingConversationsCron, AgentHeartbeatOptions.DefaultWaitingConversationsCron));
    recurringJobs.AddOrUpdate<AgentHeartbeatJobs>(
        AgentHeartbeatJobs.OverdueTasksJobId,
        job => job.DetectOverdueTasksAsync(CancellationToken.None),
        CronOrDefault(options.OverdueTasksCron, AgentHeartbeatOptions.DefaultOverdueTasksCron));
    recurringJobs.AddOrUpdate<AgentHeartbeatJobs>(
        AgentHeartbeatJobs.StaleDealsJobId,
        job => job.DetectStaleDealsAsync(CancellationToken.None),
        CronOrDefault(options.StaleDealsCron, AgentHeartbeatOptions.DefaultStaleDealsCron));
}

static string CronOrDefault(string? cron, string fallback) =>
    string.IsNullOrWhiteSpace(cron) ? fallback : cron.Trim();
