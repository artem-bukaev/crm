using System.Text.Json;
using Crm.Application.Interfaces;
using Crm.Domain.Common;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Crm.Infrastructure.Persistence;

public sealed class CrmDbContext(DbContextOptions<CrmDbContext> options, ICurrentActor? actor = null)
    : DbContext(options), ICrmDataStore
{
    private static readonly string[] SensitiveAuditProperties =
    [
        nameof(User.PasswordHash),
        nameof(Agent.ApiKeyHash)
    ];

    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<CrmTask> Tasks => Set<CrmTask>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentAction> AgentActions => Set<AgentAction>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => Set<TEntity>();

    public new void Add<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Add(entity);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var auditLogs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            var entity = entry.Entity;
            var action = AuditAction.Updated;
            string? beforeJson = null;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.UpdatedAt = now;
                action = AuditAction.Created;
            }
            else
            {
                beforeJson = SerializeValues(entry.Properties.ToDictionary(x => x.Metadata.Name, x => x.OriginalValue));
                var wasDeleted = entry.Property(nameof(IAuditableEntity.IsDeleted)).OriginalValue is true;
                action = entity.IsDeleted && !wasDeleted ? AuditAction.Deleted : AuditAction.Updated;
                entity.UpdatedAt = now;
            }

            auditLogs.Add(new AuditLog
            {
                EntityType = ResolveEntityType(entity),
                EntityId = entity.Id,
                Action = action,
                UserId = actor?.UserId,
                AgentId = actor?.AgentId,
                BeforeJson = beforeJson,
                AfterJson = SerializeValues(entry.Properties.ToDictionary(x => x.Metadata.Name, x => x.CurrentValue)),
                CreatedAt = now
            });
        }

        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(x => x.Email);
            entity.Property(x => x.FirstName).HasMaxLength(128);
            entity.Property(x => x.LastName).HasMaxLength(128);
            entity.Property(x => x.MiddleName).HasMaxLength(128);
            entity.Property(x => x.Phone).HasMaxLength(64);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.TelegramUsername).HasMaxLength(128);
            entity.Property(x => x.Position).HasMaxLength(160);
            entity.Property(x => x.Source).HasMaxLength(160);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(x => x.Name);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.LegalName).HasMaxLength(512);
            entity.Property(x => x.Inn).HasMaxLength(32);
            entity.Property(x => x.Website).HasMaxLength(512);
            entity.Property(x => x.Phone).HasMaxLength(64);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Address).HasMaxLength(1000);
        });

        modelBuilder.Entity<Pipeline>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<PipelineStage>(entity =>
        {
            entity.HasIndex(x => new { x.PipelineId, x.SortOrder });
            entity.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasIndex(x => x.StageId);
            entity.HasIndex(x => x.PipelineId);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Source).HasMaxLength(160);
        });

        modelBuilder.Entity<CrmTask>(entity =>
        {
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(8000);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(x => x.ExternalMessageId);
            entity.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Direction).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.ExternalMessageId).HasMaxLength(256);
            entity.Property(x => x.Text).HasMaxLength(8000);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ApiKeyHash).HasMaxLength(64);
            entity.HasIndex(x => x.ApiKeyHash);
        });

        modelBuilder.Entity<AgentAction>(entity =>
        {
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(64);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.TargetEntityType).HasConversion<string>().HasMaxLength(64);
            entity.Property(x => x.InputJson).HasColumnType("jsonb");
            entity.Property(x => x.BeforeJson).HasColumnType("jsonb");
            entity.Property(x => x.AfterJson).HasColumnType("jsonb");
            entity.Property(x => x.ReasoningSummary).HasMaxLength(2000);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
        });

        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasIndex(x => x.Status);
            entity.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(64);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(256);
            entity.Property(x => x.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
            entity.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(64);
            entity.Property(x => x.Action).HasConversion<string>().HasMaxLength(64);
            entity.Property(x => x.BeforeJson).HasColumnType("jsonb");
            entity.Property(x => x.AfterJson).HasColumnType("jsonb");
        });
    }

    private static CrmEntityType ResolveEntityType(IAuditableEntity entity) =>
        entity switch
        {
            User => CrmEntityType.User,
            Contact => CrmEntityType.Contact,
            Company => CrmEntityType.Company,
            Pipeline => CrmEntityType.Pipeline,
            PipelineStage => CrmEntityType.PipelineStage,
            Deal => CrmEntityType.Deal,
            CrmTask => CrmEntityType.Task,
            Activity => CrmEntityType.Activity,
            Message => CrmEntityType.Message,
            Agent => CrmEntityType.Agent,
            AgentAction => CrmEntityType.AgentAction,
            ApprovalRequest => CrmEntityType.ApprovalRequest,
            _ => throw new InvalidOperationException($"Unsupported audit entity {entity.GetType().Name}.")
        };

    private static string SerializeValues(Dictionary<string, object?> values)
    {
        foreach (var property in SensitiveAuditProperties)
        {
            if (values.ContainsKey(property))
            {
                values[property] = "[REDACTED]";
            }
        }

        return JsonSerializer.Serialize(values, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
