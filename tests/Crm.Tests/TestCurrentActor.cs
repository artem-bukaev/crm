using Crm.Application.Interfaces;

namespace Crm.Tests;

public sealed class TestCurrentActor : ICurrentActor
{
    public Guid? UserId { get; set; }
    public Guid? AgentId { get; set; }
}
