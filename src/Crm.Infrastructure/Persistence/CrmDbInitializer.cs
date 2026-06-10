using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Persistence;

public sealed class CrmDbInitializer(CrmDbContext db, ILogger<CrmDbInitializer> logger)
{
    public async Task InitializeAsync(bool applyMigrations, CancellationToken cancellationToken = default)
    {
        if (applyMigrations)
        {
            logger.LogInformation("Applying CRM database migrations");
            await db.Database.MigrateAsync(cancellationToken);
        }

        await SeedAsync(cancellationToken);
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Pipelines.AnyAsync(cancellationToken))
        {
            return;
        }

        var pipeline = new Pipeline
        {
            Name = "Default Sales Pipeline",
            IsDefault = true
        };

        var newStage = new PipelineStage { PipelineId = pipeline.Id, Name = "New", SortOrder = 10, Probability = 10 };
        var contactedStage = new PipelineStage { PipelineId = pipeline.Id, Name = "Contacted", SortOrder = 20, Probability = 25 };
        var negotiationStage = new PipelineStage { PipelineId = pipeline.Id, Name = "Negotiation", SortOrder = 30, Probability = 60 };
        var wonStage = new PipelineStage { PipelineId = pipeline.Id, Name = "Won", SortOrder = 40, Probability = 100, IsWon = true };
        var lostStage = new PipelineStage { PipelineId = pipeline.Id, Name = "Lost", SortOrder = 50, Probability = 0, IsLost = true };

        var company = new Company
        {
            Name = "Demo Company",
            Website = "https://example.com"
        };

        var contact = new Contact
        {
            FirstName = "Ivan",
            LastName = "Petrov",
            Email = "ivan.petrov@example.com",
            Phone = "+79990000000",
            CompanyId = company.Id,
            Status = ContactStatus.Active
        };

        var deal = new Deal
        {
            Title = "Demo Deal",
            ContactId = contact.Id,
            CompanyId = company.Id,
            PipelineId = pipeline.Id,
            StageId = newStage.Id,
            Amount = 100000,
            Currency = "RUB",
            Probability = newStage.Probability,
            Status = DealStatus.Open
        };

        var agent = new Agent
        {
            Name = "Sales Assistant Agent",
            Description = "Demo AI agent for sales assistance",
            IsActive = true
        };

        var task = new CrmTask
        {
            Title = "Follow up with Ivan Petrov",
            Description = "Demo follow-up task seeded for the CRM MVP.",
            ContactId = contact.Id,
            CompanyId = company.Id,
            DealId = deal.Id,
            Priority = CrmTaskPriority.Normal,
            DueAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        db.AddRange(pipeline, newStage, contactedStage, negotiationStage, wonStage, lostStage, company, contact, deal, agent, task);
        await db.SaveChangesAsync(cancellationToken);
    }
}
