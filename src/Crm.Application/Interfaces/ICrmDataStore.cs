namespace Crm.Application.Interfaces;

public interface ICrmDataStore
{
    IQueryable<TEntity> Query<TEntity>() where TEntity : class;
    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
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
