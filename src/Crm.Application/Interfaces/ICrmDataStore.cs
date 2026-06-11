namespace Crm.Application.Interfaces;

public interface ICrmDataStore
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : class;
    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Identity of the caller executing the current request.
/// Exactly one of <see cref="UserId"/> (human JWT) or <see cref="AgentId"/> (agent API key) is set
/// for authenticated requests; both are null for unauthenticated/system flows.
/// </summary>
public interface ICurrentActor
{
    Guid? UserId { get; }
    Guid? AgentId { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public interface IMessageSender
{
    Task SendAsync(string channel, string recipient, string text, CancellationToken cancellationToken = default);
}

public interface IExternalCrmSyncService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}

public interface IAgentRuntime
{
    Task<string> SummarizeAsync(string input, CancellationToken cancellationToken = default);
}

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
